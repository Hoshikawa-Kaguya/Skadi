using System;
using Native.Sdk.Cqp;
using Native.Sdk.Cqp.EventArgs;

namespace com.cbgan.SuiseiBot.Code.Tool
{
    internal class Utils
    {
        #region 时间戳转换工具

        /// <summary>
        /// 检查参数数组长度
        /// </summary>
        /// <param name="args">指令数组</param>
        /// <param name="len">至少需要的参数个数</param>
        /// <param name="e">CQGroupMessageEventArgs</param>
        /// <returns>长度合法性</returns>
        public static bool CheckForLength(string[] args, int len, CQGroupMessageEventArgs e)
        {
            if (args.Length < (len + 1))
            {
                e.FromGroup.SendGroupMessage(
                                             CQApi.CQCode_At(e.FromQQ.Id),
                                             "\n请输入正确的参数个数。");
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// 获取当前时间戳
        /// </summary>
        public static Func<long> GetNowTimeStamp =
            () => (DateTime.Now - new DateTime(1970, 1, 1, 8, 0, 0, 0)).Ticks / 10000;

        /// <summary>
        /// 获取今天零点的时间戳
        /// </summary>
        public static Func<long> GetTodayStamp =
            () => (DateTime.Today - new DateTime(1970, 1, 1, 8, 0, 0, 0)).Ticks / 10000;

        /// <summary>
        /// 将long类型时间戳转换为DateTime
        /// 时间戳单位(毫秒)
        /// </summary>
        public static Func<long, System.DateTime> TimeStampToDateTime =
            TimeStamp => new System.DateTime(1970, 1, 1, 8, 0, 0, 0).AddMilliseconds(TimeStamp);

        /// <summary>
        /// 将DateTime转换为long时间戳
        /// </summary>
        public static Func<System.DateTime, long> DateTimeToTimeStamp =
            dateTime => (dateTime - (new System.DateTime(1970, 1, 1, 8, 0, 0, 0))).Ticks / 10000;

        #endregion
    }
}