using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AntiRain.ChatModule.PCRGuildBattle;
using AntiRain.DatabaseUtils;
using AntiRain.DatabaseUtils.Helpers.PCRGuildBattleDB;
using AntiRain.TypeEnum;
using AntiRain.TypeEnum.CommandType;
using AntiRain.TypeEnum.GuildBattleType;
using AntiRain.Tool;
using Sora;
using Sora.Entities.CQCodes;
using Sora.Entities.Info;
using Sora.Enumeration.ApiType;
using Sora.EventArgs.SoraEvent;
using YukariToolBox.FormatLog;

namespace AntiRain.ChatModule.PcrGuildBattle
{
    //TODO 等待重构
    internal class GuildBattleManager
    {
        #region 属性

        private GroupMessageEventArgs  eventArgs     { get; init; }
        private PCRGuildBattleCommand  CommandType   { get; set; }
        private GuildBattleMgrDBHelper GuildBattleDB { get; set; }
        
        #endregion

        #region 构造函数

        public GuildBattleManager(GroupMessageEventArgs GBattleEventArgs, PCRGuildBattleCommand commandType)
        {
            eventArgs          = GBattleEventArgs;
            CommandType        = commandType;
            this.GuildBattleDB = new GuildBattleMgrDBHelper(GBattleEventArgs.LoginUid);
        }

        #endregion

        #region 指令分发

        public async void GuildBattleResponse() //指令分发
        {
            if (eventArgs == null) throw new ArgumentNullException(nameof(eventArgs));
            //查找是否存在这个公会
            switch (GuildBattleDB.GuildExists(eventArgs.SourceGroup))
            {
                case 0:
                    Log.Debug("GuildExists", "guild not found");
                    await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id),
                                                       "\r\n此群未被登记为公会");
                    return;
                case -1:
                    await BotUtils.DatabaseFailedTips(eventArgs);
                    return;
            }

            Log.Info($"会战[群:{eventArgs.SourceGroup.Id}]", $"开始处理指令{CommandType}");

