using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using PuppeteerSharp;
using Skadi.Tool;
using Sora.Attributes.Command;
using Sora.Entities;
using Sora.Entities.Segment;
using Sora.Entities.Segment.DataModel;
using Sora.Enumeration;
using Sora.Enumeration.ApiType;
using Sora.Enumeration.EventParamsType;
using Sora.EventArgs.SoraEvent;
using YukariToolBox.LightLog;

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
        CommandExpressions = new[] {@"#sk"},
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
        float tMem = GC.GetTotalAllocatedBytes(true) / (1024 * 1024f);
        float mem = Environment.WorkingSet / (1024 * 1024f);
        double cpu = await GetCpuUsageForProcess();

        msg.AppendLine("Skadi-Status");
        msg.AppendLine("Ciallo～(∠・ω< )⌒☆");
        msg.AppendLine($"消息数量:{msgCount}");
        msg.AppendLine($"运行时间:{DateTime.Now - StaticVar.StartTime:g}");
        msg.AppendLine($"GC Allocated:{tMem.ToString("F2")}MB");
        msg.AppendLine($"RAM:{mem.ToString("F2")}MB");
        msg.AppendLine($"CPU:{cpu.ToString("F2")}%");
        msg.Append($"P_THC:{ThreadPool.ThreadCount}");
        await eventArgs.Reply(msg.ToString());
    }

    [UsedImplicitly]
    [SoraCommand(
        SourceType = SourceFlag.Group,
        CommandExpressions = new[] {@"^看看\s.+$"},
        MatchType = MatchType.Regex,
        PermissionLevel = MemberRoleType.Admin)]
    public static async ValueTask Curl(GroupMessageEventArgs eventArgs)
    {
        string[] args        = eventArgs.Message.RawText.Split(' ');
        string   url         = args[1];
        bool     all         = args.Contains("-a");
        bool     autoRemove  = args.Contains("-ar");
        bool     fakeMessage = args.Contains("-f");

        if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
        {
            await eventArgs.Reply("这不是url啊kora");
            return;
        }

        eventArgs.IsContinueEventChain = false;

        await eventArgs.Reply("running...");
        Log.Info("Curl", $"获取到url [{url}]");

        Page page = await StaticVar.Chrome.NewPageAsync();
        await page.SetViewportAsync(new ViewPortOptions
        {
            Width  = 1920,
            Height = 1080
        });

        Response    resp    = await page.GoToAsync(url);
        MessageBody message = new MessageBody();
        TimeSpan    timeout = TimeSpan.FromMinutes(1);

        //单图网页
        if (resp.Status == HttpStatusCode.OK
         && resp.Headers.ContainsKey("content-type")
         && resp.Headers["content-type"].Contains("image/"))
        {
            Log.Info("Curl", "获取到图片");
            message.Add(SoraSegment.Image(url));
        }
        else
        {
            Log.Info("Curl", "生成截图...");
            string picB64 = await page.ScreenshotBase64Async(new ScreenshotOptions
            {
                FullPage = all,
                Type     = ScreenshotType.Png
            });
            message.Add(SoraSegment.Image($"base64://{picB64}"));
        }
        //关闭页面
        await page.CloseAsync();
        await page.DisposeAsync();

        (ApiStatus status, int msgId) ret;
        if (fakeMessage)
            ret = await eventArgs.SourceGroup.SendGroupForwardMsg(new[] {new CustomNode("色色", 114514, message)},
                      timeout);
        else
            ret = await eventArgs.Reply(message, timeout);

        if (ret.status.RetCode == ApiStatusType.Ok && autoRemove)
        {
            Log.Info("Curl", $"自动撤回消息[{ret.msgId}]");
            BotUtil.AutoRemoveMessage(ret.msgId, eventArgs.LoginUid);
        }
    }

    private static async Task<double> GetCpuUsageForProcess()
    {
        var startTime     = DateTime.UtcNow;
        var startCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
        await Task.Delay(500);

        var endTime       = DateTime.UtcNow;
        var endCpuUsage   = Process.GetCurrentProcess().TotalProcessorTime;
        var cpuUsedMs     = (endCpuUsage - startCpuUsage).TotalMilliseconds;
        var totalMsPassed = (endTime     - startTime).TotalMilliseconds;
        var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
        return cpuUsageTotal * 100;
    }
}