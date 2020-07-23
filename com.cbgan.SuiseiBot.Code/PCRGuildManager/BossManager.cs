using System;
using com.cbgan.SuiseiBot.Code.ChatHandlers;
using com.cbgan.SuiseiBot.Code.Database;
using com.cbgan.SuiseiBot.Code.Resource;
using com.cbgan.SuiseiBot.Code.Tool;
using Native.Sdk.Cqp;
using Native.Sdk.Cqp.Enum;
using Native.Sdk.Cqp.Model;
using Native.Sdk.Cqp.EventArgs;

namespace com.cbgan.SuiseiBot.Code.PCRGuildManager
{
    internal class BossManager
    {
        #region 指令处理
        public static void BossResponse(object Sender, CQGroupMessageEventArgs GBossEventArgs,
                                               PCRGuildCommandType commandType)//功能响应
        {
            if (GBossEventArgs == null) throw new ArgumentNullException(nameof(GBossEventArgs));
            Group qqGroup = GBossEventArgs.FromGroup;
            QQ senderQQ = GBossEventArgs.FromQQ;
            GroupMemberInfo memberInfo = GBossEventArgs.CQApi.GetGroupMemberInfo(qqGroup.Id, senderQQ.Id);

            //index=0为命令本身，其余为参数
            string[] commandArgs = GBossEventArgs.Message.Text.Split(' ');
            //数据库实例
            BossDBHelper bossDB = new BossDBHelper(Sender, GBossEventArgs);

            //查找是否存在这个公会
            if (!bossDB.GuildExists())
            {
                ConsoleLog.Debug("GuildExists", "guild not found");
                qqGroup.SendGroupMessage(CQApi.CQCode_At(senderQQ.Id),
                                         "\r\n此群未被登记为公会",
                                         "\r\n请使用以下指令创建公会",
                                         $"\r\n{PCRGuildHandle.GetCommandHelp(commandType)}");
                return;
            }
            //TODO 检查是否存在公会

            switch (commandType)
            {
                case PCRGuildCommandType.UpdateBoss:
                    //检查执行者权限
                    if (memberInfo.MemberType == QQGroupMemberType.Member)
                    {
                        //执行者为普通群员时拒绝执行指令
                        qqGroup.SendGroupMessage(CQApi.CQCode_At(senderQQ.Id),
                                                 "此指令只允许管理者执行");
                        ConsoleLog.Warning($"会战[群:{qqGroup.Id}]", $"群成员{memberInfo.Nick}正在尝试执行指令{commandType}");
                        return;
                    }
                    ConsoleLog.Info($"会战[群:{qqGroup.Id}]", $"开始处理指令{commandType}");

                    int result = bossDB.StartLoadBoss();

                    //判断返回值
                    switch (result)
                    {
                        case -1://已经执行过开始命令
                            qqGroup.SendGroupMessage(CQApi.CQCode_At(senderQQ.Id),
                                                     "\r\n发现上期会战Boss数据了惹",
                                                     "\r\n请检查是否未结束上期会战哦");
                            break;
                        case 0:
                            qqGroup.SendGroupMessage(CQApi.CQCode_AtAll(),
                                                     "\r\n成功加载Boss数据，快来康康吧！");
                            break;
                        default:
                            qqGroup.SendGroupMessage(CQApi.CQCode_At(senderQQ.Id),
                                                     "\r\nERROR",
                                                     "\r\n遇到未知错误,请联系当前机器人维护者");
                            break;
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        #endregion
    }
}
