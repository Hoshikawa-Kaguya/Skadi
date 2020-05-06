using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Interface;
using Native.Sdk.Cqp.Model;

namespace com.cbgan.SuiseiBot.Code
{
    public class SurpriseMFKHandle
    {
        #region 属性
        public object sender { private set; get; }
        public CQGroupMessageEventArgs eventArgs { private set; get; }
        #endregion

        #region 构造函数
        public SurpriseMFKHandle(object sender, CQGroupMessageEventArgs e)
        {
            this.eventArgs = e;
            this.sender = sender;
        }
        #endregion

        #region 调用时间记录Dictionary
        /// <param type="long">QQ号</param>
        /// <param type="DateTime">上次调用时间</param>
        private static Dictionary<long, DateTime> use_time = new Dictionary<long, DateTime>();
        #endregion

        public void Get_Chat()//消息接收并判断是否响应
        {
            if (eventArgs == null || sender == null) return;
            #region 计算调用间隔并判断是否恶意刷屏
            DateTime time = System.DateTime.Now;//获取当前时间
            DateTime last_use_time;
            use_time.TryGetValue(eventArgs.FromQQ.Id, out last_use_time);
            long timeSpan = (long)(time - last_use_time).TotalSeconds;//计算时间间隔(s)
            use_time[eventArgs.FromQQ.Id] = time;//刷新调用时间
            if (timeSpan <= 60)//一分钟内调用
            {
                eventArgs.FromGroup.SendGroupMessage("再玩？再玩把你牙拔了当球踢\n(不要频繁使用娱乐功能)");
                eventArgs.FromGroup.CQApi.SetGroupMemberBanSpeak(eventArgs.FromGroup.Id, eventArgs.FromQQ.Id, new TimeSpan(1, 0, 0));//禁言一小时
                return;
            }
            #endregion
            Group_Response();
        }
        private void Group_Response()//功能响应
        {
            string chat = eventArgs.Message;
            Group QQgroup = eventArgs.FromGroup;
            //生成一个随机数
            if (chat.Equals(".r"))
            {
                Random random_gen = new Random();
                QQgroup.SendGroupMessage("n=", random_gen.Next(0, 100));
            }
            //禁言套餐
            if (chat.Equals("给老子来个禁言套餐"))
            {
                Random random_gen = new Random();
                TimeSpan ban_time = new TimeSpan(0, random_gen.Next(1, 10), 0);
                eventArgs.CQApi.SetGroupMemberBanSpeak(eventArgs.FromGroup.Id, eventArgs.FromQQ.Id, ban_time);
            }
            if (chat.Equals("给爷来个优质睡眠套餐"))
            {
                TimeSpan ban_time = new TimeSpan(8, 0, 0);
                eventArgs.CQApi.SetGroupMemberBanSpeak(eventArgs.FromGroup.Id, eventArgs.FromQQ.Id, ban_time);
            }
            //恶臭机器人
            if (chat.Equals("请问可以告诉我你的年龄吗？"))
            {
                QQgroup.SendGroupMessage("24岁，是学生");
                QQgroup.SendGroupMessage("哼，哼，啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊啊");
            }
        }
    }
}
