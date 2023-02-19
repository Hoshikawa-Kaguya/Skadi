using System;
using System.Threading.Tasks;
using Skadi.Interface;
using Sora.EventArgs.WebsocketEvent;
using YukariToolBox.LightLog;

namespace Skadi.ServerInterface;

internal static class ServerDisconnectEvent
{
    public static ValueTask OnServerDisconnectEvent(Guid _, ConnectionEventArgs eventArgs)
    {
        IGenericStorage genericStorage = SkadiApp.GetService<IGenericStorage>();

        Log.Info("ServerDisconnect", "移除无效的配置");
        genericStorage.RemoveUserConfig(eventArgs.SelfId);

        return ValueTask.CompletedTask;
    }
}