using System.Collections.Generic;
using com.cbgan.SuiseiBot.Code.Resource.Enum;

namespace com.cbgan.SuiseiBot.Code.Resource.CommandHelp
{
    /// <summary>
    /// 大佬快教教我这个指令怎么用.jpg
    /// </summary>
    internal static class GuildCommandHelp
    {
        public static Dictionary<PCRGuildCommandType, string> HelpText = new Dictionary<PCRGuildCommandType, string>();

        public static void InitHelpText()
        {
            HelpText.Add(PCRGuildCommandType.CreateGuild, "#建会 [区域(cn/tw/jp)] [公会名]");
            HelpText.Add(PCRGuildCommandType.JoinGuild, "#入会 [@成员(或@自己)]");
        }
    }
}
