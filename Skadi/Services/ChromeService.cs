using System.Threading.Tasks;
using PuppeteerSharp;

namespace Skadi.Services;

//TODO IOC
public class ChromeService
{
    private Browser _browser { get; }

    public ChromeService()
    {
        Task initTask = Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless          = true,
            IgnoreHTTPSErrors = true,
            Timeout           = 60000,
            Args              = new[] { "--no-sandbox" }
        });
        initTask.Wait();
    }

    public void Dispose()
    {

    }
}