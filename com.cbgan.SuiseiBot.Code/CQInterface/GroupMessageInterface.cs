using System;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Interface;
using Native.Sdk.Cqp.Enum;
using Native.Sdk.Cqp.Model;
using com.cbgan.SuiseiBot.Code.Tool;
using com.cbgan.SuiseiBot.Code.ChatHandlers;
using com.cbgan.SuiseiBot.Code.Resource.Commands;
using com.cbgan.SuiseiBot.Code.Resource.TypeEnum.CmdType;

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
            if (sender == null || e == null) return;
            ConsoleLog.Info($"收到信息[群:{e.FromGroup.Id}]",$"{(e.Message.Text).Replace("\r\n", "\\r\\n")}");
            e.Handler = true;

            //以#开头的消息全部交给PCR处理
            if (e.Message.Text.Trim().StartsWith("#"))
            {
                PCRGuildHandle pcrGuild =new PCRGuildHandle(sender,e);
                pcrGuild.GetChat();
                return;
            }

            //全字指令匹配
            WholeMatchCmd.KeyWords.TryGetValue(e.Message, out WholeMatchCmdType cmdType); //查找关键字
            if (cmdType != 0) ConsoleLog.Info("触发关键词", $"消息类型={cmdType}");
            switch (cmdType)
            {
                //输入debug
                case WholeMatchCmdType.Debug: 
                    DefaultHandle dh = new DefaultHandle(sender, e);
                    dh.GetChat(cmdType);
                    return;
                //娱乐功能
                case WholeMatchCmdType.SurpriseMFK_Random:
                case WholeMatchCmdType.SurpriseMFK_Ban:
                case WholeMatchCmdType.SurpriseMFK_RedTea:
                case WholeMatchCmdType.SurpriseMFK_24YearsOld:
                    SurpriseMFKHandle smfh = new SurpriseMFKHandle(sender, e);
                    smfh.GetChat(cmdType);
                    return;
                //慧酱签到啦
                case WholeMatchCmdType.Suisei_SignIn: 
                    SuiseiHanlde suisei = new SuiseiHanlde(sender, e);
                    suisei.GetChat(cmdType);
                    return;
                default:
                    break;
            }

            //参数指令匹配
            KeywordCmdType keywordType = KeywordCmd.TryGetKeywordType(e.Message.Text);
            if (keywordType != 0) ConsoleLog.Info("触发关键词", $"消息类型={cmdType}");
            switch (keywordType)
            {
                case KeywordCmdType.PCRTools_GetGuildRank:
                    PCRToolsHandle pcrTools = new PCRToolsHandle(sender, e);
                    pcrTools.GetChat(keywordType);
                    return;
                default:
                    break;
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
        }
    }
}
