using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PuppeteerSharp;
using SixLabors.ImageSharp;
using Skadi.Config;
using Skadi.ServerInterface;
using Skadi.TimerEvent;
using Skadi.Tool;
using Sora;
using Sora.Entities.Segment;
using Sora.EventArgs.SoraEvent;
using Sora.Net.Config;
using Sora.Util;
using YukariToolBox.LightLog;

namespace Skadi;

internal static class ServiceStartUp
{
    // //控制台实例
    // private static ConsoleInterface ConsoleInterface { get; set; }

    public static async Task Main()
    {
        Log.LogConfiguration.EnableConsoleOutput();
        //修改控制台标题
        Console.Title = @"Skadi";
        Log.Info("初始化", "Skadi初始化...");
        //初始化配置文件
        Log.Info("初始化", "初始化服务器全局配置...");

        if (!ConfigManager.GlobalConfigFileInit() || !ConfigManager.TryGetGlobalConfig(out var globalConfig))
        {
            Log.Fatal(new IOException("无法获取用户配置文件(StartUp)"), "初始化", "用户配置文件初始化失败");
            Environment.Exit(-1);
            return;
        }

        Log.SetLogLevel(globalConfig.LogLevel);

        //显示Log等级
        Log.Debug("Log Level", globalConfig.LogLevel.ToString());

        //初始化字符编码
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        //初始化浏览器
        Log.Info("初始化", "初始化浏览器...");
        await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultChromiumRevision);
        StaticVar.Chrome = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless          = true,
            IgnoreHTTPSErrors = true
        });

        //TODO 可能很久之后才会写了
        // //启动机器人WebAPI服务器
        // Log.Info("初始化", "启动机器人WebAPI服务器...");
        // ConsoleInterface = new ConsoleInterface(globalConfig.SkadiAPILocation, globalConfig.SkadiAPIPort);

        Log.Info("初始化", "启动反向WS服务器...");
        //初始化服务器
        var server = SoraServiceFactory.CreateService(new ServerConfig
        {
            Host                     = globalConfig.Location,
            Port                     = globalConfig.Port,
            AccessToken              = globalConfig.AccessToken,
            UniversalPath            = globalConfig.UniversalPath,
            HeartBeatTimeOut         = TimeSpan.FromSeconds(globalConfig.HeartBeatTimeOut),
            ApiTimeOut               = TimeSpan.FromMilliseconds(globalConfig.OnebotApiTimeOut),
            SuperUsers               = globalConfig.SuperUsers,
            EnableSoraCommandManager = true,
            ThrowCommandException    = false,
            SendCommandErrMsg        = false,
            CommandExceptionHandle   = CommandError
        });
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            BotUtil.BotCrash(args.ExceptionObject as Exception);
        };
        Console.CancelKeyPress += (_, args) =>
        {
            Log.Info("Ctr-C", "Skadi正在停止...");
            StaticVar.Chrome.CloseAsync();
            Thread.Sleep(1000);
            args.Cancel = true;
            Environment.Exit(0);
        };

        StaticVar.SoraCommandManager = server.Event.CommandManager;

        //服务器回调
        //初始化
        server.Event.OnClientConnect += InitializationEvent.Initialization;
        //私聊事件
        server.Event.OnPrivateMessage += PrivateMessageEvent.PrivateMessageParse;
        //群聊戳一戳
        server.Event.OnGroupPoke += GroupPokeEvent.GroupPokeEventParse;
        //关闭连接事件处理
        server.ConnManager.OnCloseConnectionAsync += SubscriptionTimer.DelTimerEvent;
        server.ConnManager.OnCloseConnectionAsync += ServerDisconnectEvent.OnServerDisconnectEvent;

        //启动服务器
        await server.StartService().RunCatch(BotUtil.BotCrash);
        StaticVar.StartTime = DateTime.Now;
        await Task.Delay(-1);
    }

    private static async void CommandError(Exception e, BaseMessageEventArgs eventArgs, string log)
    {
        string b64Pic =
            MediaUtil.DrawTextImage(log, Color.Red, Color.Black);
        switch (eventArgs)
        {
            case GroupMessageEventArgs g:
                await g.Reply(SoraSegment.Image($"base64://{b64Pic}"));
                break;
            case PrivateMessageEventArgs p:
                await p.Reply(SoraSegment.Image($"base64://{b64Pic}"));
                break;
        }
    }
}