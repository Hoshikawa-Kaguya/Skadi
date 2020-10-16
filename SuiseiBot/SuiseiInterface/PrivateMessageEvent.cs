using System.Threading.Tasks;
using Sora.EventArgs.SoraEvent;

namespace SuiseiBot.SuiseiInterface
{
    /// <summary>
    /// 私聊事件
    /// </summary>
    internal static class PrivateMessageEvent
    {
        /// <summary>
        /// 私聊处理
        /// </summary>
        public static async ValueTask PrivateMessageParse(object sender, PrivateMessageEventArgs privateMessage)
        {
            //简单的机器人响应
            if (privateMessage.Message.RawText.Equals("suisei"))
            {
                await privateMessage.Sender.SendPrivateMessage("すいちゃんは——今日もかわいい！");
            }
        }
    }
}
