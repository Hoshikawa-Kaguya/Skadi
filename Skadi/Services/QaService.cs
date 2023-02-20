using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Skadi.Entities;
using Skadi.Interface;
using Skadi.Tool;
using Sora.Command;
using Sora.Entities;
using Sora.Entities.Segment;
using Sora.Entities.Segment.DataModel;
using Sora.Enumeration;
using Sora.EventArgs.SoraEvent;
using YukariToolBox.LightLog;
using Path = System.IO.Path;

// ReSharper disable PossibleMultipleEnumeration

namespace Skadi.Services;

internal class QaService : IQaService, IDisposable
{
    private Guid MatchCmdId { get; }

#region Qa Buffer

    private Dictionary<string, (QaKey a, MessageBody q)> MessageBuffer { get; }

    private int ModifyCount { get; set; }

    private Task SyncTask { get; }

    private CancellationTokenSource SyncCancelToken { get; }

#endregion

    public QaService()
    {
        Log.Info("QA", "Service init");
        IGenericStorage storage = SkadiApp.GetService<IGenericStorage>();
        CommandManager  command = SkadiApp.GetService<CommandManager>();

        var readTask = storage.ReadQaData();
        readTask.AsTask().Wait();
        MessageBuffer = readTask.Result.ToDictionary(i => i.Key.GetQaKeyMd5(),
                                                     i => (i.Key, i.Value));
        Log.Info("QA", $"获取到{MessageBuffer.Count}条QA记录");

        MatchCmdId =
            command.RegisterGroupDynamicCommand(MessageMatch, GetAnswer, "qa_service");

        SyncCancelToken = new CancellationTokenSource();
        SyncTask        = SaveQaData(SyncCancelToken.Token);
        // SyncTask.Start();
        Log.Info("QA", $"sync: {SyncTask.Status}");

        ModifyCount = 0;
    }

    public async ValueTask<bool> AddNewQA(long loginUid, long groupId, MessageBody message)
    {
        //处理消息切片
        (MessageBody qMsg, MessageBody aMsg) = GenQaSlice(message);
        MessageBody localQMsg = RebuildQMessage(qMsg);
        MessageBody localAMsg = await RebuildAMessage(aMsg, loginUid);
        //构建qa内容
        QaKey key = new()
        {
            GroupId  = groupId,
            SourceId = loginUid,
            ReqMsg   = localQMsg
        };
        Log.Error("fuck", key.GetQaKeyMd5());
        if (MessageBuffer.TryAdd(key.GetQaKeyMd5(), (key, localAMsg)))
        {
            ModifyCount++;
            return true;
        }

        return false;
    }

    public async ValueTask<bool> DeleteQA(long loginUid, long groupId, MessageBody question)
    {
        MessageBody localQMsg = RebuildQMessage(question);
        QaKey key = new()
        {
            GroupId  = groupId,
            SourceId = loginUid,
            ReqMsg   = localQMsg
        };
        string md5 = key.GetQaKeyMd5();
        var (q, a) = GetQaData(md5);
        if (q is null || a is null) return false;
        MessageBuffer.Remove(md5);
        ModifyCount++;
        await DeleteQaImgFile(a);
        return true;
    }

    public async ValueTask GetAnswer(GroupMessageEventArgs args)
    {
        MessageBody localQMsg = RebuildQMessage(args.Message.MessageBody);
        QaKey input = new()
        {
            GroupId  = args.SourceGroup,
            SourceId = args.LoginUid,
            ReqMsg   = localQMsg
        };
        string md5 = input.GetQaKeyMd5();
        var (q, a) = GetQaData(md5);
        if (q is null || a is null)
            Log.Error("QA", $"未找到[{md5}]的QA数据");
        await args.Reply(a);
    }

    public List<MessageBody> GetAllQA(long loginUid, long groupId)
    {
        List<MessageBody> msg =
            MessageBuffer.Values.Where(i => i.a.SourceId == loginUid && i.a.GroupId == groupId)
                         .Select(i => i.q)
                         .ToList();
        return msg;
    }

    private (QaKey, MessageBody) GetQaData(string md5)
    {
        return !MessageBuffer.TryGetValue(md5, out var message) ? (null, null) : message;
    }

#region Match

    public bool MessageMatch(BaseMessageEventArgs args)
    {
        if (args is not GroupMessageEventArgs gArgs)
            return false;
        MessageBody localQMsg = RebuildQMessage(args.Message.MessageBody);
        QaKey input = new()
        {
            GroupId  = gArgs.SourceGroup,
            SourceId = gArgs.LoginUid,
            ReqMsg   = localQMsg
        };
        string md5 = input.GetQaKeyMd5();
        Log.Error("fuck", md5);
        bool matched = MessageBuffer.ContainsKey(md5);
        if (matched) Log.Info("QA", $"触发回答{md5}");
        return matched;
    }

#endregion

#region Data

    private Dictionary<QaKey, MessageBody> GetQaDataDict()
    {
        return MessageBuffer.ToDictionary(i => i.Value.a, i => i.Value.q);
    }

