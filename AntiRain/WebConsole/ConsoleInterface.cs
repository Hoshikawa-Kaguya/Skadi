using BeetleX.EventArgs;
using BeetleX.FastHttpApi;
using Sora.Tool;

namespace AntiRain.WebConsole
{
    internal class ConsoleInterface
    {
        #region 属性
        private HttpApiServer AntiRainApiServer { get; set; }
        #endregion

        #region 构造函数
        internal ConsoleInterface(string location, int port)
        {
            AntiRainApiServer                      = new HttpApiServer();
            AntiRainApiServer.Options.Host         = location;
            AntiRainApiServer.Options.Port         = port;
            AntiRainApiServer.Options.LogLevel     = LogType.Off;
            AntiRainApiServer.Options.LogToConsole = false;
            AntiRainApiServer.Options.Debug        = false;
            AntiRainApiServer.Options.CrossDomain = new OptionsAttribute
            {
                AllowOrigin = "*"
            };
            AntiRainApiServer.Register(typeof(UtilApi).Assembly);
            AntiRainApiServer.Open();
            ConsoleLog.Debug("AntiRain初始化",$"AntiRain API服务正在运行[{location}:{port}]");
        }
        #endregion
    }
}
