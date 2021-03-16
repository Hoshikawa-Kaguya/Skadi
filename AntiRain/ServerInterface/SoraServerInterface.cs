using System;
using System.Text;
using System.Threading.Tasks;
using AntiRain.DatabaseUtils;
using AntiRain.DatabaseUtils.Helpers.PCRGuildBattleDB;
using AntiRain.IO.Config;
using AntiRain.Resource.PCRResource;
using AntiRain.TimerEvent;
using AntiRain.Tool;
using AntiRain.WebConsole;
using Sora.Interfaces;
using Sora.Net;
using Sora.OnebotModel;
using YukariToolBox.Extensions;
using YukariToolBox.FormatLog;

namespace AntiRain.ServerInterface
{
    static class SoraServerInterface
    {
        //控制台实例
        public static ConsoleInterface ConsoleInterface { get; private set; }

        static async Task Main()
        {
            //修改控制台标题
            Console.Title = @"AntiRain";
            Log.Info("AntiRain初始化", "AntiRain初始化...");
            //初始化配置文件
            Log.Info("AntiRain初始化", "初始化服务器全局配置...");
            //全局文件初始化不需要uid，不使用构造函数重载
            ConfigManager configManager = new();
            configManager.GlobalConfigFileInit();
            configManager.LoadGlobalConfig(out var globalConfig, false);

            Log.SetLogLevel(globalConfig.LogLevel);
            //显示Log等级
            Log.Debug("Log Level", globalConfig.LogLevel);

            //初始化资源数据库
            Log.Info("AntiRain初始化", "初始化资源...");
            DatabaseInit.GlobalDataInit();

            //检查是否开启角色数据下载
            //TODO 咕一段时间
            // if (globalConfig.ResourceConfig.UseCharaDatabase)
            // {
            //     //更新PCR角色数据库
            //     CharaParser charaParser = new CharaParser();
            //     if(!await charaParser.UpdateCharaNameByCloud()) Log.Error("AntiRain初始化","更新角色数据库失败");
            // }

            //初始化字符编码
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            //指令匹配初始化
            Command.CommandAdapter.KeywordResourseInit();
            Command.CommandAdapter.RegexResourseInit();
            Command.CommandAdapter.PCRGuildBattlecmdResourseInit();

            //启动机器人WebAPI服务器
            Log.Info("AntiRain初始化", "启动机器人WebAPI服务器...");
            ConsoleInterface = new ConsoleInterface(globalConfig.AntiRainAPILocation, globalConfig.AntiRainAPIPort);

            Log.Info("AntiRain初始化", "启动反向WS服务器...");
            //初始化服务器
            ISoraService server = SoraServiceFactory.CreateInstance(new ServerConfig
            {
                Host             = globalConfig.Location,
                Port             = globalConfig.Port,
                AccessToken      = globalConfig.AccessToken,
                UniversalPath    = globalConfig.UniversalPath,
                HeartBeatTimeOut = TimeSpan.FromSeconds(globalConfig.HeartBeatTimeOut),
                ApiTimeOut       = TimeSpan.FromMilliseconds(globalConfig.OnebotApiTimeOut)
            });

            //服务器回调
            //初始化
            server.Event.OnClientConnect += InitalizationEvent.Initalization;
            //群聊事件
            server.Event.OnGroupMessage += GroupMessageEvent.GroupMessageParse;
            //私聊事件
            server.Event.OnPrivateMessage += PrivateMessageEvent.PrivateMessageParse;
            //群聊戳一戳
            server.Event.OnGroupPoke += GroupPokeEvent.GroupPokeEventParse;
            //关闭连接事件处理
            server.ConnManager.OnCloseConnectionAsync += TimerEventParse.StopTimer;
            server.ConnManager.OnHeartBeatTimeOut     += TimerEventParse.StopTimer;

            //启动服务器
            await server.StartService().RunCatch(BotUtils.BotCrash);
            await Task.Delay(-1);
        }
    }
}