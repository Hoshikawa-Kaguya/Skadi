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
        IStorageService storageService = SkadiApp.GetService<IStorageService>();

        Log.Info("ServerDisconnect", "移除无效的配置");
        storageService.RemoveUserConfig(eventArgs.SelfId);

        return ValueTask.CompletedTask;
    }
}