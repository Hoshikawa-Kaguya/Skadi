using System.Threading.Tasks;
using JetBrains.Annotations;
using Sora.Attributes.Command;
using Sora.Entities.MessageElement.CQModel;
using Sora.Enumeration;
using Sora.EventArgs.SoraEvent;

namespace AntiRain.Command
{
    /// <summary>
    /// Bot实用工具
    /// </summary>
    [CommandGroup]
    public static class BotUtils
    {
        /// <summary>
        /// Echo
        /// </summary>
        [UsedImplicitly]
        [GroupCommand(CommandExpressions = new[] {@"^echo\s[\s\S]+$"},
                      MatchType          = MatchType.Regex)]
        public static async ValueTask Echo(GroupMessageEventArgs eventArgs)
        {
            //处理开头字符串
            if (eventArgs.Message.MessageBody[0].MessageType == CQType.Text)
            {
                if (eventArgs.Message.MessageBody[0].DataObject is Text str && str.Content.StartsWith("echo "))
                {
                    if (str.Content.Equals("echo ")) eventArgs.Message.MessageBody.RemoveAt(0);
                    else eventArgs.Message.MessageBody[0] = str.Content[5..];
                }
            }

            //复读
            if (eventArgs.Message.MessageBody.Count != 0) await eventArgs.Reply(eventArgs.Message.MessageBody);
        }
    }
}