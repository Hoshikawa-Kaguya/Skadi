using System;
using System.Threading.Tasks;
using AntiRain.DatabaseUtils;
using AntiRain.IO;
using AntiRain.TimerEvent;
using Sora.EventArgs.SoraEvent;
using YukariToolBox.FormatLog;

namespace AntiRain.ServerInterface
{
    /// <summary>
    /// 初始化事件
    /// </summary>
    internal static class InitalizationEvent
    {
        /// <summary>
        /// 初始化处理
        /// </summary>
        internal static ValueTask Initalization(string _, ConnectEventArgs connectEvent)
        {
            Log.Info("AntiRain初始化", "与onebot客户端连接成功，初始化资源...");
            //初始化配置文件
            Log.Info("AntiRain初始化", $"初始化用户[{connectEvent.LoginUid}]的配置");
            if (!ConfigManager.UserConfigFileInit(connectEvent.LoginUid) ||
                !ConfigManager.TryGetUserConfig(connectEvent.LoginUid, out var userConfig))
            {
                Log.Fatal("AntiRain初始化", "无法获取用户配置文件");
                Environment.Exit(-1);
                return ValueTask.CompletedTask;
            }

            //在控制台显示启用模块
            Log.Info("已启用的模块",
                     $"\n{userConfig.ModuleSwitch}");
            //显示代理信息
            if (userConfig.ModuleSwitch.Hso && !string.IsNullOrEmpty(userConfig.HsoConfig.PximyProxy))
            {
                Log.Debug("Hso Proxy", userConfig.HsoConfig.PximyProxy);
            }

            //初始化数据库
            DatabaseInit.UserDataInit(connectEvent);

            //初始化定时器线程
            if (userConfig.ModuleSwitch.Bili_Subscription)
            {
                SubscriptionTimer.TimerEventAdd(connectEvent);
            }

            return ValueTask.CompletedTask;
        }
    }
}