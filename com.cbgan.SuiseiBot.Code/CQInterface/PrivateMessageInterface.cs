using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Interface;
using Native.Sdk.Cqp.Model;

namespace com.cbgan.SuiseiBot.Code.CQInterface
{
    public class PrivateMessageInterface : IPrivateMessage
    {
        public void PrivateMessage(object sender, CQPrivateMessageEventArgs e)
        {
            QQ id = e.FromQQ;
            if (e.Message.Text.Equals("debug"))
            {
                
            }
            e.Handler = true;
        }
    }
}
