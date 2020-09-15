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
using System.Text;
using SuiseiBot.Code.DatabaseUtils;
using SuiseiBot.Code.DatabaseUtils.Helpers.PCRDBHelper;
using SuiseiBot.Code.Resource.TypeEnum;
using SuiseiBot.Code.Tool;

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
            bool dbSuccess;
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
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                             "\r\nERROR",
                                             "\r\n数据库错误");
                    return;
            }
            
            ConsoleLog.Info($"会战[群:{QQGroup.Id}]", $"开始处理指令{CommandType}");
            switch (CommandType)
            {
                case PCRGuildCmdType.BattleStart:
                    //检查执行者权限
                    if(!IsAdmin()) return;
                    dbSuccess = BattleStart();
                    break;

                case PCRGuildCmdType.BattleEnd:
                    //检查执行者权限
                    if(!IsAdmin()) return;
                    dbSuccess = BattleEnd();
                    break;

                case PCRGuildCmdType.Attack:
                    dbSuccess = Attack();
                    break;

                case PCRGuildCmdType.RequestAttack:
                    dbSuccess = RequestAttack();
                    break;

                case PCRGuildCmdType.UndoRequestAtk:
                    dbSuccess = UndoRequest();
                    break;

                case PCRGuildCmdType.DeleteAttack:
                    //检查执行者权限
                    if(!IsAdmin()) return;
                    dbSuccess = DelAttack();
                    break;

                case PCRGuildCmdType.UndoAttack:
                    dbSuccess = UndoAtk();
                    break;

                case PCRGuildCmdType.ShowProgress:
                    dbSuccess = ShowProgress();
                    break;

                default:
                    PCRGuildHandle.GetUnknowCommand(GBEventArgs);
                    ConsoleLog.Warning($"会战[群:{QQGroup.Id}]", $"接到未知指令{CommandType}");
                    return;
            }
            if(!dbSuccess) QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                                          "\r\nERROR",
                                                          "\r\n数据库错误");
        }
        #endregion

        #region 指令
        /// <summary>
        /// 开始会战
        /// </summary>
        /// <returns>
        /// <para><see langword="true"/> 数据写入成功</para>
        /// <para><see langword="false"/> 数据库错误</para>
        /// </returns>
        private bool BattleStart()
        {
            //检查成员
            if (!GuildBattleDB.CheckMemberExists(SenderQQ.Id,out bool database))
            {
                if(database)
                {
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id), "\n你不是这个公会的成员");
                    return true;
                }
                return false;
            }
            //判断返回值
            switch (GuildBattleDB.StartBattle())
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
                    return true;
                case -1:
                    return false;
            }
            return false;
        }

        /// <summary>
        /// 结束会战
        /// </summary>
        /// <returns>
        /// <para><see langword="true"/> 数据写入成功</para>
        /// <para><see langword="false"/> 数据库错误</para>
        /// </returns>
        private bool BattleEnd()
        {
            //TODO: EXCEL导出公会战数据
            //检查成员
            if (!GuildBattleDB.CheckMemberExists(SenderQQ.Id,out bool database))
            {
                if(database)
                {
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id), "\n你不是这个公会的成员");
                    return true;
                }
                return false;
            }
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
                    return true;
                case -1:
                    return false;
            }
            return false;
        }

        /// <summary>
        /// 申请出刀
        /// </summary>
        /// <returns>
        /// <para><see langword="true"/> 数据写入成功</para>
        /// <para><see langword="false"/> 数据库错误</para>
        /// </returns>
        private bool RequestAttack()
        {
            //检查是否进入会战
            switch (GuildBattleDB.CheckInBattle())
            {
                case 0:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id), "公会战还没开呢");
                    return true;
                case -1:
                    return false;
                case 1:
                    break;
                default:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id), "遇到了未知错误");
                    return true;
            }

            bool substitute;//代刀标记
            long atkUid;
            //指令检查
            switch (Utils.CheckForLength(CommandArgs,0))
            {
                case LenType.Legitimate:
                    //检查成员
                    if (!GuildBattleDB.CheckMemberExists(SenderQQ.Id,out bool database))
                    {
                        if(database)
                        {
                            QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id), "\n不是这个公会的还想打会战？");
                            return true;
                        }
                        return false;
                    }
                    atkUid     = SenderQQ.Id;
                    substitute = false;
                    break;
                case LenType.Extra://代刀
                    //检查是否有多余参数和AT
                    if (GBEventArgs.Message.CQCodes.Count       == 1             &&
                        GBEventArgs.Message.CQCodes[0].Function == CQFunction.At &&
                        Utils.CheckForLength(CommandArgs,1)     == LenType.Legitimate)
                    {
                        //从CQCode中获取QQ号
                        Dictionary<string,string> codeInfo =  GBEventArgs.Message.CQCodes[0].Items;
                        if (codeInfo.TryGetValue("qq",out string uid))
                        {
                            atkUid = Convert.ToInt64(uid);
                            //检查成员
                            if (!GuildBattleDB.CheckMemberExists(atkUid,out database))
                            {
                                if(database)
                                {
                                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id), "\n此成员不是这个公会的成员");
                                    return true;
                                }
                                return false;
                            }
                        }
                        else
                        {
                            ConsoleLog.Error("CQCode parse error","can't get uid in cqcode");
                            return true;
                        }
                    }
                    else
                    {
                        QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                                 "\r\n听不见！重来！（有多余参数）");
                        return true;
                    }
                    substitute = true;
                    break;
                default:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                             "发生未知错误，请联系机器人管理员");
                    ConsoleLog.Error("Unknown error","LenType");
                    return true;
            }

            //获取成员信息和上一次的出刀类型
            MemberInfo member    = GuildBattleDB.GetMemberInfo(atkUid);
            GuildInfo  guildInfo = GuildBattleDB.GetGuildInfo(QQGroup.Id);
            if (member                                                         == null) return false;
            if (GuildBattleDB.GetLastAttack(atkUid, out AttackType lastAttack) == -1) return false;

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
                    return true;
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
                    return true;
                default:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                             "发生未知错误，请联系机器人管理员");
                    ConsoleLog.Error("Unknown error","member.Flag");
                    return true;
            }

            int todayAtkCount = GuildBattleDB.GetTodayAttackCount(atkUid);
            ConsoleLog.Debug("atk count",todayAtkCount);
            if (todayAtkCount == -1) return false;
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
                return true;
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
                return true;
            }else return false;
        }

        /// <summary>
        /// 取消出刀申请
        /// </summary>
        /// <returns>
        /// <para><see langword="true"/> 数据写入成功</para>
        /// <para><see langword="false"/> 数据库错误</para>
        /// </returns>
        private bool UndoRequest()
        {
            //检查是否进入会战
            switch (GuildBattleDB.CheckInBattle())
            {
                case 0:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id), "公会战还没开呢");
                    return true;
                case -1:
                    return false;
                case 1:
                    break;
                default:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id), "遇到了未知错误");
                    return true;
            }

            bool substitute;//代刀标记
            long atkUid;
            //指令检查
            switch (Utils.CheckForLength(CommandArgs,0))
            {
                case LenType.Legitimate:
                    //检查成员
                    if (!GuildBattleDB.CheckMemberExists(SenderQQ.Id,out bool database))
                    {
                        if(database)
                        {
                            QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id), "\n不是这个公会的还想打会战？");
                            return true;
                        }
                        return false;
                    }
                    atkUid     = SenderQQ.Id;
                    substitute = false;
                    break;
                case LenType.Extra://代刀
                    //检查是否有多余参数和AT
                    if (GBEventArgs.Message.CQCodes.Count       == 1             &&
                        GBEventArgs.Message.CQCodes[0].Function == CQFunction.At &&
                        Utils.CheckForLength(CommandArgs,1)     == LenType.Legitimate)
                    {
                        //从CQCode中获取QQ号
                        Dictionary<string,string> codeInfo =  GBEventArgs.Message.CQCodes[0].Items;
                        if (codeInfo.TryGetValue("qq",out string uid))
                        {
                            atkUid = Convert.ToInt64(uid);
                            //检查成员
                            if (!GuildBattleDB.CheckMemberExists(atkUid,out database))
                            {
                                if(database)
                                {
                                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id), "\n此成员不是这个公会的成员");
                                    return true;
                                }
                                return false;
                            }
                        }
                        else
                        {
                            ConsoleLog.Error("CQCode parse error","can't get uid in cqcode");
                            return true;
                        }
                    }
                    else
                    {
                        QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                                 "\r\n听不见！重来！（有多余参数）");
                        return true;
                    }
                    substitute = true;
                    break;
                default:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                             "发生未知错误，请联系机器人管理员");
                    ConsoleLog.Error("Unknown error","LenType");
                    return true;
            }

            //获取成员信息
            MemberInfo member = GuildBattleDB.GetMemberInfo(atkUid);
            if (member == null) return false;
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
                    return true;
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
                    return true;
                case FlagType.EnGage:
                    if (GuildBattleDB.UpdateMemberStatus(atkUid, FlagType.IDLE, null))
                    {
                        QQGroup.SendGroupMessage("已取消出刀申请");
                        return true;
                    }
                    else return false;
                default: //如果跑到这了，我完蛋了
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                             "发生未知错误，请联系机器人管理员");
                    ConsoleLog.Error("Unknown error","member.Flag");
                    return true;
            }
        }

        /// <summary>
        /// 出刀
        /// </summary>
        /// <returns>
        /// <para><see langword="true"/> 数据写入成功</para>
        /// <para><see langword="false"/> 数据库错误</para>
        /// </returns>
        private bool Attack()
        {
            //检查是否进入会战
            switch (GuildBattleDB.CheckInBattle())
            {
                case 0:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id), "公会战还没开呢");
                    return true;
                case -1:
                    return false;
                case 1:
                    break;
                default:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id), "遇到了未知错误");
                    return true;
            }

            bool substitute; //代刀标记
            long atkUid;
            #region 处理传入参数
            switch (Utils.CheckForLength(CommandArgs,1))
            {
                case LenType.Illegal:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id), "\n兄啊伤害呢");
                    return true;
                case LenType.Legitimate: //正常出刀
                    //检查成员
                    if (!GuildBattleDB.CheckMemberExists(SenderQQ.Id,out bool database))
                    {
                        if(database)
                        {
                            QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id), "\n不是这个公会的还想打会战？");
                            return true;
                        }
                        return false;
                    }
                    atkUid     = SenderQQ.Id;
                    substitute = false;
                    break;
                case LenType.Extra: //代刀
                    //检查是否有多余参数和AT
                    if (GBEventArgs.Message.CQCodes.Count       == 1             &&
                        GBEventArgs.Message.CQCodes[0].Function == CQFunction.At &&
                        Utils.CheckForLength(CommandArgs,2)     == LenType.Legitimate)
                    {
                        //从CQCode中获取QQ号
                        Dictionary<string,string> codeInfo =  GBEventArgs.Message.CQCodes[0].Items;
                        if (codeInfo.TryGetValue("qq",out string uid))
                        {
                            atkUid = Convert.ToInt64(uid);
                            //检查成员
                            if (!GuildBattleDB.CheckMemberExists(atkUid,out database))
                            {
                                if(database)
                                {
                                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id), "\n此成员不是这个公会的成员");
                                    return true;
                                }
                                return false;
                            }
                        }
                        else
                        {
                            ConsoleLog.Error("CQCode parse error","can't get uid in cqcode");
                            return true;
                        }
                    }
                    else
                    {
                        QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                                 "\r\n听不见！重来！（有多余参数）");
                        return true;
                    }
                    substitute = true;
                    break;
                default:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                             "发生未知错误，请联系机器人管理员");
                    ConsoleLog.Error("Unknown error","LenType");
                    return true;
            }
            #endregion

            //处理参数得到伤害值并检查合法性
            if (!long.TryParse(CommandArgs[1], out long dmg) || dmg < 0) 
            {
                QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                         "\r\n兄啊这伤害好怪啊");
                return true;
            }
            ConsoleLog.Debug("Dmg info parse",$"DEBUG\r\ndmg = {dmg} | attack_user = {atkUid}");

            //获取成员状态信息
            MemberInfo atkMemberInfo = GuildBattleDB.GetMemberInfo(atkUid);
            if (atkMemberInfo == null) return false;
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
                    return true;
            }
            ConsoleLog.Debug("member flag check",$"DEBUG\r\nuser = {atkUid} | flag = {atkMemberInfo.Flag}");

            //获取会战进度信息
            GuildInfo atkGuildInfo = GuildBattleDB.GetGuildInfo(QQGroup.Id);
            if (atkGuildInfo == null) return false;
            ConsoleLog.Debug("guild info check",$"DEBUG\r\nguild = {atkGuildInfo.Gid} | flag = {atkMemberInfo.Flag}");

            #region 出刀类型判断
            //获取上一刀的信息
            if (GuildBattleDB.GetLastAttack(atkUid, out AttackType lastAttackType) == -1) return false;
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
            if (attackId == -1) return false;

            if (needChangeBoss) //进入下一个boss
            {
                //TODO 下树提示
                if (!GuildBattleDB.CleanTree(atkGuildInfo)) return false;
                if (atkGuildInfo.Order == 5) //进入下一个周目
                {
                    ConsoleLog.Debug("change boss","go to next round");
                    if (!GuildBattleDB.GotoNextRound(atkGuildInfo)) return false;
                }
                else //进入下一个Boss
                {
                    ConsoleLog.Debug("change boss","go to next boss");
                    if (!GuildBattleDB.GotoNextBoss(atkGuildInfo)) return false;
                }
            }
            else
            {
                //更新boss数据
                if (!GuildBattleDB.ModifyBossHP(atkGuildInfo, atkGuildInfo.HP - dmg)) return false;
            }

            //报刀后成员变为空闲
            if (!GuildBattleDB.UpdateMemberStatus(atkUid, FlagType.IDLE, null)) return false;

            //消息提示
            StringBuilder message = new StringBuilder();
            if (curAttackType == AttackType.FinalOutOfRange) message.Append("过度伤害！ 已自动修正boss血量\r\n");
            message.Append(CQApi.CQCode_At(atkUid));
            message.Append($"\r\n对{atkGuildInfo.Round}周目{atkGuildInfo.Order}王造成伤害\r\n");
            message.Append(dmg.ToString("N0"));
            message.Append("\r\n\r\n目前进度：");
            GuildInfo latestGuildInfo = GuildBattleDB.GetGuildInfo(QQGroup.Id);
            if (latestGuildInfo == null) return false;
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
            QQGroup.SendGroupMessage(message);
            return true;
        }

        /// <summary>
        /// 撤刀
        /// </summary>
        /// <returns>
        /// <para><see langword="true"/> 数据写入成功</para>
        /// <para><see langword="false"/> 数据库错误</para>
        /// </returns>
        private bool UndoAtk()
        {
            //检查成员
            if (!GuildBattleDB.CheckMemberExists(SenderQQ.Id,out bool database))
            {
                if(database)
                {
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id), "\n你不是这个公会的成员");
                    return true;
                }
                return false;
            }

            #region 参数检查
            switch (Utils.CheckForLength(CommandArgs,0))
            {
                case LenType.Legitimate: //正常
                    break;
                case LenType.Extra:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id), "\n有多余参数");
                    return true;
                default:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                             "发生未知错误，请联系机器人管理员");
                    ConsoleLog.Error("Unknown error","LenType");
                    return true;
            }
            #endregion

            //获取上一次的出刀类型
            int lastAtkAid = GuildBattleDB.GetLastAttack(SenderQQ.Id,out _);
            if (lastAtkAid == -1) return false;

            //删除记录
            switch (DelAtkByAid(lastAtkAid))
            {
                case 0:
                    return true;
                case 1:
                    break;
                default:
                    return false;
            }
            QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                     $"出刀编号为 {lastAtkAid} 的出刀记录已被删除");
            //显示进度
            if (!ShowProgress()) return false;

            return true;
        }

        /// <summary>
        /// 删刀
        /// </summary>
        /// <returns>
        /// <para><see langword="true"/> 数据写入成功</para>
        /// <para><see langword="false"/> 数据库错误</para>
        /// </returns>
        private bool DelAttack()
        {
            //检查成员
            if (!GuildBattleDB.CheckMemberExists(SenderQQ.Id,out bool database))
            {
                if(database)
                {
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id), "\n你不是这个公会的成员");
                    return true;
                }
                return false;
            }

            #region 参数检查
            switch (Utils.CheckForLength(CommandArgs,1))
            {
                case LenType.Illegal:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id), "\n兄啊刀号呢");
                    return true;
                case LenType.Legitimate: //正常
                    break;
                case LenType.Extra:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id), "\n有多余参数");
                    return true;
                default:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                             "发生未知错误，请联系机器人管理员");
                    ConsoleLog.Error("Unknown error","LenType");
                    return true;
            }

            //处理参数得到刀号并检查合法性
            if (!int.TryParse(CommandArgs[1], out int aid) || aid < 0) 
            {
                QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                         "\r\n兄啊这不是刀号");
                return true;
            }
            ConsoleLog.Debug("get aid", aid);
            #endregion

            //删除记录
            switch (DelAtkByAid(aid))
            {
                case 0:
                    return true;
                case 1:
                    break;
                default:
                    return false;
            }
            QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                     $"出刀编号为 {aid} 的出刀记录已被删除");
            //显示进度
            if (!ShowProgress()) return false;

            return true;
        }

        /// <summary>
        /// 显示会战进度
        /// </summary>
        /// <returns>
        /// <para><see langword="true"/> 数据查询成功</para>
        /// <para><see langword="false"/> 数据库错误</para>
        /// </returns>
        private bool ShowProgress()
        {
            GuildInfo guildInfo = GuildBattleDB.GetGuildInfo(QQGroup.Id);
            if (guildInfo == null) return false;

            StringBuilder message = new StringBuilder();
            message.Append($"{guildInfo.GuildName} 当前进度：\r\n");
            message.Append($"{guildInfo.Round}周目{guildInfo.Order}王\r\n");
            message.Append($"阶段{guildInfo.BossPhase}\r\n");
            message.Append($"剩余血量:{guildInfo.HP}/{guildInfo.TotalHP}");

            QQGroup.SendGroupMessage(message.ToString());
            return true;
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
            //删除出刀信息
            if (!GuildBattleDB.DelAtkByID(aid)) return -1;
            //更新boss数据
            return GuildBattleDB.ModifyBossHP(guildInfo, guildInfo.HP + atkInfo.Damage) ? 1 : -1;
        }

        /// <summary>
        /// 检查成员权限等级是否为管理员及以上
        /// </summary>
        private bool IsAdmin()
        {
            GroupMemberInfo memberInfo = GBEventArgs.CQApi.GetGroupMemberInfo(GBEventArgs.FromGroup.Id, GBEventArgs.FromQQ.Id);

            bool isAdmin = memberInfo.MemberType == QQGroupMemberType.Manage ||
                           memberInfo.MemberType == QQGroupMemberType.Creator;
            //非管理员执行的警告信息
            if (!isAdmin)
            {
                //执行者为普通群员时拒绝执行指令
                GBEventArgs.FromGroup.SendGroupMessage(CQApi.CQCode_At(GBEventArgs.FromQQ.Id),
                                                       "此指令只允许管理者执行");
                ConsoleLog.Warning($"会战[群:{GBEventArgs.FromGroup.Id}]", $"群成员{memberInfo.Nick}正在尝试执行指令{CommandType}");
            }

            return isAdmin;
        }
        #endregion
    }
}