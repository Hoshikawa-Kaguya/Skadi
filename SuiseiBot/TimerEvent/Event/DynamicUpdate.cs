using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using BilibiliApi;
using BilibiliApi.Dynamic;
using BilibiliApi.Dynamic.Enums;
using BilibiliApi.Dynamic.Models;
using BilibiliApi.Dynamic.Models.Card;
using Native.Sdk.Cqp;
using Newtonsoft.Json.Linq;
using SuiseiBot.Code.DatabaseUtils.Helpers;
using SuiseiBot.Code.IO.Config;
using SuiseiBot.Code.IO.Config.ConfigFile;
using SuiseiBot.Code.Tool.LogUtils;

namespace SuiseiBot.Code.TimerEvent.Event
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
            Config           config        = new Config(cqApi.GetLoginQQ().Id);
            Module                  moduleEnable  = config.LoadedConfig.ModuleSwitch;
            List<GroupSubscription> Subscriptions = config.LoadedConfig.SubscriptionConfig.GroupsConfig;
            //数据库
            SubscriptionDBHelper dbHelper = new SubscriptionDBHelper(cqApi);
            //检查模块是否启用
            if (!moduleEnable.Bili_Subscription || !moduleEnable.PCR_Subscription) return;
            foreach (GroupSubscription subscription in Subscriptions)
            {
                //PCR动态订阅
                if (subscription.PCR_Subscription)
                {
                    await GetDynamic(cqApi, 353840826, subscription.GroupId, dbHelper);
                }
                //臭DD的订阅
                foreach (long biliUser in subscription.SubscriptionId)
                {
                    await GetDynamic(cqApi, biliUser, subscription.GroupId, dbHelper);
                }
            }
        }

        private static Task GetDynamic(CQApi cqApi, long biliUser, List<long> groupId, SubscriptionDBHelper dbHelper)
        {
            string   message;
            Dynamic  biliDynamic;
            //获取动态文本
            try
            {
                JObject cardData = DynamicAPIs.GetBiliDynamicJson((ulong)biliUser, out CardType cardType);
                switch (cardType)
                {
                    //检查动态类型
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
                        ConsoleLog.Debug("动态获取", $"ID:{biliUser}的动态获取成功，动态类型未知");
                        return Task.CompletedTask;
                }
            }
            catch (Exception e)
            {
                ConsoleLog.Error("获取动态更新时发生错误",ConsoleLog.ErrorLogBuilder(e));
                return Task.CompletedTask;
            }
            //获取用户信息
            UserInfo sender = biliDynamic.GetUserInfo();
            ConsoleLog.Info("动态获取", $"{sender.UserName}的动态获取成功");
            //检查是否是最新的
            
            List<long> targetGroups = new List<long>();
            foreach (long group in groupId)
            {
                //检查是否已经发送过消息
                if (!dbHelper.IsLatest(group, sender.Uid, biliDynamic.UpdateTime))
                    targetGroups.Add(group);
            }
            //没有群需要发送消息
            if(targetGroups.Count == 0)
            {
                ConsoleLog.Info("动态获取", $"{sender.UserName}的动态已是最新");
                return Task.CompletedTask;
            }
            //向未发生消息的群发送消息
            string messageToSend = msgBuilder(sender, message, biliDynamic);
            foreach (long targetGroup in targetGroups)
            {
                ConsoleLog.Info("动态获取", $"向群{targetGroup}发送动态信息");
                cqApi.SendGroupMessage(targetGroup, messageToSend);
                dbHelper.Update(targetGroup, sender.Uid, biliDynamic.UpdateTime);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// 生成格式化信息
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="message">文字信息</param>
        /// <param name="biliDynamic">动态实例</param>
        /// <returns></returns>
        private static string msgBuilder(UserInfo sender,string message,Dynamic biliDynamic)
        {
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
            return sendMessageBuilder.ToString();
        }
    }
}
