using System.Threading.Tasks;
using AntiRain.ChatModule;
using AntiRain.ChatModule.PcrGuildBattle;
using AntiRain.Command;
using AntiRain.IO;
using AntiRain.Config.ConfigModule;
using AntiRain.TypeEnum.CommandType;
using Sora.Enumeration.EventParamsType;
using Sora.EventArgs.SoraEvent;
using YukariToolBox.FormatLog;

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
            //读取配置文件
            if (!ConfigManager.TryGetUserConfig(groupMessage.LoginUid, out UserConfig userConfig))
            {
                await groupMessage.SourceGroup.SendGroupMessage("读取配置文件(User)时发生错误\r\n请联系机器人管理员");
                Log.Error("AntiRain会战管理", "无法读取用户配置文件");
                return;
            }

            //指令匹配
            //#开头的指令(会战) -> 关键词 -> 正则
            //会战管理
            if (CommandAdapter.GetPCRGuildBattlecmdType(groupMessage.Message.RawText,
                                                        out var battleCommand))
            {
                Log.Info("PCR会战管理", $"获取到指令[{battleCommand}]");
                //判断模块使能
                if (userConfig.ModuleSwitch.PCR_GuildManager)
                {
                    PcrGuildBattleChatHandle chatHandle = new(sender, groupMessage, battleCommand);
                    chatHandle.GetChat();
                }
                else
                {
                    Log.Warning("AntiRain会战管理", "会战功能未开启");
                }
            }

            //聊天关键词
            if (CommandAdapter.GetKeywordType(groupMessage.Message.RawText, out var keywordCommand))
            {
                Log.Info("关键词触发", $"触发关键词[{keywordCommand}]");
                switch (keywordCommand)
                {
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
                Log.Info("正则触发", $"触发正则匹配[{regexCommand}]");
                switch (regexCommand)
                {
                    //调试
                    case RegexCommand.Debug:
                        if (groupMessage.SenderInfo.Role == MemberRoleType.Member)
                        {
                            Log.Warning("Auth Warning", $"成员[{groupMessage.Sender.Id}]正尝试执行调试指令");
                        }
                        break;
                    //将其他的的全部交给娱乐模块处理
                    default:
                        if (userConfig.ModuleSwitch.HaveFun)
                        {
                            Surprise surprise = new(sender, groupMessage);
                            surprise.GetChat(regexCommand);
                        }

                        break;
                }
            }
        }
    }
}