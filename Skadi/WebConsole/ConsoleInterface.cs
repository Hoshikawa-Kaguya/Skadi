using System.Reflection;
using BeetleX.EventArgs;
using BeetleX.FastHttpApi;
using YukariToolBox.LightLog;

namespace Skadi.WebConsole
{
    internal class ConsoleInterface
    {
        #region 属性

        private HttpApiServer SkadiApiServer { get; set; }

        #endregion

        #region 构造函数

        internal ConsoleInterface(string location, int port)
        {
            SkadiApiServer                      = new HttpApiServer();
            SkadiApiServer.Options.Host         = location;
            SkadiApiServer.Options.Port         = port;
            SkadiApiServer.Options.LogLevel     = LogType.Off;
            SkadiApiServer.Options.LogToConsole = false;
            SkadiApiServer.Options.Debug        = false;
            SkadiApiServer.Options.CrossDomain = new OptionsAttribute
            {
                AllowOrigin = "*"
            };
            SkadiApiServer.Register(Assembly.GetExecutingAssembly());
            SkadiApiServer.Open();
            Log.Debug("Skadi初始化", $"Skadi API服务正在运行[{location}:{port}]");
        }

        #endregion
    }
}