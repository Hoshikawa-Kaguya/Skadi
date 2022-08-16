using System;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using PuppeteerSharp;
using Sora.Attributes.Command;
using Sora.Entities;
using Sora.Entities.Segment;
using Sora.Entities.Segment.DataModel;
using Sora.Enumeration;
using Sora.Enumeration.ApiType;
using Sora.EventArgs.SoraEvent;

namespace Skadi.Command;

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
                if (str.Content.Equals("echo "))
                    eventArgs.Message.MessageBody.RemoveAt(0);
                else
                    eventArgs.Message.MessageBody[0] = SoraSegment.Text(str.Content[6..]);
            }

        //复读
        if (eventArgs.Message.MessageBody.Count != 0)
            await eventArgs.Reply(eventArgs.Message.MessageBody);
    }

    [UsedImplicitly]
    [SoraCommand(
        SourceType = SourceFlag.Group,
        CommandExpressions = new[] {@"fs"},
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
        msg.Append($"u:{DateTime.Now - StaticVar.StartTime:g}");
        await eventArgs.Reply(msg.ToString());
    }

    [UsedImplicitly]
    [SoraCommand(
        SourceType = SourceFlag.Group,
        CommandExpressions = new[] {@"^看看\s.+$"},
        MatchType = MatchType.Regex)]
    public static async ValueTask Curl(GroupMessageEventArgs eventArgs)
    {
        string[] args = eventArgs.Message.RawText.Split(' ');
        string   url  = args[1];
        if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
        {
            await eventArgs.Reply("这不是url啊kora");
            return;
        }

        eventArgs.IsContinueEventChain = false;

        await eventArgs.Reply("running...");

        Page page = await StaticVar.Chrome.NewPageAsync();
        await page.SetViewportAsync(new ViewPortOptions
        {
            Width  = 1920,
            Height = 1080
        });

        await page.GoToAsync(url);
        ElementHandle img = await page.QuerySelectorAsync("body > img");
        //单图网页
        if (img is not null)
        {
            string imgUrl = (await img.GetPropertyAsync("src")).RemoteObject.Value.ToString();
            await eventArgs.Reply(SoraSegment.Image(imgUrl));
            return;
        }

        string picB64 = await page.ScreenshotBase64Async(new ScreenshotOptions
        {
            FullPage       = false,
            OmitBackground = true,
            Type           = ScreenshotType.Png
        });

        await eventArgs.Reply(SoraSegment.Image($"base64://{picB64}"));
    }
}