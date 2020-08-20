using System;
using System.Threading;
using Native.Sdk.Cqp;

namespace com.cbgan.SuiseiBot.Code.TimerEvent
{
    internal class TimerInit
    {
        private Timer DDThread;

        public TimerInit(CQApi cqApi)
        {
            DDThread = new Timer(DDEvent,                   //事件处理
                                 cqApi,                     //酷Q API
                                 new TimeSpan(0),           //即刻执行
                                 new TimeSpan(0, 1, 0, 0)); //刷新间隔为一小时

            // TimerThread = new Timer(TimerTest,                                              //事件处理
            //                         cqApi,                                                    //酷Q API
            //                         new TimeSpan(0),                                    //立即执行
            //                         new TimeSpan(0, 0, 0, 1));//刷新间隔

        }

        /// <summary>
        /// 提醒DD动态更新的事件
        /// </summary>
        /// <param name="apiObject">CQApi</param>
        private void DDEvent(object apiObject)
        {
            CQApi cqApi = (CQApi)apiObject;
        }

        // /// <summary>
        // /// 定时器测试代码
        // /// </summary>
        // /// <param name="apiObject">CQApi</param>
        // private void TimerTest(object apiObject)
        // {
        //     CQApi cqApi = (CQApi)apiObject;
        //     //cqApi.SendPrivateMessage(919897176, "呐");
        //     //ConsoleLog.Debug("Timer", cqApi.GetLoginQQ().ToString());
        // }
    }
}
