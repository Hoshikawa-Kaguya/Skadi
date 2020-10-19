using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sora.Entities;
using Sora.Entities.CQCodes;
using Sora.Entities.Info;
using Sora.Enumeration.ApiEnum;
using Sora.Enumeration.EventParamsType;
using Sora.EventArgs.SoraEvent;
using Sora.Tool;
using SuiseiBot.DatabaseUtils;
using SuiseiBot.DatabaseUtils.Helpers.PCRGuildBattleDB;
using SuiseiBot.Resource.TypeEnum;
using SuiseiBot.Resource.TypeEnum.CommandType;
using SuiseiBot.Resource.TypeEnum.GuildBattleType;
using SuiseiBot.Tool;

namespace SuiseiBot.ChatModule.PCRGuildBattle
{
    internal class GuildBattleManager
    {
        #region 属性
        private GroupMessageEventArgs  GBEventArgs   { get; set; }
        private Group                  SourceGroup       { get; set; }
        private User                   SenderQQ      { get; set; }
        private PCRGuildBattleCommand  CommandType   { get; set; }
        private GuildBattleMgrDBHelper GuildBattleDB { get; set; }
        private string[]               CommandArgs   { get; set; }
        #endregion

        #region 构造函数
        public GuildBattleManager(GroupMessageEventArgs GBattleEventArgs, PCRGuildBattleCommand commandType)
        {
            this.GBEventArgs   = GBattleEventArgs;
            this.SourceGroup       = GBEventArgs.SourceGroup;
            this.SenderQQ      = GBEventArgs.Sender;
            this.CommandType   = commandType;
            this.GuildBattleDB = new GuildBattleMgrDBHelper(GBEventArgs);
            this.CommandArgs   = GBEventArgs.Message.RawText.Trim().Split(' ');
        }
        #endregion

        #region 指令分发
        public async void GuildBattleResponse() //指令分发
        {
            if (GBEventArgs == null) throw new ArgumentNullException(nameof(GBEventArgs));
            //查找是否存在这个公会
            switch (GuildBattleDB.GuildExists())
            {
                case 0:
                    ConsoleLog.Debug("GuildExists", "guild not found");
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
                                             "\r\n此群未被登记为公会");
                    return;
                case -1:
                    DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                    return;
            }
            
            ConsoleLog.Info($"会战[群:{SourceGroup.Id}]", $"开始处理指令{CommandType}");

