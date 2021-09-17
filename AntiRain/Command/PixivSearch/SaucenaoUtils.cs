using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AntiRain.Config;
using AntiRain.Config.ConfigModule;
using AntiRain.Tool;
using PyLibSharp.Requests;
using Sora.Entities;
using Sora.Entities.MessageElement;
using YukariToolBox.FormatLog;

namespace AntiRain.Command.PixivSearch
{
    public static class SaucenaoUtils
    {
        public static async ValueTask<MessageBody> SearchByUrl(string apiKey, string url, long sender, long selfId)
        {
            Log.Debug("pic", "send api request");
            var req =
                await
                    Requests.PostAsync($"http://saucenao.com/search.php?output_type=2&numres=16&db=5&api_key={apiKey}&url={url}",
                                       new ReqParams { Timeout = 20000 });

            var res     = req.Json();
            var resCode = Convert.ToInt32(res?["header"]?["status"] ?? -1);
            Log.Debug("pic", $"get api result code [{resCode}]");

            //API返回失败
            if (res == null || resCode != 0)
                return sender.ToAt() + "图片获取失败";

            var resData = res["results"]?.ToObject<List<SaucenaoResult>>();

            //API返回空值
            if (resData == null)
                return sender.ToAt() + "处理API返回发生错误";

            //未找到图片
            if (resData.Count == 0)
                return sender.ToAt() + "查询到的图片相似度过低，请尝试别的图片";

            var parsedPic = resData.OrderByDescending(pic => Convert.ToDouble(pic.Header.Similarity))
                                   .First();


            if (!ConfigManager.TryGetUserConfig(selfId, out UserConfig userConfig))
            {
                //用户配置获取失败
                Log.Error("Config", "无法获取用户配置文件");
                return sender.ToAt() + "处理用户配置发生错误\r\nMessage:无法读取用户配置";
            }

            var imageUrl = string.IsNullOrEmpty(userConfig.HsoConfig.PximyProxy)
                ? $"https://pixiv.lancercmd.cc/{parsedPic.PixivData.PixivId}"
                : $"{userConfig.HsoConfig.PximyProxy.Trim('/')}/{parsedPic.PixivData.PixivId}";
            var imgCqCode = BotUtils.GetPixivImg(parsedPic.PixivData.PixivId, imageUrl);

            return sender.ToAt()                +
                   $"\r\n图片名:{parsedPic.PixivData.Title}\r\n" +
                   imgCqCode                                  +
                   $"\r\nid:{parsedPic.PixivData.PixivId}\r\n相似度:{parsedPic.Header.Similarity}%";
        }
    }
}