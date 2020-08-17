using System;
using System.Linq;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Interface;
using Native.Sdk.Cqp.Enum;
using Native.Sdk.Cqp.Model;
using com.cbgan.SuiseiBot.Code.Tool;
using com.cbgan.SuiseiBot.Code.PCRGuildManager;
using com.cbgan.SuiseiBot.Code.Resource;
using com.cbgan.SuiseiBot.Code.ChatHandlers;
using com.cbgan.SuiseiBot.Code.Resource.Commands;
using com.cbgan.SuiseiBot.Code.Resource.Enum;

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
            ConsoleLog.Info($"收到信息[群:{e.FromGroup.Id}]",$"{(e.Message.Text).Replace("\r\n", "\\r\\n")}");

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
                //一般特殊指令匹配
                if (e.Message.Text.Contains(' '))
                {
                    SpecialKeywordsType keywordType = SpecialKeywords.TryGetKeywordType(e.Message.Text);
                    if (keywordType != 0) ConsoleLog.Info("触发关键词", $"消息类型={chatType}");
                    switch (keywordType)
                    {
                        case SpecialKeywordsType.PCRTools:
                            PCRToolsHandle pcrTools = new PCRToolsHandle(sender, e);
                            pcrTools.GetChat();
                            break;
                        default:
                            break;
                    }
                }
            }
            
            //TODO 转换B站小程序URL
            //检查所发消息中是否有卡片消息
             foreach (CQCode cqCode in e.Message.CQCodes)
             {
                 if (cqCode.Function.Equals(CQFunction.Rich))
                 {
                     int infoIndex = e.Message.Text.IndexOf("text=", StringComparison.Ordinal);
                     string miniAppTextInfo =
                         e.Message.Text.Substring(infoIndex, e.Message.Text.Length - infoIndex - 1);
                     ConsoleLog.Info("收到卡片消息", miniAppTextInfo);
                 }
             }
            e.Handler = true;
        }
    }
}
