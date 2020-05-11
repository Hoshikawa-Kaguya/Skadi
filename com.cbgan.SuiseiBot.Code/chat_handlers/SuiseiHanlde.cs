using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using com.cbgan.SuiseiBot.Code.database;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Interface;
using Native.Sdk.Cqp.Model;

namespace com.cbgan.SuiseiBot.Code.handlers
{
    internal class SuiseiHanlde
    {
        #region 属性
        public object sender { private set; get; }
        public CQGroupMessageEventArgs eventArgs { private set; get; }
        #endregion

        #region 构造函数
        public SuiseiHanlde(object sender, CQGroupMessageEventArgs e)
        {
            this.eventArgs = e;
            this.sender = sender;
        }
        #endregion

        public void GetChat()
        {
            eventArgs.CQLog.Info("收到消息", "签到");
            SuiseiDBHandle suiseiDB = new SuiseiDBHandle(sender, eventArgs);
            eventArgs.CQApi.SendPrivateMessage(2027107091, "当前好感度 = ", suiseiDB.SignIn());
            eventArgs.Handler = true;
        }
    }
}
