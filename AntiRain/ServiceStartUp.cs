using System;
using System.Text;
using System.Threading.Tasks;
using AntiRain.Config;
using AntiRain.ServerInterface;
using AntiRain.TimerEvent;
using AntiRain.Tool;
using AntiRain.WebConsole;
using Sora.Interfaces;
using Sora.Net;
using Sora.Net.Config;
using YukariToolBox.Extensions;
using YukariToolBox.FormatLog;

namespace AntiRain
{
    internal static class ServiceStartUp
    {
        //控制台实例
        private static ConsoleInterface ConsoleInterface { get; set; }

        public static async Task Main()
        {
            //[Description(@"^谁是[\u4e00-\u9fa5]+$")] FindChara,//TODO 最后一个PCR功能（
            //修改控制台标题
            Console.Title = @"AntiRain";
            Log.Info("AntiRain初始化", "AntiRain初始化...");
            //初始化配置文件
            Log.Info("AntiRain初始化", "初始化服务器全局配置...");

            if (!ConfigManager.GlobalConfigFileInit() || !ConfigManager.TryGetGlobalConfig(out var globalConfig))
            {
                Log.Fatal("AntiRain初始化", "无法获取用户配置文件");
                Environment.Exit(-1);
                return;
            }

            Log.SetLogLevel(globalConfig.LogLevel);
            //显示Log等级
            Log.Debug("Log Level", globalConfig.LogLevel);

            //初始化资源数据库
            Log.Info("AntiRain初始化", "初始化资源...");

            //初始化字符编码
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            //启动机器人WebAPI服务器
            Log.Info("AntiRain初始化", "启动机器人WebAPI服务器...");
            ConsoleInterface = new ConsoleInterface(globalConfig.AntiRainAPILocation, globalConfig.AntiRainAPIPort);

            Log.Info("AntiRain初始化", "启动反向WS服务器...");
            //初始化服务器
            ISoraService server = SoraServiceFactory.CreateService(new ServerConfig
            {
                Host                     = globalConfig.Location,
                Port                     = globalConfig.Port,
                AccessToken              = globalConfig.AccessToken,
                UniversalPath            = globalConfig.UniversalPath,
                HeartBeatTimeOut         = TimeSpan.FromSeconds(globalConfig.HeartBeatTimeOut),
                ApiTimeOut               = TimeSpan.FromMilliseconds(globalConfig.OnebotApiTimeOut),
                SuperUsers               = globalConfig.SuperUsers,
                EnableSoraCommandManager = true
            });

            //服务器回调
            //初始化
            server.Event.OnClientConnect += InitalizationEvent.Initalization;
            //私聊事件
            server.Event.OnPrivateMessage += PrivateMessageEvent.PrivateMessageParse;
            //群聊戳一戳
            server.Event.OnGroupPoke += GroupPokeEvent.GroupPokeEventParse;
            //关闭连接事件处理
            server.ConnManager.OnCloseConnectionAsync += SubscriptionTimer.DelTimerEvent;

            //启动服务器
            await server.StartService().RunCatch(BotUtils.BotCrash);
            await Task.Delay(-1);
        }
    }
}