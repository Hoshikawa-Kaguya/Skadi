using System;
using System.Collections.Generic;
using Native.Sdk.Cqp.EventArgs;

namespace SuiseiBot.Code.Tool
{
    internal class CheckInCD
    {
        #region 调用记录用结构体
        internal struct User
        {
            internal long GroupId;
            internal long UserId;
        }
        #endregion

        #region 调用时间记录Dictionary
        /// <param type="long">QQ号</param>
        /// <param type="DateTime">上次调用时间</param>
        private static readonly Dictionary<User, DateTime> LastChatDate = new Dictionary<User, DateTime>();
        #endregion

        #region 调用时间检查
        /// <summary>
        /// 检查用户调用时是否在CD中
        /// 对任何可能刷屏的指令都有效
        /// </summary>
        /// <param name="eventArgs">CQGroupMessageEventArgs</param>
        /// <returns>是否在CD中</returns>
        public static bool isInCD(CQGroupMessageEventArgs eventArgs)
        {
            DateTime time = DateTime.Now; //获取当前时间
            User user = new User
            {
                GroupId = eventArgs.FromGroup.Id,
                UserId  = eventArgs.FromQQ.Id
            };
            LastChatDate.TryGetValue(user, out DateTime last_use_time);
            long timeSpan = (long)(time - last_use_time).TotalSeconds; //计算时间间隔(s)
            LastChatDate[user] = time;                                 //刷新调用时间
            if (timeSpan <= 60)
            {
                eventArgs.FromGroup.SendGroupMessage("再玩？再玩把你牙拔了当球踢\n(不要频繁使用娱乐功能)");
                eventArgs.FromGroup.CQApi.SetGroupMemberBanSpeak( //禁言一小时
                                                                 eventArgs.FromGroup.Id,
                                                                 eventArgs.FromQQ.Id,
                                                                 new TimeSpan(1, 0, 0));
                return true;
            }
            return false;
        }
        #endregion
    }
}
