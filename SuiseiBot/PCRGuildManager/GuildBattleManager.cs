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
using System.Text;

namespace SuiseiBot.Code.PCRGuildManager
{
    internal class GuildBattleManager
    {

        
        #region 指令处理

        public static void GuildBattleResponse(object Sender, CQGroupMessageEventArgs GBattleEventArgs,
                                               PCRGuildCmdType commandType) //功能响应
        {
            if (GBattleEventArgs == null) throw new ArgumentNullException(nameof(GBattleEventArgs));
            Group           qqGroup    = GBattleEventArgs.FromGroup;
            QQ              senderQQ   = GBattleEventArgs.FromQQ;
            GroupMemberInfo memberInfo = GBattleEventArgs.CQApi.GetGroupMemberInfo(qqGroup.Id, senderQQ.Id);

            //index=0为命令本身，其余为参数
            string[] commandArgs = GBattleEventArgs.Message.Text.Split(' ');
            //数据库实例
            GuildBattleMgrDBHelper guildBattleDB = new GuildBattleMgrDBHelper(Sender, GBattleEventArgs);

            //查找是否存在这个公会
            if (!guildBattleDB.GuildExists())
            {
                ConsoleLog.Debug("GuildExists", "guild not found");
                qqGroup.SendGroupMessage(CQApi.CQCode_At(senderQQ.Id),
                                         "\r\n此群未被登记为公会",
                                         "\r\n请使用以下指令创建公会",
                                         $"\r\n{PCRGuildHandle.GetCommandHelp(commandType)}");
                return;
            }

            //指示是否是管理员操作的
            bool isAdminAction = (memberInfo.MemberType == QQGroupMemberType.Manage ||
                                  memberInfo.MemberType == QQGroupMemberType.Creator);
            ConsoleLog.Info($"会战[群:{qqGroup.Id}]", $"开始处理指令{commandType}");
            int ret = 0;//命令执行返回值
            switch (commandType)
            {
                case PCRGuildCmdType.BattleStart:
                    //检查执行者权限
                    if (!isAdminAction)
                    {
                        //执行者为普通群员时拒绝执行指令
                        qqGroup.SendGroupMessage(CQApi.CQCode_At(senderQQ.Id),
                                                 "此指令只允许管理者执行");
                        ConsoleLog.Warning($"会战[群:{qqGroup.Id}]", $"群成员{memberInfo.Nick}正在尝试执行指令{commandType}");
                        return;
                    }

                    int resStart = guildBattleDB.StartBattle();
                    //判断返回值
                    switch (resStart)
                    {
                        case -1: //已经执行过开始命令
                            qqGroup.SendGroupMessage(CQApi.CQCode_At(senderQQ.Id),
                                                     "\r\n出刀统计已经开始了嗷",
                                                     "\r\n此时会战已经开始或上一期仍未结束",
                                                     "\r\n请检查是否未结束上期会战的出刀统计");
                            break;
                        case 0:
                            qqGroup.SendGroupMessage(CQApi.CQCode_AtAll(),
                                                     "\r\n新的一期会战开始啦！");
                            break;
                        default:
                            qqGroup.SendGroupMessage(CQApi.CQCode_At(senderQQ.Id),
                                                     "\r\nERROR",
                                                     "\r\n遇到未知错误,请联系当前机器人维护者");
                            break;
                    }

                    break;
                case PCRGuildCmdType.BattleEnd:
                    //检查执行者权限
                    if (!isAdminAction)
                    {
                        //执行者为普通群员时拒绝执行指令
                        qqGroup.SendGroupMessage(CQApi.CQCode_At(senderQQ.Id),
                                                 "此指令只允许管理者执行");
                        ConsoleLog.Warning($"会战[群:{qqGroup.Id}]", $"群成员{memberInfo.Nick}正在尝试执行指令{commandType}");
                        return;
                    }

                    int resEnd = guildBattleDB.StartBattle();
                    //判断返回值
                    switch (resEnd)
                    {
                        case -1: //已经执行过开始命令
                            qqGroup.SendGroupMessage(CQApi.CQCode_At(senderQQ.Id),
                                                     "\r\n出刀统计已经开始了嗷",
                                                     "\r\n此时会战已经开始或上一期仍未结束",
                                                     "\r\n请检查是否未结束上期会战的出刀统计");
                            break;
                        case 0:
                            qqGroup.SendGroupMessage(CQApi.CQCode_AtAll(),
                                                     "\r\n新的一期会战开始啦！");
                            break;
                        default:
                            qqGroup.SendGroupMessage(CQApi.CQCode_At(senderQQ.Id),
                                                     "\r\nERROR",
                                                     "\r\n遇到未知错误,请联系当前机器人维护者");
                            break;
                    }

                    break;
                case PCRGuildCmdType.Attack:
                    if (!int.TryParse(commandArgs[1], out int dmg) || dmg<0 )
                    {
                        qqGroup.SendGroupMessage(CQApi.CQCode_At(senderQQ.Id),
                                                 "\r\n伤害输入有误！仅能输入自然数！");
                        break;
                    }

                    ret = guildBattleDB.Attack(uid: senderQQ.Id,
                                               dmg: dmg,
                                               out AttackType AttType,
                                               out FlagType flag,
                                               out int status,
                                               out bool isChangeBoss
                                              );
                    StringBuilder sb = new StringBuilder();
                    switch (ret)
                    {
                        case 0:
                            
                            sb.Append("\r\n出刀成功！");
                            if (status == 2)
                            {
                                sb.Append("\r\n不过伤害过多，已修正伤害为BOSS剩余HP；");
                            }
                            else if (status == 1)
                            {
                                sb.Append("\r\n未尾刀请不要报为尾刀，已修正为通常刀；");
                            }
                            var nowProgress=
                                guildBattleDB.ShowProgress();
                            if (isChangeBoss)
                            {
                                sb.Append("\r\n@全体成员 BOSS已经成功切换啦");
                            }

                            sb.Append($"\r\n目前第{nowProgress.Round}周目的老{nowProgress.Order}状态如下：");
                            sb.Append($"\r\n血量：{nowProgress.HP} / {nowProgress.HP}");
                            sb.Append($"\r\n阶段：{nowProgress.BossPhase}");
                            break;
                        case -1:
                            sb.Append("\r\n你并不在公会，请先入会！");
                            break;
                        case -2:
                            sb.Append("\r\n公会战还没开始，请等待管理员的指令！");
                            break;
                        case -3:
                            sb.Append("\r\n目前禁止出刀，请等待管理员的指令！");
                            break;
                        case -4:
                            sb.Append("\r\n目前禁止出刀，请等待补时刀出完！");
                            break;
                        case -99:
                            sb.Append("\r\n数据库出错，请联系管理员！");
                            break;
                    }
                    qqGroup.SendGroupMessage(CQApi.CQCode_At(senderQQ.Id), sb.ToString());
                    break;
                default:
                    PCRGuildHandle.GetUnknowCommand(GBattleEventArgs);
                    ConsoleLog.Warning($"会战[群:{qqGroup.Id}]", $"接到未知指令{commandType}");
                    break;
            }
        }

        #endregion
    }
}