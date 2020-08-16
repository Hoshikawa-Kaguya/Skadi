using System;

namespace com.cbgan.SuiseiBot.Code.Tool
{
    internal class Utils
    {
        #region 时间戳转换工具

        /// <summary>
        /// 检查参数数组长度
        /// </summary>
        /// <param>指令数组
        ///     <name>args</name>
        /// </param>
        /// <param>至少需要的参数个数
        ///     <name>len</name>
        /// </param>
        /// <returns>长度合法性</returns>
        public static Func<string[], int, bool> CheckForLength = (args, len) => args.Length >= len + 1;

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