            switch (CommandType)
            {
                //会战开始
                case PCRGuildBattleCommand.BattleStart:
                    //检查执行者权限和参数
                    if(!await IsAdmin() || !await ZeroArgsCheck() || !await MemberCheck()) return;
                    BattleStart();
                    break;

                //会战结束
                case PCRGuildBattleCommand.BattleEnd:
                    //检查执行者权限和参数
                    if(!await IsAdmin() || !await ZeroArgsCheck() || !await MemberCheck()) return;
                    BattleEnd();
                    break;

                //出刀
                case PCRGuildBattleCommand.Attack:
                    if(!await InBattleCheck() || !await MemberCheck()) return;
                    Attack();
                    break;

                //出刀申请
                case PCRGuildBattleCommand.RequestAttack:
                    if(!await InBattleCheck() || !await MemberCheck()) return;
                    RequestAttack();
                    break;

                //撤刀
                case PCRGuildBattleCommand.UndoRequestAtk:
                    if(!await InBattleCheck() || !await MemberCheck()) return;
                    UndoRequest();
                    break;

                //删刀
                case PCRGuildBattleCommand.DeleteAttack:
                    //检查执行者权限
                    if(!await IsAdmin() || !await MemberCheck() || !await InBattleCheck()) return;
                    DelAttack();
                    break;
                
                //撤销出刀申请
                case PCRGuildBattleCommand.UndoAttack:
                    if(!await ZeroArgsCheck() || !await MemberCheck() || !await InBattleCheck()) return;
                    UndoAtk();
                    break;

                //查看进度
                case PCRGuildBattleCommand.ShowProgress:
                    if(!await ZeroArgsCheck()) return;
                    GuildInfo guildInfo = GuildBattleDB.GetGuildInfo(SourceGroup.Id);
                    if (guildInfo == null)
                    {
                        DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                        break;
                    }
                    if (await InBattleCheck())
                    {
                        ShowProgress(guildInfo);
                    }
                    break;

                //SL
                case PCRGuildBattleCommand.SL:
                    if(!await ZeroArgsCheck() || !await MemberCheck() || !await InBattleCheck()) return;
                    SL();
                    break;
                
                //撤销SL
                case PCRGuildBattleCommand.UndoSL:
                    //检查执行者权限
                    if(!await IsAdmin() || !await MemberCheck() || !await InBattleCheck()) return;
                    SL(true);
                    break;

                //上树
                case PCRGuildBattleCommand.ClimbTree:
                    if(!await ZeroArgsCheck() || !await MemberCheck() || !await InBattleCheck()) return;
                    ClimbTree();
                    break;

                //下树
                case PCRGuildBattleCommand.LeaveTree:
                    if(!await IsAdmin() || !await MemberCheck() || !await InBattleCheck()) return;
                    LeaveTree();
                    break;

                //查树
                case PCRGuildBattleCommand.ShowTree:
                    if (!await ZeroArgsCheck() || !await InBattleCheck()) return;
                    CheckTree();
                    break;

                //修改进度
                case PCRGuildBattleCommand.ModifyProgress:
                    if(!await IsAdmin() || !await MemberCheck() || !await InBattleCheck()) return;
                    ModifyProgress();
                    break;

                //查余刀
                case PCRGuildBattleCommand.ShowRemainAttack:
                    if (!await ZeroArgsCheck() || !await MemberCheck() || !await InBattleCheck()) return;
                    ShowRemainAttack();
                    break;

                //催刀
                case PCRGuildBattleCommand.UrgeAttack:
                    if (!await IsAdmin() || !await ZeroArgsCheck() || !await MemberCheck() || !await InBattleCheck()) return;
                    UrgeAttack();
                    break;

                //显示完整出刀表
                case PCRGuildBattleCommand.ShowAllAttackList:
                    if (!await IsAdmin() || !await ZeroArgsCheck() || !await MemberCheck() || !await InBattleCheck()) return;
                    ShowAllAttackList();
                    break;

                //显示出刀表
                case PCRGuildBattleCommand.ShowAttackList:
                    if(!await MemberCheck() || !await InBattleCheck()) return;
                    ShowAttackList();
                    break;

                default:
                    ConsoleLog.Warning($"会战[群:{SourceGroup.Id}]", $"接到未知指令{CommandType}");
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
            GuildInfo guildInfo = GuildBattleDB.GetGuildInfo(SourceGroup.Id);
            if (guildInfo == null)
            {
                DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                return;
            }
            //判断返回值
            switch (GuildBattleDB.StartBattle(guildInfo))
            {
                case 0: //已经执行过开始命令
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
                                             "\r\n上一次的出刀统计未结束",
                                             "\r\n此时会战已经开始或上一期仍未结束",
                                             "\r\n请检查是否未结束上期会战的出刀统计");
                    break;
                case 1:
                    await SourceGroup.SendGroupMessage(CQCode.CQAtAll(),
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
        private async void BattleEnd()
        {
            //判断返回值
            switch (GuildBattleDB.EndBattle())
            {
                case 0: //已经执行过开始命令
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
                                             "\r\n出刀统计并没有启动",
                                             "\r\n请检查是否未开始会战的出刀统计");
                    break;
                case 1:
                    await SourceGroup.SendGroupMessage(CQCode.CQAtAll(),
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
        private async void RequestAttack()
        {
            bool substitute;//代刀标记
            long atkUid;
            //指令检查
            switch (SuiseiUtils.CheckForLength(CommandArgs,0))
            {
                case LenType.Legitimate:
                    //检查成员
                    if (!await MemberCheck()) return;
                    atkUid     = SenderQQ.Id;
                    substitute = false;
                    break;
                case LenType.Extra://代刀
                    //检查是否有多余参数和AT
                    if (SuiseiUtils.CheckForLength(CommandArgs,1) == LenType.Legitimate)
                    {
                        //从CQCode中获取QQ号
                        atkUid = await GetUidInMsg();
                        if (atkUid == -1) return;
                    }
                    else
                    {
                        await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
                                                 "\r\n听不见！重来！（有多余参数）");
                        return;
                    }
                    substitute = true;
                    break;
                default:
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
                                                       "发生未知错误，请联系机器人管理员");
                    ConsoleLog.Error("Unknown error","LenType");
                    return;
            }

            //获取成员信息和上一次的出刀类型
            MemberInfo member    = GuildBattleDB.GetMemberInfo(atkUid);
            GuildInfo  guildInfo = GuildBattleDB.GetGuildInfo(SourceGroup.Id);
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
                        await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
                                                           "\n兄啊",CQCode.CQAt(atkUid),"在树上啊");
                    }
                    else
                    {
                        await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
                                                           "\n好好爬你的树，你出个🔨的刀");
                    }
                    return;
                case FlagType.EnGage:
                    if (substitute)
                    {
                        await SourceGroup.SendGroupMessage("成员",CQCode.CQAt(atkUid),
                                                           "\n已经在出刀中");
                    }
                    else
                    {
                        await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
                                                            "\n你不是已经在出刀吗？");
                    }
                    return;
                default:
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
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
                    await SourceGroup.SendGroupMessage("成员",CQCode.CQAt(atkUid),
                                                       "今日已出完三刀");
                }
                else
                {
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
                                                       "今日已出完三刀");
                }
                return;
            }

            //修改成员状态
            if (GuildBattleDB.UpdateMemberStatus(atkUid, FlagType.EnGage, $"{guildInfo.Round}:{guildInfo.Order}")) 
            {
                if (substitute)
                {
                    await SourceGroup.SendGroupMessage("成员",CQCode.CQAt(atkUid),
                                                       "开始出刀！");
                }
                else
                {
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
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
        private async void UndoRequest()
        {
            bool substitute;//代刀标记
            long atkUid;
            //指令检查
            switch (SuiseiUtils.CheckForLength(CommandArgs,0))
            {
                case LenType.Legitimate:
                    //检查成员
                    if (!await MemberCheck()) return;
                    atkUid     = SenderQQ.Id;
                    substitute = false;
                    break;
                case LenType.Extra://代刀
                    //检查是否有多余参数和AT
                    if (SuiseiUtils.CheckForLength(CommandArgs,1) == LenType.Legitimate)
                    {
                        //从CQCode中获取QQ号
                        atkUid = await GetUidInMsg();
                        if (atkUid == -1) return;
                    }
                    else
                    {
                        await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
                                                           "\r\n听不见！重来！（有多余参数）");
                        return;
                    }
                    substitute = true;
                    break;
                default:
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
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
                        await SourceGroup.SendGroupMessage("成员", CQCode.CQAt(atkUid)
                                                         , "\n并未出刀");
                    }
                    else
                    {
                        await SourceGroup.SendGroupMessage(CQCode.CQAt(atkUid)
                                                         , "\n并未申请出刀");
                    }
                    break;
                case FlagType.OnTree:
                    if (substitute)
                    {
                        await SourceGroup.SendGroupMessage("成员", CQCode.CQAt(atkUid),
                                                           "在树上挂着呢");
                    }
                    else
                    {
                        await SourceGroup.SendGroupMessage(CQCode.CQAt(atkUid),
                                                           "想下树？找管理员");
                    }
                    break;
                case FlagType.EnGage:
                    if (GuildBattleDB.UpdateMemberStatus(atkUid, FlagType.IDLE, null))
                    {
                        await SourceGroup.SendGroupMessage("已取消出刀申请");
                        break;
                    }
                    else
                    {
                        DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                        return;
                    }
                default: //如果跑到这了，我完蛋了
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
                                                       "发生未知错误，请联系机器人管理员");
                    ConsoleLog.Error("Unknown error","member.Flag");
                    break;
            }
        }

        /// <summary>
        /// 出刀
        /// </summary>
        private async void Attack()
        {
            bool substitute; //代刀标记
            long atkUid;

            #region 处理传入参数
            switch (SuiseiUtils.CheckForLength(CommandArgs,1))
            {
                case LenType.Illegal:
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id), "\n兄啊伤害呢");
                    return;
                case LenType.Legitimate: //正常出刀
                    //检查成员
                    if (!await MemberCheck()) return;
                    atkUid     = SenderQQ.Id;
                    substitute = false;
                    break;
                case LenType.Extra: //代刀
                    //检查是否有多余参数和AT
                    if (SuiseiUtils.CheckForLength(CommandArgs,2) == LenType.Legitimate)
                    {
                        //从CQCode中获取QQ号
                        atkUid = await GetUidInMsg();
                        if (atkUid == -1) return;
                    }
                    else
                    {
                        await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
                                                           "\r\n听不见！重来！（有多余参数）");
                        return;
                    }
                    substitute = true;
                    break;
                default:
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
                                                       "发生未知错误，请联系机器人管理员");
                    ConsoleLog.Error("Unknown error","LenType");
                    return;
            }
            #endregion

            //处理参数得到伤害值并检查合法性
            if (!long.TryParse(CommandArgs[1], out long dmg) || dmg < 0) 
            {
                await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
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
                        await SourceGroup.SendGroupMessage("成员",CQCode.CQAt(atkUid),
                                                           "未申请出刀");
                    }
                    else
                    {
                        await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
                                                           "请先申请出刀再重拳出击");
                    }
                    return;
            }
            ConsoleLog.Debug("member flag check",$"DEBUG\r\nuser = {atkUid} | flag = {atkMemberInfo.Flag}");
            #endregion

            //获取会战进度信息
            GuildInfo atkGuildInfo = GuildBattleDB.GetGuildInfo(SourceGroup.Id);
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
                List<long> treeList = GuildBattleDB.GetTree();
                if (treeList == null)
                {
                    DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                    return;
                }
                //下树提示
                if (treeList.Count != 0)
                {
                    if (!GuildBattleDB.CleanTree())
                    {
                        DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                        return;
                    }
                    List<CQCode>  treeTips = new List<CQCode>();
                    treeTips.Add(CQCode.CQText("以下成员已下树:\r\n"));
                    //添加AtCQCode
                    foreach (long uid in treeList)
                    {
                        treeTips.Add(CQCode.CQAt(uid));
                    }
                    //发送下树提示
                    await SourceGroup.SendGroupMessage(treeTips);
                }
                //判断周目
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

            List<CQCode> message = new List<CQCode>();
            if (curAttackType == AttackType.FinalOutOfRange) message.Add(CQCode.CQText("过度伤害！ 已自动修正boss血量\r\n"));
            message.Add(CQCode.CQAt(atkUid));
            message.Add(CQCode.CQText($"\r\n对{atkGuildInfo.Round}周目{atkGuildInfo.Order}王造成伤害\r\n"));
            message.Add(CQCode.CQText(dmg.ToString("N0")));
            message.Add(CQCode.CQText("\r\n\r \n目前进度："));
            GuildInfo latestGuildInfo = GuildBattleDB.GetGuildInfo(SourceGroup.Id);
            if (latestGuildInfo == null)
            {
                DBMsgUtils.DatabaseFailedTips(GBEventArgs);
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
            if(atkMemberInfo.Flag == FlagType.OnTree) message.Add(CQCode.CQText("\r\n已自动下树"));
            await SourceGroup.SendGroupMessage(message);

            #endregion
        }

        /// <summary>
        /// 撤刀
        /// </summary>
        private async void UndoAtk()
        {
            //获取上一次的出刀类型
            int lastAtkAid = GuildBattleDB.GetLastAttack(SenderQQ.Id,out _);
            switch (lastAtkAid)
            {
                case 0:
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
                                                       "并没有找到出刀记录");
                    return;
                case -1:
                    DBMsgUtils.DatabaseFailedTips(GBEventArgs);
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
                    DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                    return;
            }
            await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
                                               $"出刀编号为 {lastAtkAid} 的出刀记录已被删除");
            //获取目前会战进度
            GuildInfo guildInfo = GuildBattleDB.GetGuildInfo(SourceGroup.Id);
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
        private async void DelAttack()
        {
            #region 参数检查
            switch (SuiseiUtils.CheckForLength(CommandArgs,1))
            {
                case LenType.Illegal:
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id), "\n兄啊刀号呢");
                    return;
                case LenType.Legitimate: //正常
                    break;
                case LenType.Extra:
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id), "\n有多余参数");
                    return;
                default:
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
                                                       "发生未知错误，请联系机器人管理员");
                    ConsoleLog.Error("Unknown error","LenType");
                    return;
            }

            //处理参数得到刀号并检查合法性
            if (!int.TryParse(CommandArgs[1], out int aid) || aid < 0) 
            {
                await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
                                                   "\r\n兄啊这不是刀号");
                return;
            }
            ConsoleLog.Debug("get aid", aid);
            #endregion

            //删除记录
            switch (await DelAtkByAid(aid))
            {
                case 0:
                    return;
                case 1:
                    break;
                default:
                    DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                    return;
            }
            await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
                                               $"出刀编号为 {aid} 的出刀记录已被删除");
            //获取目前会战进度
            GuildInfo guildInfo = GuildBattleDB.GetGuildInfo(SourceGroup.Id);
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
        private async void SL(bool cleanSL = false)
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
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id), "并不在出刀中");
                    return;
                }
                //判断今天是否使用过SL
                if (member.SL >= SuiseiUtils.GetUpdateStamp())
                {
                    await SourceGroup.SendGroupMessage("成员 ",CQCode.CQAt(SenderQQ.Id), "今天已使用过SL");
                }
                else
                {
                    if (!GuildBattleDB.SetMemberSL(SenderQQ.Id))
                    {
                        DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                        return;
                    }
                    await SourceGroup.SendGroupMessage("成员 ", CQCode.CQAt(SenderQQ.Id), "已使用SL");
                }
            }
            else//清空SL
            {
                //仅能管理员执行 需要额外参数
                //判断今天是否使用过SL

                #region 参数检查
                long memberUid;

                switch (SuiseiUtils.CheckForLength(CommandArgs,0))
                {
                    case LenType.Legitimate: //正常
                        memberUid = SenderQQ.Id;
                        break;
                    case LenType.Extra://管理员撤销
                        memberUid = await GetUidInMsg();
                        if (memberUid == -1) return;
                        break;
                    default:
                        await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
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

                if (member.SL >= SuiseiUtils.GetUpdateStamp())
                {
                    if (!GuildBattleDB.SetMemberSL(memberUid, true))
                    {
                        DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                        return;
                    }
                    await SourceGroup.SendGroupMessage("成员 ",CQCode.CQAt(memberUid), "已撤回今天的SL");
                }
                else
                {
                    await SourceGroup.SendGroupMessage("成员 ", CQCode.CQAt(memberUid), "今天未使用过SL");
                }
            } 
        }

        /// <summary>
        /// 上树
        /// </summary>
        private async void ClimbTree()
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
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
                                                       "已上树");
                    return;
                case FlagType.IDLE:
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
                                                       "闲着没事不要爬树(未申请出刀)");
                    return;
                case FlagType.OnTree:
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
                                                       "都在树上嫌树不够高？");
                    return;
                default:
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
                                                       "发生未知错误，请联系机器人管理员");
                    ConsoleLog.Error("Unknown error","member.Flag");
                    return;
            }
        }

        /// <summary>
        /// 下树
        /// </summary>
        private async void LeaveTree()
        {
            #region 参数检查
            long memberUid;
            switch (SuiseiUtils.CheckForLength(CommandArgs,1))
            {
                case LenType.Legitimate: //正常
                    memberUid = await GetUidInMsg();
                    if (memberUid == -1) return;
                    break;
                case LenType.Extra:
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
                                                       "有多余参数");
                    return;
                case LenType.Illegal:
                    if(!await IsAdmin()) return;
                    memberUid = SenderQQ.Id;
                    break;
                default:
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
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
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(memberUid),
                                                       "你 轴 歪 了\n(正在出刀不要乱用指令)");
                    return;
                case FlagType.IDLE:
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(memberUid),
                                                       "弟啊你不在树上");
                    return;
                case FlagType.OnTree:
                    if (!GuildBattleDB.UpdateMemberStatus(memberUid, FlagType.IDLE, null))
                    {
                        DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                    }
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(memberUid),
                                                       "已下树");
                    return;
                default:
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(memberUid),
                                                       "发生未知错误，请联系机器人管理员");
                    ConsoleLog.Error("Unknown error","member.Flag");
                    return;
            }
        }

        /// <summary>
        /// 查树
        /// </summary>
        private async void CheckTree()
        {
            List<long> treeList = GuildBattleDB.GetTree();
            if (treeList == null || treeList.Count == 0)
            {
                await SourceGroup.SendGroupMessage("没有人在树上");
                return;
            }
            //获取群成员列表
            (APIStatusType apiStatus,List<GroupMemberInfo> groupMembers) = await SourceGroup.GetGroupMemberList();
            if (apiStatus != APIStatusType.OK)
            {
                ConsoleLog.Error("API Error",$"API ret error {apiStatus}");
                return;
            }
            //构造群消息文本
            StringBuilder message = new StringBuilder();
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
            await SourceGroup.SendGroupMessage(message.ToString());
        }

        /// <summary>
        /// 显示会战进度
        /// </summary>
        private async void ShowProgress(GuildInfo guildInfo)
        {
            StringBuilder message = new StringBuilder();
            message.Append($"{guildInfo.GuildName} 当前进度：\r\n");
            message.Append($"{guildInfo.Round}周目{guildInfo.Order}王\r\n");
            message.Append($"阶段{guildInfo.BossPhase}\r\n");
            message.Append($"剩余血量:{guildInfo.HP}/{guildInfo.TotalHP}");

            await SourceGroup.SendGroupMessage(message.ToString());
        }

        /// <summary>
        /// 修改进度
        /// </summary>
        private async void ModifyProgress()
        {
            #region 处理传入参数
            //检查参数长度
            switch (SuiseiUtils.CheckForLength(CommandArgs,3))
            {
                case LenType.Legitimate:
                    break;
                case LenType.Extra:case LenType.Illegal:
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
                                                       "非法指令格式");
                    return;
                default:
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
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
                await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
                                                   "有非法参数");
                return;
            }
            //获取公会信息
            GuildInfo guildInfo = GuildBattleDB.GetGuildInfo(SourceGroup.Id);
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
                await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
                                                   "有非法参数");
                return;
            }
            #endregion

            if (!GuildBattleDB.ModifyProgress(targetRound, targetOrder, targetHp, bossInfo.HP, bossInfo.Phase))
            {
                DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                return;
            }
            await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
                                               "公会目前进度已修改为\r\n"                 +
                                               $"{targetRound}周目{targetOrder}王\r\n" +
                                               $"{targetHp}/{bossInfo.HP}");
        }

        /// <summary>
        /// 查刀
        /// </summary>
        private async void ShowRemainAttack()
        {
            Dictionary<long, int> remainAtkList = GetRemainAtkList();
            if(remainAtkList == null)
            {
                DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                return;
            }
            if (remainAtkList.Count == 0)
            {
                await SourceGroup.SendGroupMessage("今天已经出完刀啦~\r\n大家辛苦啦~");
                return;
            }
            //获取群成员列表
            (APIStatusType apiStatus,List<GroupMemberInfo> groupMembers) = await SourceGroup.GetGroupMemberList();
            if (apiStatus != APIStatusType.OK)
            {
                ConsoleLog.Error("API Error",$"API ret error {apiStatus}");
                return;
            }
            //构造群消息文本
            StringBuilder message = new StringBuilder();
            message.Append("今日余刀为:");
            //获取群成员名片和余刀数
            remainAtkList.Select(member => new
                         {
                             card = groupMembers
                                    .Where(groupMember => groupMember.UserId == member.Key)
                                    .Select(groupMember => groupMember.Card)
                                    .First(),
                             name = groupMembers
                                    .Where(groupMember => groupMember.UserId == member.Key)
                                    .Select(groupMember => groupMember.Nick)
                                    .First(),
                             count = member.Value
                         })
                         .ToList()
                         //将成员名片与对应刀数插入消息
                         .ForEach(member => message.Append($"\r\n剩余{member.count}刀 " +
                                                           $"| {(string.IsNullOrEmpty(member.card) ? member.name : member.card)}"));
            await SourceGroup.SendGroupMessage(message.ToString());
        }

        /// <summary>
        /// 催刀
        /// 只允许管理员执行
        /// </summary>
        private async void UrgeAttack()
        {
            Dictionary<long, int> remainAtkList = GetRemainAtkList();
            if(remainAtkList == null)
            {
                DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                return;
            }
            if (remainAtkList.Count == 0)
            {
                await SourceGroup.SendGroupMessage("别催了别催了，孩子都出完刀了呜呜呜");
                return;
            }

            //构造群消息文本
            List<CQCode>  message = new List<CQCode>();
            List<List<CQCode>> messageList = new List<List<CQCode>>();
            message.Add(CQCode.CQText("还没出完刀的朋友萌："));
            //艾特成员并展示其剩余刀数
            remainAtkList.ToList().ForEach(member =>
                                           {
                                               message.Add(CQCode.CQText("\r\n"));
                                               message.Add(CQCode.CQAt(member.Key));
                                               message.Add(CQCode.CQText($"：剩余{member.Value}刀"));
                                           });
            message.Add(CQCode.CQText("\r\n快来出刀啦~"));
            await SourceGroup.SendGroupMessage(message);
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
                DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                return;
            }
            if (todayAttacksList.Count == 0)
            {
                await SourceGroup.SendGroupMessage("今天还没人出刀呢！");
                return;
            }
            //获取群成员列表
            (APIStatusType apiStatus,List<GroupMemberInfo> groupMembers) = await SourceGroup.GetGroupMemberList();
            if (apiStatus != APIStatusType.OK)
            {
                ConsoleLog.Error("API Error",$"API ret error {apiStatus}");
                return;
            }
            //构造群消息文本
            StringBuilder message = new StringBuilder();
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
                                                              $"{GetBossCode(record.atkInfo.Round, record.atkInfo.Order)} | " +
                                                              $"{record.atkInfo.Damage}"
                                                             )
                                    );
            await SourceGroup.SendGroupMessage(message.ToString());
        }

        /// <summary>
        /// 查询个人出刀表
        /// </summary>
        private async void ShowAttackList()
        {
            #region 参数检查
            long memberUid;
            switch (SuiseiUtils.CheckForLength(CommandArgs,0))
            {
                case LenType.Legitimate: //正常
                    memberUid = SenderQQ.Id;
                    break;
                case LenType.Extra: //管理员查询
                    if(!await IsAdmin()) return;//检查权限
                    memberUid = await GetUidInMsg();
                    if (memberUid == -1) return;
                    break;
                default:
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
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

            List<GuildBattle> todayAttacksList = GuildBattleDB.GetTodayAttacks(memberUid);
            //首先检查是否记录为空
            if (todayAttacksList == null)
            {
                DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                return;
            }
            if (todayAttacksList.Count == 0)
            {
                await SourceGroup.SendGroupMessage(await IsAdmin() ? "成员" : "",
                                                   CQCode.CQAt(SenderQQ.Id),
                                                   await IsAdmin() ? "今天还没出刀呢！" : "你今天还没出刀呢！");
                return;
            }
            //构造群消息文本
            List<CQCode> message = new List<CQCode>();
            message.Add(CQCode.CQAt(SenderQQ.Id));
            message.Add(CQCode.CQText("的今日出刀信息：\r\n"));
            message.Add(CQCode.CQText("刀号|伤害目标|伤害"));
            todayAttacksList.ForEach(record => message.Add(
                                                              CQCode.CQText("\r\n"                                          +
                                                                            $"{record.Aid} | "                              +
                                                                            $"{GetBossCode(record.Round, record.Order)} | " +
                                                                            $"{record.Damage}")
                                                             )
                                    );
            await SourceGroup.SendGroupMessage(message);
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
            GuildInfo guildInfo = GuildBattleDB.GetGuildInfo(SourceGroup.Id);
            if (guildInfo == null) return -1;
            GuildBattle atkInfo = GuildBattleDB.GetAtkByID(aid);

            //检查是否当前boss
            if (guildInfo.Round != atkInfo.Round || guildInfo.Order != atkInfo.Order)
            {
                await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
                                                   "\r\n非当前所处boss不允许删除");
                return 0;
            }
            ConsoleLog.Debug("Del atk type",atkInfo.Attack);
            //检查是否为尾刀
            if (atkInfo.Attack == AttackType.Final || atkInfo.Attack == AttackType.FinalOutOfRange ||
                atkInfo.Attack == AttackType.CompensateKill) 
            {
                await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
                                                   "\r\n尾刀不允许删除");
                return 0;
            }
            //判断数据是否非法
            if (guildInfo.HP + atkInfo.Damage > guildInfo.TotalHP)
            {
                await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
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
        private Dictionary<long,int> GetRemainAtkList()
        {
            Dictionary<long, int> atkCountList = GuildBattleDB.GetTodayAtkCount();
            List<MemberInfo>      memberList   = GuildBattleDB.GetAllMembersInfo(SourceGroup.Id);
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
                                         : 3                                                         //出刀计数中没有这个成员则是一刀都没有出
                             })
                             .ToList()
                             //选取还有剩余刀的成员
                             .Where(member => member.count > 0)
                             .Select(member => new {member.Uid, member.count})
                             .ToDictionary(member => member.Uid,
                                           member => member.count);
        }

        /// <summary>
        /// 检查发送消息的成员权限等级是否为管理员及以上
        /// </summary>
        /// <returns>
        /// <para><see langword="true"/> 成员为管理员或群主</para>
        /// <para><see langword="false"/> 成员不是管理员</para>
        /// </returns>
        private async ValueTask<bool> IsAdmin(bool shwoWarning = true)
        {
            GroupSenderInfo senderInfo = GBEventArgs.SenderInfo;

            bool isAdmin = senderInfo.Role == MemberRoleType.Admin ||
                           senderInfo.Role == MemberRoleType.Owner;
            //非管理员执行的警告信息
            if (!isAdmin)
            {
                //执行者为普通群员时拒绝执行指令
                if(shwoWarning)await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
                                                                      "此指令只允许管理者执行");
                ConsoleLog.Warning($"会战[群:{SourceGroup.Id}]", $"群成员{senderInfo.Nick}正在尝试执行指令{CommandType}");
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
        private async ValueTask<bool> InBattleCheck()
        {
            //检查是否进入会战
            switch (GuildBattleDB.CheckInBattle())
            {
                case 0:
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id), "公会战还没开呢");
                    return false;
                case -1:
                    DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                    return false;
                case 1:
                    return true;
                default:
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id), "遇到了未知错误");
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
        private async ValueTask<bool> ZeroArgsCheck()
        {
            //检查参数
            switch (SuiseiUtils.CheckForLength(CommandArgs,0))
            {
                case LenType.Extra:
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
                                             "\r\n听不见！重来！（有多余参数）");
                    return false;
                case LenType.Legitimate:
                    return true;
                default:
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
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
        private async ValueTask<bool> MemberCheck()
        {
            //检查成员
            switch (GuildBattleDB.CheckMemberExists(SenderQQ.Id))
            {
                case 1:
                    return true;
                case 0:
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id), "不是这个公会的成员");
                    return false;
                case -1:
                    DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                    return false;
                default:
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
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
        private async ValueTask<bool> MemberCheck(long uid)
        {
            //检查成员
            switch (GuildBattleDB.CheckMemberExists(uid))
            {
                case 1:
                    return true;
                case 0:
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(uid), "不是这个公会的成员");
                    return false;
                case -1:
                    DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                    return false;
                default:
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(uid),
                                             "发生未知错误，请联系机器人管理员");
                    ConsoleLog.Error("Unknown error","LenType");
                    return false;
            }
        }

        /// <summary>
        /// 从消息的CQ码中获取用户ID（单CQ码）
        /// </summary>
        private async ValueTask<long> GetUidInMsg()
        {
            List<long> AtUserList = GBEventArgs.Message.GetAllAtList();
            if (AtUserList.Count == 0) return -1;

            //检查成员
            if (await MemberCheck(AtUserList.First()))
            {
                return AtUserList.First();
            }
            return -1;
        }

        const string ROUND_CODE = "ABCDEFGHIJKLNMOPQRSTUVWXYZ";
        private string GetBossCode(int round, int order)
        {
            return round > 26 ? $"{round} - {order}" : $"{ROUND_CODE[round - 1]}{order}";
        }
        #endregion
    }
}
