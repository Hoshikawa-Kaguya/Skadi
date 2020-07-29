using com.cbgan.SuiseiBot.Code.Database;
using com.cbgan.SuiseiBot.Code.Resource;
using com.cbgan.SuiseiBot.Code.Tool;
using com.cbgan.SuiseiBot.Code.TimerEvent;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Interface;
using System;
using System.IO;

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
            ConsoleLog.AllocConsole();
            Console.Title = "SuiseiBot(请勿关闭此窗口)";
            //修改环境文件夹
            ConsoleLog.Info("获取到环境路径", Directory.GetCurrentDirectory());
            System.Environment.SetEnvironmentVariable("Path", Directory.GetCurrentDirectory());
            ConsoleLog.Info("初始化", "SuiseiBot初始化");
            DatabaseInit.Init(e);//数据库初始化
            //将关键词写入内存
            ChatKeywords.KeywordInit();
            PCRGuildCommand.PCRGuildCommandInit();
            CommandHelpText.InitHelpText();
            SpecialKeywords.SpecialKeywordsInit();
            //初始化定时器线程
            timer = new TimerInit(e.CQApi);
            e.Handler = true;
        }
    }
}
