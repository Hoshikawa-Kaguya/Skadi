using System;
using Native.Sdk.Cqp.EventArgs;
using com.cbgan.SuiseiBot.Code.Resource;
using Native.Sdk.Cqp.Model;

namespace com.cbgan.SuiseiBot.Code.PCRGuildManager
{
    internal class GuildBattleManager
    {
        #region 指令处理
        public static void GuildBattleResponse(object Sender, CQGroupMessageEventArgs GBattleEventArgs,
                                               PCRGuildCommandType commandType) //功能响应
        {
            if (GBattleEventArgs == null) throw new ArgumentNullException(nameof(GBattleEventArgs));
            Group qqGroup = GBattleEventArgs.FromGroup;
            QQ senderQQ = GBattleEventArgs.FromQQ;

            // switch (commandType)
            // {
            //     
            // }
            throw new NotImplementedException();
        }
        #endregion
    }
}