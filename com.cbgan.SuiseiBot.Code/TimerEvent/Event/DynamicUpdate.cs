using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BilibiliApi;
using BilibiliApi.Dynamic;
using BilibiliApi.Dynamic.CardEnum;
using BilibiliApi.Dynamic.DynamicData;
using BilibiliApi.Dynamic.DynamicData.Card;
using com.cbgan.SuiseiBot.Code.IO.Config;
using com.cbgan.SuiseiBot.Code.Tool.Log;
using Native.Sdk.Cqp;
using Newtonsoft.Json.Linq;

namespace com.cbgan.SuiseiBot.Code.TimerEvent.Event
{
    internal class DynamicUpdate
    {
        /// <summary>
        /// 自动获取B站动态
        /// </summary>
        /// <param name="cqApi">CQApi</param>
        public static async void BiliUpdateCheck(CQApi cqApi)
        {
            //读取配置文件
            ConfigIO                config        = new ConfigIO(cqApi.GetLoginQQ().Id);
            Module                  moduleEnable  = config.LoadedConfig.ModuleSwitch;
            List<GroupSubscription> Subscriptions = config.LoadedConfig.SubscriptionConfig.GroupsConfig;
            //检查模块是否启用
            if (!moduleEnable.TimeToDD) return;
            //List<GroupInfo> groupList = cqApi.GetGroupList().GetGroupInfos();
            foreach (GroupSubscription subscription in Subscriptions)
            {
                foreach (long user in subscription.Users)
                {
                    await GetDynamic(cqApi, user, subscription.GroupId);
                }
            }
        }

        private static Task GetDynamic(CQApi cqApi, long uid, List<long> groupId)
        {
            string  message;
            Dynamic biliDynamic;
            //获取动态文本
            JObject cardData = NetUtils.GetBiliDynamicJson(uid, out CardType cardType);
            //检查动态类型
            switch (cardType)
            {
                case CardType.PlainText:
                    PlainTextCard plainTextCard = new PlainTextCard(cardData);
                    plainTextCard.ContentType = ContentType.CQCode;
                    message                   = plainTextCard.ToString();
                    biliDynamic               = plainTextCard;
                    break;
                case CardType.TextAndPic:
                    TextAndPicCard textAndPicCard = new TextAndPicCard(cardData);
                    textAndPicCard.ContentType = ContentType.CQCode;
                    message                    = textAndPicCard.ToString();
                    biliDynamic                = textAndPicCard;
                    break;
                default:
                    return Task.CompletedTask;
            }
            //获取用户信息
            UserInfo sender = biliDynamic.GetUserInfo();
            ConsoleLog.Info("动态获取", $"{sender.UserName}的动态获取成功");
            //格式化动态信息
            StringBuilder sendMessageBuilder = new StringBuilder();
            sendMessageBuilder.Append("获取到了来自 ");
            sendMessageBuilder.Append(sender.UserName);
            sendMessageBuilder.Append(" 的动态：\r\n");
            sendMessageBuilder.Append(message);
            sendMessageBuilder.Append("\r\n更新时间：");
            sendMessageBuilder.Append(biliDynamic.UpdateTime);
            //因为可能会被腾讯误杀所以暂不发送链接
            // sendMessageBuilder.Append("\r\n动态链接：");
            // sendMessageBuilder.Append(biliDynamic.GetDynamicUrl());
            foreach (long group in groupId)
            {
                cqApi.SendGroupMessage(group, sendMessageBuilder.ToString());
            }
            return Task.CompletedTask;
        }
    }
}
