using com.cbgan.SuiseiBot.Code.Database;
using com.cbgan.SuiseiBot.Code.Tool;
using com.cbgan.SuiseiBot.Resource;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.cbgan.SuiseiBot.Code.CQInterface
{
    public class AppEnableInterface : IAppEnable
    {
        public void AppEnable(object sender, CQAppEnableEventArgs e)
        {
            ChatKeywords.Keyword_init();
            Utils.AllocConsole();
            Console.Title = "SuiseiBot(请勿关闭此窗口)";
            DatabaseInit.Init(e);//数据库初始化
            e.Handler = true;
        }
    }
}
