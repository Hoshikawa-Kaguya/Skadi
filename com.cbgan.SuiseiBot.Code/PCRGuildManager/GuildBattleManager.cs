using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Model;
using System;

namespace com.cbgan.SuiseiBot.Code.PCRGuildManager
{
    internal class GuildBattleManagerHandle
    {
        #region 属性

        public object Sender { private set; get; }
        public CQGroupMessageEventArgs MgrEventArgs { private set; get; }
        public string GBMgrCommand { private get; set; }
        public Group QQgroup { private get; set; }

        #endregion

        #region 构造函数

        public GuildBattleManagerHandle(object sender, CQGroupMessageEventArgs e)
        {
            this.MgrEventArgs = e;
            this.Sender = sender;
        }

        #endregion

        public static bool TryParseCommand(string command)
        {
            throw new NotImplementedException();
        }
    }
}
