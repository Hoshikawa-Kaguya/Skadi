using System.Collections.Generic;
using SuiseiBot.Code.Resource.TypeEnum.CmdType;

namespace SuiseiBot.Code.Resource.Commands
{
    internal static class PCRGuildCmd
    {
        public static Dictionary<string, PCRGuildCmdType> PCRGuildCommands = new Dictionary<string, PCRGuildCmdType>();
        /// <summary>
        /// 初始化公会相关的指令
        /// </summary>
        public static void PCRGuildCommandInit()
        {
            //公会管理模块的指令
            PCRGuildCommands.Add("建会", PCRGuildCmdType.CreateGuild);
            PCRGuildCommands.Add("删除公会",PCRGuildCmdType.DeleteGuild);
            PCRGuildCommands.Add("入会", PCRGuildCmdType.JoinGuild);
            PCRGuildCommands.Add("查看成员", PCRGuildCmdType.ListMember);
            PCRGuildCommands.Add("退会", PCRGuildCmdType.QuitGuild);
            PCRGuildCommands.Add("清空成员", PCRGuildCmdType.QuitAll);
            PCRGuildCommands.Add("一键入会", PCRGuildCmdType.JoinAll);
            //出刀模块的指令（在做了在做了
            PCRGuildCommands.Add("开始会战",PCRGuildCmdType.BattleStart);
            PCRGuildCommands.Add("更新BOSS",PCRGuildCmdType.UpdateBoss);
            PCRGuildCommands.Add("出刀",PCRGuildCmdType.Attack);
            PCRGuildCommands.Add("申请出刀",PCRGuildCmdType.RequestAttack);
            PCRGuildCommands.Add("取消出刀",PCRGuildCmdType.UndoRequestAtk);
        }
    }
}
