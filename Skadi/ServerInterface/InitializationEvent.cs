using System;
using System.IO;
using System.Threading.Tasks;
using Skadi.Config;
using Skadi.DatabaseUtils;
using Skadi.IO;
using Skadi.TimerEvent;
using Sora.EventArgs.SoraEvent;
using YukariToolBox.LightLog;

namespace Skadi.ServerInterface;

/// <summary>
/// 初始化事件
/// </summary>
internal static class InitializationEvent
{
    /// <summary>
    /// 初始化处理
    /// </summary>
    internal static ValueTask Initialization(string _, ConnectEventArgs connectEvent)
    {
        Log.Info("Skadi初始化", "与onebot客户端连接成功，初始化资源...");
        //初始化配置文件
        Log.Info("Skadi初始化", $"初始化用户[{connectEvent.LoginUid}]的配置");
        if (!ConfigManager.UserConfigFileInit(connectEvent.LoginUid)
            || !ConfigManager.TryGetUserConfig(connectEvent.LoginUid, out var userConfig))
        {
            Log.Fatal(new IOException("无法获取用户配置文件(Initialization)"), "Skadi初始化", "用户配置文件初始化失败");
            Environment.Exit(-1);
            return ValueTask.CompletedTask;
        }

        //在控制台显示启用模块
        Log.Info("已启用的模块",
                 $"\n{userConfig.ModuleSwitch}");
        //显示代理信息
        if (userConfig.ModuleSwitch.Hso && !string.IsNullOrEmpty(userConfig.HsoConfig.PximyProxy))
            Log.Debug("Hso Proxy", userConfig.HsoConfig.PximyProxy);

        //初始化数据库
        DatabaseInit.UserDataInit(connectEvent);

        //初始化QA
        StaticVar.QaConfigFile = new QAConfigFile(connectEvent.LoginUid);
        StaticVar.ServiceReady.Set();

        //初始化定时器线程
        if (userConfig.ModuleSwitch.BiliSubscription)
            SubscriptionTimer.TimerEventAdd(connectEvent);

        return ValueTask.CompletedTask;
    }
}