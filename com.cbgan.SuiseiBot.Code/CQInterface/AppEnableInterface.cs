using SuiseiBot.Database;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Interface;
using System.IO;
using SuiseiBot.Config;
using SuiseiBot.CommandHelp;
using SuiseiBot.Commands;
using SuiseiBot.IO.Code.TimerEvent;
using SuiseiBot.Tool.Log;

namespace SuiseiBot.IO.Code.CQInterface
{
    public class AppEnableInterface : IAppEnable
    {
        private static TimerInit timer;
        public void AppEnable(object sender, CQAppEnableEventArgs e)
        {
            if (sender == null || e == null)
            {
                e.Handler = true;
                return;
            }
            //打开控制台
            //ConsoleLog.AllocConsole();
            //Console.Title = "SuiseiBot(请勿关闭此窗口)";

            //初始化配置文件
            Config.Config config = new Config.Config(e.CQApi.GetLoginQQ().Id);
            //设置Log等级
            ConsoleLog.SetLogLevel(config.LoadedConfig.LogLevel);
            //读取应用信息
            ConsoleLog.Debug("APP AuthCode(native plugin ID)", e.CQApi.AppInfo.AuthCode);
            //修改环境文件夹，初始化环境
            ConsoleLog.Info("获取到环境路径", Directory.GetCurrentDirectory());
            System.Environment.SetEnvironmentVariable("Path", Directory.GetCurrentDirectory());
            //显示Log等级
            ConsoleLog.Debug("Log Level", config.LoadedConfig.LogLevel);
            //在控制台显示启用模块
            ConsoleLog.Info("已启用的模块",
                            $"\n{config.LoadedConfig.ModuleSwitch.ToString()}");

            //数据库初始化
            ConsoleLog.Info("初始化", "SuiseiBot初始化");
            DatabaseInit.Init(e);

            //将关键词和帮助文本写入内存
            WholeMatchCmd.KeywordInit();
            PCRGuildCmd.PCRGuildCommandInit();
            GuildCommandHelp.InitHelpText();
            KeywordCmd.SpecialKeywordsInit(e.CQApi);

            //初始化定时器线程
            if (config.LoadedConfig.ModuleSwitch.Bili_Subscription || config.LoadedConfig.ModuleSwitch.PCR_Subscription)
            {
                timer = new TimerInit(e.CQApi, config.LoadedConfig.SubscriptionConfig.FlashTime);
            }
            e.Handler = true;
        }
    }
}
