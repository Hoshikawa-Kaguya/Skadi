using System;
using System.IO;
using Native.Sdk.Cqp.EventArgs;
using SuiseiBot.Code.DatabaseUtils;
using SuiseiBot.Code.IO.Config;
using SuiseiBot.Code.Resource.CommandHelp;
using SuiseiBot.Code.Resource.Commands;
using SuiseiBot.Code.TimerEvent;
using SuiseiBot.Code.Tool.LogUtils;

namespace SuiseiBot.Code.CQInterface
{
    public static class AppEnableInterface
    {
        private static TimerInit timer;
        public static void AppEnable(object sender, CQAppEnableEventArgs e)
        {
            try
            {
                //打开控制台
                //ConsoleLog.AllocConsole();
                Console.Title = "SuiseiBot";

                //初始化配置文件
                Config config = new Config(e.CQApi.GetLoginQQ().Id);
                //设置Log等级
                ConsoleLog.SetLogLevel(config.LoadedConfig.LogLevel);
                //读取应用信息
                ConsoleLog.Debug("APP AuthCode(native plugin ID)", e.CQApi.AppInfo.AuthCode);
                //修改环境文件夹，初始化环境
                ConsoleLog.Debug("获取到环境路径", Directory.GetCurrentDirectory());
                Environment.SetEnvironmentVariable("Path", Directory.GetCurrentDirectory());
                //显示Log等级
                ConsoleLog.Debug("Log Level", config.LoadedConfig.LogLevel);
                //在控制台显示启用模块
                ConsoleLog.Info("已启用的模块",
                                $"\n{config.LoadedConfig.ModuleSwitch}");
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
            catch (Exception exception)
            {
                ConsoleLog.Error("error",ConsoleLog.ErrorLogBuilder(exception));
            }
        }
    }
}
