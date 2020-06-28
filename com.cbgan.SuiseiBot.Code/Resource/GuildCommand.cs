using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace com.cbgan.SuiseiBot.Code.Resource
{
    internal class GuildCommand
    {
        public static Dictionary<string, PCRGuildCommandType> GuildCommands = new Dictionary<string, PCRGuildCommandType>();
        /// <summary>
        /// 初始化公会相关的指令
        /// </summary>
        public static void GuildCommandInit()
        {
            //公会管理模块的指令
            GuildCommands.Add("建会", PCRGuildCommandType.CreateGuild);
            GuildCommands.Add("入会", PCRGuildCommandType.JoinGuild);
            GuildCommands.Add("查看成员", PCRGuildCommandType.ListMember);
            GuildCommands.Add("退会", PCRGuildCommandType.QuitGuild);
            GuildCommands.Add("清空成员", PCRGuildCommandType.QuitAll);
            GuildCommands.Add("一键入会", PCRGuildCommandType.JoinAll);
            //出道模块的指令（在做了在做了
        }
    }
}
