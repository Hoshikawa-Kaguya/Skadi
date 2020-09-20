using Native.Sdk.Cqp;
using Native.Sdk.Cqp.Enum;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Model;
using SuiseiBot.Code.ChatHandle.PCRHandle;
using SuiseiBot.Code.Resource.TypeEnum.CmdType;
using SuiseiBot.Code.Resource.TypeEnum.GuildBattleType;
using SuiseiBot.Code.Tool.LogUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SuiseiBot.Code.DatabaseUtils;
using SuiseiBot.Code.DatabaseUtils.Helpers.PCRDBHelper;
using SuiseiBot.Code.Resource.TypeEnum;
using SuiseiBot.Code.Tool;

//TODO 加入指令的帮助文本

namespace SuiseiBot.Code.PCRGuildManager
{
    internal class GuildBattleManager
    {
        #region 属性
        private CQGroupMessageEventArgs GBEventArgs   { get; set; }
        private Group                   QQGroup       { get; set; }
        private QQ                      SenderQQ      { get; set; }
        private PCRGuildCmdType         CommandType   { get; set; }
        private GuildBattleMgrDBHelper  GuildBattleDB { get; set; }
        private string[]                CommandArgs   { get; set; }
        #endregion

        #region 构造函数
        public GuildBattleManager(CQGroupMessageEventArgs GBattleEventArgs, PCRGuildCmdType commandType)
        {
            this.GBEventArgs   = GBattleEventArgs;
            this.QQGroup       = GBEventArgs.FromGroup;
            this.SenderQQ      = GBEventArgs.FromQQ;
            this.CommandType   = commandType;
            this.GuildBattleDB = new GuildBattleMgrDBHelper(GBEventArgs);
            this.CommandArgs   = GBEventArgs.Message.Text.Trim().Split(' ');
        }
        #endregion

        #region 指令分发
        public void GuildBattleResponse() //指令分发
        {
            if (GBEventArgs == null) throw new ArgumentNullException(nameof(GBEventArgs));
            //查找是否存在这个公会
            switch (GuildBattleDB.GuildExists())
            {
                case 0:
                    ConsoleLog.Debug("GuildExists", "guild not found");
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                             "\r\n此群未被登记为公会",
                                             "\r\n请使用以下指令创建公会",
                                             $"\r\n{PCRGuildHandle.GetCommandHelp(CommandType)}");
                    return;
                case -1:
                    DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                    return;
            }
            
            ConsoleLog.Info($"会战[群:{QQGroup.Id}]", $"开始处理指令{CommandType}");

            switch (CommandType)
            {
                case PCRGuildCmdType.BattleStart:
                    //检查执行者权限和参数
                    if(!IsAdmin() || !ZeroArgsCheck() || !MemberCheck()) return;
                    BattleStart();
                    break;

                case PCRGuildCmdType.BattleEnd:
                    //检查执行者权限和参数
                    if(!IsAdmin() || !ZeroArgsCheck() || !MemberCheck()) return;
                    BattleEnd();
                    break;

                case PCRGuildCmdType.Attack:
                    if(!InBattleCheck() || !MemberCheck()) return;
                    Attack();
                    break;

                case PCRGuildCmdType.RequestAttack:
                    if(!InBattleCheck() || !MemberCheck()) return;
                    RequestAttack();
                    break;

                case PCRGuildCmdType.UndoRequestAtk:
                    if(!InBattleCheck() || !MemberCheck()) return;
                    UndoRequest();
                    break;

                case PCRGuildCmdType.DeleteAttack:
                    //检查执行者权限
                    if(!IsAdmin() || !MemberCheck() || !InBattleCheck()) return;
                    DelAttack();
                    break;

                case PCRGuildCmdType.UndoAttack:
                    if(!ZeroArgsCheck() || !MemberCheck() || !InBattleCheck()) return;
                    UndoAtk();
                    break;

                case PCRGuildCmdType.ShowProgress:
                    if(!ZeroArgsCheck()) return;
                    GuildInfo guildInfo = GuildBattleDB.GetGuildInfo(QQGroup.Id);
                    if (guildInfo == null)
                    {
                        DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                        break;
                    }
                    if (InBattleCheck())
                    {
                        ShowProgress(guildInfo);
                    }
                    break;

                case PCRGuildCmdType.SL:
                    if(!ZeroArgsCheck() || !MemberCheck() || !InBattleCheck()) return;
                    SL();
                    break;
                
                case PCRGuildCmdType.UndoSL:
                    //检查执行者权限
                    if(!IsAdmin() || !MemberCheck() || !InBattleCheck()) return;
                    SL(true);
                    break;

                case PCRGuildCmdType.ClimbTree:
                    if(!ZeroArgsCheck() || !MemberCheck() || !InBattleCheck()) return;
                    ClimbTree();
                    break;

                case PCRGuildCmdType.LeaveTree:
                    if(!IsAdmin() || !MemberCheck() || !InBattleCheck()) return;
                    LeaveTree();
                    break;

                case PCRGuildCmdType.ShowTree:
                    if (!ZeroArgsCheck() || !InBattleCheck()) return;
                    CheckTree();
                    break;

                case PCRGuildCmdType.ModifyProgress:
                    if(!IsAdmin() || !MemberCheck() || !InBattleCheck()) return;
                    ModifyProgress();
                    break;

                case PCRGuildCmdType.ShowRemainAttack:
                    if (!ZeroArgsCheck() || !MemberCheck() || !InBattleCheck()) return;
                    ShowRemainAttack();
                    break;

                case PCRGuildCmdType.UrgeAttack:
                    if (!IsAdmin() || !ZeroArgsCheck() || !MemberCheck() || !InBattleCheck()) return;
                    UrgeAttack();
                    break;

                case PCRGuildCmdType.ShowAttackList:
                    if (!IsAdmin() || !ZeroArgsCheck() || !MemberCheck() || !InBattleCheck()) return;
                    ShowAttackList();
                    break;

                default:
                    PCRGuildHandle.GetUnknowCommand(GBEventArgs);
                    ConsoleLog.Warning($"会战[群:{QQGroup.Id}]", $"接到未知指令{CommandType}");
                    break;
            }
        }
        #endregion

