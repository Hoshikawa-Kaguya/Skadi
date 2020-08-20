using System;
using com.cbgan.SuiseiBot.Code.Apis.BiliDynamicApi.CardEnum;
using com.cbgan.SuiseiBot.Code.Apis.BiliDynamicApi.DynamicData;
using com.cbgan.SuiseiBot.Code.Apis.BiliDynamicApi.DynamicData.Card;
using com.cbgan.SuiseiBot.Code.Tool;
using Native.Sdk.Cqp;
using Newtonsoft.Json.Linq;

namespace com.cbgan.SuiseiBot.Code.TimerEvent.DD
{
    internal class DDHelper
    {
        public static void TimeToDD(CQApi cqApi)
        {
            long groupId = 883740678;
            long uid = 353840826;
            string message;
            Dynamic biliDynamic;
            //获取动态文本
            JObject cardData = Apis.BiliDynamicApi.NetUtils.GetBiliDynamicJson(uid, out CardType cardType);
            //检查动态类型
            switch (cardType)
            {
                case CardType.PlainText:
                    PlainTextCard plainTextCard = new PlainTextCard(cardData);
                    message = plainTextCard.ToString();
                    biliDynamic = plainTextCard;
                    break;
                case CardType.TextAndPic:
                    TextAndPicCard textAndPicCard = new TextAndPicCard(cardData);
                    message = textAndPicCard.ToString();
                    biliDynamic = textAndPicCard;
                    break;
                default:
                    return;
            }
            //获取用户信息
            UserInfo sender = biliDynamic.GetUserInfo();
            ConsoleLog.Info("动态获取", $"{sender.UserName}的动态获取成功");
            cqApi.SendGroupMessage(groupId, message);
        }
    }
}
