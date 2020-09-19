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
            PCRGuildCommands.Add("结束会战",PCRGuildCmdType.BattleEnd);
            PCRGuildCommands.Add("出刀",PCRGuildCmdType.Attack);
            PCRGuildCommands.Add("申请出刀",PCRGuildCmdType.RequestAttack);
            PCRGuildCommands.Add("取消申请",PCRGuildCmdType.UndoRequestAtk);
            PCRGuildCommands.Add("删刀",PCRGuildCmdType.DeleteAttack);
            PCRGuildCommands.Add("撤刀",PCRGuildCmdType.UndoAttack);
            PCRGuildCommands.Add("进度", PCRGuildCmdType.ShowProgress);
            PCRGuildCommands.Add("SL",PCRGuildCmdType.SL);
            PCRGuildCommands.Add("撤回SL",PCRGuildCmdType.UndoSL);
            PCRGuildCommands.Add("挂树",PCRGuildCmdType.ClimbTree);
            PCRGuildCommands.Add("下树",PCRGuildCmdType.LeaveTree);
            PCRGuildCommands.Add("查树",PCRGuildCmdType.ShowTree);
            PCRGuildCommands.Add("修改进度",PCRGuildCmdType.ModifyProgress);
            PCRGuildCommands.Add("查刀", PCRGuildCmdType.ShowRemainAttack);
            PCRGuildCommands.Add("催刀", PCRGuildCmdType.UrgeAttack);
            PCRGuildCommands.Add("出刀记录", PCRGuildCmdType.ShowAttackList);
        }
    }
}
