using com.cbgan.SuiseiBot.Code.Database;
using com.cbgan.SuiseiBot.Code.Resource;
using com.cbgan.SuiseiBot.Code.Tool;
using com.cbgan.SuiseiBot.Resource;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Interface;
using System;

namespace com.cbgan.SuiseiBot.Code.CQInterface
{
    public class AppEnableInterface : IAppEnable
    {
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
            ConsoleLog.Info("初始化", "SuiseiBot初始化");
            DatabaseInit.Init(e);//数据库初始化
            //将关键词写入内存
            ChatKeywords.KeywordInit();
            GuildCommand.GuildCommandInit();
            CommandHelpText.InitHelpText();
            e.Handler = true;
        }
    }
}
