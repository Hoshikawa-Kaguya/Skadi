using com.cbgan.SuiseiBot.Code.Database;
using com.cbgan.SuiseiBot.Code.TimerEvent;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Interface;
using System.IO;
using com.cbgan.SuiseiBot.Code.IO.Config;
using com.cbgan.SuiseiBot.Code.Resource.CommandHelp;
using com.cbgan.SuiseiBot.Code.Resource.Commands;
using com.cbgan.SuiseiBot.Code.Tool.Log;

namespace com.cbgan.SuiseiBot.Code.CQInterface
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

            //修改环境文件夹，初始化环境
            ConsoleLog.Info("获取到环境路径", Directory.GetCurrentDirectory());
            System.Environment.SetEnvironmentVariable("Path", Directory.GetCurrentDirectory());

            //初始化配置文件
            ConfigIO config = new ConfigIO(e.CQApi.GetLoginQQ().Id);
            //设置Log等级
            ConsoleLog.SetLogLevel(config.LoadedConfig.LogLevel);
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
            KeywordCmd.SpecialKeywordsInit();

            //初始化定时器线程
            if (config.LoadedConfig.ModuleSwitch.Bili_Subscription || config.LoadedConfig.ModuleSwitch.PCR_Subscription)
            {
                timer = new TimerInit(e.CQApi, config.LoadedConfig.SubscriptionConfig.FlashTime);
            }
            e.Handler = true;
        }
    }
}
