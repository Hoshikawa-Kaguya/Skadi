using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AntiRain.TimerEvent.Event;
using Sora.EventArgs.SoraEvent;
using Sora.EventArgs.WebsocketEvent;
using YukariToolBox.FormatLog;

namespace AntiRain.TimerEvent
{
    internal static class SubscriptionTimer
    {
        #region 订阅数据

        /// <summary>
        /// 计时器
        /// </summary>
        private static readonly Timer Timer = new(SubscriptionEvent, //事件处理
                                                  null,              //初始化数据
                                                  new TimeSpan(0),   //即刻执行
                                                  new TimeSpan(0, 0, 0, 60));

        /// <summary>
        /// 订阅列表
        /// </summary>
        private static readonly List<ConnectEventArgs> SubDictionary = new();

        #endregion

        #region 计时器初始化/停止

        /// <summary>
        /// 添加新的订阅
        /// </summary>
        /// <param name="eventArgs">ConnectEventArgs</param>
        internal static void TimerEventAdd(ConnectEventArgs eventArgs)
        {
            //尝试添加订阅
            if (!SubDictionary.Exists(args => args.LoginUid == eventArgs.LoginUid))
            {
                SubDictionary.Add(eventArgs);
                return;
            }

            Log.Error("SubTimer", "添加订阅账号失败");
        }

        /// <summary>
        /// 删除计时器
        /// </summary>
        /// <param name="id">onebot连接标识</param>
        /// <param name="eventArgs">ConnectEventArgs</param>
        internal static ValueTask DelTimerEvent(Guid id, ConnectionEventArgs eventArgs)
        {
            //尝试移除订阅
            if (!SubDictionary.Exists(args => args.LoginUid == eventArgs.SelfId))
            {
                Log.Error("SubTimer", "未找到该账号的订阅信息");
                return ValueTask.CompletedTask;
            }
            try
            {
                SubDictionary.RemoveAll(args => args.LoginUid == eventArgs.SelfId);
            }
            catch (Exception e)
            {
                Log.Error("SubTimer", "移除订阅账号失败");
                Log.Error("SubTimer", Log.ErrorLogBuilder(e));
            }

            return ValueTask.CompletedTask;
        }

        #endregion

        #region 更新事件方法

        /// <summary>
        /// 提醒DD动态更新的事件
        /// </summary>
        private static void SubscriptionEvent(object obj)
        {
            Log.Info("Sub", "订阅事件刷新");
            foreach (var eventArg in SubDictionary)
            {
                Log.Info("Sub", $"更新[{eventArg.LoginUid}]的订阅");
                SubscriptionUpdate.BiliUpdateCheck(eventArg);
            }
        }

        #endregion
    }
}