using System.Threading.Tasks;
using BeetleX.FastHttpApi;
using YukariToolBox.LightLog;

namespace AntiRain.WebConsole
{
    [Controller]
    public class UtilApi
    {
        [Get]
        public Task<object> Test(IHttpContext context)
        {
            Log.Debug("UtilApi", $"Get Test api request from {context.Request.RemoteEndPoint}");
            return Task.FromResult<object>(new TextResult("好耶", true));
        }
    }
}