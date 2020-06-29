using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Interface;
using com.cbgan.SuiseiBot.Resource;
using com.cbgan.SuiseiBot.Code.Tool;
using com.cbgan.SuiseiBot.Code.PCRGuildManager;
using com.cbgan.SuiseiBot.Code.Resource;
using com.cbgan.SuiseiBot.Code.ChatHandlers;

namespace com.cbgan.SuiseiBot.Code.CQInterface
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
            if (sender == null || e == null)
            {
                e.Handler = true;
                return;
            }
            ConsoleLog.Info($"收到信息[群:{e.FromGroup.Id}]",$"[{(e.Message.Text).Replace("\r\n", "\\r\\n")}]");

            //以#开头的消息全部交给PCR处理
            if (e.Message.Text.Trim().StartsWith("#"))
            {
                PCRGuildHandle pcrGuild =new PCRGuildHandle(sender,e);
                pcrGuild.GetChat();
            }
            else
            {
                //其他全字匹配功能
                ChatKeywords.KeyWords.TryGetValue(e.Message, out KeywordType chatType); //查找关键字
                if (chatType != 0) ConsoleLog.Info("触发关键词",$"消息类型={chatType}");
                switch (chatType)
                {
                    case 0:case KeywordType.Debug: //输入无法被分类
                        DefaultHandle dh = new DefaultHandle(sender, e);
                        dh.GetChat(chatType);
                        break;
                    case KeywordType.SurpriseMFK: //娱乐功能
                        SurpriseMFKHandle smfh = new SurpriseMFKHandle(sender, e);
                        smfh.GetChat(); //进行响应
                        break;
                    case KeywordType.Suisei: //慧酱签到啦
                        SuiseiHanlde suisei = new SuiseiHanlde(sender, e);
                        suisei.GetChat();
                        break;
                    default:
                        break;
                }
            }
            e.Handler = true;
        }
    }
}
