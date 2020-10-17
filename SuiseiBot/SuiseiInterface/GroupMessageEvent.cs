using System.Threading.Tasks;
using Sora.EventArgs.SoraEvent;

namespace SuiseiBot.SuiseiInterface
{
    /// <summary>
    /// 群聊事件
    /// </summary>
    internal static class GroupMessageEvent
    {
        /// <summary>
        /// 群聊处理
        /// </summary>
        public static ValueTask GroupMessageParse(object sender, GroupMessageEventArgs groupMessage)
        {
            //TODO 指令响应分发
            return ValueTask.CompletedTask;
        }
    }
}
