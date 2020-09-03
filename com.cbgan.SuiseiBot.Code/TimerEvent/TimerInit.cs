using System;
using System.Threading;
using Native.Sdk.Cqp;
using SuiseiBot.IO.Code.TimerEvent.Event;

namespace SuiseiBot.IO.Code.TimerEvent
{
    internal class TimerInit
    {
        private static Timer subscriptionThread;

        public TimerInit(CQApi cqApi,int updateSpan)
        {
            subscriptionThread = new Timer(SubscriptionEvent,                   //事件处理
                                           cqApi,                     //酷Q API
                                           new TimeSpan(0),           //即刻执行
                                           new TimeSpan(0, 0, 0, updateSpan)); //设置刷新间隔
        }

        /// <summary>
        /// 提醒DD动态更新的事件
        /// </summary>
        /// <param name="apiObject">CQApi</param>
        private void SubscriptionEvent(object apiObject)
        {
            CQApi cqApi = (CQApi)apiObject;
            DynamicUpdate.BiliUpdateCheck(cqApi);
        }
    }
}
