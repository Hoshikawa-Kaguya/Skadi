using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Interface;
using com.cbgan.SuiseiBot.Code.ChatHandlers;
using com.cbgan.SuiseiBot.Code.IO.Config;
using com.cbgan.SuiseiBot.Code.Resource.Commands;
using com.cbgan.SuiseiBot.Code.Resource.TypeEnum.CmdType;
using com.cbgan.SuiseiBot.Code.Tool.Log;

namespace com.cbgan.SuiseiBot.Code.CQInterface
{
    public class GroupMessageInterface : IGroupMessage
    {
        private CQGroupMessageEventArgs eventArgs { set; get; }
        /// <summary>
        /// 收到群消息
        /// </summary>
        /// <param name="sender">事件来源</param>
        /// <param name="e">事件参数</param>
        public void GroupMessage(object sender, CQGroupMessageEventArgs e)
        {
            if (sender == null || e == null) return;
            this.eventArgs = e;
            ConsoleLog.Info($"收到信息[群:{eventArgs.FromGroup.Id}]",$"{(eventArgs.Message.Text).Replace("\r\n", "\\r\\n")}");
            //读取配置文件
            ConfigIO config = new ConfigIO(eventArgs.CQApi.GetLoginQQ().Id);
            Module moduleEnable = config.LoadedConfig.ModuleSwitch;

            //以#开头的消息全部交给PCR处理
            if (eventArgs.Message.Text.Trim().StartsWith("#") && moduleEnable.PCR_GuildManager)
            {
                PCRGuildHandle pcrGuild =new PCRGuildHandle(sender,eventArgs);
                pcrGuild.GetChat();
                return;
            }

            //全字指令匹配
            WholeMatchCmd.KeyWords.TryGetValue(eventArgs.Message, out WholeMatchCmdType cmdType); //查找关键字
            if (cmdType != 0) ConsoleLog.Info("触发关键词", $"消息类型={cmdType}");
            switch (cmdType)
            {
                //输入debug
                case WholeMatchCmdType.Debug:
                    CheckEnable(moduleEnable.Debug);
                    if(!moduleEnable.Debug) return;
                    DefaultHandle dh = new DefaultHandle(sender, eventArgs);
                    dh.GetChat(cmdType);
                    return;
                //娱乐功能
                case WholeMatchCmdType.SurpriseMFK_Random:
                case WholeMatchCmdType.SurpriseMFK_Ban:
                case WholeMatchCmdType.SurpriseMFK_RedTea:
                case WholeMatchCmdType.SurpriseMFK_24YearsOld:
                    CheckEnable(moduleEnable.HaveFun);
                    if (!moduleEnable.HaveFun) return;
                    SurpriseMFKHandle smfh = new SurpriseMFKHandle(sender, eventArgs);
                    smfh.GetChat(cmdType);
                    return;
                //慧酱签到啦
                case WholeMatchCmdType.Suisei_SignIn:
                    CheckEnable(moduleEnable.Suisei);
                    if (!moduleEnable.Suisei) return;
                    SuiseiHanlde suisei = new SuiseiHanlde(sender, eventArgs);
                    suisei.GetChat(cmdType);
                    return;
                default:
                    break;
            }

            //参数指令匹配
            KeywordCmdType keywordType = KeywordCmd.TryGetKeywordType(eventArgs.Message.Text);
            if (keywordType != 0) ConsoleLog.Info("触发关键词", $"消息类型={cmdType}");
            switch (keywordType)
            {
                case KeywordCmdType.PCRTools_GetGuildRank:
                    CheckEnable(moduleEnable.PCR_GuildRank);
                    if (!moduleEnable.PCR_GuildRank) return;
                    PCRToolsHandle pcrTools = new PCRToolsHandle(sender, eventArgs);
                    pcrTools.GetChat(keywordType);
                    return;
                default:
                    break;
            }

            eventArgs.Handler = true;
        }

        #region 模块启用检查
        private void CheckEnable(bool module)
        {
            if (!module)
            {
                this.eventArgs.FromGroup.SendGroupMessage("此模块未启用");
            }
        }
        #endregion
    }
}
