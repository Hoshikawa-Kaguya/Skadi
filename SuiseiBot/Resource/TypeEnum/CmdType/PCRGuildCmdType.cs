namespace SuiseiBot.Code.Resource.TypeEnum.CmdType
{
    internal enum PCRGuildCmdType : int
    {
        /// <summary>
        /// 建会
        /// </summary>
        CreateGuild = 1,
        /// <summary>
        /// 入会
        /// </summary>
        JoinGuild = 2,
        /// <summary>
        /// 查看成员
        /// </summary>
        ListMember = 3,
        /// <summary>
        /// 退会
        /// </summary>
        QuitGuild = 4,
        /// <summary>
        /// 清空成员
        /// </summary>
        QuitAll = 5,
        /// <summary>
        /// 一键入会
        /// </summary>
        JoinAll = 6,
        /// <summary>
        /// 删除公会
        /// </summary>
        DeleteGuild = 7,
        /// <summary>
        /// 会战开始指令
        /// </summary>
        BattleStart = 101,
        /// <summary>
        /// 会战结束命令
        /// </summary>
        BattleEnd = 102,
        /// <summary>
        /// 申请出刀命令
        /// </summary>
        RequestAttack = 103,
        /// <summary>
        /// 出刀命令
        /// </summary>
        Attack = 104,
        /// <summary>
        /// 删刀命令
        /// </summary>
        DeleteAttack = 105,
        /// <summary>
        /// 撤销刀命令
        /// </summary>
        UndoAttack = 106,
        /// <summary>
        /// SL命令
        /// </summary>
        SL = 107,
        /// <summary>
        /// 撤回SL命令
        /// </summary>
        UndoSL = 108,
        /// <summary>
        /// 查看进度命令
        /// </summary>
        ShowProgress = 109,
        /// <summary>
        /// 挂树命令
        /// </summary>
        ClimbTree = 110,
        /// <summary>
        /// 查树命令
        /// </summary>
        ShowOnTree = 111,
        /// <summary>
        /// 下树命令
        /// </summary>
        KickFromTree = 112,
        /// <summary>
        /// 出刀表命令
        /// </summary>
        ShowAttackList = 113,
        /// <summary>
        /// 查余刀命令
        /// </summary>
        ShowRemainAttack = 114,
        /// <summary>
        /// 催刀命令
        /// </summary>
        UrgeAttack = 115,
        /// <summary>
        /// 取消出刀申请
        /// </summary>
        UndoRequestAtk = 116
    }
}
