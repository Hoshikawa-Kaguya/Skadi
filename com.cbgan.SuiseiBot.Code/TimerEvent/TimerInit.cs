using System;
using System.Threading;
using Native.Sdk.Cqp;

namespace com.cbgan.SuiseiBot.Code.TimerEvent
{
    internal class TimerInit
    {
        private Timer TimerThread;

        public TimerInit(CQApi cqApi)
        {
            // TimerThread = new Timer(ThreadEvent,                                              //事件处理
            //                         cqApi,                                                    //酷Q API
            //                         DateTime.Today + new TimeSpan(1, 5, 0, 0) - DateTime.Now,   //将定时器延时到凌晨5点时执行
            //                         new TimeSpan(1, 0, 0, 0));                                  //刷新间隔为一天

            TimerThread = new Timer(TimerTest,                                              //事件处理
                                    cqApi,                                                    //酷Q API
                                    new TimeSpan(0),                                    //立即执行
                                    new TimeSpan(0, 0, 0, 1));//刷新间隔

        }

        /// <summary>
        /// 定时器触发执行代码
        /// </summary>
        /// <param name="apiObject">CQApi</param>
        private void ThreadEvent(object apiObject)
        {

        }

        /// <summary>
        /// 定时器测试代码
        /// </summary>
        /// <param name="apiObject">CQApi</param>
        private void TimerTest(object apiObject)
        {
            CQApi cqApi = (CQApi)apiObject;
            //cqApi.SendPrivateMessage(919897176, "呐");
            //ConsoleLog.Debug("Timer", cqApi.GetLoginQQ().ToString());
        }
    }
}
