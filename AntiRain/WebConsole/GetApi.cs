using System.Threading.Tasks;
using BeetleX.FastHttpApi;

namespace AntiRain.WebConsole
{
    [Controller]
    public class GetApi
    {
        [Get]
        public Task<object> Test(IHttpContext context)
        {
            return Task.FromResult<object>("好耶");
        }
    }
}
