using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using com.cbgan.SuiseiBot.Code.Apis.BiliDynamicApi.CardEnum;
using com.cbgan.SuiseiBot.Code.Network;
using Newtonsoft.Json.Linq;

namespace com.cbgan.SuiseiBot.Code.Apis.BiliDynamicApi
{
    internal static class NetUtils
    {
        /// <summary>
        /// 获取链接
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        internal static string GetDynamicUrl(long uid, long offset = 0)
        {
            Dictionary<string, long> urlParams = new Dictionary<string, long>
            {
                {"host_uid", uid},
                {"offset_dynamic_id", offset}
            };
            StringBuilder urlBuilder = new StringBuilder();
            urlBuilder.Append("https://api.vc.bilibili.com/dynamic_svr/v1/dynamic_svr/space_history?");
            urlBuilder.Append(string.Join("&", urlParams.Select(param => $"{param.Key}={param.Value}")));
            return urlBuilder.ToString();
        }

        /// <summary>
        /// 从服务器获取最新的动态数据
        /// </summary>
        /// <param name="uid">用户ID</param>
        /// <param name="cardType">动态类型</param>
        /// <returns></returns>
        internal static JObject GetBiliDynamicJson(long uid,out CardType cardType)
        {
            //响应JSON
            JObject cardJObject;
            try
            {
                JObject dataJObject = JObject.Parse(HTTPUtils.GetHttpResponse(GetDynamicUrl(uid)));
                string  code        = dataJObject["code"]?.ToString();
                if (code == null || !code.Equals("0"))
                {
                    cardType = (CardType) (-1);
                    return null;
                }
                //检查是否是置顶动态[4]
                cardJObject = (int)dataJObject["data"]?["cards"]?[0]?["extra"]?["is_space_top"] == 0
                    ? JObject.Parse(dataJObject["data"]?["cards"]?[0]?.ToString() ?? string.Empty)
                    : JObject.Parse(dataJObject["data"]?["cards"]?[1]?.ToString() ?? string.Empty);
                cardType = Enum.IsDefined(typeof(CardType), (int) cardJObject["desc"]?["type"])
                    ? (CardType) ((int) cardJObject["desc"]?["type"])
                    : CardType.Unknown;
            }
            catch (Exception e)
            {
                Console.WriteLine($"获取JSON时发生了错误\n{e}");
                cardType = (CardType)(-1);
                throw;
            }
            return cardJObject;
        }
    }
}