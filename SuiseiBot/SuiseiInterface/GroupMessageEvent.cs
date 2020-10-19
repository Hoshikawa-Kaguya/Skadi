using System.Threading.Tasks;
using Sora.EventArgs.SoraEvent;
using Sora.Tool;
using SuiseiBot.ChatModule.PCRGuildBattle;
using SuiseiBot.Resource;
using SuiseiBot.Resource.TypeEnum.CommandType;

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
            //指令匹配
            //#开头的指令 -> 关键词 -> 正则
            //会战管理
            if (Command.GetPCRGuildBattlecmdType(groupMessage.Message.RawText, out PCRGuildBattleCommand battleCommand))
            {
                ConsoleLog.Info("PCR会战管理",$"获取到指令[{battleCommand}]");
                PcrGuildBattleChatHandle chatHandle = new PcrGuildBattleChatHandle(sender, groupMessage, battleCommand);
                chatHandle.GetChat();
            }
            //聊天关键词
            if (Command.GetKeywordType(groupMessage.Message.RawText, out KeywordCommand keywordCommand))
            {
                ConsoleLog.Info("关键词触发",$"触发关键词[{keywordCommand}]");
            }
            //正则匹配
            if (Command.GetRegexType(groupMessage.Message.RawText, out RegexCommand regexCommand))
            {
                ConsoleLog.Info("正则触发",$"触发正则匹配[{regexCommand}]");
            }
            return ValueTask.CompletedTask;
        }
    }
}
