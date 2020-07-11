using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.cbgan.SuiseiBot.Code.Resource
{
    internal static class CommandHelpText
    {
        public static Dictionary<PCRGuildCommandType, string> HelpText = new Dictionary<PCRGuildCommandType, string>();

        public static void InitHelpText()
        {
            HelpText.Add(PCRGuildCommandType.CreateGuild, "#建会 [区域(cn/tw/jp)] [公会名]");
            HelpText.Add(PCRGuildCommandType.JoinGuild, "#入会 [@成员(或@自己)]");
        }
    }
}
