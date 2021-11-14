using AntiRain.Tool;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PyLibSharp.Requests;
using Sora.Entities;
using Sora.Entities.Segment;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using YukariToolBox.FormatLog;

namespace AntiRain.Command.ImageSearch;

internal static class Util
{
    public static (bool success, string sender, string text, List<string> media)
        GetTweet(string tweetId, string token)
    {
        Log.Info("Twitter", $"Get tweet by id[{tweetId}]");
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
                                   }
                               });
        Log.Info("Twitter", $"Twitter api http code:{res.StatusCode}");
        if (res.StatusCode != HttpStatusCode.OK)
        {
            Log.Error("Twitter", "Twitter api net error");
            return (false, string.Empty, $"Twitter api net error [{res.StatusCode}]", null);
        }

        var data = res.Json();
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

    public static MessageBody GenTwitterResult(string tweetUrl, string token, JToken apiRet)
    {
        var tId = Path.GetFileName(tweetUrl);
        var (success, sender, text, media) = GetTweet(tId, token);

        if (!success) return $"推特API错误\r\nMessage:{text}";

        var msg = new MessageBody
        {
            $"[Saucenao-Twitter]\r\n推文:{text}\r\n用户:{sender}\r\n"
        };
        foreach (var pic in media) msg.Add(SoraSegment.Image(pic));

        msg.Add($"\r\nLink:{tweetUrl}");
        msg.Add($"\r\n[{apiRet["header"]?["similarity"]}%]");
        return msg;
    }

    public static MessageBody GenPixivResult(string url, long pid, JToken apiRet)
    {
        var imgSegment = BotUtils.GetPixivImg(pid, url);
        return "[Saucenao-Pixiv]"                             +
               $"\r\n图片名:{apiRet["data"]?["title"]}"          +
               $"\r\n作者:{apiRet["data"]?["member_name"]}\r\n" +
               imgSegment                                     +
               $"\r\nPixiv Id:{pid}\r\n"                      +
               $"[{apiRet["header"]?["similarity"]}%]";
    }
}