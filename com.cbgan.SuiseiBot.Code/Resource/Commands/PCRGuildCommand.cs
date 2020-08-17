using System.Collections.Generic;
using com.cbgan.SuiseiBot.Code.Resource.Enum;

namespace com.cbgan.SuiseiBot.Code.Resource.Commands
{
    internal static class PCRGuildCommand
    {
        public static Dictionary<string, PCRGuildCommandType> PCRGuildCommands = new Dictionary<string, PCRGuildCommandType>();
        /// <summary>
        /// 初始化公会相关的指令
        /// </summary>
        public static void PCRGuildCommandInit()
        {
            //公会管理模块的指令
            PCRGuildCommands.Add("建会", PCRGuildCommandType.CreateGuild);
            PCRGuildCommands.Add("入会", PCRGuildCommandType.JoinGuild);
            PCRGuildCommands.Add("查看成员", PCRGuildCommandType.ListMember);
            PCRGuildCommands.Add("退会", PCRGuildCommandType.QuitGuild);
            PCRGuildCommands.Add("清空成员", PCRGuildCommandType.QuitAll);
            PCRGuildCommands.Add("一键入会", PCRGuildCommandType.JoinAll);
            //出道模块的指令（在做了在做了
            PCRGuildCommands.Add("开始会战",PCRGuildCommandType.BattleStart);
            PCRGuildCommands.Add("更新BOSS",PCRGuildCommandType.UpdateBoss);
        }
    }
}
