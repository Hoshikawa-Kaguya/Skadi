using System.Threading.Tasks;
using Sora.EventArgs.SoraEvent;
using Sora.Tool;
using SuiseiBot.IO.Config;
using SuiseiBot.IO.Config.ConfigModule;

namespace SuiseiBot.SuiseiInterface
{
    /// <summary>
    /// 初始化事件
    /// </summary>
    internal static class InitalizationEvent
    {
        /// <summary>
        /// 初始化处理
        /// </summary>
        internal static ValueTask Initalization(object sender, ConnectEventArgs connectEvent)
        {
            ConsoleLog.Info("SuiseiBot初始化","与onebot客户端连接成功，初始化资源...");
            //初始化配置文件
            ConsoleLog.Info("SuiseiBot初始化",$"初始化用户[{connectEvent.GetLoginUserId()}]配置");
            Config config = new Config(connectEvent.GetLoginUserId());
            config.UserConfigFileInit();
            config.LoadUserConfig(out UserConfig userConfig, false);


            //在控制台显示启用模块
            ConsoleLog.Info("已启用的模块",
                            $"\n{userConfig.ModuleSwitch}");
            //显示代理信息
            if (userConfig.ModuleSwitch.Hso && !string.IsNullOrEmpty(userConfig.HsoConfig.PximyProxy))
            {
                ConsoleLog.Debug("Hso Proxy", userConfig.HsoConfig.PximyProxy);
            }

            //TODO 数据库
            //TODO 计时器

            return ValueTask.CompletedTask;
        }
    }
}
