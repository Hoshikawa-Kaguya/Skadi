using com.cbgan.SuiseiBot.Code.database;
using com.cbgan.SuiseiBot.Code.handlers;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.cbgan.SuiseiBot.Code
{
    public class AppEnableEvent : IAppEnable
    {
        public void AppEnable(object sender, CQAppEnableEventArgs e)
        {
            DatabaseInit.Init(e);//数据库初始化
            e.Handler = true;
        }
    }
}
