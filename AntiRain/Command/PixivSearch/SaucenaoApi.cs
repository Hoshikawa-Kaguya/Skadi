using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AntiRain.Config;
using AntiRain.Config.ConfigModule;
using AntiRain.Resource;
using AntiRain.Tool;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PyLibSharp.Requests;
using Sora.Entities;
using Sora.Entities.Segment;
using YukariToolBox.FormatLog;
using Font = System.Drawing.Font;

namespace AntiRain.Command.PixivSearch
{
    public static class SaucenaoApi
    {
        public static async ValueTask<MessageBody> SearchByUrl(string apiKey, string url, long selfId)
        {
            Log.Debug("pic", "send api request");
            var req =
                await
                    Requests.PostAsync($"http://saucenao.com/search.php?output_type=2&numres=16&db=999&api_key={apiKey}&url={url}",
                                       new ReqParams { Timeout = 20000 });

            var res     = req.Json();
            var resCode = Convert.ToInt32(res?["header"]?["status"] ?? -1);
            Log.Debug("pic", $"get api result code [{resCode}]");

            //API返回失败
            if (res == null || resCode != 0)
                return "图片获取失败";

            var resData = res["results"] as JArray;

            //API返回空值
            if (resData == null)
                return "处理API返回发生错误";

            //未找到图片
            if (resData.Count == 0)
                return "查找结果为空";

            var parsedPic = resData.First();

            if (!ConfigManager.TryGetUserConfig(selfId, out UserConfig userConfig))
            {
                //用户配置获取失败
                Log.Error("Config", "无法获取用户配置文件");
                return "处理用户配置发生错误\r\nMessage:无法读取用户配置";
            }

            switch (Convert.ToInt32(parsedPic["header"]?["index_id"]))
            {
                //pixiv
                case 5:
                {
                    var pid        = Convert.ToInt64(parsedPic["data"]?["pixiv_id"]);
                    var imageUrl   = GenPixivUrl(userConfig.HsoConfig.PximyProxy, pid);
                    var (_, imgSegment) = BotUtils.GetPixivImg(pid, imageUrl);
                    return "[Saucenao-Pixiv]"                                +
                           $"\r\n图片名:{parsedPic["data"]?["title"]}"          +
                           $"\r\n作者:{parsedPic["data"]?["member_name"]}\r\n" +
                           imgSegment                                        +
                           $"\r\nPixiv Id:{pid}\r\n"                         +
                           $"[{parsedPic["header"]?["similarity"]}%]";
                }
                //twitter
                case 41:
                {
                    //TODO 推特API获取图片
                    return "[Saucenao-Twitter]" +
                           //$"\r\n推文:{}\r\n"      +
                           $"\r\n用户:{parsedPic["data"]?["twitter_user_handle"]}" +
                           //imgCqCode                                         +
                           $"\r\nLink:{parsedPic["data"]?["ext_urls"]?[0]}" +
                           $"\r\n[{parsedPic["header"]?["similarity"]}%]";
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
                    var msg = new MessageBody
                    {
                        $"[Saucenao-UnknownDatabase]\r\n{parsedPic["header"]?["index_name"]}"
                    };
                    var source = parsedPic["data"]?["source"]?.ToString() ?? string.Empty;
                    if (!string.IsNullOrEmpty(source))
                    {
                        //包含pixiv链接
                        if (source.IndexOf("pixiv", StringComparison.Ordinal) != -1)
                        {
                            msg += "\r\n[Pixiv-Source]";
                            var pid        = Convert.ToInt64(Path.GetFileName(source));
                            var imageUrl   = GenPixivUrl(userConfig.HsoConfig.PximyProxy, pid);
                            var (info, imgSegment) =  BotUtils.GetPixivImg(pid, imageUrl);
                            msg += imgSegment;
                            msg += $"\r\n{info["body"]?["illustTitle"]?.ToString() ?? string.Empty}";
                            msg += $"\r\nPixiv Id:{info["body"]?["illustId"]?.ToString() ?? string.Empty}";
                            msg += $"\r\n作者:{info["body"]?["userName"]?.ToString() ?? string.Empty}";
                        }
                        //包含twitter链接
                        if (source.IndexOf("twitter", StringComparison.Ordinal) != -1)
                        {
                            msg += "\r\n[Twitter-Source]";
                            msg += $"\r\nLink:{source}";
                        }
                    }
                    else
                    {
                        var img = BotUtils.DrawText(parsedPic["data"]?.ToString(Formatting.Indented) ?? string.Empty,
                                                    new Font(StaticResource.FontCollection.Families[0], 15),
                                                    Color.Black, Color.White);
                        var base64Img = BotUtils.ImgToBase64String(img);
                        msg += SegmentBuilder.Image($"base64://{base64Img}");
                    }

                    return msg;
            }
        }

        private static string GenPixivUrl(string proxy, long pid)
            => string.IsNullOrEmpty(proxy)
                ? $"https://pixiv.lancercmd.cc/{pid}"
                : $"{proxy.Trim('/')}/{pid}";
    }
}