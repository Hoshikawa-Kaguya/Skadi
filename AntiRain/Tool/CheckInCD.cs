using System;
using System.Collections.Generic;

namespace AntiRain.Tool
{
    internal static class CheckInCD
    {
        #region 调用记录用结构体

        public struct CheckUser
        {
            internal long GroupId { get; set; }
            internal long UserId  { get; set; }
        }

        #endregion

        #region 调用时间检查

        /// <summary>
        /// 检查用户调用时是否在CD中
        /// 对任何可能刷屏的指令都有效
        /// </summary>
        /// <param name="checkDict">调用记录字典</param>
        /// <param name="groupId">群号</param>
        /// <param name="userId">用户ID</param>
        /// <returns>是否在CD中</returns>
        public static bool IsInCD(this Dictionary<CheckUser, DateTime> checkDict, long groupId, long userId)
        {
#if DEBUG
            return false;
#else
            var time = DateTime.Now; //获取当前时间
            var user = new CheckUser
            {
                GroupId = groupId,
                UserId = userId
            };
            //尝试从字典中取出上一次调用的时间
            if (checkDict.TryGetValue(user, out DateTime last_use_time) &&
                (long)(time - last_use_time).TotalSeconds < 60)
            {
                //刷新调用时间
                checkDict[user] = time;
                return true;
            }

            //刷新/写入调用时间
            checkDict[user] = time;
            return false;
#endif
        }

        #endregion
    }
}