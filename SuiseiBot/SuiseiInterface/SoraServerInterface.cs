using System;
using System.Threading.Tasks;
using Sora;
using Sora.Tool;
using SuiseiBot.IO.Config;
using SuiseiBot.IO.Config.ConfigModule;

namespace SuiseiBot.SuiseiInterface
{
    static class SoraServerInterface
    {
        static async Task Main(string[] args)
        {
            //修改控制台标题
            Console.Title = @"SuiseiBot";
            ConsoleLog.Info("SuiseiBot初始化","SuiseiBot初始化...");
            //初始化配置文件
            ConsoleLog.Info("SuiseiBot初始化","初始化服务器全局配置...");
            //全局文件初始化不需要uid，填0仅占位，不使用构造函数重载
            Config config = new Config(0);
            config.GlobalConfigFileInit();
            config.LoadGlobalConfig(out GlobalConfig globalConfig, false);


            ConsoleLog.SetLogLevel(globalConfig.LogLevel);
            //显示Log等级
            ConsoleLog.Debug("Log Level", globalConfig.LogLevel);
            //TODO 指令初始化

            ConsoleLog.Info("SuiseiBot初始化","启动反向WS服务器...");
            //初始化服务器
            SoraWSServer server = new SoraWSServer(new ServerConfig
            {
                Location         = globalConfig.Location,
                Port             = (int)globalConfig.Port,
                AccessToken      = globalConfig.AccessToken,
                UniversalPath    = globalConfig.UniversalPath,
                ApiPath          = globalConfig.ApiPath,
                EventPath        = globalConfig.EventPath,
                HeartBeatTimeOut = (int)globalConfig.HeartBeatTimeOut,
                ApiTimeOut       = (int)globalConfig.ApiTimeOut
            });

            //服务器回调
            //初始化
            server.Event.OnClientConnect += InitalizationEvent.Initalization;
            //群聊事件
            server.Event.OnGroupMessage += GroupMessageEvent.GroupMessageParse;
            //私聊事件
            server.Event.OnPrivateMessage += PrivateMessageEvent.PrivateMessageParse;

            await server.StartServerAsync();
        }
    }
}
