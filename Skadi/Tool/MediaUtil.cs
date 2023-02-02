using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PyLibSharp.Requests;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Skadi.Config;
using Sora.Entities;
using Sora.Entities.Segment;
using Sora.Entities.Segment.DataModel;
using Sora.Enumeration.ApiType;
using Sora.EventArgs.SoraEvent;
using YukariToolBox.LightLog;

namespace Skadi.Tool;

internal static class MediaUtil
{
#region 静态资源

    private static Font Arial { get; }

    static MediaUtil()
    {
        //加载字体
        Log.Debug("Arial Font", "Init font");
        using var arialFontMs         = new MemoryStream(FontResource.Deng);
        var       arialFontCollection = new FontCollection();
        var       arialFontFamily     = arialFontCollection.Add(arialFontMs);
        Arial = arialFontFamily.CreateFont(35);
    }

#endregion

#region Pixiv图片消息段生成

    public static async Task SendPixivImageMessage(this GroupMessageEventArgs eventArgs, long pid, int index)
    {
        if (!ConfigManager.TryGetUserConfig(eventArgs.LoginUid, out var userConfig))
        {
            Log.Error("Config|Hso", "无法获取用户配置文件");
            await eventArgs.Reply("ERR:无法获取用户配置文件");
            return;
        }

        //处理图片代理连接
        string imageUrl;
        if (!string.IsNullOrEmpty(userConfig.HsoConfig.PximyProxy))
        {
            imageUrl = $"{userConfig.HsoConfig.PximyProxy.Trim('/')}/{pid}";
            Log.Debug("Hso", $"Get proxy url {imageUrl}");
        }
        else
        {
            Log.Warning("Hso", "未设置代理服务器");
            return;
        }

        (int statusCode, bool r18, int count) = GetPixivImgInfo(Convert.ToInt64(pid), out _);

        if (statusCode != 200)
        {
            await eventArgs.Reply($"哇哦，发生了网络错误[{statusCode}]");
            return;
        }

        if (r18)
        {
            await eventArgs.Reply(SoraSegment.Image(new MemoryStream(ImageResourse.R18_NO)));
            return;
        }

        ApiStatus apiStatus;

        if (index == -1 && count > 1)
        {
            var customNodes = new List<CustomNode>();
            for (int i = 0; i < count; i++)
                customNodes.Add(new CustomNode("色色",
                                               114514,
                                               SoraSegment.Image($"{imageUrl}/{i}", true, 4)));

            (apiStatus, _, _) =
                await eventArgs.SourceGroup.SendGroupForwardMsg(customNodes, TimeSpan.FromMinutes(2));
        }
        else
        {
            if ((index > 0 && count <= 1) || index > count - 1)
            {
                await eventArgs.Reply("没有这张色图欸(404)");
                return;
            }

            (apiStatus, _) =
                await eventArgs.Reply(SoraSegment.Image(imageUrl, true, 4), TimeSpan.FromMinutes(2));
        }

        if (apiStatus.RetCode != ApiStatusType.Ok)
            await eventArgs.Reply("图被夹了啊啊啊啊啊啊啊啊啊啊啊");
    }

    public static string GenPixivUrl(string proxy, long pid, int index = 0)
    {
        return $"{proxy.Trim('/')}/{pid}/{index}";
    }

    public static (int statusCode, bool r18, int count) GetPixivImgInfo(long pid, out JToken json)
    {
        Log.Debug("pixiv api", "sending illust info request");
        json = null;
        try
        {
            var pixApiReq = Requests.Get($"https://pixiv.yukari.one/api/illust/{pid}",
                                         new ReqParams
                                         {
                                             Timeout                   = 5000,
                                             IsThrowErrorForTimeout    = false,
                                             IsThrowErrorForStatusCode = false
                                         });

            Log.Debug("pixiv api", $"get illust info response({pixApiReq.StatusCode})");
            if (pixApiReq.StatusCode != HttpStatusCode.OK) 
                return ((int)pixApiReq.StatusCode, false, 0);

            JToken infoJson = pixApiReq.Json();
            json = infoJson;
            return (200,
                Convert.ToBoolean(infoJson["illust"]?["x_restrict"]),
                Convert.ToInt32(infoJson["illust"]?["page_count"]));

        }
        catch (Exception e)
        {
            Log.Error(e, "GetPixivImg", "can not get illust info");
            return (-1, false, 0);
        }
    }

#endregion

#region 推特API处理

    /// <summary>
    /// 获取推特推文信息
    /// </summary>
    /// <param name="tweetId">推文ID</param>
    internal static (bool success, string sender, string text, List<string> media)
        GetTweet(string tweetId)
    {
        Log.Info("Twitter", $"Get tweet by id[{tweetId}]");

        JToken data;
        try
        {
            var res = Requests.Get($"https://pixiv.yukari.one/api/tweet/{tweetId}",
                                   new ReqParams
                                   {
                                       IsThrowErrorForStatusCode = false,
                                       IsThrowErrorForTimeout    = false,
                                       Timeout                   = 10000
                                   });

            Log.Info("Twitter", $"Twitter api http code:{res.StatusCode}");
            if (res is not { StatusCode: HttpStatusCode.OK })
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
                   .ToList()
                   ?? new List<string>();
        urls.RemoveAll(string.IsNullOrEmpty);
        if (urls.Count == 0)
        {
            Log.Error("Twitter", "Twitter api no content(no url)");
            return (false, string.Empty, "Twitter api no content(no url)", null);
        }

        var authorName = data["includes"]?["users"]
                         ?.First(t => t["id"]?.ToString() == data["data"]?["author_id"]?.ToString())
                         ["name"]?.ToString()
                         ?? string.Empty;
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
        FontRectangle strRect = TextMeasurer.Measure(text, new TextOptions(Arial));
        //图片大小
        (int width, int height) = ((int)strRect.Width + frameSize * 2, (int)strRect.Height + frameSize * 2);
        //创建图片
        using Image<Rgba32> img = new(width, height);
        //绘制
        img.Mutate(x =>
                       x.Fill(backColor)
                        .DrawText(text, Arial, fontColor, new PointF(frameSize, frameSize / 2 - 1)));
        //转换base64
        using var byteStream = new MemoryStream();
        img.Save(byteStream, PngFormat.Instance);
        img.Dispose();

        return byteStream.Length != 0
            ? Convert.ToBase64String(byteStream.GetBuffer(), 0, (int)byteStream.Length)
            : string.Empty;
    }

#endregion
}