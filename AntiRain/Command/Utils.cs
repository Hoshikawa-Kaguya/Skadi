using System;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using Sora.Attributes.Command;
using Sora.Entities;
using Sora.Entities.Segment;
using Sora.Entities.Segment.DataModel;
using Sora.Enumeration;
using Sora.Enumeration.ApiType;
using Sora.EventArgs.SoraEvent;

namespace AntiRain.Command;

/// <summary>
/// Bot实用工具
/// </summary>
[CommandGroup]
public static class Utils
{
    /// <summary>
    /// Echo
    /// </summary>
    [UsedImplicitly]
    [SoraCommand(
        SourceType = SourceFlag.Group,
        CommandExpressions = new[] {@"^echo\s[\s\S]+$"},
        MatchType = MatchType.Regex,
        SuperUserCommand = true)]
    public static async ValueTask Echo(GroupMessageEventArgs eventArgs)
    {
        eventArgs.IsContinueEventChain = false;
        //处理开头字符串
        if (eventArgs.Message.MessageBody[0].MessageType == SegmentType.Text)
            if (eventArgs.Message.MessageBody[0].Data is TextSegment str && str.Content.StartsWith("|echo "))
            {
                if (str.Content.Equals("echo ")) eventArgs.Message.MessageBody.RemoveAt(0);
                else eventArgs.Message.MessageBody[0] = SoraSegment.Text(str.Content[6..]);
            }

        //复读
        if (eventArgs.Message.MessageBody.Count != 0) await eventArgs.Reply(eventArgs.Message.MessageBody);
    }

    [UsedImplicitly]
    [SoraCommand(
        SourceType = SourceFlag.Group,
        CommandExpressions = new[] { @"fs" },
        MatchType = MatchType.Full,
        SuperUserCommand = true)]
    public static async ValueTask Status(GroupMessageEventArgs eventArgs)
    {
        eventArgs.IsContinueEventChain = false;

        (ApiStatus apiStatus, _, _, JObject data) = await eventArgs.SoraApi.GetStatus();
        if (apiStatus.RetCode != ApiStatusType.Ok)
        {
            await eventArgs.Reply("diannaobaozhale");
            return;
        }
        ulong msgCount = Convert.ToUInt64(data["message_received"] ?? 0) + Convert.ToUInt64(data["message_sent"] ?? 0);
        StringBuilder msg = new StringBuilder();
        msg.AppendLine("Ciallo～(∠・ω< )⌒☆");
        msg.AppendLine($"m:{msgCount}");
        msg.Append($"u:{(DateTime.Now - StaticVar.StartTime):g}");
        await eventArgs.Reply(msg.ToString());
    }
}