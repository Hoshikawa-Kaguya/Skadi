using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AntiRain.DatabaseUtils.Helpers;
using AntiRain.IO;
using AntiRain.Config.ConfigModule;
using BilibiliApi.Dynamic;
using BilibiliApi.Dynamic.Enums;
using BilibiliApi.Dynamic.Models;
using BilibiliApi.Dynamic.Models.Card;
using BilibiliApi.Live;
using BilibiliApi.Live.Enums;
using BilibiliApi.Live.Models;
using BilibiliApi.User;
using BilibiliApi.User.Models;
using Sora;
using Sora.Entities.Base;
using Sora.Entities.CQCodes;
using Sora.EventArgs.SoraEvent;
using YukariToolBox.FormatLog;
using YukariToolBox.Time;

namespace AntiRain.TimerEvent.Event
{
    internal static class SubscriptionUpdate
    {
        /// <summary>
        /// 自动获取B站动态
        /// </summary>
        /// <param name="connectEventArgs">连接事件参数</param>
        public static async void BiliUpdateCheck(ConnectEventArgs connectEventArgs)
        {
            //读取配置文件
            ConfigManager.TryGetUserConfig(connectEventArgs.LoginUid, out var loadedConfig);
            ModuleSwitch            moduleEnable  = loadedConfig.ModuleSwitch;
            List<GroupSubscription> Subscriptions = loadedConfig.SubscriptionConfig.GroupsConfig;
            //数据库
            SubscriptionDBHelper dbHelper = new(connectEventArgs.LoginUid);
            //检查模块是否启用
            if (!moduleEnable.Bili_Subscription) return;
            foreach (var subscription in Subscriptions)
            {
                //臭DD的订阅
                foreach (var biliUser in subscription.SubscriptionId)
                {
                    await GetDynamic(connectEventArgs.SoraApi, biliUser, subscription.GroupId, dbHelper);
                }

                //直播动态订阅
                foreach (var biliUser in subscription.LiveSubscriptionId)
                {
                    await GetLiveStatus(connectEventArgs.SoraApi, biliUser, subscription.GroupId, dbHelper);
                }
            }
        }

        private static async ValueTask GetLiveStatus(SoraApi soraApi, long biliUser, List<long> groupId,
                                                     SubscriptionDBHelper dbHelper)
        {
            LiveInfo      live;
            UserSpaceInfo biliUserInfo;
            //获取数据
            try
            {
                biliUserInfo = UserApis.GetLiveRoomInfo(biliUser);
                live         = LiveAPIs.GetLiveRoomInfo(biliUserInfo.LiveRoomInfo.ShortId);
            }
            catch (Exception e)
            {
                Log.Error("获取直播状态时发生错误", Log.ErrorLogBuilder(e));
                return;
            }

            //需要更新数据的群
            Dictionary<long, LiveStatusType> updateDict = groupId
                                                          .Where(gid => dbHelper.GetLastLiveStatus(gid, biliUser) !=
                                                                        live.LiveStatus)
                                                          .ToDictionary(gid => gid, _ => live.LiveStatus);
            //更新数据库
            foreach (var status in updateDict)
            {
                if (!dbHelper.UpdateLiveStatus(status.Key, biliUser, live.LiveStatus))
                {
                    Log.Error("Database", "更新直播订阅数据失败");
                }
            }

            //需要消息提示的群
            var targetGroup = updateDict.Where(group => group.Value == LiveStatusType.Online)
                                        .Select(group => group.Key)
                                        .ToList();
            if (targetGroup.Count == 0) return;
            //构建提示消息
            List<CQCode>  msgList = new();
            StringBuilder message = new();
            message.Append(biliUserInfo.Name);
            message.Append(" 正在直播！\r\n");
            message.Append(biliUserInfo.LiveRoomInfo.Title);
            msgList.AddText(message.ToString());
            msgList.Add(CQCode.CQImage(biliUserInfo.LiveRoomInfo.CoverUrl));
            message.Clear();
            message.Append("直播间地址:");
            message.Append(biliUserInfo.LiveRoomInfo.LiveUrl);
            msgList.AddText(message.ToString());
            foreach (var gid in targetGroup)
            {
                Log.Info("直播订阅", $"获取到{biliUserInfo.Name}正在直播，向群[{gid}]发送动态信息");
                await soraApi.SendGroupMessage(gid, msgList);
            }
        }

