using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using AntiRain.Config;
using AntiRain.Tool;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PyLibSharp.Requests;
using SixLabors.ImageSharp;
using Sora.Entities;
using Sora.Entities.Segment;
using YukariToolBox.LightLog;

namespace AntiRain.Command.ImageSearch;

public static class SaucenaoApi
{
    private readonly struct DbIndex
    {
        internal const int PIXIV    = 5;
        internal const int TWITTER  = 41;
        internal const int E_HENTAI = 38;
        internal const int N_HENTAI = 18;
    }

    public static async ValueTask<MessageBody> SearchByUrl(string apiKey, string url, long selfId)
    {
        Log.Debug("pic search", "send api request");
        JToken res;
        try
        {
            var serverResponse = await
                Requests.PostAsync(
                    $"http://saucenao.com/search.php?output_type=2&numres=16&db=999&api_key={apiKey}&url={HttpUtility.UrlEncode(url)}",
                    new ReqParams
                    {
                        Timeout                = 20000,
                        IsThrowErrorForTimeout = false
                    });
            res = serverResponse.Json();
        }
        catch (Exception e)
        {
            Log.Error("NetError", Log.ErrorLogBuilder(e));
            return $"服务器网络错误[{e.Message}]";
        }

        var resCode = Convert.ToInt32(res?["header"]?["status"] ?? -1);
        Log.Debug("pic", $"get api result code [{resCode}]");

        //API返回失败
        if (res is null || resCode != 0) return "图片获取失败";

        //API返回空值
        if (res["results"] is not JArray resData) return "处理API返回发生错误";

        //未找到图片
        if (resData.Count == 0) return "查找结果为空";

        JToken parsedPic = resData.OrderByDescending(t => Convert.ToSingle(t["header"]?["similarity"])).First();

        JToken pixivPic = resData.Where(t => Convert.ToInt32(t["header"]?["index_id"]) == DbIndex.PIXIV)
                                 .MaxBy(t => Convert.ToSingle(t["header"]?["similarity"]));

        JToken twitterPic = resData.Where(t => Convert.ToInt32(t["header"]?["index_id"]) == DbIndex.TWITTER)
                                   .MaxBy(t => Convert.ToSingle(t["header"]?["similarity"]));


        if (!ConfigManager.TryGetUserConfig(selfId, out var userConfig))
        {
            //用户配置获取失败
            Log.Error("Config|SaucenaoApi", "无法获取用户配置文件");
            return $"处理用户配置发生错误{Environment.NewLine}Message:无法读取用户配置";
        }

        //优先pixiv
        if (pixivPic is not null &&
            Math.Abs(Convert.ToSingle(pixivPic["header"]?["similarity"]) -
                Convert.ToSingle(parsedPic["header"]?["similarity"])) <= 5)
        {
            var pid      = Convert.ToInt64(pixivPic["data"]?["pixiv_id"]);
            Log.Info("SaucenaoApi", $"获取到pixiv图片[{pid}]");
            var imageUrl = MediaUtil.GenPixivUrl(userConfig.HsoConfig.PximyProxy, pid);
            return GenPixivResult(imageUrl, pid, parsedPic);
        }

        //优先推特
        if (twitterPic is not null &&
            Math.Abs(Convert.ToSingle(twitterPic["header"]?["similarity"]) -
                Convert.ToSingle(parsedPic["header"]?["similarity"])) <= 5)
        {
            var tweetUrl = twitterPic["data"]?["ext_urls"]?[0]?.ToString();
            Log.Info("SaucenaoApi", $"获取到tweet[{tweetUrl}]");
            return string.IsNullOrEmpty(tweetUrl)
                ? "服务器歇逼了（无法获取推文链接，请稍后再试）"
                : GenTwitterResult(tweetUrl, userConfig.TwitterApiToken, parsedPic);
        }

        int databaseId = Convert.ToInt32(parsedPic["header"]?["index_id"]);
        Log.Debug("pic search", $"get pic type:{databaseId}");
        //其他类型图片
        switch (databaseId)
        {
            //eh
            case DbIndex.E_HENTAI:
                return "[Saucenao-EHentai]"                      +
                    $"{Environment.NewLine}Source:{parsedPic["data"]?["source"]}" +
                    $"{Environment.NewLine}Name:{parsedPic["data"]?["jp_name"]}"  +
                    $"{Environment.NewLine}[{parsedPic["header"]?["similarity"]}%]";
            case DbIndex.N_HENTAI:
                return "[Saucenao-NHentai]"                      +
                    $"{Environment.NewLine}Source:{parsedPic["data"]?["source"]}" +
                    $"{Environment.NewLine}Name:{parsedPic["data"]?["jp_name"]}"  +
                    $"{Environment.NewLine}[{parsedPic["header"]?["similarity"]}%]";
            //unknown
            default:
            {
                Log.Warning("SaucenaoApi", "未检索到期望数据库");
                //检查源
                string source = parsedPic["data"]?["source"]?.ToString() ?? string.Empty;

                //包含pixiv链接
                if ((source.IndexOf("pixiv", StringComparison.Ordinal) != -1 || source.IndexOf("pximg", StringComparison.Ordinal) != -1) &&
                    long.TryParse(Path.GetFileName(source), out long pid))
                {
                    var imageUrl = MediaUtil.GenPixivUrl(userConfig.HsoConfig.PximyProxy, pid);
                    return GenPixivResult(imageUrl, pid, parsedPic);
                }

                //包含twitter链接
                if (source.IndexOf("twitter", StringComparison.Ordinal) != -1)
                    return GenTwitterResult(source, userConfig.TwitterApiToken, parsedPic);

                //ext url
                //pixiv
                string purl = parsedPic["data"]?["ext_urls"]?.Select(t => t.ToString()).ToArray()
                                                             .FirstOrDefault(pu => pu.IndexOf("pximg", StringComparison.Ordinal) != -1);
                if (!string.IsNullOrEmpty(purl))
                {
                    long.TryParse(Path.GetFileName(purl), out long pxPid);
                    var imageUrl = MediaUtil.GenPixivUrl(userConfig.HsoConfig.PximyProxy, pxPid);
                    return GenPixivResult(imageUrl, pxPid, parsedPic);
                }

                //danbooru
                string durl = parsedPic["data"]?["ext_urls"]?.Select(t => t.ToString()).ToArray()
                                                             .FirstOrDefault(du => du.IndexOf("danbooru", StringComparison.Ordinal) != -1);
                if (!string.IsNullOrEmpty(durl)) return $"[Danbooru]{Environment.NewLine}{durl}";

                Log.Debug("pic search", $"get unknown source:{source}");
                string b64Pic =
                    MediaUtil.DrawTextImage(parsedPic["data"]?.ToString(Formatting.Indented) ?? string.Empty,
                        Color.Black, Color.White);

                var msg = new MessageBody
                {
                    $"[Saucenao-UnknownDatabase]{Environment.NewLine}{parsedPic["header"]?["index_name"]}",
                    SoraSegment.Image($"base64://{b64Pic}"),
                    $"{Environment.NewLine}[{parsedPic["header"]?["similarity"]}%]"
                };

                return msg;
            }
        }
    }

