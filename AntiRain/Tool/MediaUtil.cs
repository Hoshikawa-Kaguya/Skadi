using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using AntiRain.Resource;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PyLibSharp.Requests;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Sora.Entities.Segment;
using YukariToolBox.LightLog;

namespace AntiRain.Tool;

internal static class MediaUtil
{
    #region 静态资源

    private static Font font { get; }

    static MediaUtil()
    {
        //加载字体
        Log.Debug("Font","Init font");
        using var fontMs         = new MemoryStream(FontRes.JetBrainsMono);
        var       fontCollection = new FontCollection();
        var       fontFamily     = fontCollection.Install(fontMs);
        font = fontFamily.CreateFont(24);
    }

    #endregion

    #region Pixiv图片消息段生成

    public static string GenPixivUrl(string proxy, long pid, int index = 0)
        => string.IsNullOrEmpty(proxy)
            ? $"https://pixiv.lancercmd.cc/{pid}"
            : $"{proxy.Trim('/')}/{pid}/{index}";

    public static SoraSegment GetPixivImg(long pid, string proxyUrl)
    {
        try
        {
            var pixApiReq = Requests.Get($"https://pixiv.yukari.one/api/illust/{pid}",
                                         new ReqParams
                                         {
                                             Timeout                   = 5000,
                                             IsThrowErrorForTimeout    = false,
                                             IsThrowErrorForStatusCode = false
                                         });
            var imgSegment = SoraSegment.Image(proxyUrl);

            if (pixApiReq.StatusCode == HttpStatusCode.OK)
            {
                var infoJson = pixApiReq.Json();
                if (Convert.ToBoolean(infoJson["error"]))
                    return SoraSegment.Text($"[ERROR:网络错误，无法获取图片详细信息({infoJson["message"]})]");
                return Convert.ToBoolean(infoJson["body"]?["xRestrict"])
                    ? SoraSegment.Text("[H是不行的]")
                    : imgSegment;
            }

            return SoraSegment.Text($"[ERROR:网络错误，无法获取图片详细信息({pixApiReq.StatusCode})]");
        }
        catch (Exception e)
        {
            Log.Error(e, "GetPixivImg", "can not get illust info");
            return SoraSegment.Text($"[ERROR:网络错误，无法获取图片详细信息({e.Message})]");
        }
    }

    #endregion

    #region 推特API处理

    /// <summary>
    /// 获取推特推文信息
    /// </summary>
    /// <param name="tweetId">推文ID</param>
    /// <param name="token">api key v2</param>
    internal static (bool success, string sender, string text, List<string> media)
        GetTweet(string tweetId, string token)
    {
        Log.Info("Twitter", $"Get tweet by id[{tweetId}]");

        JToken data;
        try
        {
            var res = Requests.Get($"https://api.twitter.com/2/tweets/{tweetId}",
                                   new ReqParams
                                   {
                                       Params = new Dictionary<string, string>
                                       {
                                           {"expansions", "attachments.media_keys,author_id"},
                                           {"media.fields", "url"}
                                       },
                                       Header = new Dictionary<HttpRequestHeader, string>
                                       {
                                           {HttpRequestHeader.Authorization, $"Bearer {token}"}
                                       },
                                       IsThrowErrorForStatusCode = false,
                                       IsThrowErrorForTimeout    = false
                                   });

            Log.Info("Twitter", $"Twitter api http code:{res.StatusCode}");
            if (res is not {StatusCode: HttpStatusCode.OK})
            {
                Log.Error("Twitter", "Twitter api net error");
                return (false, string.Empty, $"Twitter api net error [{res.StatusCode}]", null);
            }

            data = res.Json();
        }
        catch (Exception e)
        {
            Log.Error(e, "Twitter API", "调用推特API时发生网络错误");
            return (false, null, "调用推特api时发生网路错误", null);
        }


        Log.Debug("Twitter", $"Get twitter api data:{data.ToString(Formatting.None)}");
        var urls = data["includes"]?["media"]?
                   .Where(m => m["type"]?.ToString() == "photo")
                   .Select(t => t["url"]?.ToString() ?? string.Empty)
                   .ToList() ?? new List<string>();
        urls.RemoveAll(string.IsNullOrEmpty);
        if (urls.Count == 0)
        {
            Log.Error("Twitter", "Twitter api no content(no url)");
            return (false, string.Empty, "Twitter api no content(no url)", null);
        }

        var authorName = data["includes"]?["users"]
                         ?.First(t => t["id"]?.ToString() == data["data"]?["author_id"]?.ToString())
                         ["name"]?.ToString() ?? string.Empty;
        Log.Info("Twitter", $"Get twitter image [count:{urls.Count}]");
        return (true,
                authorName,
                data["data"]?["text"]?.ToString() ?? string.Empty,
                urls);
    }

    #endregion

    #region 图片绘制

    /// <summary>
    /// 绘制文字图片
    /// </summary>
    public static string DrawTextImage(string text, Color fontColor, Color backColor, int frameSize = 5)
    {
        //计算图片大小
        var strRect = TextMeasurer.Measure(text, new RendererOptions(font));
        //图片大小
        var (width, height) = ((int) strRect.Width + frameSize * 2, (int) strRect.Height + frameSize * 2);
        //创建图片
        var img = new Image<Rgba32>(width, height);
        //绘制
        img.Mutate(x => x.Fill(backColor)
                         .DrawText(text, font, fontColor,
                                   new PointF(frameSize, frameSize / 2 - 1)));
        //转换base64
        using var byteStream = new MemoryStream();
        img.Save(byteStream, PngFormat.Instance);
        img.Dispose();

        return byteStream.Length != 0 
            ? Convert.ToBase64String(byteStream.GetBuffer(), 0, (int) byteStream.Length) 
            : string.Empty;
    }

    #endregion
}