using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using BilibiliApi.Video;
using BilibiliApi.Video.Models;
using JetBrains.Annotations;
using Sora;
using Sora.Attributes.Command;
using Sora.Entities.CQCodes;
using Sora.Enumeration;
using Sora.EventArgs.SoraEvent;

namespace AntiRain.Command
{
    /// <summary>
    /// BV/AV号解析
    /// </summary>
    [CommandGroup]
    public static class BlibiliVideo
    {
        [UsedImplicitly]
        [GroupCommand(CommandExpressions = new[] {@"^(?:BV|bv|AV|av)[a-zA-Z0-9]+$"},
                      MatchType          = MatchType.Regex)]
        public static async ValueTask GetVideoInfo(GroupMessageEventArgs eventArgs)
        {
            VideoInfo videoInfo = VideoApis.GetVideoInfo(eventArgs.Message.RawText);
            if (videoInfo.Code != 0)
            {
                await eventArgs.Reply($"API发送错误({videoInfo.Code})\r\nmessage:{videoInfo.Message}");
                return;
            }

            List<CQCode>  sendMessage    = new();
            StringBuilder messageBuilder = new();
            messageBuilder.Append("Bilibili视频解析\r\n");
            messageBuilder.Append($"[{videoInfo.Bid}(av{videoInfo.Aid})]\r\n");
            sendMessage.AddText(messageBuilder.ToString());
            messageBuilder.Clear();
            sendMessage.Add(CQCode.CQImage(videoInfo.CoverUrl));
            messageBuilder.Append($"Link:https://b23.tv/{videoInfo.Bid}\r\n");
            messageBuilder.Append($"标题:{videoInfo.Title}\r\n");
            messageBuilder.Append($"简介:{videoInfo.Desc}\r\n");
            messageBuilder.Append($"UP:{videoInfo.AuthName}\r\nhttps://space.bilbili.com/{videoInfo.AuthUid}\r\n");
            messageBuilder.Append($"投稿时间:{videoInfo.PublishTime:yyyy-MM-dd HH:mm:ss}");
            sendMessage.AddText(messageBuilder.ToString());
            await eventArgs.Reply(sendMessage);
        }
    }
}