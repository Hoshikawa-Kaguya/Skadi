using com.cbgan.SuiseiBot.Code.Tool;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Interface;

namespace com.cbgan.SuiseiBot.Code.CQInterface
{
    public class PrivateMessageInterface : IPrivateMessage
    {
        public void PrivateMessage(object sender, CQPrivateMessageEventArgs e)
        {
            ConsoleLog.Info($"收到信息[私信:{e.FromQQ.Id}]",$"{(e.Message.Text).Replace("\r\n", "\\r\\n")}\n{e.Message.Id}");
            if (sender == null || e == null)
            {
                e.Handler = true;
                return;
            }
            if (e.Message.Text.Equals("在？"))
            {
                e.FromQQ.SendPrivateMessage("噫hihihihihih");
            }
            e.Handler = true;
        }
    }
}
