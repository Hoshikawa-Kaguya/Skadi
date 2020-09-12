using Native.Sdk.Cqp;
using Native.Sdk.Cqp.Enum;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Model;
using SuiseiBot.Code.ChatHandle.PCRHandle;
using SuiseiBot.Code.DatabaseUtils.Helpers;
using SuiseiBot.Code.Resource.TypeEnum.CmdType;
using SuiseiBot.Code.Resource.TypeEnum.GuildBattleType;
using SuiseiBot.Code.Tool.LogUtils;
using System;
using System.Collections.Generic;
using System.Text;
using SuiseiBot.Code.DatabaseUtils;
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
            this.GuildBattleDB = new GuildBattleMgrDBHelper(SenderQQ, GBEventArgs);
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
            switch (CommandType)
            {
                case PCRGuildCmdType.BattleStart:
                    //检查执行者权限
                    if(!IsAdmin()) return;
                    
                    BattleStart();
                    break;
                case PCRGuildCmdType.BattleEnd:
                    //检查执行者权限
                    if(!IsAdmin()) return;

                    BattleEnd();
                    break;
                case PCRGuildCmdType.Attack:
                    Attack(commandArgs);
                    break;
                default:
                    PCRGuildHandle.GetUnknowCommand(GBEventArgs);
                    ConsoleLog.Warning($"会战[群:{QQGroup.Id}]", $"接到未知指令{CommandType}");
                    break;
            }
        }
        #endregion

        #region 指令
        private void BattleStart()
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
                default:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                             "\r\nERROR",
                                             "\r\n遇到未知错误,请联系当前机器人维护者");
                    break;
            }
        }

        private void BattleEnd()
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
                default:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                             "\r\nERROR",
                                             "\r\n遇到未知错误,请联系当前机器人维护者");
                    break;
            }
        }

        private void Attack(string[] CommandArgs)
        {
            if (!GuildBattleDB.CheckInBattle())
            {
                QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id), "公会战还没开呢");
                return;
            }
            long attcakUid;
            //判断参数类型
            switch (Utils.CheckForLength(CommandArgs,1))
            {
                case LenType.Illegal:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id), "\n兄啊你的伤害呢");
                    return;
                case LenType.Legitimate://正常出刀
                    //检查成员
                    if (!GuildBattleDB.CheckMember(SenderQQ.Id))
                    {
                        QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id), "\n不是这个公会的还想打会战？");
                        return;
                    }
                    attcakUid = SenderQQ.Id;
                    break;
                case LenType.Extra://代刀
                    //检查是否有多余参数和AT
                    if (GBEventArgs.Message.CQCodes.Count       == 1             &&
                        GBEventArgs.Message.CQCodes[0].Function == CQFunction.At &&
                        Utils.CheckForLength(CommandArgs,2)     == LenType.Legitimate)
                    {
                        //从CQCode中获取QQ号
                        Dictionary<string,string> codeInfo =  GBEventArgs.Message.CQCodes[0].Items;
                        if (codeInfo.TryGetValue("qq",out string uid))
                        {
                            attcakUid = Convert.ToInt64(uid);
                            //检查成员
                            if (!GuildBattleDB.CheckMember(attcakUid))
                            {
                                QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id), "\n此成员不是这个公会的成员");
                                return;
                            }
                        }
                        else
                        {
                            ConsoleLog.Error("CQCode parse error","can't get uid in cqcode");
                            return;
                        }
                    }
                    else
                    {
                        QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                                 "\r\n听不见！重来！（有多余参数）");
                        return;
                    }
                    break;
                default:
                    QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                             "发生未知错误，请联系机器人管理员");
                    ConsoleLog.Error("Unknown error","LenType");
                    return;
            }
            //处理参数得到伤害值并检查合法性
            if (!long.TryParse(CommandArgs[1], out long dmg) || dmg < 0) 
            {
                QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id),
                                         "\r\n兄啊你的伤害好jb怪啊");
                return;
            }
            ConsoleLog.Debug("Dmg info parse",$"DEBUG\r\ndmg = {dmg} | attack_user = {attcakUid}");

            //获取成员状态信息
            MemberStatus atkMemberStatus = GuildBattleDB.GetMemberStatus(attcakUid);

            //成员状态检查
            switch (atkMemberStatus.Flag)
            {
                //进入出刀判断
                case FlagType.EnGage:case FlagType.OnTree:
                    break;
                //当前并未开始出刀，请先申请出刀=>返回
                case FlagType.IDLE:
                    return;
            }


            // int ret = GuildBattleDB.Attack(uid: SenderQQ.Id,
            //                                dmg: dmg,
            //                                out AttackType AttType,
            //                                out FlagType flag,
            //                                out bool isChangeBoss
            //                               ); //命令执行返回值
            // ConsoleLog.Debug("Guild Battle",$"user:{SenderQQ.Id} dmg:{dmg} type:{AttType}");
            // StringBuilder sb = new StringBuilder();
            // //检查成员状态
            // switch (flag)
            // {
            //     case FlagType.EnGage:
            //         break;
            //     case FlagType.IDLE:
            //
            // }
            // switch (AttType)
            // {
            //     
            // }
            // switch (ret)
            // {
            //     case 0:
            //         //TODO 优化出刀类型判断和消息文本的构建
            //         sb.Append("\r\n出刀成功！");
            //         if (status == 2)
            //         {
            //             sb.Append("\r\n不过伤害过多，已修正伤害为BOSS剩余HP；");
            //         }
            //         else if (status == 1)
            //         {
            //             sb.Append("\r\n未尾刀请不要报为尾刀，已修正为通常刀；");
            //         }
            //         var nowProgress =
            //             GuildBattleDB.ShowProgress();
            //         if (isChangeBoss)
            //         {
            //             sb.Append("\r\n@全体成员 BOSS已经成功切换啦");
            //         }
            //
            //         sb.Append($"\r\n目前第{nowProgress.Round}周目的老{nowProgress.Order}状态如下：");
            //         sb.Append($"\r\n血量：{nowProgress.HP} / {nowProgress.HP}");
            //         sb.Append($"\r\n阶段：{nowProgress.BossPhase}");
            //         break;
            //     case -1:
            //         sb.Append("\r\n你并不在公会，请先入会！");
            //         break;
            //     case -2:
            //         sb.Append("\r\n公会战还没开始，请等待管理员的指令！");
            //         break;
            //     case -3:
            //         sb.Append("\r\n目前禁止出刀，请等待管理员的指令！");
            //         break;
            //     case -4:
            //         sb.Append("\r\n目前禁止出刀，请等待补时刀出完！");
            //         break;
            //     case -99:
            //         sb.Append("\r\n数据库出错，请联系管理员！");
            //         break;
            // }
            // QQGroup.SendGroupMessage(CQApi.CQCode_At(SenderQQ.Id), sb.ToString());
        }
        #endregion

        #region 权限检查
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