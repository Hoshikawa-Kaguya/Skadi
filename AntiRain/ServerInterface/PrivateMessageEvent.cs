using System.Threading.Tasks;
using Sora.Entities.Segment;
using Sora.EventArgs.SoraEvent;

namespace AntiRain.ServerInterface
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
            if (privateMessage.Message.RawText.Equals("在"))
            {
                await privateMessage.Sender
                                    .SendPrivateMessage(SoraSegment
                                                            .Image("https://i.loli.net/2020/11/02/2OgZ1M6YNV5kntS.gif"));
            }
        }
    }
}