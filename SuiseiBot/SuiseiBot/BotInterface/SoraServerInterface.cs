using System.Threading.Tasks;
using Fleck;
using Sora;
using Sora.Tool;

namespace SuiseiBot.BotInterface
{
    static class SoraServerInterface
    {
        static async Task Main(string[] args)
        {
            ConsoleLog.Info("SuiseiBot","SuiseiBot初始化...");
            ConsoleLog.SetLogLevel(LogLevel.Debug);
            SoraWSServer server = new SoraWSServer(new ServerConfig());

            //服务器回调
            //初始化
            server.Event.OnClientConnect += InitalizationEvent.Initalization;

            await server.StartServerAsync();
        }
    }
}