    private static MessageBody GenTwitterResult(string tweetUrl, string token, JToken apiRet)
    {
        string tId = Path.GetFileName(tweetUrl);
        (bool success, string sender, string text, List<string> media) = MediaUtil.GetTweet(tId, token);

        if (!success) return $"推特API错误{Environment.NewLine}Message:{text}";

        var msg = new MessageBody
        {
            $"[Saucenao-Twitter]{Environment.NewLine}推文:{text}{Environment.NewLine}用户:{sender}{Environment.NewLine}"
        };
        foreach (var pic in media) msg.Add(SoraSegment.Image(pic));

        msg.Add($"{Environment.NewLine}Link:{tweetUrl}");
        msg.Add($"{Environment.NewLine}[{apiRet["header"]?["similarity"]}%]");
        return msg;
    }

    private static MessageBody GenPixivResult(string image, long pid, JToken apiRet)
    {
        (int statusCode, bool r18, int count) = MediaUtil.GetPixivImgInfo(pid, out JToken json);
        if (statusCode is not 200 and not 404) return $"[网络错误{statusCode}]";
        MessageBody   msg = new();
        StringBuilder sb  = new();

        sb.AppendLine("[Saucenao-Pixiv]");
        sb.AppendLine($"图片名:{json["body"]?["title"]}");
        sb.Append($"作者:{json["body"]?["userName"]}");
        msg.Add(sb.ToString());
        sb.Clear();

        if (r18) msg.Add($"{Environment.NewLine}[H是不行的]{Environment.NewLine}");
        else if (statusCode != 404) msg.Add(SoraSegment.Image(image, true, 4));
        else msg.Add("哈哈，图被删了");

        sb.AppendLine($"Pixiv Id:{pid}");
        sb.Append($"[{apiRet["header"]?["similarity"]}%]");
        if (count != 1)
        {
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("有多张图片，使用下面的指令查看合集图片");
            sb.Append($"[让我康康{pid}]");
        }

        msg.Add(sb.ToString());

        return msg;
    }
}