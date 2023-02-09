using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Skadi.Interface;
using Skadi.Tool;
using Sora.Entities;
using Sora.Entities.Segment;
using Sora.Entities.Segment.DataModel;
using Sora.Enumeration;

// ReSharper disable PossibleMultipleEnumeration

namespace Skadi.Services;

internal class QaService : IQaService
{
    public async ValueTask<int> AddNewQA(long loginUid, long groupId, MessageBody message)
    {
        (MessageBody qMsg, MessageBody aMsg) = GenQaSlice(message);
        MessageBody localAMsg = await RebuildMessage(aMsg, loginUid);
        throw new NotImplementedException();
    }

    public int DeleteQA(long loginUid, long groupId, MessageBody question)
    {
        throw new NotImplementedException();
    }

    public MessageBody GetAnswer(long loginUid, long groupId, MessageBody question)
    {
        throw new NotImplementedException();
    }

    public List<MessageBody> GetAllQA(long loginUid, long groupId)
    {
        throw new NotImplementedException();
    }

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
        string qStrPrefix = qFirstSegment.Content[3..].Trim();
        if (string.IsNullOrEmpty(qStrPrefix))
            qMessage.RemoveAt(0);
        else
            qMessage[0] = qStrPrefix;

        //回答切片
        int aStrIndex = aFirstSegment.Content.IndexOf("你答", StringComparison.Ordinal);
        if (aStrIndex == 0)
        {
            aMessage[0] = aFirstSegment.Content[2..].Trim();
        }
        else
        {
            aMessage[0] = aFirstSegment.Content[(aStrIndex + 2)..].Trim();
            qMessage.Add(aFirstSegment.Content[..aStrIndex].Trim());
        }

        return (qMessage, aMessage);
    }

    /// <summary>
    /// 重构qa的消息，下载图片并替换防止图片过期
    /// </summary>
    private async ValueTask<MessageBody> RebuildMessage(MessageBody input, long loginUid)
    {
        Dictionary<int, string> imgUrls = new();
        if (input.Count == 1 && input[0].Data is CardImageSegment cardImage)
        {
            imgUrls.Add(0, cardImage.ImageFile);
        }

        for (int i = 0; i < input.Count; i++)
        {
            if (input[i].MessageType != SegmentType.Image
                || input[i].Data is not ImageSegment image)
                continue;
            imgUrls.Add(i, image.ImgFile);
        }
        if (imgUrls.Count == 0) return input;

        string path = StorageService.GetUserDataDirPath(loginUid, "qa_img");
        foreach ((int index, string url) in imgUrls)
        {
            string ext = Path.GetExtension(url);
            if (string.IsNullOrEmpty(ext))
                ext = ".jpg";
            string file = $"{path}/{Guid.NewGuid()}{ext}";
            for (int i = 0; i < 5; i++)
            {
                await BotUtil.DownloadFile(url, file);
                await Task.Delay(200);
                if (File.Exists(file))
                    break;
            }
            if (!File.Exists(file))
                continue;
            input[index] = SoraSegment.Image(file);
        }
        return input;
    }
}