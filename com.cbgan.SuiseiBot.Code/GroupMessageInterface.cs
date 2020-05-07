using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Interface;
using Native.Sdk.Cqp.Model;
using Native.Sdk.Cqp;

namespace com.cbgan.SuiseiBot.Code
{
    public class GroupMessageInterface : IGroupMessage
    {
        /// <summary>
        /// 收到群消息
        /// </summary>
        /// <param name="sender">事件来源</param>
        /// <param name="e">事件参数</param>
        public void GroupMessage(object sender, CQGroupMessageEventArgs e)
        {
            int Chat_Type = 0;
            ChatKeywords.key_word.TryGetValue(e.Message, out Chat_Type);//查找关键字
            switch (Chat_Type)
            {
                case 0://输入无法被分类
                    DefaultHandle dh=new DefaultHandle(sender, e);
                    dh.Get_Chat();
                    break;
                case 1://娱乐功能
                    SurpriseMFKHandle smfh = new SurpriseMFKHandle(sender, e);
                    smfh.Get_Chat();//进行响应
                    break;
                default:
                    break;
            }
            e.Handler = true;
        }
    }
}
