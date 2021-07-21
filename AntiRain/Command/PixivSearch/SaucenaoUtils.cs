using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AntiRain.Config;
using AntiRain.Config.ConfigModule;
using PyLibSharp.Requests;
using Sora.Entities;
using Sora.Entities.MessageElement;
using Sora.EventArgs.SoraEvent;
using YukariToolBox.FormatLog;

namespace AntiRain.Command.PixivSearch
{
    public static class SaucenaoUtils
    {
        public static async ValueTask<MessageBody> SearchByUrl(string apiKey, string url,
                                                               GroupMessageEventArgs eventArgs)
        {
            Log.Debug("pic", "send api request");
            var req =
                await
                    Requests.PostAsync($"http://saucenao.com/search.php?output_type=2&numres=16&db=5&api_key={apiKey}&url={url}",
                                       new ReqParams {Timeout = 20000});

            var res     = req.Json();
            var resCode = Convert.ToInt32(res?["header"]?["status"] ?? -1);
            Log.Debug("pic", $"get api result code [{resCode}]");

            //API返回失败
            if (res == null || resCode != 0)
                return eventArgs.Sender.CQCodeAt() + "图片获取失败";

            var resData = res["results"]?.ToObject<List<SaucenaoResult>>();

            //API返回空值
            if (resData == null)
                return eventArgs.Sender.CQCodeAt() + "处理API返回发生错误";

            //未找到图片
            if (resData.Count == 0)
                return eventArgs.Sender.CQCodeAt() + "查询到的图片相似度过低，请尝试别的图片";

            var parsedPic = resData.OrderByDescending(pic => Convert.ToDouble(pic.Header.Similarity))
                                   .First();


            if (!ConfigManager.TryGetUserConfig(eventArgs.LoginUid, out UserConfig userConfig))
            {
                //用户配置获取失败
                Log.Error("Config", "无法获取用户配置文件");
                return eventArgs.Sender.CQCodeAt() + "处理用户配置发生错误\r\nMessage:无法读取用户配置";
            }

            string picUrl;
            if (string.IsNullOrEmpty(userConfig.HsoConfig.PximyProxy))
            {
                var (success, msg, urls) = await GetPixivCatInfo(parsedPic.PixivData.PixivId);

                //代理连接处理失败
                if (!success)
                    return eventArgs.Sender.CQCodeAt() + $"处理代理连接发生错误\r\nApi Message:{msg}";

                picUrl = urls[0];
            }
            else
            {
                picUrl = $"{userConfig.HsoConfig.PximyProxy.Trim('/')}/{parsedPic.PixivData.PixivId}";
            }

            return eventArgs.Sender.CQCodeAt()                +
                   $"\r\n图片名:{parsedPic.PixivData.Title}\r\n" +
                   CQCodes.CQImage(picUrl)                    +
                   $"id:{parsedPic.PixivData.PixivId}\r\n相似度:{parsedPic.Header.Similarity}%";
        }

        /// <summary>
        /// PixivCat代理连接生成
        /// </summary>
        /// <param name="pid">pid</param>
        private static async ValueTask<(bool success, string message, List<string> urls)>
            GetPixivCatInfo(long pid)
        {
            try
            {
                var res = await Requests.PostAsync("https://api.pixiv.cat/v1/generate", new ReqParams
                {
                    Header = new Dictionary<HttpRequestHeader, string>
                    {
                        {HttpRequestHeader.ContentType, "application/x-www-form-urlencoded; charset=UTF-8"}
                    },
                    PostContent =
                        new FormUrlEncodedContent(new[] {new KeyValuePair<string, string>("p", pid.ToString())}),
                    Timeout = 5000
                });
                if (res.StatusCode != HttpStatusCode.OK) return (false, $"pixivcat respose ({res.StatusCode})", null);
                //检查返回数据
                var proxyJson = res.Json();
                if (proxyJson == null) return (false, "get null respose from pixivcat", null);
                if (!Convert.ToBoolean(proxyJson["success"] ?? false))
                    return (false, $"pixivcat failed({proxyJson["error"]})", null);
                //是否为多张图片
                var urls = Convert.ToBoolean(proxyJson["multiple"] ?? false)
                    ? proxyJson["original_urls_proxy"]?.ToObject<List<string>>()
                    : new List<string> {proxyJson["original_url_proxy"]?.ToString() ?? string.Empty};
                return (true, "OK", urls);
            }
            catch (Exception e)
            {
                Log.Error("pixiv api error", Log.ErrorLogBuilder(e));
                return (false, $"pixiv api error ({e})", null);
            }
        }
    }
}