            switch (CommandType)
            {
                //会战开始
                case PCRGuildBattleCommand.BattleStart:
                    //检查执行者权限和参数
                    if (!await eventArgs.AuthCheck(CommandType.ToString()) || !await eventArgs.ZeroArgsCheck() || !await MemberCheck()) return;
                    BattleStart();
                    break;

                //会战结束
                case PCRGuildBattleCommand.BattleEnd:
                    //检查执行者权限和参数
                    if (!await eventArgs.AuthCheck(CommandType.ToString()) || !await eventArgs.ZeroArgsCheck() || !await MemberCheck()) return;
                    BattleEnd();
                    break;

                //出刀
                case PCRGuildBattleCommand.Attack:
                    if (!await InBattleCheck() || !await MemberCheck()) return;
                    Attack();
                    break;

                //出刀申请
                case PCRGuildBattleCommand.RequestAttack:
                    if (!await InBattleCheck() || !await MemberCheck()) return;
                    RequestAttack();
                    break;

                //撤刀
                case PCRGuildBattleCommand.UndoRequestAtk:
                    if (!await InBattleCheck() || !await MemberCheck()) return;
                    UndoRequest();
                    break;

                //删刀
                case PCRGuildBattleCommand.DeleteAttack:
                    //检查执行者权限
                    if (!await eventArgs.AuthCheck(CommandType.ToString()) || !await MemberCheck() || !await InBattleCheck()) return;
                    DelAttack();
                    break;

                //撤销出刀申请
                case PCRGuildBattleCommand.UndoAttack:
                    if (!await eventArgs.ZeroArgsCheck() || !await MemberCheck() || !await InBattleCheck()) return;
                    UndoAtk();
                    break;

                //查看进度
                case PCRGuildBattleCommand.ShowProgress:
                    if (!await eventArgs.ZeroArgsCheck()) return;
                    GuildInfo guildInfo = GuildBattleDB.GetGuildInfo(eventArgs.SourceGroup.Id);
                    if (guildInfo == null)
                    {
                        await BotUtils.DatabaseFailedTips(eventArgs);
                        break;
                    }

                    if (await InBattleCheck())
                    {
                        ShowProgress(guildInfo);
                    }

                    break;

                //SL
                case PCRGuildBattleCommand.SL:
                    if (!await eventArgs.ZeroArgsCheck() || !await MemberCheck() || !await InBattleCheck()) return;
                    SL();
                    break;

                //撤销SL
                case PCRGuildBattleCommand.UndoSL:
                    //检查执行者权限
                    if (!await eventArgs.AuthCheck(CommandType.ToString()) || !await MemberCheck() || !await InBattleCheck()) return;
                    SL(true);
                    break;

                //上树
                case PCRGuildBattleCommand.ClimbTree:
                    if (!await eventArgs.ZeroArgsCheck() || !await MemberCheck() || !await InBattleCheck()) return;
                    ClimbTree();
                    break;

                //下树
                case PCRGuildBattleCommand.LeaveTree:
                    if (!await eventArgs.AuthCheck(CommandType.ToString()) || !await MemberCheck() || !await InBattleCheck()) return;
                    LeaveTree();
                    break;

                //查树
                case PCRGuildBattleCommand.ShowTree:
                    if (!await eventArgs.ZeroArgsCheck() || !await InBattleCheck()) return;
                    CheckTree();
                    break;

                //修改进度
                case PCRGuildBattleCommand.ModifyProgress:
                    if (!await eventArgs.AuthCheck(CommandType.ToString()) || !await MemberCheck() || !await InBattleCheck()) return;
                    ModifyProgress();
                    break;

                //查余刀
                case PCRGuildBattleCommand.ShowRemainAttack:
                    if (!await eventArgs.ZeroArgsCheck() || !await MemberCheck() || !await InBattleCheck()) return;
                    ShowRemainAttack();
                    break;

                //催刀
                case PCRGuildBattleCommand.UrgeAttack:
                    if (!await eventArgs.AuthCheck(CommandType.ToString()) || !await eventArgs.ZeroArgsCheck() || !await MemberCheck() ||
                        !await InBattleCheck()) return;
                    UrgeAttack();
                    break;

                //显示完整出刀表
                case PCRGuildBattleCommand.ShowAllAttackList:
                    if (!await eventArgs.AuthCheck(CommandType.ToString()) || !await eventArgs.ZeroArgsCheck() || !await MemberCheck() ||
                        !await InBattleCheck()) return;
                    ShowAllAttackList();
                    break;

                //显示出刀表
                case PCRGuildBattleCommand.ShowAttackList:
                    if (!await MemberCheck() || !await InBattleCheck()) return;
                    ShowAttackList();
                    break;
                default:
                    Log.Warning($"会战[群:{eventArgs.SourceGroup.Id}]", $"接到未知指令{CommandType}");
                    break;
            }
        }

        #endregion

        #region 指令

        /// <summary>
        /// 开始会战
        /// </summary>
        private async void BattleStart()
        {
            GuildInfo guildInfo = GuildBattleDB.GetGuildInfo(eventArgs.SourceGroup.Id);
            if (guildInfo == null)
            {
                await BotUtils.DatabaseFailedTips(eventArgs);
                return;
            }

            //判断返回值
            switch (GuildBattleDB.StartBattle(guildInfo))
            {
                case 0: //已经执行过开始命令
                    await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id),
                                                       "\r\n上一次的出刀统计未结束",
                                                       "\r\n此时会战已经开始或上一期仍未结束",
                                                       "\r\n请检查是否未结束上期会战的出刀统计");
                    break;
                case 1:
                    await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAtAll(),
                                                       "\r\n新的一期会战开始啦！");
                    break;
                case -1:
                    await BotUtils.DatabaseFailedTips(eventArgs);
                    break;
            }
        }

        /// <summary>
        /// 结束会战
        /// </summary>
        private async void BattleEnd()
        {
            //判断返回值
            switch (GuildBattleDB.EndBattle(eventArgs.SourceGroup))
            {
                case 0: //已经执行过开始命令
                    await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id),
                                                       "\r\n出刀统计并没有启动",
                                                       "\r\n请检查是否未开始会战的出刀统计");
                    break;
                case 1:
                    GuildBattleDB.CleanTree(eventArgs.SourceGroup);
                    await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAtAll(),
                                                       "\r\n会战结束啦~");
                    break;
                case -1:
                    await BotUtils.DatabaseFailedTips(eventArgs);
                    break;
            }
        }

        /// <summary>
        /// 申请出刀
        /// </summary>
        private async void RequestAttack()
        {
            bool     substitute; //代刀标记
            long     atkUid;
            string[] commandArgs = eventArgs.ToCommandArgs();

            //指令检查
            switch (BotUtils.CheckForLength(commandArgs, 0))
            {
                case LenType.Legitimate:
                    //检查成员
                    if (!await MemberCheck()) return;
                    atkUid     = eventArgs.Sender.Id;
                    substitute = false;
                    break;
                case LenType.Extra: //代刀
                    //检查是否有多余参数和AT
                    if (BotUtils.CheckForLength(commandArgs, 1) == LenType.Legitimate)
                    {
                        //从CQCode中获取QQ号
                        atkUid = eventArgs.GetFirstUidInAt();
                        if (atkUid == -1) return;
                    }
                    else
                    {
                        await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id),
                                                           "\r\n听不见！重来！（有多余参数）");
                        return;
                    }

                    substitute = true;
                    break;
                default:
                    await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id),
                                                       "发生未知错误，请联系机器人管理员");
                    Log.Error("Unknown error", "LenType");
                    return;
            }

            //获取成员信息和上一次的出刀类型
            MemberInfo member    = GuildBattleDB.GetMemberInfo(atkUid, eventArgs.SourceGroup);
            GuildInfo  guildInfo = GuildBattleDB.GetGuildInfo(eventArgs.SourceGroup.Id);
            //数据库错误
            if (member == null || GuildBattleDB.GetLastAttack(atkUid, out AttackType lastAttack) == -1)
            {
                await BotUtils.DatabaseFailedTips(eventArgs);
                return;
            }

            Log.Debug("member status", member.Flag);
            //检查成员状态
            switch (member.Flag)
            {
                //空闲可以出刀
                case FlagType.IDLE:
                    break;
                case FlagType.OnTree:
                    if (substitute)
                    {
                        await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id),
                                                           "\n兄啊", CQCode.CQAt(atkUid), "在树上啊");
                    }
                    else
                    {
                        await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id),
                                                           "\n好好爬你的树，你出个🔨的刀");
                    }

                    return;
                case FlagType.EnGage:
                    if (substitute)
                    {
                        await eventArgs.SourceGroup.SendGroupMessage("成员", CQCode.CQAt(atkUid),
                                                           "\n已经在出刀中");
                    }
                    else
                    {
                        await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id),
                                                           "\n你不是已经在出刀吗？");
                    }

                    return;
                default:
                    await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id),
                                                       "发生未知错误，请联系机器人管理员");
                    Log.Error("Unknown error", "member.Flag");
                    return;
            }

            int todayAtkCount = GuildBattleDB.GetTodayAttackCount(atkUid);
            Log.Debug("atk count", todayAtkCount);
            if (todayAtkCount == -1)
            {
                await BotUtils.DatabaseFailedTips(eventArgs);
                return;
            }

            //检查今日出刀数量
            if (!(lastAttack == AttackType.Final || lastAttack == AttackType.FinalOutOfRange) && todayAtkCount >= 3)
            {
                if (substitute)
                {
                    await eventArgs.SourceGroup.SendGroupMessage("成员", CQCode.CQAt(atkUid),
                                                       "今日已出完三刀");
                }
                else
                {
                    await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id),
                                                       "今日已出完三刀");
                }

                return;
            }

            //修改成员状态
            if (GuildBattleDB.UpdateMemberStatus(atkUid, eventArgs.SourceGroup, FlagType.EnGage, $"{guildInfo.Round}:{guildInfo.Order}"))
            {
                List<long> atkMemberList = GuildBattleDB.GetInAtk(eventArgs.SourceGroup); //正在出刀中的成员列表
                if (atkMemberList == null)
                {
                    await BotUtils.DatabaseFailedTips(eventArgs);
                    return;
                }

                //发送消息段
                List<CQCode> msgToSend = new();

                if (substitute)
                {
                    msgToSend.Add(CQCode.CQText("成员"));
                    msgToSend.Add(CQCode.CQAt(atkUid));
                    msgToSend.Add(CQCode.CQText("开始出刀！"));
                    if (atkMemberList.Count != 0)
                    {
                        msgToSend.Add(CQCode.CQText($"\r\n当前正在出刀人数 {atkMemberList.Count}"));
                    }

                    await eventArgs.SourceGroup.SendGroupMessage(msgToSend);
                }
                else
                {
                    msgToSend.Add(CQCode.CQAt(atkUid));
                    msgToSend.Add(CQCode.CQText("开始出刀！"));
                    if (atkMemberList.Count != 0)
                    {
                        msgToSend.Add(CQCode.CQText($"\r\n当前正在出刀人数 {atkMemberList.Count}"));
                    }

                    await eventArgs.SourceGroup.SendGroupMessage(msgToSend);
                }
            }
            else
            {
                await BotUtils.DatabaseFailedTips(eventArgs);
            }
        }

        /// <summary>
        /// 取消出刀申请
        /// </summary>
        private async void UndoRequest()
        {
            bool     substitute; //代刀标记
            long     atkUid;
            string[] commandArgs = eventArgs.ToCommandArgs();

            //指令检查
            switch (BotUtils.CheckForLength(commandArgs, 0))
            {
                case LenType.Legitimate:
                    //检查成员
                    if (!await MemberCheck()) return;
                    atkUid     = eventArgs.Sender.Id;
                    substitute = false;
                    break;
                case LenType.Extra: //代刀
                    //检查是否有多余参数和AT
                    if (BotUtils.CheckForLength(commandArgs, 1) == LenType.Legitimate)
                    {
                        //从CQCode中获取QQ号
                        atkUid = eventArgs.GetFirstUidInAt();
                        if (atkUid == -1) return;
                    }
                    else
                    {
                        await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id),
                                                           "\r\n听不见！重来！（有多余参数）");
                        return;
                    }

                    substitute = true;
                    break;
                default:
                    await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id),
                                                       "发生未知错误，请联系机器人管理员");
                    Log.Error("Unknown error", "LenType");
                    return;
            }

            //获取成员信息
            MemberInfo member = GuildBattleDB.GetMemberInfo(atkUid, eventArgs.SourceGroup);
            if (member == null)
            {
                await BotUtils.DatabaseFailedTips(eventArgs);
                return;
            }

            Log.Debug("member status", member.Flag);

            switch (member.Flag)
            {
                case FlagType.IDLE:
                    if (substitute)
                    {
                        await eventArgs.SourceGroup.SendGroupMessage("成员", CQCode.CQAt(atkUid)
                                                         , "\n并未出刀");
                    }
                    else
                    {
                        await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(atkUid)
                                                         , "\n并未申请出刀");
                    }

                    break;
                case FlagType.OnTree:
                    if (substitute)
                    {
                        await eventArgs.SourceGroup.SendGroupMessage("成员", CQCode.CQAt(atkUid),
                                                           "在树上挂着呢");
                    }
                    else
                    {
                        await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(atkUid),
                                                           "想下树？找管理员");
                    }

                    break;
                case FlagType.EnGage:
                    if (GuildBattleDB.UpdateMemberStatus(atkUid, eventArgs.SourceGroup, FlagType.IDLE, null))
                    {
                        await eventArgs.SourceGroup.SendGroupMessage("已取消出刀申请");
                        break;
                    }
                    else
                    {
                        await BotUtils.DatabaseFailedTips(eventArgs);
                        return;
                    }
                default: //如果跑到这了，我完蛋了
                    await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id),
                                                       "发生未知错误，请联系机器人管理员");
                    Log.Error("Unknown error", "member.Flag");
                    break;
            }
        }

        /// <summary>
        /// 出刀
        /// </summary>
        private async void Attack()
        {
            bool     substitute; //代刀标记
            long     atkUid;
            string[] commandArgs = eventArgs.ToCommandArgs();

            #region 处理传入参数

            switch (BotUtils.CheckForLength(commandArgs, 1))
            {
                case LenType.Illegal:
                    await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id), "\n兄啊伤害呢");
                    return;
                case LenType.Legitimate: //正常出刀
                    //检查成员
                    if (!await MemberCheck()) return;
                    atkUid     = eventArgs.Sender.Id;
                    substitute = false;
                    break;
                case LenType.Extra: //代刀
                    //检查是否有多余参数和AT
                    if (BotUtils.CheckForLength(commandArgs, 2) == LenType.Legitimate)
                    {
                        //从CQCode中获取QQ号
                        atkUid = eventArgs.GetFirstUidInAt();
                        if (atkUid == -1) return;
                    }
                    else
                    {
                        await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id),
                                                           "\r\n听不见！重来！（有多余参数）");
                        return;
                    }

                    substitute = true;
                    break;
                default:
                    await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id),
                                                       "发生未知错误，请联系机器人管理员");
                    Log.Error("Unknown error", "LenType");
                    return;
            }

            #endregion

            //处理参数得到伤害值并检查合法性
            if (!long.TryParse(commandArgs[1], out long dmg) || dmg < 0)
            {
                await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id),
                                                   "\r\n兄啊这伤害好怪啊");
                return;
            }

            Log.Debug("Dmg info parse", $"DEBUG\r\ndmg = {dmg} | attack_user = {atkUid}");

            #region 成员信息检查

            //获取成员状态信息
            MemberInfo atkMemberInfo = GuildBattleDB.GetMemberInfo(atkUid, eventArgs.SourceGroup);
            if (atkMemberInfo == null)
            {
                await BotUtils.DatabaseFailedTips(eventArgs);
                return;
            }

            //成员状态检查
            switch (atkMemberInfo.Flag)
            {
                //进入出刀判断
                case FlagType.EnGage:
                case FlagType.OnTree:
                    break;
                //当前并未开始出刀，请先申请出刀=>返回
                case FlagType.IDLE:
                    if (substitute)
                    {
                        await eventArgs.SourceGroup.SendGroupMessage("成员", CQCode.CQAt(atkUid),
                                                           "未申请出刀");
                    }
                    else
                    {
                        await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id),
                                                           "请先申请出刀再重拳出击");
                    }

                    return;
            }

            Log.Debug("member flag check", $"DEBUG\r\nuser = {atkUid} | flag = {atkMemberInfo.Flag}");

            #endregion

            //获取会战进度信息
            GuildInfo atkGuildInfo = GuildBattleDB.GetGuildInfo(eventArgs.SourceGroup.Id);
            if (atkGuildInfo == null)
            {
                await BotUtils.DatabaseFailedTips(eventArgs);
                return;
            }

            Log.Debug("guild info check", $"DEBUG\r\nguild = {atkGuildInfo.Gid} | flag = {atkMemberInfo.Flag}");

            #region 出刀类型判断

            //获取上一刀的信息
            if (GuildBattleDB.GetLastAttack(atkUid, out AttackType lastAttackType) == -1)
            {
                await BotUtils.DatabaseFailedTips(eventArgs);
                return;
            }

            //判断是否进入下一个boss
            bool needChangeBoss = dmg >= atkGuildInfo.HP;
            //出刀类型判断
            AttackType curAttackType;
            //判断顺序: 补时刀->尾刀->通常刀
            if (lastAttackType == AttackType.Final || lastAttackType == AttackType.FinalOutOfRange) //补时
            {
                curAttackType = dmg >= atkGuildInfo.HP
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
            if (needChangeBoss) dmg = atkGuildInfo.HP;
            Log.Debug("attack type", curAttackType);

            #endregion

            //向数据库插入新刀
            int attackId = GuildBattleDB.NewAttack(atkUid, atkGuildInfo, dmg, curAttackType);
            if (attackId == -1)
            {
                await BotUtils.DatabaseFailedTips(eventArgs);
                return;
            }

            #region Boss状态修改

            if (needChangeBoss) //进入下一个boss
            {
                //获取需要修改的成员列表
                List<long> treeList      = GuildBattleDB.GetTree(eventArgs.SourceGroup);
                List<long> atkMemberList = GuildBattleDB.GetInAtk(eventArgs.SourceGroup);
                if (treeList == null || atkMemberList == null)
                {
                    await BotUtils.DatabaseFailedTips(eventArgs);
                    return;
                }

                #region 下树检查

                //下树提示
                if (treeList.Count != 0)
                {
                    if (!GuildBattleDB.CleanTree(eventArgs.SourceGroup))
                    {
                        await BotUtils.DatabaseFailedTips(eventArgs);
                        return;
                    }

                    List<CQCode> treeTips = new();
                    treeTips.AddText("以下成员已下树:\r\n");
                    //添加AtCQCode
                    treeTips.AddRange(treeList.Select(CQCode.CQAt));

                    //发送下树提示
                    await eventArgs.SourceGroup.SendGroupMessage(treeTips);
                }

                #endregion

                #region 成员状态重置

                if (atkMemberList.Count != 0)
                {
                    if (!GuildBattleDB.CleanAtkStatus(eventArgs.SourceGroup))
                    {
                        await BotUtils.DatabaseFailedTips(eventArgs);
                        return;
                    }
                }

                #endregion

                #region 周目交换检查

                if (atkGuildInfo.Order == 5) //进入下一个周目
                {
                    Log.Debug("change boss", "go to next round");
                    if (!GuildBattleDB.GotoNextRound(atkGuildInfo))
                    {
                        await BotUtils.DatabaseFailedTips(eventArgs);
                        return;
                    }
                }
                else //进入下一个Boss
                {
                    Log.Debug("change boss", "go to next boss");
                    if (!GuildBattleDB.GotoNextBoss(atkGuildInfo))
                    {
                        await BotUtils.DatabaseFailedTips(eventArgs);
                        return;
                    }
                }

                #endregion
            }
            else
            {
                //更新boss数据
                if (!GuildBattleDB.ModifyBossHP(atkGuildInfo, atkGuildInfo.HP - dmg))
                {
                    await BotUtils.DatabaseFailedTips(eventArgs);
                    return;
                }
            }

            #endregion

            //报刀后成员变为空闲
            if (!GuildBattleDB.UpdateMemberStatus(atkUid, eventArgs.SourceGroup, FlagType.IDLE, null))
            {
                await BotUtils.DatabaseFailedTips(eventArgs);
                return;
            }

            int memberCount = GuildBattleDB.GetMemberCount(eventArgs.SourceGroup);
            if (memberCount == -1)
            {
                await BotUtils.DatabaseFailedTips(eventArgs);
                return;
            }

            //获取剩余刀数
            int remainCount = memberCount*3 - GuildBattleDB.GetTodayAttackCount();

            #region 消息提示

            List<CQCode> message = new();
            if (curAttackType == AttackType.FinalOutOfRange) message.Add(CQCode.CQText("过度伤害！ 已自动修正boss血量\r\n"));
            message.Add(CQCode.CQAt(atkUid));
            message.Add(CQCode.CQText($"\r\n对{atkGuildInfo.Round}周目{atkGuildInfo.Order}王造成伤害\r\n"));
            message.Add(CQCode.CQText(dmg.ToString("N0")));
            message.Add(CQCode.CQText("\r\n\r \n目前进度："));
            GuildInfo latestGuildInfo = GuildBattleDB.GetGuildInfo(eventArgs.SourceGroup.Id);
            if (latestGuildInfo == null)
            {
                await BotUtils.DatabaseFailedTips(eventArgs);
                return;
            }

            message.Add(CQCode.CQText($"{latestGuildInfo.Round}周目{latestGuildInfo.Order}王\r\n"));
            message.Add(CQCode.CQText($"{latestGuildInfo.HP:N0}/{latestGuildInfo.TotalHP:N0}\r\n"));
            message.Add(CQCode.CQText($"出刀编号：{attackId}"));
            switch (curAttackType)
            {
                case AttackType.FinalOutOfRange:
                case AttackType.Final:
                    message.Add(CQCode.CQText("\r\n已被自动标记为尾刀"));
                    break;
                case AttackType.Compensate:
                    message.Add(CQCode.CQText("\r\n已被自动标记为补时刀"));
                    break;
                case AttackType.Offline:
                    message.Add(CQCode.CQText("\r\n已被自动标记为掉刀"));
                    break;
                case AttackType.CompensateKill:
                    message.Add(CQCode.CQText("\r\n注意！你使用补时刀击杀了boss,没有时间补偿"));
                    break;
            }

            //下树检查
            if (atkMemberInfo.Flag == FlagType.OnTree)
            {
                message.Add(CQCode.CQText("\r\n已自动下树"));
                TreeTipManager.DelTreeMember(eventArgs.Sender);
            }

            message.Add(CQCode.CQText($"\r\n今日总余刀数量:{remainCount}"));
            await eventArgs.SourceGroup.SendGroupMessage(message);

            #endregion
        }

        /// <summary>
        /// 撤刀
        /// </summary>
        private async void UndoAtk()
        {
            //获取上一次的出刀类型
            int lastAtkAid = GuildBattleDB.GetLastAttack(eventArgs.Sender.Id, out _);
            switch (lastAtkAid)
            {
                case 0:
                    await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id),
                                                       "并没有找到出刀记录");
                    return;
                case -1:
                    await BotUtils.DatabaseFailedTips(eventArgs);
                    return;
            }

            //删除记录
            switch (await DelAtkByAid(lastAtkAid))
            {
                case 0:
                    return;
                case 1:
                    break;
                default:
                    await BotUtils.DatabaseFailedTips(eventArgs);
                    return;
            }

            await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id),
                                               $"出刀编号为 {lastAtkAid} 的出刀记录已被删除");
            //获取目前会战进度
            GuildInfo guildInfo = GuildBattleDB.GetGuildInfo(eventArgs.SourceGroup.Id);
            if (guildInfo == null)
            {
                await BotUtils.DatabaseFailedTips(eventArgs);
                return;
            }

            //显示进度
            ShowProgress(guildInfo);
        }

        /// <summary>
        /// 删刀
        /// 只允许管理员执行
        /// </summary>
        private async void DelAttack()
        {
            string[] commandArgs = eventArgs.ToCommandArgs();

            #region 参数检查

            switch (BotUtils.CheckForLength(commandArgs, 1))
            {
                case LenType.Illegal:
                    await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id), "\n兄啊刀号呢");
                    return;
                case LenType.Legitimate: //正常
                    break;
                case LenType.Extra:
                    await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id), "\n有多余参数");
                    return;
                default:
                    await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id),
                                                       "发生未知错误，请联系机器人管理员");
                    Log.Error("Unknown error", "LenType");
                    return;
            }

            //处理参数得到刀号并检查合法性
            if (!int.TryParse(commandArgs[1], out int aid) || aid < 0)
            {
                await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id),
                                                   "\r\n兄啊这不是刀号");
                return;
            }

            Log.Debug("get aid", aid);

            #endregion

            //删除记录
            switch (await DelAtkByAid(aid))
            {
                case 0:
                    return;
                case 1:
                    break;
                default:
                    await BotUtils.DatabaseFailedTips(eventArgs);
                    return;
            }

            await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id),
                                               $"出刀编号为 {aid} 的出刀记录已被删除");
            //获取目前会战进度
            GuildInfo guildInfo = GuildBattleDB.GetGuildInfo(eventArgs.SourceGroup.Id);
            if (guildInfo == null)
            {
                await BotUtils.DatabaseFailedTips(eventArgs);
                return;
            }

            //显示进度
            ShowProgress(guildInfo);
        }

        /// <summary>
        /// SL
        /// </summary>
        private async void SL(bool cleanSL = false)
        {
            string[] commandArgs = eventArgs.ToCommandArgs();

            if (!cleanSL) //设置SL
            {
                //查找成员信息 
                MemberInfo member = GuildBattleDB.GetMemberInfo(eventArgs.Sender.Id, eventArgs.SourceGroup);
                if (member == null)
                {
                    await BotUtils.DatabaseFailedTips(eventArgs);
                    return;
                }

                //判断成员状态
                if (member.Flag != FlagType.EnGage)
                {
                    await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id), "并不在出刀中");
                    return;
                }

                //判断今天是否使用过SL
                if (member.SL >= BotUtils.GetUpdateStamp())
                {
                    await eventArgs.SourceGroup.SendGroupMessage("成员 ", CQCode.CQAt(eventArgs.Sender.Id), "今天已使用过SL");
                }
                else
                {
                    if (!GuildBattleDB.SetMemberSL(eventArgs.Sender.Id, eventArgs.SourceGroup))
                    {
                        await BotUtils.DatabaseFailedTips(eventArgs);
                        return;
                    }

                    await eventArgs.SourceGroup.SendGroupMessage("成员 ", CQCode.CQAt(eventArgs.Sender.Id), "已使用SL");
                }
            }
            else //清空SL
            {
                //仅能管理员执行 需要额外参数
                //判断今天是否使用过SL

                #region 参数检查

                long memberUid;

                switch (BotUtils.CheckForLength(commandArgs, 0))
                {
                    case LenType.Legitimate: //正常
                        memberUid = eventArgs.Sender.Id;
                        break;
                    case LenType.Extra: //管理员撤销
                        memberUid = eventArgs.GetFirstUidInAt();
                        if (memberUid == -1) return;
                        break;
                    default:
                        await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id),
                                                           "发生未知错误，请联系机器人管理员");
                        Log.Error("Unknown error", "LenType");
                        return;
                }

                Log.Debug("get Uid", memberUid);

                //查找成员信息 
                MemberInfo member = GuildBattleDB.GetMemberInfo(memberUid, eventArgs.SourceGroup);
                if (member == null)
                {
                    await BotUtils.DatabaseFailedTips(eventArgs);
                    return;
                }

                #endregion

                if (member.SL >= BotUtils.GetUpdateStamp())
                {
                    if (!GuildBattleDB.SetMemberSL(memberUid, eventArgs.SourceGroup, true))
                    {
                        await BotUtils.DatabaseFailedTips(eventArgs);
                        return;
                    }

                    await eventArgs.SourceGroup.SendGroupMessage("成员 ", CQCode.CQAt(memberUid), "已撤回今天的SL");
                }
                else
                {
                    await eventArgs.SourceGroup.SendGroupMessage("成员 ", CQCode.CQAt(memberUid), "今天未使用过SL");
                }
            }
        }

        /// <summary>
        /// 上树
        /// </summary>
        private async void ClimbTree()
        {
            //获取成员信息
            MemberInfo member = GuildBattleDB.GetMemberInfo(eventArgs.Sender.Id, eventArgs.SourceGroup);
            if (member == null)
            {
                await BotUtils.DatabaseFailedTips(eventArgs);
                return;
            }

            switch (member.Flag)
            {
                case FlagType.EnGage:
                    if (!GuildBattleDB.UpdateMemberStatus(eventArgs.Sender.Id, eventArgs.SourceGroup, FlagType.OnTree, null))
                    {
                        await BotUtils.DatabaseFailedTips(eventArgs);
                    }

                    await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id),
                                                       "已上树");
                    //添加上树提示
                    TreeTipManager.AddTreeMember(eventArgs.SourceGroup, eventArgs.Sender, DateTime.Now);
                    return;
                case FlagType.IDLE:
                    await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id),
                                                       "闲着没事不要爬树(未申请出刀)");
                    return;
                case FlagType.OnTree:
                    await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id),
                                                       "都在树上嫌树不够高？");
                    return;
                default:
                    await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id),
                                                       "发生未知错误，请联系机器人管理员");
                    Log.Error("Unknown error", "member.Flag");
                    return;
            }
        }

        /// <summary>
        /// 下树
        /// </summary>
        private async void LeaveTree()
        {
            string[] commandArgs = eventArgs.ToCommandArgs();
            #region 参数检查

            long memberUid;
            switch (BotUtils.CheckForLength(commandArgs, 1))
            {
                case LenType.Legitimate: //正常
                    memberUid = eventArgs.GetFirstUidInAt();
                    if (memberUid == -1) return;
                    break;
                case LenType.Extra:
                    await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id),
                                                       "有多余参数");
                    return;
                case LenType.Illegal:
                    if (!eventArgs.IsAdminSession()) return;
                    memberUid = eventArgs.Sender.Id;
                    break;
                default:
                    await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id),
                                                       "发生未知错误，请联系机器人管理员");
                    Log.Error("Unknown error", "LenType");
                    return;
            }

            Log.Debug("get Uid", memberUid);

            //查找成员信息 
            MemberInfo member = GuildBattleDB.GetMemberInfo(memberUid, eventArgs.SourceGroup);
            if (member == null)
            {
                await BotUtils.DatabaseFailedTips(eventArgs);
                return;
            }

            #endregion

            TreeTipManager.DelTreeMember(eventArgs.Sender);

            switch (member.Flag)
            {
                case FlagType.EnGage:
                    await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(memberUid),
                                                       "你 轴 歪 了\n(正在出刀不要乱用指令)");
                    return;
                case FlagType.IDLE:
                    await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(memberUid),
                                                       "弟啊你不在树上");
                    return;
                case FlagType.OnTree:
                    if (!GuildBattleDB.UpdateMemberStatus(memberUid, eventArgs.SourceGroup, FlagType.IDLE, null))
                    {
                        await BotUtils.DatabaseFailedTips(eventArgs);
                    }

                    await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(memberUid),
                                                       "已下树");
                    return;
                default:
                    await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(memberUid),
                                                       "发生未知错误，请联系机器人管理员");
                    Log.Error("Unknown error", "member.Flag");
                    return;
            }
        }

        /// <summary>
        /// 查树
        /// </summary>
        private async void CheckTree()
        {
            List<long> treeList = GuildBattleDB.GetTree(eventArgs.SourceGroup);
            if (treeList == null || treeList.Count == 0)
            {
                await eventArgs.SourceGroup.SendGroupMessage("没有人在树上");
                return;
            }

            //获取群成员列表
            (APIStatusType apiStatus, List<GroupMemberInfo> groupMembers) = await eventArgs.SourceGroup.GetGroupMemberList();
            if (apiStatus != APIStatusType.OK)
            {
                Log.Error("API Error", $"API ret error {apiStatus}");
                return;
            }

            //构造群消息文本
            StringBuilder message = new();
            message.Append("目前挂树的成员为:");
            treeList.Select(member => groupMembers
                                      .Where(groupMember => groupMember.UserId == member)
                                      .Select(groupMember => (string.IsNullOrEmpty(groupMember.Card)
                                                  ? groupMember.Nick
                                                  : groupMember.Card))
                                      .First())
                    .ToList()
                    //将成员名片添加进消息文本
                    .ForEach(name => message.Append($"\r\n{name}"));
            await eventArgs.SourceGroup.SendGroupMessage(message.ToString());
        }

        /// <summary>
        /// 显示会战进度
        /// </summary>
        private async void ShowProgress(GuildInfo guildInfo)
        {
            StringBuilder message = new();
            message.Append($"{guildInfo.GuildName} 当前进度：\r\n");
            message.Append($"{guildInfo.Round}周目{guildInfo.Order}王\r\n");
            message.Append($"阶段{guildInfo.BossPhase}\r\n");
            message.Append($"剩余血量:{guildInfo.HP}/{guildInfo.TotalHP}");

            await eventArgs.SourceGroup.SendGroupMessage(message.ToString());
        }

        /// <summary>
        /// 修改进度
        /// </summary>
        private async void ModifyProgress()
        {
            #region 处理传入参数

            string[] commandArgs = eventArgs.ToCommandArgs();
            //检查参数长度
            switch (BotUtils.CheckForLength(commandArgs, 3))
            {
                case LenType.Legitimate:
                    break;
                case LenType.Extra:
                case LenType.Illegal:
                    await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id),
                                                       "非法指令格式");
                    return;
                default:
                    await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id),
                                                       "发生未知错误，请联系机器人管理员");
                    Log.Error("Unknown error", "LenType");
                    return;
            }

            //处理参数值
            if (!int.TryParse(commandArgs[1], out int targetRound) ||
                targetRound < 0                                         ||
                !int.TryParse(commandArgs[2], out int targetOrder) ||
                targetOrder < 0                                         ||
                targetOrder > 5                                         ||
                !long.TryParse(commandArgs[3], out long targetHp)  ||
                targetHp < 0)
            {
                await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id),
                                                   "有非法参数");
                return;
            }

            //获取公会信息
            GuildInfo guildInfo = GuildBattleDB.GetGuildInfo(eventArgs.SourceGroup.Id);
            if (guildInfo == null)
            {
                await BotUtils.DatabaseFailedTips(eventArgs);
                return;
            }

            //从数据获取最大血量
            GuildBattleBoss bossInfo = GuildBattleDB.GetBossInfo(targetRound, targetOrder, guildInfo.ServerId);
            if (bossInfo == null)
            {
                await BotUtils.DatabaseFailedTips(eventArgs);
                return;
            }

            if (targetHp >= bossInfo.HP)
            {
                await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id),
                                                   "有非法参数");
                return;
            }

            #endregion

            if (!GuildBattleDB.ModifyProgress(targetRound, targetOrder, targetHp, bossInfo.HP, bossInfo.Phase, eventArgs.SourceGroup))
            {
                await BotUtils.DatabaseFailedTips(eventArgs);
                return;
            }

            await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id),
                                               "公会目前进度已修改为\r\n"                     +
                                               $"{targetRound}周目{targetOrder}王\r\n" +
                                               $"{targetHp}/{bossInfo.HP}");
        }

        /// <summary>
        /// 查刀
        /// </summary>
        private async void ShowRemainAttack()
        {
            Dictionary<long, int> remainAtkList = GetRemainAtkList();
            if (remainAtkList == null)
            {
                await BotUtils.DatabaseFailedTips(eventArgs);
                return;
            }

            if (remainAtkList.Count == 0)
            {
                await eventArgs.SourceGroup.SendGroupMessage("今天已经出完刀啦~\r\n大家辛苦啦~");
                return;
            }

            //获取群成员列表
            (APIStatusType apiStatus, List<GroupMemberInfo> groupMembers) = await eventArgs.SourceGroup.GetGroupMemberList();
            if (apiStatus != APIStatusType.OK)
            {
                Log.Error("API Error", $"API ret error {apiStatus}");
                return;
            }

            //构造群消息文本
            StringBuilder message = new();
            message.Append("今日余刀为:");
            //获取群成员名片和余刀数
            remainAtkList.Select(member => new
                         {
                             card = !groupMembers
                                     .Where(groupMember => groupMember.UserId == member.Key)
                                     .Select(groupMember => groupMember.Card).Any()
                                 ? string.Empty
                                 : groupMembers
                                   .Where(groupMember => groupMember.UserId == member.Key)
                                   .Select(groupMember => groupMember.Card)
                                   .First(),
                             name = !groupMembers
                                     .Where(groupMember => groupMember.UserId == member.Key)
                                     .Select(groupMember => groupMember.Nick).Any()
                                 ? string.Empty
                                 : groupMembers
                                   .Where(groupMember => groupMember.UserId == member.Key)
                                   .Select(groupMember => groupMember.Nick)
                                   .First(),
                             count = member.Value
                         })
                         .ToList()
                         //将成员名片与对应刀数插入消息
                         .ForEach(member => message.Append($"\r\n剩余{member.count}刀 " +
                                                           $"| {(string.IsNullOrEmpty(member.card) ? member.name : member.card)}"));
            await eventArgs.SourceGroup.SendGroupMessage(message.ToString());
        }

        /// <summary>
        /// 催刀
        /// 只允许管理员执行
        /// </summary>
        private async void UrgeAttack()
        {
            Dictionary<long, int> remainAtkList = GetRemainAtkList();
            if (remainAtkList == null)
            {
                await BotUtils.DatabaseFailedTips(eventArgs);
                return;
            }

            if (remainAtkList.Count == 0)
            {
                await eventArgs.SourceGroup.SendGroupMessage("别催了别催了，孩子都出完刀了呜呜呜");
                return;
            }

            //构造群消息文本
            List<CQCode> message = new();
            message.Add(CQCode.CQText("还没出完刀的朋友萌："));
            //艾特成员并展示其剩余刀数
            remainAtkList.ToList().ForEach(member =>
                                           {
                                               message.Add(CQCode.CQText("\r\n"));
                                               message.Add(CQCode.CQAt(member.Key));
                                               message.Add(CQCode.CQText($"：剩余{member.Value}刀"));
                                           });
            message.Add(CQCode.CQText("\r\n快来出刀啦~"));
            await eventArgs.SourceGroup.SendGroupMessage(message);
        }

        /// <summary>
        /// 查询完整出刀列表
        /// </summary>
        private async void ShowAllAttackList()
        {
            List<GuildBattle> todayAttacksList = GuildBattleDB.GetTodayAttacks();
            //首先检查是否记录为空
            if (todayAttacksList == null)
            {
                await BotUtils.DatabaseFailedTips(eventArgs);
                return;
            }

            if (todayAttacksList.Count == 0)
            {
                await eventArgs.SourceGroup.SendGroupMessage("今天还没人出刀呢！");
                return;
            }

            //获取群成员列表
            (APIStatusType apiStatus, List<GroupMemberInfo> groupMembers) = await eventArgs.SourceGroup.GetGroupMemberList();
            if (apiStatus != APIStatusType.OK)
            {
                Log.Error("API Error", $"API ret error {apiStatus}");
                return;
            }

            //获取公会区服
            Server server = GuildBattleDB.GetGuildInfo(eventArgs.SourceGroup)?.ServerId ?? (Server) 4;
            if ((int) server == 4)
            {
                await BotUtils.DatabaseFailedTips(eventArgs);
                return;
            }

            //构造群消息文本
            StringBuilder message = new();
            message.Append("今日出刀信息：\r\n");
            message.Append("刀号|出刀成员|伤害目标|伤害");
            todayAttacksList.Select(atk => new
                            {
                                card = groupMembers
                                       .Where(groupMember => groupMember.UserId == atk.Uid)
                                       .Select(groupMember => groupMember.Card)
                                       .First(),
                                name = groupMembers
                                       .Where(groupMember => groupMember.UserId == atk.Uid)
                                       .Select(groupMember => groupMember.Nick)
                                       .First(),
                                atkInfo = atk
                            })
                            .ToList()
                            .ForEach(record => message.Append(
                                                              "\r\n" +
                                                              $"{record.atkInfo.Aid} | " +
                                                              $"{record.name} | " +
                                                              $"{GetBossCode(GuildBattleDB.GetRoundPhase(server, record.atkInfo.Round), record.atkInfo.Order)} | " +
                                                              $"{record.atkInfo.Damage}"
                                                             )
                                    );
            await eventArgs.SourceGroup.SendGroupMessage(message.ToString());
        }

        /// <summary>
        /// 查询个人出刀表
        /// </summary>
        private async void ShowAttackList()
        {
            #region 参数检查

            string[] commandArgs = eventArgs.ToCommandArgs();
            long     memberUid;
            switch (BotUtils.CheckForLength(commandArgs, 0))
            {
                case LenType.Legitimate: //正常
                    memberUid = eventArgs.Sender.Id;
                    break;
                case LenType.Extra:       //管理员查询
                    if (!eventArgs.IsAdminSession()) return; //检查权限
                    memberUid = eventArgs.GetFirstUidInAt();
                    if (memberUid == -1) return;
                    break;
                default:
                    await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id),
                                                       "发生未知错误，请联系机器人管理员");
                    Log.Error("Unknown error", "LenType");
                    return;
            }

            Log.Debug("get Uid", memberUid);

            //查找成员信息 
            MemberInfo member = GuildBattleDB.GetMemberInfo(memberUid, eventArgs.SourceGroup);
            if (member == null)
            {
                await BotUtils.DatabaseFailedTips(eventArgs);
                return;
            }

            #endregion

            List<GuildBattle> todayAttacksList = GuildBattleDB.GetTodayAttacks(memberUid);
            //首先检查是否记录为空
            if (todayAttacksList == null)
            {
                await BotUtils.DatabaseFailedTips(eventArgs);
                return;
            }

            if (todayAttacksList.Count == 0)
            {
                await eventArgs.SourceGroup.SendGroupMessage(eventArgs.IsAdminSession() ? "成员" : "",
                                                   CQCode.CQAt(eventArgs.Sender.Id),
                                                   eventArgs.IsAdminSession() ? "今天还没出刀呢！" : "你今天还没出刀呢！");
                return;
            }

            //构造群消息文本
            List<CQCode> message = new();
            message.Add(CQCode.CQAt(eventArgs.Sender.Id));
            message.Add(CQCode.CQText("的今日出刀信息：\r\n"));
            message.Add(CQCode.CQText("刀号|伤害目标|伤害"));
            todayAttacksList.ForEach(record => message.Add(
                                                           CQCode.CQText("\r\n" +
                                                                         $"{record.Aid} | " +
                                                                         $"{GetBossCode(record.Round, record.Order)} | " +
                                                                         $"{record.Damage}")
                                                          )
                                    );
            await eventArgs.SourceGroup.SendGroupMessage(message);
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
        private async ValueTask<int> DelAtkByAid(int aid)
        {
            GuildInfo guildInfo = GuildBattleDB.GetGuildInfo(eventArgs.SourceGroup.Id);
            if (guildInfo == null) return -1;
            GuildBattle atkInfo = GuildBattleDB.GetAtkByID(aid);

            //检查是否当前boss
            if (guildInfo.Round != atkInfo.Round || guildInfo.Order != atkInfo.Order)
            {
                await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id),
                                                   "\r\n非当前所处boss不允许删除");
                return 0;
            }

            Log.Debug("Del atk type", atkInfo.Attack);
            //检查是否为尾刀
            if (atkInfo.Attack == AttackType.Final || atkInfo.Attack == AttackType.FinalOutOfRange ||
                atkInfo.Attack == AttackType.CompensateKill)
            {
                await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id),
                                                   "\r\n尾刀不允许删除");
                return 0;
            }

            //判断数据是否非法
            if (guildInfo.HP + atkInfo.Damage > guildInfo.TotalHP)
            {
                await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id),
                                                   "\r\n删刀后血量超出上线，请联系管理员检查机器人所在进度");
                return 0;
            }

            //删除出刀信息
            if (!GuildBattleDB.DelAtkByID(aid)) return -1;
            //更新boss数据
            return GuildBattleDB.ModifyBossHP(guildInfo, guildInfo.HP + atkInfo.Damage) ? 1 : -1;
        }

        /// <summary>
        /// 获取今日的余刀表
        /// </summary>
        /// <returns>
        /// <para>余刀表</para>
        /// <para><see langword="null"/> 数据库错误</para>
        /// </returns>
        private Dictionary<long, int> GetRemainAtkList()
        {
            Dictionary<long, int> atkCountList = GuildBattleDB.GetTodayAtkCount();
            List<MemberInfo>      memberList   = GuildBattleDB.GetAllMembersInfo(eventArgs.SourceGroup.Id);
            //首先检查数据库是否发生了错误
            if (atkCountList == null || memberList == null) return null;

            //计算每个成员的剩余刀量
            return memberList.Select(atkMember => new
                             {
                                 atkMember.Uid,
                                 count =
                                     //查找出刀计数表中是否有此成员
                                     atkCountList.Any(member => member.Key == atkMember.Uid)
                                         ? 3 - atkCountList.First(i => i.Key == atkMember.Uid).Value //计算剩余刀量
                                         : 3 //出刀计数中没有这个成员则是一刀都没有出
                             })
                             .ToList()
                             //选取还有剩余刀的成员
                             .Where(member => member.count > 0)
                             .Select(member => new {member.Uid, member.count})
                             .ToDictionary(member => member.Uid,
                                           member => member.count);
        }

        /// <summary>
        /// 检查是否已经进入会战
        /// </summary>
        /// <returns>
        /// <para><see langword="true"/> 已经进入会战</para>
        /// <para><see langword="false"/> 未进入或发生了其他错误</para>
        /// </returns>
        private async ValueTask<bool> InBattleCheck()
        {
            //检查是否进入会战
            switch (GuildBattleDB.CheckInBattle(eventArgs.SourceGroup))
            {
                case 0:
                    await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id), "公会战还没开呢");
                    return false;
                case -1:
                    await BotUtils.DatabaseFailedTips(eventArgs);
                    return false;
                case 1:
                    return true;
                default:
                    await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id), "遇到了未知错误");
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
        private async ValueTask<bool> MemberCheck()
        {
            //检查成员
            switch (GuildBattleDB.CheckMemberExists(eventArgs.Sender.Id, eventArgs.SourceGroup))
            {
                case 1:
                    return true;
                case 0:
                    await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id), "不是这个公会的成员");
                    return false;
                case -1:
                    await BotUtils.DatabaseFailedTips(eventArgs);
                    return false;
                default:
                    await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id),
                                                       "发生未知错误，请联系机器人管理员");
                    Log.Error("Unknown error", "LenType");
                    return false;
            }
        }

        private const string PHASE_CODE = "ABCD";

        private string GetBossCode(int phase, int order)
            => phase > 4 ? $"{phase} - {order}" : $"{PHASE_CODE[phase - 1]}{order}";

        #endregion
    }
}