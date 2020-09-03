using com.cbgan.SuiseiBot.Code.ChatHandlers;
using com.cbgan.SuiseiBot.Code.Database.Helpers;
using com.cbgan.SuiseiBot.Code.Resource.TypeEnum.CmdType;
using com.cbgan.SuiseiBot.Code.Tool.Log;
using Native.Sdk.Cqp;
using Native.Sdk.Cqp.Enum;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Model;
using System;

namespace com.cbgan.SuiseiBot.Code.PCRGuildManager
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

            //TODO 自动更新boss数据
            //指示是否是管理员操作的
            bool isAdminAction = (memberInfo.MemberType == QQGroupMemberType.Manage ||
                                  memberInfo.MemberType == QQGroupMemberType.Creator);
            ConsoleLog.Info($"会战[群:{qqGroup.Id}]", $"开始处理指令{commandType}");
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
                default:
                    PCRGuildHandle.GetUnknowCommand(GBattleEventArgs);
                    ConsoleLog.Warning($"会战[群:{qqGroup.Id}]", $"接到未知指令{commandType}");
                    break;
            }
        }

        #endregion
    }
}