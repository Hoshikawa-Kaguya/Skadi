using System;
using System.Collections.Generic;
using com.cbgan.SuiseiBot.Code.Tool;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Model;

namespace com.cbgan.SuiseiBot.Code.ChatHandlers
{
    internal class SurpriseMFKHandle
    {
        #region 属性
        public object Sender { private set; get; }
        public CQGroupMessageEventArgs MFKEventArgs { private set; get; }
        #endregion

        #region 构造函数
        public SurpriseMFKHandle(object sender, CQGroupMessageEventArgs eventArgs)
        {
            this.MFKEventArgs = eventArgs;
            this.Sender = sender;
        }
        #endregion

        #region 消息响应函数
        /// <summary>
        /// 消息接收函数
        /// </summary>
        public void GetChat()
        {
            if (MFKEventArgs == null || Sender == null) return;
            CheckInCD.isInCD(MFKEventArgs);
            GroupResponse();
            MFKEventArgs.Handler = true;
        }

        /// <summary>
        /// 响应函数
        /// </summary>
        private void GroupResponse()//功能响应
        {
            string chat = MFKEventArgs.Message;
            Group QQgroup = MFKEventArgs.FromGroup;
            switch (chat)
            {
                case ".r":
                    Random randomGen = new Random();
                    QQgroup.SendGroupMessage("n=", randomGen.Next(0, 100));
                    break;

                case "给老子来个禁言套餐":
                    Random banTime = new Random();
                    TimeSpan banTimeSpan = new TimeSpan(0, banTime.Next(1, 10), 0);
                    MFKEventArgs.CQApi.SetGroupMemberBanSpeak(
                        MFKEventArgs.FromGroup.Id,
                        MFKEventArgs.FromQQ.Id,
                        banTimeSpan);
                    break;

                case "给爷来个优质睡眠套餐":
                    TimeSpan banTimeSleep = new TimeSpan(8, 0, 0);
                    MFKEventArgs.CQApi.SetGroupMemberBanSpeak(
                        MFKEventArgs.FromGroup.Id,
                        MFKEventArgs.FromQQ.Id,
                        banTimeSleep);
                    break;

                case "请问可以告诉我你的年龄吗？":
                    QQgroup.SendGroupMessage("24岁，是学生");
                    QQgroup.SendGroupMessage("哼，哼，啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊");
                    break;

                case "debug":
                    QQgroup.SendGroupMessage($"收到来自[{MFKEventArgs.CQApi.GetGroupMemberInfo(MFKEventArgs.FromGroup.Id, MFKEventArgs.FromQQ.Id).Nick}]debug指令");
                    break;

                default:
                    break;
            }
        }
        #endregion
    }
}
