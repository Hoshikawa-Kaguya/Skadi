using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PuppeteerSharp;
using Skadi.Config;
using Skadi.ServerInterface;
using Skadi.Services;
using Skadi.TimerEvent;
using Skadi.Tool;
using Sora;
using Sora.Interfaces;
using Sora.Net.Config;
using Sora.Util;
using YukariToolBox.LightLog;

namespace Skadi;

internal static class ServiceStartUp
{
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
        StaticStuff.Services.AddScoped<IChromeService, ChromeService>();

        Log.Info("初始化", "启动反向WS服务器...");
        //初始化服务器
        ISoraService server = SoraServiceFactory.CreateService(new ServerConfig
        {
            Host          = globalConfig.Location,
            Port          = globalConfig.Port,
            AccessToken   = globalConfig.AccessToken,
            UniversalPath = globalConfig.UniversalPath,
            HeartBeatTimeOut =
                TimeSpan.FromSeconds(globalConfig.HeartBeatTimeOut),
            ApiTimeOut =
                TimeSpan.FromMilliseconds(globalConfig.OnebotApiTimeOut),
            SuperUsers               = globalConfig.SuperUsers,
            EnableSoraCommandManager = true,
            ThrowCommandException    = false,
            SendCommandErrMsg        = false,
            CommandExceptionHandle   = BotUtil.CommandError
        });
        StaticStuff.Services.AddSingleton(server.Event.CommandManager);
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            BotUtil.BotCrash(args.ExceptionObject as Exception);
        };

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
        StaticStuff.StartTime = DateTime.Now;

        Console.CancelKeyPress += (_, args) =>
        {
            Log.Info("Ctr-C", "Skadi正在停止...");
            StaticStuff.Services.Clear();
            Thread.Sleep(1000);
            args.Cancel = true;
            Environment.Exit(0);
        };

        await Task.Delay(-1);
    }
}