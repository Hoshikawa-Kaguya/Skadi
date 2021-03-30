using System.Threading.Tasks;
using AntiRain.ChatModule.PcrGuildBattle;
using AntiRain.Command;
using AntiRain.IO;
using AntiRain.Config.ConfigModule;
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
        }
    }
}