        #region 指令
        /// <summary>
        /// 开始会战
        /// </summary>
        private void BattleStart()
        {
            GuildInfo guildInfo = GuildBattleDB.GetGuildInfo(QQGroup.Id);
            if (guildInfo == null)
            {
                DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                return;
            }
            //判断返回值
            switch (GuildBattleDB.StartBattle(guildInfo))
            {
                case 0: //已经执行过开始命令
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                             "\r\n上一次的出刀统计未结束",
                                             "\r\n此时会战已经开始或上一期仍未结束",
                                             "\r\n请检查是否未结束上期会战的出刀统计");
                    break;
                case 1:
                    QQGroup.SendGroupMessage(CQApi.CQCode_AtAll(),
                                             "\r\n新的一期会战开始啦！");
                    break;
                case -1:
                    DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                    break;
            }
        }

        /// <summary>
        /// 结束会战
        /// </summary>
        private void BattleEnd()
        {
            //TODO: EXCEL导出公会战数据
            //判断返回值
            switch (GuildBattleDB.EndBattle())
            {
                case 0: //已经执行过开始命令
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                             "\r\n出刀统计并没有启动",
                                             "\r\n请检查是否未开始会战的出刀统计");
                    break;
                case 1:
                    QQGroup.SendGroupMessage(CQApi.CQCode_AtAll(),
                                             "\r\n会战结束啦~");
                    break;
                case -1:
                    DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                    break;
            }
        }

        /// <summary>
        /// 申请出刀
        /// </summary>
        private void RequestAttack()
        {
            bool substitute;//代刀标记
            long atkUid;
            //指令检查
            switch (Utils.CheckForLength(CommandArgs,0))
            {
                case LenType.Legitimate:
                    //检查成员
                    if (!MemberCheck()) return;
                    atkUid     = SenderQQ.Id;
                    substitute = false;
                    break;
                case LenType.Extra://代刀
                    //检查是否有多余参数和AT
                    if (Utils.CheckForLength(CommandArgs,1) == LenType.Legitimate)
                    {
                        //从CQCode中获取QQ号
                        atkUid = GetUidInMsg();
                        if (atkUid == -1) return;
                    }
                    else
                    {
                        QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                                 "\r\n听不见！重来！（有多余参数）");
                        return;
                    }
                    substitute = true;
                    break;
                default:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                             "发生未知错误，请联系机器人管理员");
                    ConsoleLog.Error("Unknown error","LenType");
                    return;
            }

            //获取成员信息和上一次的出刀类型
            MemberInfo member    = GuildBattleDB.GetMemberInfo(atkUid);
            GuildInfo  guildInfo = GuildBattleDB.GetGuildInfo(QQGroup.Id);
            if (member == null || GuildBattleDB.GetLastAttack(atkUid, out AttackType lastAttack) == -1)
            {
                DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                return;
            }

            ConsoleLog.Debug("member status",member.Flag);
            //检查成员状态
            switch (member.Flag)
            {
                //空闲可以出刀
                case FlagType.IDLE:
                    break;
                case FlagType.OnTree:
                    if (substitute)
                    {
                        QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                                 "\n兄啊",CQApi.CQCode_At(atkUid),"在树上啊");
                    }
                    else
                    {
                        QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                                 "\n好好爬你的树，你出个🔨的刀");
                    }
                    return;
                case FlagType.EnGage:
                    if (substitute)
                    {
                        QQGroup.SendGroupMessage("成员",CQApi.CQCode_At(atkUid),
                                                 "\n已经在出刀中");
                    }
                    else
                    {
                        QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                                 "\n你不是已经在出刀吗？");
                    }
                    return;
                default:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                             "发生未知错误，请联系机器人管理员");
                    ConsoleLog.Error("Unknown error","member.Flag");
                    return;
            }

            int todayAtkCount = GuildBattleDB.GetTodayAttackCount(atkUid);
            ConsoleLog.Debug("atk count",todayAtkCount);
            if (todayAtkCount == -1)
            {
                DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                return;
            }
            //检查今日出刀数量
            if (!(lastAttack == AttackType.Final || lastAttack == AttackType.FinalOutOfRange) && todayAtkCount >= 3) 
            {
                if (substitute)
                {
                    QQGroup.SendGroupMessage("成员",CQApi.CQCode_At(atkUid),
                                             "今日已出完三刀");
                }
                else
                {
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                             "今日已出完三刀");
                }
                return;
            }

            //修改成员状态
            if (GuildBattleDB.UpdateMemberStatus(atkUid, FlagType.EnGage, $"{guildInfo.Round}:{guildInfo.Order}")) 
            {
                if (substitute)
                {
                    QQGroup.SendGroupMessage("成员",CQApi.CQCode_At(atkUid),
                                             "开始出刀！");
                }
                else
                {
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                             "开始出刀！");
                }
            }
            else
            {
                DBMsgUtils.DatabaseFailedTips(GBEventArgs);
            }
        }

        /// <summary>
        /// 取消出刀申请
        /// </summary>
        private void UndoRequest()
        {
            bool substitute;//代刀标记
            long atkUid;
            //指令检查
            switch (Utils.CheckForLength(CommandArgs,0))
            {
                case LenType.Legitimate:
                    //检查成员
                    if (!MemberCheck()) return;
                    atkUid     = SenderQQ.Id;
                    substitute = false;
                    break;
                case LenType.Extra://代刀
                    //检查是否有多余参数和AT
                    if (Utils.CheckForLength(CommandArgs,1) == LenType.Legitimate)
                    {
                        //从CQCode中获取QQ号
                        atkUid = GetUidInMsg();
                        if (atkUid == -1) return;
                    }
                    else
                    {
                        QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                                 "\r\n听不见！重来！（有多余参数）");
                        return;
                    }
                    substitute = true;
                    break;
                default:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                             "发生未知错误，请联系机器人管理员");
                    ConsoleLog.Error("Unknown error","LenType");
                    return;
            }

            //获取成员信息
            MemberInfo member = GuildBattleDB.GetMemberInfo(atkUid);
            if (member == null)
            {
                DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                return;
            }
            ConsoleLog.Debug("member status",member.Flag);

            switch (member.Flag)
            {
                case FlagType.IDLE:
                    if (substitute)
                    {
                        QQGroup.SendGroupMessage("成员", CQApi.CQCode_At(atkUid)
                                               , "\n并未出刀");
                    }
                    else
                    {
                        QQGroup.SendGroupMessage(CQApi.CQCode_At(atkUid)
                                               , "\n并未申请出刀");
                    }
                    break;
                case FlagType.OnTree:
                    if (substitute)
                    {
                        QQGroup.SendGroupMessage("成员", CQApi.CQCode_At(atkUid),
                                                 "在树上挂着呢");
                    }
                    else
                    {
                        QQGroup.SendGroupMessage(CQApi.CQCode_At(atkUid),
                                                 "想下树？找管理员");
                    }
                    break;
                case FlagType.EnGage:
                    if (GuildBattleDB.UpdateMemberStatus(atkUid, FlagType.IDLE, null))
                    {
                        QQGroup.SendGroupMessage("已取消出刀申请");
                        break;
                    }
                    else
                    {
                        DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                        return;
                    }
                default: //如果跑到这了，我完蛋了
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                             "发生未知错误，请联系机器人管理员");
                    ConsoleLog.Error("Unknown error","member.Flag");
                    break;
            }
        }

        /// <summary>
        /// 出刀
        /// </summary>
        private void Attack()
        {
            bool substitute; //代刀标记
            long atkUid;

            #region 处理传入参数
            switch (Utils.CheckForLength(CommandArgs,1))
            {
                case LenType.Illegal:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id), "\n兄啊伤害呢");
                    return;
                case LenType.Legitimate: //正常出刀
                    //检查成员
                    if (!MemberCheck()) return;
                    atkUid     = SenderQQ.Id;
                    substitute = false;
                    break;
                case LenType.Extra: //代刀
                    //检查是否有多余参数和AT
                    if (Utils.CheckForLength(CommandArgs,2) == LenType.Legitimate)
                    {
                        //从CQCode中获取QQ号
                        atkUid = GetUidInMsg();
                        if (atkUid == -1) return;
                    }
                    else
                    {
                        QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                                 "\r\n听不见！重来！（有多余参数）");
                        return;
                    }
                    substitute = true;
                    break;
                default:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                             "发生未知错误，请联系机器人管理员");
                    ConsoleLog.Error("Unknown error","LenType");
                    return;
            }
            #endregion

            //处理参数得到伤害值并检查合法性
            if (!long.TryParse(CommandArgs[1], out long dmg) || dmg < 0) 
            {
                QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                         "\r\n兄啊这伤害好怪啊");
                return;
            }
            ConsoleLog.Debug("Dmg info parse",$"DEBUG\r\ndmg = {dmg} | attack_user = {atkUid}");

            #region 成员信息检查
            //获取成员状态信息
            MemberInfo atkMemberInfo = GuildBattleDB.GetMemberInfo(atkUid);
            if (atkMemberInfo == null)
            {
                DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                return;
            }
            //成员状态检查
            switch (atkMemberInfo.Flag)
            {
                //进入出刀判断
                case FlagType.EnGage:case FlagType.OnTree:
                    break;
                //当前并未开始出刀，请先申请出刀=>返回
                case FlagType.IDLE:
                    if (substitute)
                    {
                        QQGroup.SendGroupMessage("成员",CQApi.CQCode_At(atkUid),
                                                 "未申请出刀");
                    }
                    else
                    {
                        QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                                 "请先申请出刀再重拳出击");
                    }
                    return;
            }
            ConsoleLog.Debug("member flag check",$"DEBUG\r\nuser = {atkUid} | flag = {atkMemberInfo.Flag}");
            #endregion

            //获取会战进度信息
            GuildInfo atkGuildInfo = GuildBattleDB.GetGuildInfo(QQGroup.Id);
            if (atkGuildInfo == null)
            {
                DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                return;
            }
            ConsoleLog.Debug("guild info check",$"DEBUG\r\nguild = {atkGuildInfo.Gid} | flag = {atkMemberInfo.Flag}");

            #region 出刀类型判断
            //获取上一刀的信息
            if (GuildBattleDB.GetLastAttack(atkUid, out AttackType lastAttackType) == -1)
            {
                DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                return;
            }
            //判断是否进入下一个boss
            bool needChangeBoss = dmg >= atkGuildInfo.HP;
            //出刀类型判断
            AttackType curAttackType;
            //判断顺序: 补时刀->尾刀->通常刀
            if (lastAttackType == AttackType.Final || lastAttackType == AttackType.FinalOutOfRange) //补时
            {
                curAttackType = dmg >=  atkGuildInfo.HP
                    ? AttackType.CompensateKill //当补时刀的伤害也超过了boss血量,判定为普通刀
                    : AttackType.Compensate;
            }
            else
            {
                curAttackType = AttackType.Normal; //普通刀
                //尾刀判断
                if (dmg >= atkGuildInfo.HP)
                {
                    curAttackType = dmg > atkGuildInfo.HP ? AttackType.FinalOutOfRange : AttackType.Final;
                }
                //掉刀判断
                if (dmg == 0)
                    curAttackType = AttackType.Offline;
            }
            //伤害修正
            if(needChangeBoss) dmg = atkGuildInfo.HP;
            ConsoleLog.Debug("attack type",curAttackType);
            #endregion
            
            //向数据库插入新刀
            int attackId = GuildBattleDB.NewAttack(atkUid, atkGuildInfo, dmg, curAttackType);
            if (attackId == -1)
            {
                DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                return;
            }

            #region Boss状态修改
            if (needChangeBoss) //进入下一个boss
            {
                //TODO 下树提示
                if (!GuildBattleDB.CleanTree())
                {
                    DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                    return;
                }
                if (atkGuildInfo.Order == 5) //进入下一个周目
                {
                    ConsoleLog.Debug("change boss","go to next round");
                    if (!GuildBattleDB.GotoNextRound(atkGuildInfo))
                    {
                        DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                        return;
                    }
                }
                else //进入下一个Boss
                {
                    ConsoleLog.Debug("change boss","go to next boss");
                    if (!GuildBattleDB.GotoNextBoss(atkGuildInfo))
                    {
                        DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                        return;
                    }
                }
            }
            else
            {
                //更新boss数据
                if (!GuildBattleDB.ModifyBossHP(atkGuildInfo, atkGuildInfo.HP - dmg))
                {
                    DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                    return;
                }
            }
            #endregion

            //报刀后成员变为空闲
            if (!GuildBattleDB.UpdateMemberStatus(atkUid, FlagType.IDLE, null))
            {
                DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                return;
            }

            #region 消息提示

            StringBuilder message = new StringBuilder();
            if (curAttackType == AttackType.FinalOutOfRange) message.Append("过度伤害！ 已自动修正boss血量\r\n");
            message.Append(CQApi.CQCode_At(atkUid));
            message.Append($"\r\n对{atkGuildInfo.Round}周目{atkGuildInfo.Order}王造成伤害\r\n");
            message.Append(dmg.ToString("N0"));
            message.Append("\r\n\r\n目前进度：");
            GuildInfo latestGuildInfo = GuildBattleDB.GetGuildInfo(QQGroup.Id);
            if (latestGuildInfo == null)
            {
                DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                return;
            }
            message.Append($"{latestGuildInfo.Round}周目{latestGuildInfo.Order}王\r\n");
            message.Append($"{latestGuildInfo.HP:N0}/{latestGuildInfo.TotalHP:N0}\r\n");
            message.Append($"出刀编号：{attackId}");
            switch (curAttackType)
            {
                case AttackType.FinalOutOfRange:
                case AttackType.Final:
                    message.Append("\r\n已被自动标记为尾刀");
                    break;
                case AttackType.Compensate:
                    message.Append("\r\n已被自动标记为补时刀");
                    break;
                case AttackType.Offline:
                    message.Append("\r\n已被自动标记为掉刀");
                    break;
                case AttackType.CompensateKill:
                    message.Append("\r\n注意！你使用补时刀击杀了boss,没有时间补偿");
                    break;
            }
            if (atkMemberInfo.Flag == FlagType.OnTree) message.Append("\r\n已自动下树");
            QQGroup.SendGroupMessage(message);

            #endregion
        }

        /// <summary>
        /// 撤刀
        /// </summary>
        private void UndoAtk()
        {
            //获取上一次的出刀类型
            int lastAtkAid = GuildBattleDB.GetLastAttack(SenderQQ.Id,out _);
            switch (lastAtkAid)
            {
                case 0:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                             "并没有找到出刀记录");
                    return;
                case -1:
                    DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                    return;
            }

            //删除记录
            switch (DelAtkByAid(lastAtkAid))
            {
                case 0:
                    return;
                case 1:
                    break;
                default:
                    DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                    return;
            }
            QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                     $"出刀编号为 {lastAtkAid} 的出刀记录已被删除");
            //获取目前会战进度
            GuildInfo guildInfo = GuildBattleDB.GetGuildInfo(QQGroup.Id);
            if (guildInfo == null)
            {
                DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                return;
            }
            //显示进度
            ShowProgress(guildInfo);
        }

        /// <summary>
        /// 删刀
        /// 只允许管理员执行
        /// </summary>
        private void DelAttack()
        {
            #region 参数检查
            switch (Utils.CheckForLength(CommandArgs,1))
            {
                case LenType.Illegal:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id), "\n兄啊刀号呢");
                    return;
                case LenType.Legitimate: //正常
                    break;
                case LenType.Extra:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id), "\n有多余参数");
                    return;
                default:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                             "发生未知错误，请联系机器人管理员");
                    ConsoleLog.Error("Unknown error","LenType");
                    return;
            }

            //处理参数得到刀号并检查合法性
            if (!int.TryParse(CommandArgs[1], out int aid) || aid < 0) 
            {
                QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                         "\r\n兄啊这不是刀号");
                return;
            }
            ConsoleLog.Debug("get aid", aid);
            #endregion

            //删除记录
            switch (DelAtkByAid(aid))
            {
                case 0:
                    return;
                case 1:
                    break;
                default:
                    DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                    return;
            }
            QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                     $"出刀编号为 {aid} 的出刀记录已被删除");
            //获取目前会战进度
            GuildInfo guildInfo = GuildBattleDB.GetGuildInfo(QQGroup.Id);
            if (guildInfo == null)
            {
                DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                return;
            }
            //显示进度
            ShowProgress(guildInfo);
        }

        /// <summary>
        /// SL
        /// </summary>
        private void SL(bool cleanSL = false)
        {
            if (!cleanSL)//设置SL
            {
                //查找成员信息 
                MemberInfo member = GuildBattleDB.GetMemberInfo(SenderQQ.Id);
                if (member == null)
                {
                    DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                    return;
                }
                //判断成员状态
                if (member.Flag != FlagType.EnGage)
                {
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id), "并不在出刀中");
                    return;
                }
                //判断今天是否使用过SL
                if (member.SL >= Utils.GetUpdateStamp())
                {
                    QQGroup.SendGroupMessage("成员 ",CQApi.CQCode_At(SenderQQ.Id), "今天已使用过SL");
                }
                else
                {
                    if (!GuildBattleDB.SetMemberSL(SenderQQ.Id))
                    {
                        DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                        return;
                    }
                    QQGroup.SendGroupMessage("成员 ", CQApi.CQCode_At(SenderQQ.Id), "已使用SL");
                }
            }
            else//清空SL
            {
                //仅能管理员执行 需要额外参数
                //判断今天是否使用过SL
                #region 参数检查
                long memberUid;

                switch (Utils.CheckForLength(CommandArgs,0))
                {
                    case LenType.Legitimate: //正常
                        memberUid = SenderQQ.Id;
                        break;
                    case LenType.Extra://管理员撤销
                        memberUid = GetUidInMsg();
                        if (memberUid == -1) return;
                        break;
                    default:
                        QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                                 "发生未知错误，请联系机器人管理员");
                        ConsoleLog.Error("Unknown error","LenType");
                        return;
                }

                ConsoleLog.Debug("get Uid", memberUid);

                //查找成员信息 
                MemberInfo member = GuildBattleDB.GetMemberInfo(memberUid);
                if (member == null)
                {
                    DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                    return;
                }
                #endregion
                if (member.SL >= Utils.GetUpdateStamp())
                {
                    if (!GuildBattleDB.SetMemberSL(memberUid, true))
                    {
                        DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                        return;
                    }
                    QQGroup.SendGroupMessage("成员 ",CQApi.CQCode_At(memberUid), "已撤回今天的SL");
                }
                else
                {
                    QQGroup.SendGroupMessage("成员 ", CQApi.CQCode_At(memberUid), "今天未使用过SL");
                }
            } 
        }

        /// <summary>
        /// 上树
        /// </summary>
        private void ClimbTree()
        {
            //获取成员信息
            MemberInfo member = GuildBattleDB.GetMemberInfo(SenderQQ.Id);
            if (member == null)
            {
                DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                return;
            }

            switch (member.Flag)
            {
                case FlagType.EnGage:
                    if (!GuildBattleDB.UpdateMemberStatus(SenderQQ.Id, FlagType.OnTree, null))
                    {
                        DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                    }
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                             "已上树");
                    return;
                case FlagType.IDLE:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                             "闲着没事不要爬树(未申请出刀)");
                    return;
                case FlagType.OnTree:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                             "都在树上嫌树不够高？");
                    return;
                default:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                             "发生未知错误，请联系机器人管理员");
                    ConsoleLog.Error("Unknown error","member.Flag");
                    return;
            }
        }

        /// <summary>
        /// 下树
        /// </summary>
        private void LeaveTree()
        {
            #region 参数检查
            long memberUid;
            switch (Utils.CheckForLength(CommandArgs,0))
            {
                case LenType.Legitimate: //正常
                    memberUid = SenderQQ.Id;
                    break;
                case LenType.Extra: //管理员撤销
                    memberUid = GetUidInMsg();
                    if (memberUid == -1) return;
                    break;
                default:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                             "发生未知错误，请联系机器人管理员");
                    ConsoleLog.Error("Unknown error","LenType");
                    return;
            }

            ConsoleLog.Debug("get Uid", memberUid);

            //查找成员信息 
            MemberInfo member = GuildBattleDB.GetMemberInfo(memberUid);
            if (member == null)
            {
                DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                return;
            }
            #endregion

            switch (member.Flag)
            {
                case FlagType.EnGage:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(memberUid),
                                             "你 轴 歪 了\n(正在出刀不要乱用指令)");
                    return;
                case FlagType.IDLE:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(memberUid),
                                             "弟啊你不在树上");
                    return;
                case FlagType.OnTree:
                    if (!GuildBattleDB.UpdateMemberStatus(memberUid, FlagType.IDLE, null))
                    {
                        DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                    }
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(memberUid),
                                             "已下树");
                    return;
                default:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(memberUid),
                                             "发生未知错误，请联系机器人管理员");
                    ConsoleLog.Error("Unknown error","member.Flag");
                    return;
            }
        }

        /// <summary>
        /// 查树
        /// </summary>
        private void CheckTree()
        {
            List<long> treeList = GuildBattleDB.GetTree();
            if (treeList == null || treeList.Count == 0)
            {
                QQGroup.SendGroupMessage("没有人在树上");
                return;
            }
            //获取群成员列表
            List<GroupMemberInfo> groupMembers = GBEventArgs.FromGroup.GetGroupMemberList().ToList();
            //构造群消息文本
            StringBuilder message = new StringBuilder();
            message.Append("目前挂树的成员为:");
            treeList.Select(member => groupMembers
                                      .Where(groupMember => groupMember.QQ.Id == member)
                                      .Select(groupMember => groupMember.Card)
                                      .First())
                    .ToList()
                    //将成员名片添加进消息文本
                    .ForEach(name => message.Append($"\r\n{name}"));
            QQGroup.SendGroupMessage(message.ToString());
        }

        /// <summary>
        /// 显示会战进度
        /// </summary>
        private void ShowProgress(GuildInfo guildInfo)
        {
            StringBuilder message = new StringBuilder();
            message.Append($"{guildInfo.GuildName} 当前进度：\r\n");
            message.Append($"{guildInfo.Round}周目{guildInfo.Order}王\r\n");
            message.Append($"阶段{guildInfo.BossPhase}\r\n");
            message.Append($"剩余血量:{guildInfo.HP}/{guildInfo.TotalHP}");

            QQGroup.SendGroupMessage(message.ToString());
        }

        /// <summary>
        /// 修改进度
        /// </summary>
        private void ModifyProgress()
        {
            #region 处理传入参数
            //检查参数长度
            switch (Utils.CheckForLength(CommandArgs,3))
            {
                case LenType.Legitimate:
                    break;
                case LenType.Extra:case LenType.Illegal:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                             "非法指令格式");
                    return;
                default:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                             "发生未知错误，请联系机器人管理员");
                    ConsoleLog.Error("Unknown error","LenType");
                    return;
            }
            //处理参数值
            if (!int.TryParse(CommandArgs[1], out int targetRound) ||
                targetRound < 0                                    ||
                !int.TryParse(CommandArgs[2], out int targetOrder) ||
                targetOrder < 0                                    ||
                targetOrder > 5                                    ||
                !long.TryParse(CommandArgs[3], out long targetHp)  ||
                targetHp < 0)
            {
                QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                         "有非法参数");
                return;
            }
            //获取公会信息
            GuildInfo guildInfo = GuildBattleDB.GetGuildInfo(QQGroup.Id);
            if (guildInfo == null)
            {
                DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                return;
            }
            //从数据获取最大血量
            GuildBattleBoss bossInfo = GuildBattleDB.GetBossInfo(targetRound, targetOrder, guildInfo.ServerId);
            if(bossInfo == null)
            {
                DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                return;
            }
            if (targetHp >= bossInfo.HP)
            {
                QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                         "有非法参数");
                return;
            }
            #endregion

            if (!GuildBattleDB.ModifyProgress(targetRound, targetOrder, targetHp, bossInfo.HP, bossInfo.Phase))
            {
                DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                return;
            }
            QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                     "公会目前进度已修改为\r\n"                 +
                                     $"{targetRound}周目{targetOrder}王\r\n" +
                                     $"{targetHp}/{bossInfo.HP}");
        }

        /// <summary>
        /// 查刀
        /// </summary>
        private void ShowRemainAttack()
        {
            Dictionary<long, int> remainAttacksList = GuildBattleDB.CheckTodayAttacks();
            //首先检查是否记录为空
            if (remainAttacksList == null || remainAttacksList.Count == 0)
            {
                QQGroup.SendGroupMessage("今天已经出完刀啦~\r\n恭喜下班~");
                return;
            }
            //获取群成员列表
            List<GroupMemberInfo> groupMembers = GBEventArgs.FromGroup.GetGroupMemberList().ToList();
            //构造群消息文本
            StringBuilder message = new StringBuilder();
            message.Append("今日未出刀如下:");
            remainAttacksList.Select(couple => groupMembers
                                      .Where(groupMember => groupMember.QQ.Id == couple.Key)
                                      .Select(groupMember => new KeyValuePair<string,int>(groupMember.Card,3 - couple.Value))
                                      .First())
                    .ToList()
                    //将成员名片与对应刀数插入消息
                    .ForEach(record => message.Append($"\r\n{record.Key}：剩余{record.Value}刀"));
            QQGroup.SendGroupMessage(message.ToString());
        }

        /// <summary>
        /// 催刀
        /// 只允许管理员执行
        /// </summary>
        private void UrgeAttack()
        {
            Dictionary<long, int> remainAttacksList = GuildBattleDB.CheckTodayAttacks();
            //首先检查是否记录为空
            if (remainAttacksList == null || remainAttacksList.Count == 0)
            {
                QQGroup.SendGroupMessage("别催了别催了，孩子都出完刀了呜呜呜");
                return;
            }
            //获取群成员列表
            List<GroupMemberInfo> groupMembers = GBEventArgs.FromGroup.GetGroupMemberList().ToList();
            //构造群消息文本
            StringBuilder message = new StringBuilder();
            message.Append("还没出完刀的朋友萌：");
            remainAttacksList.Select(couple => groupMembers
                    .Where(groupMember => groupMember.QQ.Id == couple.Key)
                    .Select(groupMember => new KeyValuePair<long, int>(couple.Key, 3 - couple.Value))
                    .First())
                .ToList()
                //艾特成员并展示其剩余刀数
                .ForEach(record => message.Append($"\r\n{CQApi.CQCode_At(record.Key)}：剩余{record.Value}刀"));
            message.Append("\r\n快来出刀啦~");
            QQGroup.SendGroupMessage(message.ToString());
        }

        /// <summary>
        /// 查询出刀列表
        /// </summary>
        private void ShowAttackList()
        {
            List<GuildBattle> todayAttacksList = GuildBattleDB.GetTodayAttacks();
            //首先检查是否记录为空
            if (todayAttacksList == null || todayAttacksList.Count == 0)
            {
                QQGroup.SendGroupMessage("今天还没人出刀呢！");
                return;
            }
            //获取群成员列表
            List<GroupMemberInfo> groupMembers = GBEventArgs.FromGroup.GetGroupMemberList().ToList();
            //构造群消息文本
            StringBuilder message = new StringBuilder();
            message.Append("今日已出刀如下：\r\n");
            message.Append("刀号|出刀人|伤害目标|伤害");
            todayAttacksList.Select(record => groupMembers
                                      .Where(groupMember => groupMember.QQ.Id == record.Uid)
                                      .Select(groupMember => new KeyValuePair<string, GuildBattle>(groupMember.Card, record))
                                      .First())
                    .ToList()
                    //艾特成员并展示其剩余刀数
                    .ForEach(record => message.Append(
                                $"\r\n" +
                                $"{record.Value.Aid}|" +
                                $"{record.Key}|" +
                                $"{record.Value.Round}周目{record.Value.Order}王" +
                                $"{record.Value.Damage}"
                                )
                    );
            QQGroup.SendGroupMessage(message.ToString());
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 由刀号删除出刀信息
        /// </summary>
        /// <param name="aid">刀号</param>
        /// <returns>
        /// <para><see langword="1"/> 成功</para>
        /// <para><see langword="0"/> 不允许删除</para>
        /// <para><see langword="-1"/> 数据库错误</para>
        /// </returns>
        private int DelAtkByAid(int aid)
        {
            GuildInfo guildInfo = GuildBattleDB.GetGuildInfo(QQGroup.Id);
            if (guildInfo == null) return -1;
            GuildBattle atkInfo = GuildBattleDB.GetAtkByID(aid);

            //检查是否当前boss
            if (guildInfo.Round != atkInfo.Round || guildInfo.Order != atkInfo.Order)
            {
                QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                         "\r\n非当前所处boss不允许删除");
                return 0;
            }
            ConsoleLog.Debug("Del atk type",atkInfo.Attack);
            //检查是否为尾刀
            if (atkInfo.Attack == AttackType.Final || atkInfo.Attack == AttackType.FinalOutOfRange ||
                atkInfo.Attack == AttackType.CompensateKill) 
            {
                QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                         "\r\n尾刀不允许删除");
                return 0;
            }
            //判断数据是否非法
            if (guildInfo.HP + atkInfo.Damage > guildInfo.TotalHP)
            {
                QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                         "\r\n删刀后血量超出上线，请联系管理员检查机器人所在进度");
                return 0;
            }
            //删除出刀信息
            if (!GuildBattleDB.DelAtkByID(aid)) return -1;
            //更新boss数据
            return GuildBattleDB.ModifyBossHP(guildInfo, guildInfo.HP + atkInfo.Damage) ? 1 : -1;
        }

        /// <summary>
        /// 检查成员权限等级是否为管理员及以上
        /// </summary>
        /// <returns>
        /// <para><see langword="true"/> 成员为管理员或群主</para>
        /// <para><see langword="false"/> 成员不是管理员</para>
        /// </returns>
        private bool IsAdmin(bool shwoWarning = true)
        {
            GroupMemberInfo memberInfo = GBEventArgs.CQApi.GetGroupMemberInfo(GBEventArgs.FromGroup.Id, GBEventArgs.FromQQ.Id);

            bool isAdmin = memberInfo.MemberType == QQGroupMemberType.Manage ||
                           memberInfo.MemberType == QQGroupMemberType.Creator;
            //非管理员执行的警告信息
            if (!isAdmin)
            {
                //执行者为普通群员时拒绝执行指令
                if(shwoWarning)GBEventArgs.FromGroup.SendGroupMessage(CQApi.CQCode_At(GBEventArgs.FromQQ.Id),
                                                                      "此指令只允许管理者执行");
                ConsoleLog.Warning($"会战[群:{GBEventArgs.FromGroup.Id}]", $"群成员{memberInfo.Nick}正在尝试执行指令{CommandType}");
            }
            return isAdmin;
        }

        /// <summary>
        /// 检查是否已经进入会战
        /// </summary>
        /// <returns>
        /// <para><see langword="true"/> 已经进入会战</para>
        /// <para><see langword="false"/> 未进入或发生了其他错误</para>
        /// </returns>
        private bool InBattleCheck()
        {
            //检查是否进入会战
            switch (GuildBattleDB.CheckInBattle())
            {
                case 0:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id), "公会战还没开呢");
                    return false;
                case -1:
                    DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                    return false;
                case 1:
                    return true;
                default:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id), "遇到了未知错误");
                    return false;
            }
        }

        /// <summary>
        /// 零参数指令的参数检查
        /// 同时检查成员是否存在
        /// </summary>
        /// <returns>
        /// <para><see langword="true"/> 指令合法</para>
        /// <para><see langword="false"/> 有多余参数</para>
        /// </returns>
        private bool ZeroArgsCheck()
        {
            //检查参数
            switch (Utils.CheckForLength(CommandArgs,0))
            {
                case LenType.Extra:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                             "\r\n听不见！重来！（有多余参数）");
                    return false;
                case LenType.Legitimate:
                    return true;
                default:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                             "发生未知错误，请联系机器人管理员");
                    ConsoleLog.Error("Unknown error","LenType");
                    return false;
            }
        }

        /// <summary>
        /// 检查成员
        /// </summary>
        /// <returns>
        /// <para><see langword="true"/> 存在成员</para>
        /// <para><see langword="false"/> 不存在或有错误</para>
        /// </returns>
        private bool MemberCheck()
        {
            //检查成员
            switch (GuildBattleDB.CheckMemberExists(SenderQQ.Id))
            {
                case 1:
                    return true;
                case 0:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id), "不是这个公会的成员");
                    return false;
                case -1:
                    DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                    return false;
                default:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                             "发生未知错误，请联系机器人管理员");
                    ConsoleLog.Error("Unknown error","LenType");
                    return false;
            }
        }

        /// <summary>
        /// 根据UID来检查成员
        /// </summary>
        /// <param name="uid">成员UID</param>
        /// <returns>
        /// <para><see langword="true"/> 存在成员</para>
        /// <para><see langword="false"/> 不存在或有错误</para>
        /// </returns>
        private bool MemberCheck(long uid)
        {
            //检查成员
            switch (GuildBattleDB.CheckMemberExists(uid))
            {
                case 1:
                    return true;
                case 0:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(uid), "不是这个公会的成员");
                    return false;
                case -1:
                    DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                    return false;
                default:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(uid),
                                             "发生未知错误，请联系机器人管理员");
                    ConsoleLog.Error("Unknown error","LenType");
                    return false;
            }
        }

        /// <summary>
        /// 从消息的CQ码中获取用户ID（单CQ码）
        /// </summary>
        private long GetUidInMsg()
        {
            if (GBEventArgs.Message.CQCodes.Count       == 1 &&
                GBEventArgs.Message.CQCodes[0].Function == CQFunction.At)
            {
                //从CQCode中获取QQ号
                Dictionary<string, string> codeInfo = GBEventArgs.Message.CQCodes[0].Items;
                if (codeInfo.TryGetValue("qq", out string uid))
                {
                    long Uid = Convert.ToInt64(uid);
                    //检查成员
                    if (MemberCheck(Uid))
                    {
                        return Uid;
                    }
                    return -1;
                }
                else
                {
                    ConsoleLog.Error("CQCode parse error", "can't get uid in cqcode");
                }
            }
            QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ), "有非法参数");
            return -1;
        }
        #endregion
    }
}