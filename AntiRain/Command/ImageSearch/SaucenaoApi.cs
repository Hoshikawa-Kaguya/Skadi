using AntiRain.Config;
using AntiRain.Tool;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PyLibSharp.Requests;
using SixLabors.ImageSharp;
using Sora.Entities;
using Sora.Entities.Segment;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YukariToolBox.FormatLog;

namespace AntiRain.Command.ImageSearch;

public static class SaucenaoApi
{
    public static async ValueTask<MessageBody> SearchByUrl(string apiKey, string url, long selfId)
    {
        Log.Debug("pic", "send api request");
        ReqResponse serverResponse;
        try
        {
            serverResponse = await
                Requests.PostAsync($"http://saucenao.com/search.php?output_type=2&numres=16&db=999&api_key={apiKey}&url={url}",
                                   new ReqParams
                                   {
                                       Timeout                = 20000,
                                       IsThrowErrorForTimeout = false
                                   });
        }
        catch (Exception e)
        {
            Log.Error("NetError", Log.ErrorLogBuilder(e));
            return $"服务器网络错误{e.Message}";
        }

        var res     = serverResponse?.Json();
        var resCode = Convert.ToInt32(res?["header"]?["status"] ?? -1);
        Log.Debug("pic", $"get api result code [{resCode}]");

        //API返回失败
        if (serverResponse is null || res is null || resCode != 0) return "图片获取失败";

        //API返回空值
        if (res["results"] is not JArray resData) return "处理API返回发生错误";

        //未找到图片
        if (resData.Count == 0) return "查找结果为空";

        var parsedPic = resData.First();

        if (!ConfigManager.TryGetUserConfig(selfId, out var userConfig))
        {
            //用户配置获取失败
            Log.Error("Config|SaucenaoApi", "无法获取用户配置文件");
            return "处理用户配置发生错误\r\nMessage:无法读取用户配置";
        }

        switch (Convert.ToInt32(parsedPic["header"]?["index_id"]))
        {
            //pixiv
            case 5:
            {
                var pid      = Convert.ToInt64(parsedPic["data"]?["pixiv_id"]);
                var imageUrl = GenPixivUrl(userConfig.HsoConfig.PximyProxy, pid);
                return Util.GenPixivResult(imageUrl, pid, parsedPic);
            }
            //twitter
            case 41:
            {
                var tweetUrl = parsedPic["data"]?["ext_urls"]?[0]?.ToString();
                return string.IsNullOrEmpty(tweetUrl)
                    ? "服务器歇逼了（无法获取推文链接，请稍后再试）"
                    : Util.GenTwitterResult(tweetUrl, userConfig.TwitterApiToken, parsedPic);
            }
            //eh
            case 38:
                return "[Saucenao-EHentai]"                         +
                       $"\r\nSource:{parsedPic["data"]?["source"]}" +
                       $"\r\nName:{parsedPic["data"]?["jp_name"]}"  +
                       $"\r\n[{parsedPic["header"]?["similarity"]}%]";
            case 18:
                return "[Saucenao-NHentai]"                         +
                       $"\r\nSource:{parsedPic["data"]?["source"]}" +
                       $"\r\nName:{parsedPic["data"]?["jp_name"]}"  +
                       $"\r\n[{parsedPic["header"]?["similarity"]}%]";
            //unknown
            default:
            {
                //检查源
                var source = parsedPic["data"]?["source"]?.ToString() ?? string.Empty;

                //包含pixiv链接
                if (source.IndexOf("pixiv", StringComparison.Ordinal) != -1)
                {
                    var pid      = Convert.ToInt64(Path.GetFileName(source));
                    var imageUrl = GenPixivUrl(userConfig.HsoConfig.PximyProxy, pid);
                    return Util.GenPixivResult(imageUrl, pid, parsedPic);
                }

                //包含twitter链接
                if (source.IndexOf("twitter", StringComparison.Ordinal) != -1)
                    return Util.GenTwitterResult(source, userConfig.TwitterApiToken, parsedPic);

                var msg = new MessageBody();
                msg += $"[Saucenao-UnknownDatabase]\r\n{parsedPic["header"]?["index_name"]}";
                var b64Pic =
                    BotUtils.DrawTextImage(parsedPic["data"]?.ToString(Formatting.Indented) ?? string.Empty,
                                           Color.Black, Color.White);
                msg += SoraSegment.Image($"base64://{b64Pic}");
                msg += $"\r\n[{parsedPic["header"]?["similarity"]}%]";

                return msg;
            }
        }
    }

    private static string GenPixivUrl(string proxy, long pid)
    {
        return string.IsNullOrEmpty(proxy)
            ? $"https://pixiv.lancercmd.cc/{pid}"
            : $"{proxy.Trim('/')}/{pid}";
    }
}