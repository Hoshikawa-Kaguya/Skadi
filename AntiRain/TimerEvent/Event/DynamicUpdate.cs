using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AntiRain.DatabaseUtils.Helpers;
using AntiRain.IO.Config;
using AntiRain.IO.Config.ConfigModule;
using BilibiliApi.Dynamic;
using BilibiliApi.Dynamic.Enums;
using BilibiliApi.Dynamic.Models;
using BilibiliApi.Dynamic.Models.Card;
using Newtonsoft.Json.Linq;
using Sora.Entities.Base;
using Sora.Entities.CQCodes;
using Sora.EventArgs.SoraEvent;
using Sora.Tool;
using BiliUserInfo = BilibiliApi.Dynamic.Models.UserInfo;

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
            Config config = new Config(connectEventArgs.LoginUid);
            config.LoadUserConfig(out UserConfig loadedConfig);
            ModuleSwitch            moduleEnable  = loadedConfig.ModuleSwitch;
            List<GroupSubscription> Subscriptions = loadedConfig.SubscriptionConfig.GroupsConfig;
            //数据库
            SubscriptionDBHelper dbHelper = new SubscriptionDBHelper(connectEventArgs.LoginUid);
            //检查模块是否启用
            if (!moduleEnable.Bili_Subscription) return;
            foreach (GroupSubscription subscription in Subscriptions)
            {
                //PCR动态订阅
                if (subscription.PCR_Subscription)
                {
                    await GetDynamic(connectEventArgs.SoraApi, 353840826, subscription.GroupId, dbHelper);
                }
                //臭DD的订阅
                foreach (long biliUser in subscription.SubscriptionId)
                {
                    await GetDynamic(connectEventArgs.SoraApi, biliUser, subscription.GroupId, dbHelper);
                }
            }
        }

        private static Task GetDynamic(SoraApi soraApi, long biliUser, List<long> groupId, SubscriptionDBHelper dbHelper)
        {
            string       textMessage;
            Dynamic      biliDynamic;
            List<string> imgList = new List<string>();
            //获取动态文本
            try
            {
                JObject cardData = DynamicAPIs.GetBiliDynamicJson((ulong)biliUser, out CardType cardType);
                switch (cardType)
                {
                    //检查动态类型
                    case CardType.PlainText:
                        PlainTextCard plainTextCard = new PlainTextCard(cardData);
                        textMessage = plainTextCard.ToString();
                        biliDynamic = plainTextCard;
                        break;
                    case CardType.TextAndPic:
                        TextAndPicCard textAndPicCard = new TextAndPicCard(cardData);
                        imgList.AddRange(textAndPicCard.ImgList);
                        textMessage = textAndPicCard.ToString();
                        biliDynamic = textAndPicCard;
                        break;
                    default:
                        ConsoleLog.Warning("动态获取", $"ID:{biliUser}的动态获取成功，动态类型未知");
                        return Task.CompletedTask;
                }
            }
            catch (Exception e)
            {
                ConsoleLog.Error("获取动态更新时发生错误",ConsoleLog.ErrorLogBuilder(e));
                return Task.CompletedTask;
            }
            //获取用户信息
            BiliUserInfo sender = biliDynamic.GetUserInfo();
            ConsoleLog.Debug("动态获取", $"{sender.UserName}的动态获取成功");
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
                ConsoleLog.Debug("动态获取", $"{sender.UserName}的动态已是最新");
                return Task.CompletedTask;
            }
            //构建消息
            List<CQCode>  msgList = new List<CQCode>();
            StringBuilder sb      = new StringBuilder();
            sb.Append("获取到了来自 ");
            sb.Append(sender.UserName);
            sb.Append(" 的动态：\r\n");
            sb.Append(textMessage);
            msgList.Add(CQCode.CQText(sb.ToString()));
            //添加图片
            imgList.ForEach(imgUrl => msgList.Add(CQCode.CQImage(imgUrl)));
            sb.Clear();
            sb.Append("\r\n更新时间：");
            sb.Append(biliDynamic.UpdateTime);
            msgList.Add(CQCode.CQText(sb.ToString()));
            //向未发生消息的群发送消息
            foreach (long targetGroup in targetGroups)
            {
                ConsoleLog.Info("动态获取", $"获取到{sender.UserName}的最新动态，向群{targetGroup}发送动态信息");
                soraApi.SendGroupMessage(targetGroup, msgList);
                if(!dbHelper.Update(targetGroup, sender.Uid, biliDynamic.UpdateTime)) 
                    ConsoleLog.Error("数据库","更新动态记录时发生了数据库错误");
            }
            return Task.CompletedTask;
        }
    }
}
