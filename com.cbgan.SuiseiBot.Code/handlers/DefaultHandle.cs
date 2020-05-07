using System;
using System.Collections.Generic;
using System.Net.Mail;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Model;

namespace com.cbgan.SuiseiBot.Code
{
    internal class DefaultHandle
    {
        #region 属性

        public object                  sender    { private set; get; }
        public CQGroupMessageEventArgs eventArgs { private set; get; }

        #endregion

        #region 构造函数

        public DefaultHandle(object sender, CQGroupMessageEventArgs e)
        {
            this.eventArgs = e;
            this.sender    = sender;
        }

        #endregion

        public void Get_Chat() //消息接收并判断是否响应
        {
            if (eventArgs == null || sender == null) return;
            Group_Response();
        }

        private void Group_Response() //功能响应
        {
            string chat    = eventArgs.Message;
            Group  QQgroup = eventArgs.FromGroup;
            
        }
    }
}