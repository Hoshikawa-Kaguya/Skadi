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
        #endregion

        #region 构造函数
        public GuildBattleManager(CQGroupMessageEventArgs GBattleEventArgs, PCRGuildCmdType commandType)
        {
            this.GBEventArgs   = GBattleEventArgs;
            this.QQGroup       = GBEventArgs.FromGroup;
            this.SenderQQ      = GBEventArgs.FromQQ;
            this.CommandType   = commandType;
            this.GuildBattleDB = new GuildBattleMgrDBHelper(GBEventArgs);
        }
        #endregion

        #region 指令分发
        public void GuildBattleResponse() //指令分发
        {
            if (GBEventArgs == null) throw new ArgumentNullException(nameof(GBEventArgs));

            string message = GBEventArgs.Message.Text.Trim();
            //index=0为命令本身，其余为参数
            string[] commandArgs = message.Split(' ');

            //查找是否存在这个公会
            if (!GuildBattleDB.GuildExists())
            {
                ConsoleLog.Debug("GuildExists", "guild not found");
                QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                         "\r\n此群未被登记为公会",
                                         "\r\n请使用以下指令创建公会",
                                         $"\r\n{PCRGuildHandle.GetCommandHelp(CommandType)}");
                return;
            }

            ConsoleLog.Info($"会战[群:{QQGroup.Id}]", $"开始处理指令{CommandType}");
            bool databaseSuccess;
            switch (CommandType)
            {
                case PCRGuildCmdType.BattleStart:
                    //检查执行者权限
                    if(!IsAdmin()) return;
                    
                    databaseSuccess = BattleStart();
                    break;
                case PCRGuildCmdType.BattleEnd:
                    //检查执行者权限
                    if(!IsAdmin()) return;

                    databaseSuccess = BattleEnd();
                    break;
                case PCRGuildCmdType.Attack:
                    databaseSuccess = Attack(commandArgs);
                    break;
                default:
                    PCRGuildHandle.GetUnknowCommand(GBEventArgs);
                    ConsoleLog.Warning($"会战[群:{QQGroup.Id}]", $"接到未知指令{CommandType}");
                    return;
            }
            if(!databaseSuccess) QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                                          "\r\nERROR",
                                                          "\r\n数据库错误");
        }
        #endregion

        #region 指令
        private bool BattleStart()
        {
            //判断返回值
            switch (GuildBattleDB.StartBattle())
            {
                case -1: //已经执行过开始命令
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                             "\r\n出刀统计已经开始了嗷",
                                             "\r\n此时会战已经开始或上一期仍未结束",
                                             "\r\n请检查是否未结束上期会战的出刀统计");
                    break;
                case 0:
                    QQGroup.SendGroupMessage(CQApi.CQCode_AtAll(),
                                             "\r\n新的一期会战开始啦！");
                    break;
                case -99:
                    return false;
            }
            return true;
        }

        private bool BattleEnd()
        {
            //TODO: EXCEL导出公会战数据
            //判断返回值
            switch (GuildBattleDB.StartBattle())
            {
                case -1: //已经执行过开始命令
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                             "\r\n出刀统计已经开始了嗷",
                                             "\r\n此时会战已经开始或上一期仍未结束",
                                             "\r\n请检查是否未结束上期会战的出刀统计");
                    break;
                case 0:
                    QQGroup.SendGroupMessage(CQApi.CQCode_AtAll(),
                                             "\r\n新的一期会战开始啦！");
                    break;
                default:
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 出刀
        /// </summary>
        /// <param name="CommandArgs">指令</param>
        /// <returns>
        /// <para><see langword="true"/> 数据写入成功</para>
        /// <para><see langword="false"/> 数据库错误</para>
        /// </returns>
        private bool Attack(string[] CommandArgs)
        {
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
            long atkUid;
            
            #region 处理传入参数
            switch (Utils.CheckForLength(CommandArgs,1))
            {
                case LenType.Illegal:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id), "\n兄啊你的伤害呢");
                    return true;
                case LenType.Legitimate: //正常出刀
                    //检查成员
                    if (!GuildBattleDB.CheckMemberExists(SenderQQ.Id))
                    {
                        QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id), "\n不是这个公会的还想打会战？");
                        return true;
                    }
                    atkUid = SenderQQ.Id;
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
                            if (!GuildBattleDB.CheckMemberExists(atkUid))
                            {
                                QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id), "\n此成员不是这个公会的成员");
                                return true;
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
                                         "\r\n兄啊你的伤害好jb怪啊");
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
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                             "请先申请出刀再重拳出击");
                    return true;
            }
            ConsoleLog.Debug("member flag check",$"DEBUG\r\nuser = {atkUid} | flag = {atkMemberInfo.Flag}");

            //获取会战进度信息
            GuildInfo atkGuildInfo = GuildBattleDB.GetGuildInfo(QQGroup.Id);
            if (atkGuildInfo == null) return false;
            ConsoleLog.Debug("guild info check",$"DEBUG\r\nguild = {atkGuildInfo.Gid} | flag = {atkMemberInfo.Flag}");

            #region 出刀类型判断
            //获取上一刀的信息
            long lastAttackUid = GuildBattleDB.GetLastAttack(out AttackType lastAttackType);
            if (lastAttackUid == -1) return false;
            //判断是否进入下一个boss
            bool needChangeBoss = dmg >= atkGuildInfo.HP;
            //出刀类型判断
            AttackType curAttackType;
            //判断顺序: 补时刀->尾刀->通常刀
            if (lastAttackType == AttackType.Final || lastAttackType == AttackType.FinalOutOfRange) //补时
            {
                if (atkUid == lastAttackUid)
                {
                    curAttackType = dmg >=  atkGuildInfo.HP
                        ? AttackType.Normal //当补时刀的伤害也超过了boss血量,判定为普通刀（你开挂！
                        : AttackType.Compensate;
                }
                else
                {
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                             "补时刀未出不允许出刀");
                    return true;
                }
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

            if (!GuildBattleDB.NewAttack(atkUid, atkGuildInfo, dmg, curAttackType)) return false;

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
            message.Append($"{latestGuildInfo.HP:N0}/{latestGuildInfo.TotalHP:N0}");
            switch (curAttackType)
            {
                case AttackType.FinalOutOfRange:
                case AttackType.Final:
                    message.Append("\r\n已被自动标记为尾刀\r\nboss已被锁定请等待补时刀");
                    break;
                case AttackType.Compensate:
                    message.Append("\r\n已被自动标记为补时刀");
                    break;
                case AttackType.Offline:
                    message.Append("\r\n已被自动标记为掉刀");
                    break;
            }
            QQGroup.SendGroupMessage(message);
            return true;
        }
        #endregion

        #region 私有方法
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