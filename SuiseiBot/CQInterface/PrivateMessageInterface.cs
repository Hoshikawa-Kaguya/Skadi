using Native.Sdk.Cqp.EventArgs;
using SuiseiBot.Code.Tool.LogUtils;

namespace SuiseiBot.Code.CQInterface
{
    public static class PrivateMessageInterface
    {
        public static void PrivateMessage(object sender, CQPrivateMessageEventArgs e)
        {
            ConsoleLog.Info($"收到信息[私信:{e.FromQQ.Id}]",$"{(e.Message.Text).Replace("\r\n", "\\r\\n")}\n{e.Message.Id}");
            if (e.Message.Text.Equals("suisei"))
            {
                e.FromQQ.SendPrivateMessage("すいちゃんは——今日もかわいい！");
            }
            e.Handler = true;
        }
    }
}
