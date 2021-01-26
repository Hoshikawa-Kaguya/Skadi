using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AntiRain.DatabaseUtils.Helpers;
using AntiRain.IO.Config;
using AntiRain.IO.Config.ConfigModule;
using AntiRain.Tool;
using BilibiliApi.Dynamic;
using BilibiliApi.Dynamic.Enums;
using BilibiliApi.Dynamic.Models;
using BilibiliApi.Dynamic.Models.Card;
using Sora.Entities.Base;
using Sora.Entities.CQCodes;
using Sora.EventArgs.SoraEvent;
using YukariToolBox.Console;

namespace AntiRain.TimerEvent.Event
{
    internal static class DynamicUpdate
    {
        /// <summary>
        /// 自动获取B站动态
        /// </summary>
        /// <param name="connectEventArgs">连接事件参数</param>
        public static async void BiliUpdateCheck(ConnectEventArgs connectEventArgs)
        {
            //读取配置文件
            ConfigManager configManager = new(connectEventArgs.LoginUid);
            configManager.LoadUserConfig(out var loadedConfig);
            ModuleSwitch            moduleEnable  = loadedConfig.ModuleSwitch;
            List<GroupSubscription> Subscriptions = loadedConfig.SubscriptionConfig.GroupsConfig;
            //数据库
            SubscriptionDBHelper dbHelper = new SubscriptionDBHelper(connectEventArgs.LoginUid);
            //检查模块是否启用
            if (!moduleEnable.Bili_Subscription) return;
            foreach (var subscription in Subscriptions)
            {
                //PCR动态订阅
                if (subscription.PCR_Subscription)
                {
                    await GetDynamic(connectEventArgs.SoraApi, 353840826, subscription.GroupId, dbHelper);
                }
                //臭DD的订阅
                foreach (var biliUser in subscription.SubscriptionId)
                {
                    await GetDynamic(connectEventArgs.SoraApi, biliUser, subscription.GroupId, dbHelper);
                }
            }
        }

        private static ValueTask GetLiveStatus(SoraApi soraApi, long biliUser, List<long> groupId, SubscriptionDBHelper dbHelper)
        {

            return ValueTask.CompletedTask;
        }

        private static ValueTask GetDynamic(SoraApi soraApi, long biliUser, List<long> groupId, SubscriptionDBHelper dbHelper)
        {
            string       textMessage;
            Dynamic      biliDynamic;
            List<string> imgList = new();
            //获取动态文本
            try
            {
                var cardData = DynamicAPIs.GetLatestDynamic(biliUser);
                switch (cardData.cardType)
                {
                    //检查动态类型
                    case CardType.PlainText:
                        PlainTextCard plainTextCard = (PlainTextCard) cardData.cardObj;
                        textMessage = plainTextCard.ToString();
                        biliDynamic = plainTextCard;
                        break;
                    case CardType.TextAndPic:
                        TextAndPicCard textAndPicCard = (TextAndPicCard) cardData.cardObj;
                        imgList.AddRange(textAndPicCard.ImgList);
                        textMessage = textAndPicCard.ToString();
                        biliDynamic = textAndPicCard;
                        break;
                    case CardType.Forward:
                        ForwardCard forwardCard = (ForwardCard) cardData.cardObj;
                        textMessage = forwardCard.ToString();
                        biliDynamic = forwardCard;
                        break;
                    case CardType.Video:
                        VideoCard videoCard = (VideoCard) cardData.cardObj;
                        imgList.Add(videoCard.CoverUrl);
                        textMessage = videoCard.ToString();
                        biliDynamic = videoCard;
                        break;
                    case CardType.Error:
                        ConsoleLog.Error("动态获取", $"ID:{biliUser}的动态获取失败");
                        return ValueTask.CompletedTask;
                    default:
                        ConsoleLog.Debug("动态获取", $"ID:{biliUser}的动态获取成功，动态类型未知");
                        foreach (var gid in groupId)
                        {
                            if(!dbHelper.Update(gid, biliUser, BotUtils.GetNowStampLong()))
                                ConsoleLog.Error("数据库","更新动态记录时发生了数据库错误");
                        }
                        return ValueTask.CompletedTask;
                }
            }
            catch (Exception e)
            {
                ConsoleLog.Error("获取动态更新时发生错误",ConsoleLog.ErrorLogBuilder(e));
                return ValueTask.CompletedTask;
            }
            //获取用户信息
            UserInfo sender = biliDynamic.GetUserInfo();
            ConsoleLog.Debug("动态获取", $"{sender.UserName}的动态获取成功");
            //检查是否是最新的
            
            List<long> targetGroups = new();
            foreach (var group in groupId)
            {
                //检查是否已经发送过消息
                if (!dbHelper.IsLatest(group, sender.Uid, biliDynamic.UpdateTime))
                    targetGroups.Add(group);
            }
            //没有群需要发送消息
            if(targetGroups.Count == 0)
            {
                ConsoleLog.Debug("动态获取", $"{sender.UserName}的动态已是最新");
                return ValueTask.CompletedTask;
            }
            //构建消息
            List<CQCode>  msgList    = new();
            StringBuilder msgBuilder = new();
            msgBuilder.Append("获取到了来自 ");
            msgBuilder.Append(sender.UserName);
            msgBuilder.Append(" 的动态：\r\n");
            msgBuilder.Append(textMessage);
            msgList.Add(CQCode.CQText(msgBuilder.ToString()));
            //添加图片
            imgList.ForEach(imgUrl => msgList.Add(CQCode.CQImage(imgUrl)));
            msgBuilder.Clear();
            msgBuilder.Append("\r\n更新时间：");
            msgBuilder.Append(biliDynamic.UpdateTime.ToString("MM-dd HH:mm:ss"));
            msgList.Add(CQCode.CQText(msgBuilder.ToString()));
            //向未发生消息的群发送消息
            foreach (var targetGroup in targetGroups)
            {
                ConsoleLog.Info("动态获取", $"获取到{sender.UserName}的最新动态，向群{targetGroup}发送动态信息");
                soraApi.SendGroupMessage(targetGroup, msgList);
                if(!dbHelper.Update(targetGroup, sender.Uid, biliDynamic.UpdateTime)) 
                    ConsoleLog.Error("数据库","更新动态记录时发生了数据库错误");
            }
            return ValueTask.CompletedTask;
        }
    }
}
