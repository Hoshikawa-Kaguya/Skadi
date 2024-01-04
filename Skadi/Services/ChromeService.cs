using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
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

    public async Task<SoraSegment> GetChromeSelectorPic(string url, string selector)
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
        await
            page.SetUserAgentAsync("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/113.0.0.0 Safari/537.36");

        Exception exception = null;

        await page.GoToAsync(url).RunCatch(e => exception = e);
        await page.WaitForNavigationAsync(new NavigationOptions
        {
            WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
        });

        IElementHandle element = await page.QuerySelectorAsync(selector).RunCatch(e =>
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

    public async Task InitBilibili()
    {
        if (_browser is null) return;
        IPage homePage = await _browser.NewPageAsync();
        await homePage.GoToAsync("https://space.bilibili.com/1").RunCatch(e => throw e);
        await Task.Delay(1000);
    }

    public async Task<(ulong, long)> GetBilibiliDynamic(long uid)
    {
        if (_browser is null) return (0, 0);

        IPage apiPage = await _browser.NewPageAsync();
        await apiPage.GoToAsync($"https://api.bilibili.com/x/polymer/web-dynamic/v1/feed/space?host_mid={uid}");
        string jsonStr = (await (await apiPage.QuerySelectorAsync("body > pre")).GetPropertyAsync("innerText"))
                         .RemoteObject.Value.ToString();
        JObject json = JObject.Parse(jsonStr);

        bool haveTop = !string.IsNullOrEmpty(json.SelectToken("data.items[0].modules.module_tag.text")?.ToString());
        int  index   = haveTop ? 1 : 0;

        ulong dId = Convert.ToUInt64(json.SelectToken($"data.items[{index}].id_str") ?? 0);
        long  dTs = Convert.ToInt64(json.SelectToken($"data.items[{index}].modules.module_author.pub_ts") ?? 0);

        return (dId, dTs);
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
        await page.WaitForNavigationAsync(new NavigationOptions
        {
            WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
        });

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