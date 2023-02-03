using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Skadi.Entities;
using Skadi.Interface;
using Sora.Command;
using Sora.Entities;
using Sora.Entities.Segment;
using Sora.Entities.Segment.DataModel;
using Sora.Enumeration;
using Sora.Enumeration.EventParamsType;
using Sora.Util;
using YukariToolBox.LightLog;
// ReSharper disable PossibleMultipleEnumeration

namespace Skadi.Services;

internal class QaService : IQaService
{
    private string QaConfigPath { get; }

    private List<QABuf> QaBufs { get; }

    public long LoginUid { get; }

    public QaService(long loginUid)
    {
        LoginUid = loginUid;
        Log.Info($"QA[{LoginUid}]", "QA初始化");
        QaConfigPath = StorageService.GetQAFilePath(LoginUid);
        QaBufs       = new List<QABuf>();
        if (!File.Exists(QaConfigPath))
        {
            Log.Warning($"QA[{LoginUid}]", "未找到QA配置文件，创建新的配置文件");
            using TextWriter writer = File.CreateText(QaConfigPath);
            writer.Write("[]");
            writer.Close();
        }

        List<QaData> qaMsg = ReadFile();
        foreach (QaData data in qaMsg)
            RegisterNewQaCommand(data);
        Log.Info($"QA[{LoginUid}]", $"已加载{QaBufs.Count}条QA");
    }

    #region Manage

    /// <summary>
    /// 添加QA
    /// </summary>
    /// <returns>
    /// <para>-1 有相同QA</para>
    /// <para>-2 错误</para>
    /// </returns>
    public int AddNewQA(QaData newQA)
    {
        //查找相同问题的指令
        List<QaData> qaData = ReadFile();
        if (qaData.Exists(q => MessageEqual(q.qMsg, newQA.qMsg) && MessageEqual(q.aMsg, newQA.aMsg) && q.GroupId == newQA.GroupId))
        {
            return -1;
        }
        
        //注册为动态指令
        Guid cmdId = RegisterNewQaCommand(newQA);
        if (cmdId == Guid.Empty)
        {
            Log.Error($"QA[{LoginUid}]", "注册QA指令失败");
            return -2;
        }

        //更新配置文件
        qaData.Add(newQA);
        UpdateFile(qaData);
        return 0;
    }

    /// <summary>
    /// 删除QA
    /// </summary>
    /// <returns>
    /// <para>-1 没有QA</para>
    /// </returns>
    public int DeleteQA(MessageBody qMsg, long groupId)
    {
        //查找相同问题的指令
        List<QaData> qaData = ReadFile();
        if (!qaData.Exists(q => MessageEqual(q.qMsg, qMsg) && q.GroupId == groupId))
        {
            return -1;
        }
        //删除配置文件中的QA指令
        qaData.RemoveAll(q => MessageEqual(qMsg, q.qMsg) && q.GroupId == groupId);
        UpdateFile(qaData);

        //删除已经注册的动态指令
        HashSet<Guid> ids = QaBufs.Where(q => MessageEqual(q.qMsg, qMsg) && q.gid == groupId)
                                  .Select(q => q.cmdId)
                                  .ToHashSet();
        QaBufs.RemoveAll(q => MessageEqual(qMsg, q.qMsg) && q.gid == groupId);
        foreach (Guid id in ids)
        {
            CommandManager cmd = SkadiApp.GetService<CommandManager>();
            cmd?.DeleteDynamicCommand(id);
        }

        return 0;
    }

    public List<MessageBody> GetAllQA(long groupId)
    {
        List<MessageBody> temp = QaBufs.Where(q => q.gid == groupId)
                                       .Select(q => q.qMsg)
                                       .ToList();
        List<MessageBody> qList = new();

        foreach (MessageBody msg in temp)
        {
            if (qList.Exists(m => MessageEqual(m, msg)))
                continue;
            qList.Add(msg);
        }

        return qList;
    }

    private Guid RegisterNewQaCommand(QaData qaData)
    {
        CommandManager cmd = SkadiApp.GetService<CommandManager>();
        Guid cmdId =
            cmd?.RegisterGroupDynamicCommand(args => MessageEqual(args.Message.MessageBody, qaData.qMsg),
                                             async e =>
                                             {
                                                 e.IsContinueEventChain = false;
                                                 await e.Reply(qaData.aMsg);
                                             },
                                             "qa_global",
                                             MemberRoleType.Member,
                                             false,
                                             0,
                                             new[] { qaData.GroupId },
                                             null,
                                             new[] { LoginUid }) ?? Guid.Empty;
        if (cmdId != Guid.Empty)
            QaBufs.Add(new QABuf
            {
                cmdId = cmdId,
                gid   = qaData.GroupId,
                qMsg  = qaData.qMsg
            });
        return cmdId;
    }

#endregion

#region IO

    private List<QaData> ReadFile()
    {
        List<QaData> commands = new();

        JToken qaJson = JToken.Parse(File.ReadAllText(QaConfigPath));

        List<(string qMsg, string aMsg, long groupId)> temp =
            qaJson.ToObject<List<(string qMsg, string aMsg, long groupId)>>();

        if (temp == null)
            return null;
        foreach ((string qMsgStr, string aMsgStr, long groupId) in temp)
            commands.Add(new QaData
            {
                qMsg    = CQCodeUtil.DeserializeMessage(qMsgStr),
                aMsg    = CQCodeUtil.DeserializeMessage(aMsgStr),
                GroupId = groupId
            });

        return commands;
    }

    private void UpdateFile(List<QaData> data)
    {
        List<(string qMsg, string aMsg, long groupId)> temp = new();

        foreach (QaData qaData in data)
            temp.Add((qaData.qMsg.SerializeMessage(), qaData.aMsg.SerializeMessage(), qaData.GroupId));

        JToken json = JToken.FromObject(temp);
        File.WriteAllText(QaConfigPath, json.ToString(Formatting.None));
    }

#endregion

#region Util

    public static bool MessageEqual(MessageBody srcMsg, MessageBody rxMsg)
    {
        if (rxMsg is null)
            return false;
        if (!MessageCheck(rxMsg) || srcMsg.Count != rxMsg.Count)
            return false;

        for (int i = 0; i < srcMsg.Count; i++)
            switch (srcMsg[i].MessageType)
            {
                case SegmentType.Text:
                    if ((srcMsg[i].Data as TextSegment)!.Content
                        != ((rxMsg[i].Data as TextSegment)?.Content ?? string.Empty))
                        return false;
                    break;
                case SegmentType.Image:
                    if ((srcMsg[i].Data as ImageSegment)!.ImgFile != (rxMsg[i].Data as ImageSegment)?.ImgFile)
                        return false;
                    break;
                case SegmentType.At:
                    if ((srcMsg[i].Data as AtSegment)!.Target != (rxMsg[i].Data as AtSegment)?.Target)
                        return false;
                    break;
                case SegmentType.Face:
                    if ((srcMsg[i].Data as FaceSegment)!.Id != (rxMsg[i].Data as FaceSegment)?.Id)
                        return false;
                    break;
                default:
                    return false;
            }

        return true;
    }

    public static bool MessageCheck(MessageBody message)
    {
        if (message is null)
            return false;
        bool check = true;
        foreach (SoraSegment segment in message)
            check &= segment.MessageType is
                SegmentType.Text or SegmentType.At or SegmentType.Face or SegmentType.Image;
        return check;
    }

#endregion
}