using com.cbgan.SuiseiBot.Code.Database;
using com.cbgan.SuiseiBot.Code.Tool;
using com.cbgan.SuiseiBot.Code.TimerEvent;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Interface;
using System.IO;
using com.cbgan.SuiseiBot.Code.Resource.CommandHelp;
using com.cbgan.SuiseiBot.Code.Resource.Commands;

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
            //修改环境文件夹
            ConsoleLog.Info("获取到环境路径", Directory.GetCurrentDirectory());
            System.Environment.SetEnvironmentVariable("Path", Directory.GetCurrentDirectory());
            ConsoleLog.Info("初始化", "SuiseiBot初始化");
            DatabaseInit.Init(e);//数据库初始化
            //将关键词和帮助文本写入内存
            WholeMatchCmd.KeywordInit();
            PCRGuildCmd.PCRGuildCommandInit();
            GuildCommandHelp.InitHelpText();
            KeywordCmd.SpecialKeywordsInit();
            //初始化定时器线程
            timer = new TimerInit(e.CQApi);
            e.Handler = true;
        }
    }
}
