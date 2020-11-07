using System.Threading.Tasks;
using Sora.Entities.CQCodes;
using Sora.EventArgs.SoraEvent;
using Sora.Tool;

namespace AntiRain.DatabaseUtils
{
    internal class DBMsgUtils
    {
        /// <summary>
        /// 数据库发生错误时的消息提示
        /// </summary>
        public static async ValueTask DatabaseFailedTips(GroupMessageEventArgs groupEventArgs)
        {
            await groupEventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(groupEventArgs.Sender.Id),
                                                      "\r\nERROR",
                                                      "\r\n数据库错误");
            ConsoleLog.Error("database","database error");
        }
    }
}
