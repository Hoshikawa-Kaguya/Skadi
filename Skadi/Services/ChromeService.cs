using System;
using System.IO;
using System.Threading.Tasks;
using PuppeteerSharp;
using Sora.Entities.Segment;
using YukariToolBox.LightLog;

namespace Skadi.Services;

//TODO IOC
public class ChromeService : IChromeService, IDisposable
{
    private IBrowser _browser { get; }

    public ChromeService()
    {
        Log.Info("Chrome", "Browser Start");
        Task<IBrowser> initTask = Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless          = true,
            IgnoreHTTPSErrors = true,
            Timeout           = 60000,
            Args              = new[] { "--no-sandbox" }
        });
        initTask.Wait();
        _browser = initTask.Result;
    }

    public async Task<SoraSegment> GetChromeXPathPic(string url, string xpath)
    {
        IPage  page = await _browser.NewPageAsync();
        string dId  = Path.GetFileName(url);
        Log.Debug("动态ID", dId);
        await page.SetViewportAsync(new ViewPortOptions
        {
            Width  = 2000,
            Height = 1500
        });

        await page.GoToAsync(url);

        //动态
        IElementHandle dyElement = await page.WaitForXPathAsync(xpath);

        if (dyElement is null)
        {
            Log.Debug("Chrome", "无法获取XPath元素内容");
            return "404";
        }

        Log.Debug("Chrome", $"获取到XPath元素[{dyElement.RemoteObject.ObjectId}]");

        string picB64 = await dyElement.ScreenshotBase64Async(new ScreenshotOptions { Type = ScreenshotType.Png });

        //关闭页面
        await page.CloseAsync();
        await page.DisposeAsync();

        SoraSegment img = SoraSegment.Image($"base64://{picB64}");
        return img;
    }

    public async Task<SoraSegment> GetChromePagePic(string url, bool all)
    {
        IPage page = await _browser.NewPageAsync();
        await page.SetViewportAsync(new ViewPortOptions
        {
            Width  = 1920,
            Height = 1080
        });

        await page.GoToAsync(url);

        Log.Info("Curl", "生成截图...");
        string picB64 = await page.ScreenshotBase64Async(new ScreenshotOptions
        {
            FullPage = all,
            Type     = ScreenshotType.Png
        });

        //关闭页面
        await page.CloseAsync();
        await page.DisposeAsync();

        SoraSegment img = SoraSegment.Image($"base64://{picB64}");
        return img;
    }

    public async void Dispose()
    {
        Log.Info("Chrome", "Browser Stop");
        await _browser.CloseAsync();
        _browser.Dispose();
    }
}