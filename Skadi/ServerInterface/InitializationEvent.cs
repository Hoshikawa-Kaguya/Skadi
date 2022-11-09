using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Skadi.Command;
using Skadi.Config;
using Skadi.DatabaseUtils;
using Skadi.Services;
using Skadi.TimerEvent;
using Sora;
using Sora.EventArgs.SoraEvent;
using Sora.Interfaces;
using YukariToolBox.LightLog;

namespace Skadi.ServerInterface;

/// <summary>
/// 初始化事件
/// </summary>
internal static class InitializationEvent
{
    private static bool IsInit = false;

    /// <summary>
    /// 初始化处理
    /// </summary>
    internal static ValueTask Initialization(string _, ConnectEventArgs connectEvent)
    {
        if (IsInit)
        {
            Log.Error("Skadi初始化", "Skadi仅为单一账户用户设计，不支持重复初始化");
            return ValueTask.CompletedTask;
        }

        Log.Info("Skadi初始化", "与onebot客户端连接成功，初始化资源...");
        //初始化配置文件
        Log.Info("Skadi初始化", $"初始化用户[{connectEvent.LoginUid}]的配置");
        if (!ConfigManager.UserConfigFileInit(connectEvent.LoginUid) ||
            !ConfigManager.TryGetUserConfig(connectEvent.LoginUid, out var userConfig))
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
        Log.Info("Skadi初始化", "reg QA serv");
        StaticStuff.Services.AddSingleton<IQaService>(new QaService(connectEvent.LoginUid));

        //初始化定时器线程
        if (userConfig.ModuleSwitch.BiliSubscription)
            SubscriptionTimer.TimerEventAdd(connectEvent);

        IsInit = true;

        return ValueTask.CompletedTask;
    }
}