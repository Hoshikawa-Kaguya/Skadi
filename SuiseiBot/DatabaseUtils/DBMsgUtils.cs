using Sora.CQCodes;
using Sora.EventArgs.SoraEvent;

namespace SuiseiBot.DatabaseUtils
{
    internal class DBMsgUtils
    {
        /// <summary>
        /// 数据库发生错误时的消息提示
        /// </summary>
        public static void DatabaseFailedTips(GroupMessageEventArgs groupEventArgs)
        {
            groupEventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(groupEventArgs.Sender.Id),
                                                      "\r\nERROR",
                                                      "\r\n数据库错误");
        }
    }
}