    public Task SaveQaData(CancellationToken token)
    {
        return Task.Run(() =>
                        {
                            IGenericStorage storage = SkadiApp.GetService<IGenericStorage>();
                            while (true)
                                try
                                {
                                    Task.Delay(TimeSpan.FromMinutes(30), token).Wait(token);
                                    if (ModifyCount == 0) continue;
                                    storage.SaveQaData(GetQaDataDict()).AsTask().Wait(token);
                                    if (token.IsCancellationRequested) break;
                                }
                                catch (OperationCanceledException)
                                {
                                    Log.Warning("QA SYNC", "task cancel requested");
                                    break;
                                }
                        },
                        token);
    }

#endregion

#region Util

    /// <summary>
    /// qa消息切片
    /// </summary>
    private (MessageBody q, MessageBody a) GenQaSlice(MessageBody input)
    {
        //查找分割点
        SoraSegment subSegment =
            input.FirstOrDefault(s => s.Data is TextSegment t
                                      && t.Content.IndexOf("你答", StringComparison.Ordinal)
                                      != -1);
        int nextMsgIndex = input.IndexOf(subSegment);

        //预切片
        MessageBody qMessage = new(input.Take(nextMsgIndex == 0 ? 1 : nextMsgIndex).ToList());
        if (qMessage[0].Data is not TextSegment qFirstSegment)
            return (null, null);
        MessageBody aMessage = new(input.Skip(nextMsgIndex).ToList());
        if (aMessage[0].Data is not TextSegment aFirstSegment)
            return (null, null);

        //问题切片
        int aPrefixIndex = qFirstSegment.Content.IndexOf("你答", StringComparison.Ordinal);
        string qStrPrefix = aPrefixIndex == -1
            ? qFirstSegment.Content[3..].Trim()
            : qFirstSegment.Content[3..aPrefixIndex].Trim();
        if (string.IsNullOrEmpty(qStrPrefix))
            qMessage.RemoveAt(0);
        else
            qMessage[0] = qStrPrefix;

        //回答切片
        int aStrIndex = aFirstSegment.Content.IndexOf("你答", StringComparison.Ordinal);
        string aPrefix = aStrIndex == 0
            ? aFirstSegment.Content[2..].Trim()
            : aFirstSegment.Content[(aStrIndex + 2)..].Trim();
        if (!string.IsNullOrEmpty(aPrefix))
            aMessage[0] = aPrefix;
        else
            aMessage.RemoveAt(0);
        if (!aFirstSegment.Content.Contains("有人问") && aStrIndex != 0)
            qMessage.Add(aFirstSegment.Content[..aStrIndex].Trim());

        return (qMessage, aMessage);
    }

    private async ValueTask DeleteQaImgFile(MessageBody aMessage)
    {
        IGenericStorage storage = SkadiApp.GetService<IGenericStorage>();
        if (aMessage.Count == 1 && aMessage[0].Data is CardImageSegment cardImage)
        {
            await storage.DeleteFile(cardImage.ImageFile);
            return;
        }

        for (int i = 0; i < aMessage.Count; i++)
        {
            if (aMessage[i].MessageType != SegmentType.Image
                || aMessage[i].Data is not ImageSegment image)
                continue;
            await storage.DeleteFile(image.ImgFile);
        }
    }

    /// <summary>
    /// 重构qa的消息，下载图片并替换防止图片过期
    /// </summary>
    private async ValueTask<MessageBody> RebuildAMessage(MessageBody input, long loginUid)
    {
        Dictionary<int, string> imgUrls = new();
        for (int i = 0; i < input.Count; i++)
        {
            if (input[i].MessageType != SegmentType.Image
                || input[i].Data is not ImageSegment image)
                continue;
            imgUrls.Add(i, image.Url);
        }

        if (imgUrls.Count == 0) return input;
        string path = GenericStorage.GetUserDataDirPath(loginUid, "qa_img");
        foreach ((int index, string url) in imgUrls)
        {
            string ext = Path.GetExtension(url);
            if (string.IsNullOrEmpty(ext))
                ext = ".jpg";
            string file = $"{path}/{Guid.NewGuid()}{ext}";
            //尝试下载文件，失败后重试四次
            for (int i = 0; i < 5; i++)
                if (await TryDownloadFile(url, file))
                    break;
            if (!File.Exists(file)) continue;
            input.RemoveAt(index);
            input.Insert(index, SoraSegment.Image(file));
        }

        return input;
    }

    private MessageBody RebuildQMessage(MessageBody input)
    {
        MessageBody msg = new();
        for (int i = 0; i < input.Count; i++)
        {
            if (input[i].MessageType == SegmentType.Image
                && input[i].Data is ImageSegment image)
            {
                msg.Add(SoraSegment.Image(image.ImgFile));
                continue;
            }

            msg.Add(input[i]);
        }

        return msg;
    }

    private async ValueTask<bool> TryDownloadFile(string url, string file)
    {
        await BotUtil.DownloadFile(url, file, 1);
        await Task.Delay(200);
        return File.Exists(file);
    }

#endregion

    public void Dispose()
    {
        SyncCancelToken.Cancel();
        SyncTask.Wait();

        IGenericStorage storage = SkadiApp.GetService<IGenericStorage>();
        storage.SaveQaData(GetQaDataDict());

        CommandManager cmd = SkadiApp.GetService<CommandManager>();
        cmd.DeleteDynamicCommand(MatchCmdId);
    }

    ~QaService()
    {
        Dispose();
    }
}