        private static async ValueTask GetDynamic(SoraApi soraApi, long biliUser, List<long> groupId,
                                                  SubscriptionDBHelper dbHelper)
        {
            string       textMessage;
            Dynamic      biliDynamic;
            List<string> imgList = new();
            //获取动态文本
            try
            {
                var (cardObj, cardType) = DynamicAPIs.GetLatestDynamic(biliUser);
                switch (cardType)
                {
                    //检查动态类型
                    case CardType.PlainText:
                        PlainTextCard plainTextCard = (PlainTextCard) cardObj;
                        textMessage = plainTextCard.ToString();
                        biliDynamic = plainTextCard;
                        break;
                    case CardType.TextAndPic:
                        TextAndPicCard textAndPicCard = (TextAndPicCard) cardObj;
                        imgList.AddRange(textAndPicCard.ImgList);
                        textMessage = textAndPicCard.ToString();
                        biliDynamic = textAndPicCard;
                        break;
                    case CardType.Forward:
                        ForwardCard forwardCard = (ForwardCard) cardObj;
                        textMessage = forwardCard.ToString();
                        biliDynamic = forwardCard;
                        break;
                    case CardType.Video:
                        VideoCard videoCard = (VideoCard) cardObj;
                        imgList.Add(videoCard.CoverUrl);
                        textMessage = videoCard.ToString();
                        biliDynamic = videoCard;
                        break;
                    case CardType.Error:
                        //Log.Error("动态获取", $"ID:{biliUser}的动态获取失败");
                        return;
                    default:
                        Log.Debug("动态获取", $"ID:{biliUser}的动态获取成功，动态类型未知");
                        foreach (var gid in groupId.Where(gid => !dbHelper.UpdateDynamic(gid, biliUser,
                                                              DateTime.Now.ToTimeStamp())))
                        {
                            Log.Error("数据库", $"更新群[{gid}]动态记录时发生了数据库错误");
                        }

                        return;
                }
            }
            catch (Exception e)
            {
                Log.Error("获取动态更新时发生错误", Log.ErrorLogBuilder(e));
                return;
            }

            //获取用户信息
            UserInfo sender = biliDynamic.GetUserInfo();
            Log.Debug("动态获取", $"{sender.UserName}的动态获取成功");
            //检查是否是最新的
            List<long> targetGroups = groupId
                                      .Where(group => !dbHelper.IsLatestDynamic(@group, sender.Uid,
                                                                                    biliDynamic.UpdateTime))
                                      .ToList();
            //没有群需要发送消息
            if (targetGroups.Count == 0)
            {
                Log.Debug("动态获取", $"{sender.UserName}的动态已是最新");
                return;
            }

            //构建消息
            List<CQCode>  msgList    = new();
            StringBuilder msgBuilder = new();
            msgBuilder.Append("获取到了来自 ");
            msgBuilder.Append(sender.UserName);
            msgBuilder.Append(" 的动态：\r\n");
            msgBuilder.Append(textMessage);
            msgList.AddText(msgBuilder.ToString());
            //添加图片
            imgList.ForEach(imgUrl => msgList.Add(CQCode.CQImage(imgUrl)));
            msgBuilder.Clear();
            msgBuilder.Append("\r\n更新时间：");
            msgBuilder.Append(biliDynamic.UpdateTime.ToString("MM-dd HH:mm:ss"));
            msgList.AddText(msgBuilder.ToString());
            //向未发送消息的群发送消息
            foreach (var targetGroup in targetGroups)
            {
                Log.Info("动态获取", $"获取到{sender.UserName}的最新动态，向群{targetGroup}发送动态信息");
                await soraApi.SendGroupMessage(targetGroup, msgList);
                if (!dbHelper.UpdateDynamic(targetGroup, sender.Uid, biliDynamic.UpdateTime))
                    Log.Error("数据库", "更新动态记录时发生了数据库错误");
            }
        }
    }
}