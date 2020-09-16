using Native.Sdk.Cqp.EventArgs;
using SuiseiBot.Code.ChatHandle;
using SuiseiBot.Code.ChatHandle.PCRHandle;
using SuiseiBot.Code.IO.Config;
using SuiseiBot.Code.Resource.Commands;
using SuiseiBot.Code.Resource.TypeEnum.CmdType;
using SuiseiBot.Code.Tool.LogUtils;

namespace SuiseiBot.Code.CQInterface
{
    public static class GroupMessageInterface
    {
        /// <summary>
        /// 收到群消息
        /// </summary>
        /// <param name="sender">事件来源</param>
        /// <param name="eventArgs">事件参数</param>
        public static void GroupMessage(object sender, CQGroupMessageEventArgs eventArgs)
        {
            //与MiraiLog信息重复暂不显示
            //ConsoleLog.Info($"收到信息[群:{eventArgs.FromGroup.Id}]",$"{(eventArgs.Message.Text).Replace("\r\n", "\\r\\n")}");
            //读取配置文件
            Config config = new Config(eventArgs.CQApi.GetLoginQQ().Id,false);
            //Module moduleEnable = config.LoadedConfig.ModuleSwitch;

            //以#开头的消息全部交给PCR处理
            if (eventArgs.Message.Text.Trim().StartsWith("#") && //检查指令开头
                config.LoadConfig()                              //加载配置文件
            )
            {
                //检查模块使能
                if (!config.LoadedConfig.ModuleSwitch.PCR_GuildManager)
                {
                    eventArgs.FromGroup.SendGroupMessage("此模块未启用");
                    return;
                }
                PCRGuildHandle pcrGuild = new PCRGuildHandle(sender, eventArgs);
                pcrGuild.GetChat();
                return;
            }

            //全字指令匹配
            WholeMatchCmd.KeyWords.TryGetValue(eventArgs.Message, out WholeMatchCmdType cmdType); //查找关键字
            if (cmdType != 0)
            {
                ConsoleLog.Info("触发关键词", $"消息类型={cmdType}");
                //加载配置文件
                if(!config.LoadConfig()) return;
            }
            switch (cmdType)
            {
                //娱乐功能
                case WholeMatchCmdType.SurpriseMFK_Random:
                case WholeMatchCmdType.SurpriseMFK_Ban:
                case WholeMatchCmdType.SurpriseMFK_RedTea:
                case WholeMatchCmdType.SurpriseMFK_24YearsOld:
                    if (!config.LoadedConfig.ModuleSwitch.HaveFun)
                    {
                        eventArgs.FromGroup.SendGroupMessage("此模块未启用");
                        return;
                    }
                    SurpriseMFKHandle smfh = new SurpriseMFKHandle(sender, eventArgs);
                    smfh.GetChat(cmdType);
                    return;
                //慧酱签到啦
                case WholeMatchCmdType.Suisei_SignIn:
                    if (!config.LoadedConfig.ModuleSwitch.Suisei)
                    {
                        eventArgs.FromGroup.SendGroupMessage("此模块未启用");
                        return;
                    }
                    SuiseiHanlde suisei = new SuiseiHanlde(sender, eventArgs);
                    suisei.GetChat(cmdType);
                    return;
                //来点色图！
                case WholeMatchCmdType.Hso:
                    if (!config.LoadedConfig.ModuleSwitch.Hso)
                    {
                        eventArgs.FromGroup.SendGroupMessage("此模块未启用");
                        return;
                    }
                    HsoHandle hso = new HsoHandle(sender, eventArgs);
                    hso.GetChat(cmdType);
                    return;
                //输入debug
                case WholeMatchCmdType.Debug:
                    if(!config.LoadedConfig.ModuleSwitch.Debug)
                    {
                        eventArgs.FromGroup.SendGroupMessage("此模块未启用");
                        return;
                    }
                    DefaultHandle dh = new DefaultHandle(sender, eventArgs);
                    dh.GetChat();
                    return;
            }

            //参数指令匹配
            KeywordCmdType keywordType = KeywordCmd.TryGetKeywordType(eventArgs.Message.Text);
            if (keywordType != 0)
            {
                ConsoleLog.Info("触发关键词", $"消息类型={cmdType}");
                //加载配置文件
                if (!config.LoadConfig()) return;
            }
            switch (keywordType)
            {
                case KeywordCmdType.PCRTools_GetGuildRank:
                    if (!config.LoadedConfig.ModuleSwitch.PCR_GuildRank)
                    {
                        eventArgs.FromGroup.SendGroupMessage("此模块未启用");
                        return;
                    }
                    GuildRankHandle pcrTools = new GuildRankHandle(sender, eventArgs);
                    pcrTools.GetChat(keywordType);
                    return;
                case KeywordCmdType.At_Bot:
                    ConsoleLog.Info("机器人事件","机器人被AT");
                    break;
                case KeywordCmdType.Cheru_Encode:
                case KeywordCmdType.Cheru_Decode:
                    if (!config.LoadedConfig.ModuleSwitch.Cheru)
                    {
                        eventArgs.FromGroup.SendGroupMessage("此模块未启用");
                        return;
                    }
                    CheruHandle cheru = new CheruHandle(sender,eventArgs);
                    cheru.GetChat(keywordType);
                    break;
                case KeywordCmdType.Debug_Echo:
                    if(!config.LoadedConfig.ModuleSwitch.Debug)
                    {
                        eventArgs.FromGroup.SendGroupMessage("此模块未启用");
                        return;
                    }
                    DefaultHandle dh = new DefaultHandle(sender, eventArgs);
                    dh.GetChat();
                    return;
            }

            eventArgs.Handler = true;
        }
    }
}
