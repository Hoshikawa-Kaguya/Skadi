using System.Collections.Generic;
using System.Threading.Tasks;
using AntiRain.ChatModule;
using AntiRain.ChatModule.HsoModule;
using AntiRain.ChatModule.PcrGuildBattle;
using AntiRain.ChatModule.PcrUtils;
using AntiRain.Command;
using AntiRain.IO.Config;
using AntiRain.IO.Config.ConfigModule;
using AntiRain.Resource.PCRResource;
using AntiRain.TypeEnum.CommandType;
using Sora.Entities.CQCodes;
using Sora.Enumeration.EventParamsType;
using Sora.EventArgs.SoraEvent;
using YukariToolBox.Console;

namespace AntiRain.ServerInterface
{
    /// <summary>
    /// 群聊事件
    /// </summary>
    internal static class GroupMessageEvent
    {
        /// <summary>
        /// 群聊处理和事件触发分发
        /// </summary>
        public static async ValueTask GroupMessageParse(object sender, GroupMessageEventArgs groupMessage)
        {
            //配置文件实例
            ConfigManager configManager = new ConfigManager(groupMessage.LoginUid);
            //读取配置文件
            if (!configManager.LoadUserConfig(out UserConfig userConfig))
            {
                await groupMessage.SourceGroup.SendGroupMessage("读取配置文件(User)时发生错误\r\n请联系机器人管理员");
                ConsoleLog.Error("AntiRain会战管理", "无法读取用户配置文件");
                return;
            }
            //指令匹配
            //#开头的指令(会战) -> 关键词 -> 正则
            //会战管理
            if (CommandAdapter.GetPCRGuildBattlecmdType(groupMessage.Message.RawText, out PCRGuildBattleCommand battleCommand))
            {
                ConsoleLog.Info("PCR会战管理",$"获取到指令[{battleCommand}]");
                //判断模块使能
                if (userConfig.ModuleSwitch.PCR_GuildManager)
                {
                    PcrGuildBattleChatHandle chatHandle = new PcrGuildBattleChatHandle(sender, groupMessage, battleCommand);
                    chatHandle.GetChat();
                }
                else
                {
                    ConsoleLog.Warning("AntiRain会战管理", "会战功能未开启");
                }
            }

            //聊天关键词
            if (CommandAdapter.GetKeywordType(groupMessage.Message.RawText, out KeywordCommand keywordCommand))
            {
                ConsoleLog.Info("关键词触发",$"触发关键词[{keywordCommand}]");
                switch (keywordCommand)
                {
                    case KeywordCommand.Hso:
                        if (userConfig.ModuleSwitch.Hso)
                        {
                            HsoHandle hso = new HsoHandle(sender, groupMessage);
                            hso.GetChat();
                        }
                        break;
                    //将其他的的全部交给娱乐模块处理
                    default:
                        if (userConfig.ModuleSwitch.HaveFun)
                        {
                            Surprise surprise = new Surprise(sender, groupMessage);
                            surprise.GetChat(keywordCommand);
                        }
                        break;
                }
            }

            //正则匹配
            if (CommandAdapter.GetRegexType(groupMessage.Message.RawText, out RegexCommand regexCommand))
            {
                ConsoleLog.Info("正则触发",$"触发正则匹配[{regexCommand}]");
                switch (regexCommand)
                {
                    case RegexCommand.CheruDecode: case RegexCommand.CheruEncode:
                        //判断模块使能
                        if (userConfig.ModuleSwitch.Cheru)
                        {
                            CheruHandle cheru = new CheruHandle(sender, groupMessage);
                            cheru.GetChat(regexCommand);
                        }
                        break;
                    case RegexCommand.GetGuildRank:
                        //判断模块使能
                        if (userConfig.ModuleSwitch.PCR_GuildRank)
                        {
                            GuildRankHandle guildRank = new GuildRankHandle(sender, groupMessage);
                            guildRank.GetChat(regexCommand);
                        }
                        break;
                    //调试
                    case RegexCommand.Debug:
                        if (groupMessage.SenderInfo.Role == MemberRoleType.Member)
                        {
                            ConsoleLog.Warning("Auth Warning",$"成员[{groupMessage.Sender.Id}]正尝试执行调试指令");
                        }
                        else
                        {
                            if (groupMessage.Message.RawText.Length <= 5) return;
                            string para =
                                groupMessage.Message.RawText.Substring(5, groupMessage.Message.RawText.Length - 5);
                            if (int.TryParse(para, out int id))
                            {
                                CharaParser charaParser = new CharaParser();
                                List<CQCode> message = new List<CQCode>();
                                message.Add(CQCode.CQText(id.ToString()));
                                message.Add(CQCode.CQText(charaParser.FindChara(id)?.GetCharaNameCN() ?? string.Empty));
                                message.Add(CQCode.CQImage($"https://redive.estertion.win/icon/unit/{id}31.webp"));
                                await groupMessage.SourceGroup.SendGroupMessage(message);
                            }
                        }
                        break;
                    //将其他的的全部交给娱乐模块处理
                    default:
                        if (userConfig.ModuleSwitch.HaveFun)
                        {
                            Surprise surprise = new Surprise(sender, groupMessage);
                            surprise.GetChat(regexCommand);
                        }
                        break;
                }
            }
        }
    }
}
