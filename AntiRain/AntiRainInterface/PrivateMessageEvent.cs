using System.Threading.Tasks;
using Sora.Entities.CQCodes;
using Sora.EventArgs.SoraEvent;

namespace AntiRain.AntiRainInterface
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
            if (privateMessage.Message.RawText.Equals("在吗"))
            {
                await privateMessage.Sender.SendPrivateMessage(CQCode.CQImage("https://i.loli.net/2020/10/20/zWPyocxFEVp2tDT.jpg"));
            }
        }
    }
}
