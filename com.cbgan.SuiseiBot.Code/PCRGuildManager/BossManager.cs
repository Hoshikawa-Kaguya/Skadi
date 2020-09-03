using System;
using SuiseiBot.TypeEnum.CmdType;
using Native.Sdk.Cqp.Model;
using Native.Sdk.Cqp.EventArgs;

namespace SuiseiBot.PCRGuildManager
{
    internal class BossManager
    {
        #region 指令处理
        public static void BossResponse(object Sender, CQGroupMessageEventArgs GBossEventArgs,
                                               PCRGuildCmdType commandType)//功能响应
        {
            if (GBossEventArgs == null) throw new ArgumentNullException(nameof(GBossEventArgs));
            Group qqGroup = GBossEventArgs.FromGroup;
            QQ senderQQ = GBossEventArgs.FromQQ;
            GroupMemberInfo memberInfo = GBossEventArgs.CQApi.GetGroupMemberInfo(qqGroup.Id, senderQQ.Id);

            //index=0为命令本身，其余为参数
            string[] commandArgs = GBossEventArgs.Message.Text.Split(' ');
            //数据库实例
            //BossDBHelper bossDB = new BossDBHelper(Sender, GBossEventArgs);

            //接下来进行一系列操作
        }
        #endregion
    }
}
