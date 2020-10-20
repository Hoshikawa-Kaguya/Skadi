using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sora.EventArgs.SoraEvent;

namespace AntiRain.Tool
{
    internal class CheckInCD
    {
        #region 调用记录用结构体
        private struct CheckUser
        {
            internal long GroupId;
            internal long UserId;
        }
        #endregion

        #region 调用时间记录Dictionary
        /// <param type="long">QQ号</param>
        /// <param type="DateTime">上次调用时间</param>
        private static readonly Dictionary<CheckUser, DateTime> LastChatDate = new Dictionary<CheckUser, DateTime>();
        #endregion

        #region 调用时间检查
        /// <summary>
        /// 检查用户调用时是否在CD中
        /// 对任何可能刷屏的指令都有效
        /// </summary>
        /// <param name="eventArgs">CQGroupMessageEventArgs</param>
        /// <returns>是否在CD中</returns>
        public static async Task<bool> isInCD(GroupMessageEventArgs eventArgs)
        {
            DateTime time = DateTime.Now; //获取当前时间
            CheckUser user = new CheckUser
            {
                GroupId = eventArgs.SourceGroup.Id,
                UserId  = eventArgs.Sender.Id
            };
            //尝试从字典中取出上一次调用的时间
            LastChatDate.TryGetValue(user, out DateTime last_use_time);
            //计算时间间隔(s)
            long timeSpan = (long)(time - last_use_time).TotalSeconds;
            //刷新调用时间
            LastChatDate[user] = time;
            if (timeSpan <= 60)
            {
                await eventArgs.SourceGroup.SendGroupMessage("再玩？再玩把你牙拔了当球踢\n(不要频繁使用娱乐功能)");
                await eventArgs.SourceGroup.EnableGroupMemberMute( //禁言一小时
                                                            eventArgs.Sender.Id,
                                                            3600);
                return true;
            }
            return false;
        }
        #endregion
    }
}
