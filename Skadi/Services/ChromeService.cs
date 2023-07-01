using System;
using System.IO;
using System.Threading.Tasks;
using PuppeteerSharp;
using Skadi.Interface;
using Sora.Entities.Segment;
using Sora.Util;
using YukariToolBox.LightLog;

namespace Skadi.Services;

public class ChromeService : IChromeService, IDisposable
{
    private IBrowser _browser { get; }

    public ChromeService()
    {
        Log.Debug("Chrome", "Browser Start");
        Task<IBrowser> initTask = Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless          = true,
            IgnoreHTTPSErrors = true,
            Timeout           = 60000,
            Args              = new[] { "--no-sandbox" }
        });
        try
        {
            initTask.Wait();
            _browser = initTask.Result;
        }
        catch (Exception e)
        {
            Log.Error(e, "Chrome", "chrome start failed");
            _browser = null;
        }
    }

    public async Task<SoraSegment> GetChromeXPathPic(string url, string xpath)
    {
        if (_browser is null) return "chrome 错误";
        IPage  page = await _browser.NewPageAsync();
        string dId  = Path.GetFileName(url);
        Log.Debug("动态ID", dId);
        await page.SetViewportAsync(new ViewPortOptions
        {
            Width  = 2000,
            Height = 1500
        });
        await page.SetUserAgentAsync("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/113.0.0.0 Safari/537.36");

        Exception exception = null;

        await page.GoToAsync(url).RunCatch(e => exception = e);
        await Task.Delay(1000);

        IElementHandle element = await page.WaitForXPathAsync(xpath).RunCatch(e =>
        {
            exception = e;
            return null;
        });

        if (exception is not null)
        {
            Log.Error(exception, "Chrome", $"Url:{url}");
            return "浏览器错误";
        }

        if (element is null)
        {
            Log.Debug("Chrome", "无法获取XPath元素内容");
            return "404";
        }

        Log.Debug("Chrome", $"获取到XPath元素[{element.RemoteObject.ObjectId}]");

        string picB64 = await element.ScreenshotBase64Async(new ScreenshotOptions { Type = ScreenshotType.Png });

        //关闭页面
        await page.CloseAsync();
        await page.DisposeAsync();

        SoraSegment img = SoraSegment.Image($"base64://{picB64}");
        return img;
    }

    public async Task<SoraSegment> GetChromePagePic(string url, bool all)
    {
        if (_browser is null) return "chrome 错误";
        IPage page = await _browser.NewPageAsync();
        await page.SetViewportAsync(new ViewPortOptions
        {
            Width  = 1920,
            Height = 1080
        });

        Exception exception = null;

        await page.GoToAsync(url).RunCatch(e => exception = e);
        await Task.Delay(1000);

        if (exception is not null)
        {
            Log.Error(exception, "Chrome", $"Url:{url}");
            return "浏览器错误";
        }

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
        Log.Debug("Chrome", "Browser Stop");
        await _browser.CloseAsync();
        _browser.Dispose();
    }
}