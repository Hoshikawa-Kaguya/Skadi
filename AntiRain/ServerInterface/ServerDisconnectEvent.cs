using System;
using System.Threading.Tasks;
using AntiRain.Config;
using Sora.EventArgs.WebsocketEvent;
using YukariToolBox.LightLog;

namespace AntiRain.ServerInterface
{
    internal static class ServerDisconnectEvent
    {
        public static ValueTask OnServerDisconnectEvent(Guid _, ConnectionEventArgs eventargs)
        {
            Log.Info("ServerDisconnect", "移除无效的配置");
            if (!ConfigManager.TryRemoveUserConfig(eventargs.SelfId))
            {
                Log.Error("ServerDisconnect", "移除无效的配置失败");
            }

            return ValueTask.CompletedTask;
        }
    }
}