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
using com.cbgan.SuiseiBot.Code.handlers;

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
            ChatKeywords.key_word.TryGetValue(e.Message, out int Chat_Type);//查找关键字
            Console.WriteLine($"[{DateTime.Now}] INFO:收到信息[群:{e.FromGroup.Id},成员:{e.FromQQ.Id}]:[{(e.Message.Text).Replace("\r\n", "\\r\\n")}]");
            if (Chat_Type != 0) Console.WriteLine($"[{DateTime.Now}] INFO:触发关键词，消息类型={Chat_Type}");
            e.CQLog.Debug("Chat_Type", Chat_Type);
            switch (Chat_Type)
            {
                case 0://输入无法被分类
                    DefaultHandle dh=new DefaultHandle(sender, e);
                    dh.GetChat();
                    break;
                case 1://娱乐功能
                    SurpriseMFKHandle smfh = new SurpriseMFKHandle(sender, e);
                    smfh.GetChat();//进行响应
                    break;
                case 2://慧酱签到啦
                    SuiseiHanlde suisei = new SuiseiHanlde(sender, e);
                    suisei.GetChat();
                    break;
                default:
                    break;
            }
            e.Handler = true;
        }
    }
}
