using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using PyLibSharp.Requests;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Skadi.Entities.ConfigModule;
using Skadi.Interface;
using Skadi.Resource;
using Sora.Entities.Segment;
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

    public static async ValueTask<SoraSegment> GetPixivImage(long loginUid, long pid, int index, string server, bool checkSsl)
    {
        IGenericStorage genericStorage = SkadiApp.GetService<IGenericStorage>();
        UserConfig      userConfig     = genericStorage.GetUserConfig(loginUid);

        if (userConfig?.HsoConfig.YukariApiKey is null)
        {
            Log.Error("Config|Hso", "无法获取用户配置文件");
            return "ERR:无法获取用户配置文件";
        }

        //处理图片信息
        //TODO 使用服务器返回的信息，而不再使用pixiv API代理
        (int statusCode, bool r18, int count) = GetPixivImgInfo(Convert.ToInt64(pid), server, checkSsl, out JToken data);

        switch (statusCode)
        {
            case 200:
                break;
            case 400:
                return $"""
                    http code:{statusCode}
                    pixiv api err:{data}
                    """;
            default:
                return $"哇哦，发生了网络错误[{statusCode}]";
        }

        if (r18) return SoraSegment.Image(new MemoryStream(ImageResourse.R18_NO));

        if (index > count - 1) return "没有这张色图欸(404)";


        Log.Info("Pixiv", $"download image with token:{userConfig.HsoConfig.YukariApiKey}");
        
        string imageUrl = $"{server}/pixiv/{pid}/{index}";
        ReqResponse response = await Requests.GetAsync(imageUrl,
                                                       new ReqParams
                                                       {
                                                           isCheckSSLCert = checkSsl,
                                                           Timeout = 20000,
                                                           Header = new Dictionary<HttpRequestHeader, string>
                                                           {
                                                               { HttpRequestHeader.Authorization, $"Bearer {userConfig.HsoConfig.YukariApiKey}"}
                                                           }
                                                       });
        if (response.StatusCode != HttpStatusCode.OK)
            return $"代理服务器错误{response.StatusCode}";

        MemoryStream image = new(response.Content);
        Log.Info("Pixiv", $"image len:{image.Length}");
        return SoraSegment.Image(image);
    }

    public static async ValueTask<List<SoraSegment>> GetMultiPixivImage(long loginUid, long pid, string server, bool checkSsl)
    {
        //处理图片信息
        (_, _, int count) = GetPixivImgInfo(Convert.ToInt64(pid), server, checkSsl, out _);
        //发送一次错误信息
        if (count == 0) count = 1;

        var pixivImages = new List<SoraSegment>();
        //TODO
        // for (int i = 0; i < count; i++)
        //     pixivImages.Add(await GetPixivImage(loginUid, pid, i));

        return pixivImages;
    }

    public static (int statusCode, bool r18, int count) GetPixivImgInfo(long pid, string server, bool checkSsl, out JToken json)
    {
        Log.Debug("pixiv api", "sending illust info request");
        try
        {
            ReqResponse pixApiReq = Requests.Get($"{server}/pixiv/illust?pid={pid}",
                                                 new ReqParams
                                                 {
                                                     Timeout                   = 5000,
                                                     isCheckSSLCert = checkSsl,
                                                     IsThrowErrorForTimeout    = false,
                                                     IsThrowErrorForStatusCode = false
                                                 });

            Log.Debug("pixiv api", $"get illust info response({pixApiReq.StatusCode})");
            if (pixApiReq.StatusCode != HttpStatusCode.OK)
            {
                json = pixApiReq.Json();
                return ((int)pixApiReq.StatusCode, false, 0);
            }

            JToken infoJson = pixApiReq.Json();
            json = infoJson;
            return (200,
                Convert.ToBoolean(infoJson["data"]?["illust"]?["x_restrict"]),
                Convert.ToInt32(infoJson["data"]?["illust"]?["page_count"]));
        }
        catch (Exception e)
        {
            Log.Error(e, "GetPixivImg", "can not get illust info");
            json = null;
            return (-1, false, 0);
        }
    }

#endregion

#region 图片绘制

    /// <summary>
    /// 绘制文字图片
    /// </summary>
    public static string DrawTextImage(string text, Color fontColor, Color backColor, int frameSize = 5)
    {
        //计算图片大小
        FontRectangle strRect = TextMeasurer.MeasureSize(text, new TextOptions(Arial));
        //图片大小
        (int width, int height) = ((int)strRect.Width + frameSize * 2, (int)strRect.Height + frameSize * 2);
        //创建图片
        using Image<Rgba32> img = new(width, height);
        //绘制
        img.Mutate(x =>
                       x.Fill(backColor)
                        // ReSharper disable once PossibleLossOfFraction
                        .DrawText(text, Arial, fontColor, new PointF(frameSize, frameSize / 2 - 1)));
        //转换base64
        using var byteStream = new MemoryStream();
        img.Save(byteStream, PngFormat.Instance);

        return byteStream.Length != 0
            ? Convert.ToBase64String(byteStream.GetBuffer(), 0, (int)byteStream.Length)
            : string.Empty;
    }

#endregion
}