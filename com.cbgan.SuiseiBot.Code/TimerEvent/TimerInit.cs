using System;
using System.Threading;
using com.cbgan.SuiseiBot.Code.IO.Config;
using com.cbgan.SuiseiBot.Code.Tool.Log;
using Native.Sdk.Cqp;

namespace com.cbgan.SuiseiBot.Code.TimerEvent
{
    internal class TimerInit
    {
        private static Timer subscriptionThread;

        public TimerInit(CQApi cqApi)
        {
            subscriptionThread = new Timer(SubscriptionEvent,                   //事件处理
                                 cqApi,                     //酷Q API
                                 new TimeSpan(0),           //即刻执行
                                 new TimeSpan(0, 0, 0, 65)); //刷新间隔为一小时
        }

        /// <summary>
        /// 提醒DD动态更新的事件
        /// </summary>
        /// <param name="apiObject">CQApi</param>
        private void SubscriptionEvent(object apiObject)
        {
            //TODO 实装DD模块和PCR推送
            CQApi cqApi = (CQApi)apiObject;
            //加载配置文件
            ConfigIO config = new ConfigIO(cqApi.GetLoginQQ().Id);
            ConsoleLog.Info("Timer","wow");
        }
    }
}
