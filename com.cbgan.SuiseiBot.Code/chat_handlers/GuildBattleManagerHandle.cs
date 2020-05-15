using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.cbgan.SuiseiBot.Code.chat_handlers
{
    internal class GuildBattleManagerHandle
    {
        #region 属性

        public object sender { private set; get; }
        public CQGroupMessageEventArgs eventArgs { private set; get; }
        public string pcrCommand { private get; set; }
        public Group QQgroup { private get; set; }

        #endregion

        #region 构造函数

        public GuildBattleManagerHandle(object sender, CQGroupMessageEventArgs e)
        {
            this.eventArgs = e;
            this.sender = sender;
        }

        #endregion

        public static bool TryParseCommand(string command)
        {
            
            return false;
        }
    }
}
