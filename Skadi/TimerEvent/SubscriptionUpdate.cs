using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BilibiliApi;
using BilibiliApi.Live.Enums;
using BilibiliApi.Models;
using Microsoft.Extensions.DependencyInjection;
using Skadi.Database.Helpers;
using Skadi.Entities.ConfigModule;
using Skadi.Interface;
using Sora;
using Sora.Entities;
using Sora.Entities.Base;
using Sora.Entities.Segment;
using Sora.Enumeration;
using Sora.Util;
using YukariToolBox.LightLog;

namespace Skadi.TimerEvent;

internal static class SubscriptionUpdate
{
    /// <summary>
    /// 自动获取B站动态
    /// </summary>
    /// <param name="loginUid">机器人的账号</param>
    public static async void BiliUpdateCheck(long loginUid)
    {
        IGenericStorage genericStorage = SkadiApp.GetService<IGenericStorage>();
        //读取配置文件
        UserConfig config = genericStorage.GetUserConfig(loginUid);
        if (config is null)
        {
            Log.Error("Sub-Serv", $"无法获取用户[{loginUid}]的配置文件");
            return;
        }

        ModuleSwitch            moduleEnable  = config.ModuleSwitch;
        List<GroupSubscription> subscriptions = config.SubscriptionConfig.GroupsConfig;
        //数据库
        SubscriptionDbHelper dbHelper = new(loginUid);
        //检查模块是否启用
        if (!moduleEnable.BiliSubscription)
            return;
        if (!SoraServiceFactory.TryGetApi(loginUid, out SoraApi api))
        {
            Log.Error("SoraApi", $"无法获取账号[{loginUid}]API实例");
            return;
        }

        using IServiceScope scope  = SkadiApp.CreateScope();
        IChromeService      chrome = scope.ServiceProvider.GetService<IChromeService>();
        if (chrome is null)
        {
            Log.Error("Serv", "未找到浏览器服务，跳过本次更新");
            return;
        }

        foreach (GroupSubscription subscription in subscriptions)
        {
            //臭DD的订阅
            foreach (long biliUser in subscription.SubscriptionId)
                await GetDynamic(api,
                                 biliUser,
                                 subscription.GroupId,
                                 dbHelper,
                                 chrome);

            //直播动态订阅
            foreach (long biliUser in subscription.LiveSubscriptionId)
                await GetLiveStatus(api,
                                    biliUser,
                                    subscription.GroupId,
                                    dbHelper);
        }
    }

    private static async ValueTask GetLiveStatus(SoraApi              soraApi,
                                                 long                 biliUser,
                                                 List<long>           groupId,
                                                 SubscriptionDbHelper dbHelper)
    {
        //数据获取
        (UserInfo bUserInfo, _) = await BiliApis.GetLiveUserInfo(biliUser);
        if (bUserInfo is null || bUserInfo.Code != 0)
        {
            Log.Error("BiliApi", $"无法获取用户信息[{biliUser}]");
            if (bUserInfo is not null)
                Log.Error("BiliApi[GetLiveUserInfo]", $"Api error:{bUserInfo.Message}");
            return;
        }

        (LiveInfo liveInfo, _) = await BiliApis.GetLiveRoomInfo(bUserInfo.LiveId);
        if (liveInfo is null || liveInfo.Code != 0)
        {
            Log.Error("BiliApi", $"无法获取直播间信息[{biliUser}]");
            if (liveInfo is not null)
                Log.Error("BiliApi[GetLiveRoomInfo]", $"Api error:{bUserInfo.Message}");
            return;
        }

        //需要更新数据的群
        Dictionary<long, LiveStatusType> updateDict = groupId
                                                      .Where(gid => dbHelper.GetLastLiveStatus(gid, biliUser)
                                                                    != liveInfo.LiveStatus)
                                                      .ToDictionary(gid => gid, _ => liveInfo.LiveStatus);

        //更新数据库
        foreach (var status in updateDict)
            if (!dbHelper.UpdateLiveStatus(status.Key,
                                           biliUser,
                                           liveInfo.LiveStatus))
                Log.Error("Database", "更新直播订阅数据失败");

        //需要消息提示的群
        var targetGroup = updateDict
                          .Where(group => group.Value == LiveStatusType.Online)
                          .Select(group => group.Key)
                          .ToList();
        if (targetGroup.Count == 0)
            return;

        Log.Info("Sub", $"更新[{soraApi.GetLoginUserId()}]的Live订阅");
        //构建提示消息
        SoraSegment coverImg = SoraSegment.Image(liveInfo.Cover);
        if (coverImg.MessageType == SegmentType.Ignore)
        {
            Log.Error("BiliSub", "构建订阅消息失败（没有图片信息）");
            return;
        }

        MessageBody message = $"{bUserInfo.UserName} 正在直播！\r\n{liveInfo.Title}"
                              + SoraSegment.Image(liveInfo.Cover)
                              + $"直播间地址:https://live.bilibili.com/{liveInfo.RoomId}";
        foreach (long gid in targetGroup)
        {
            Log.Info("直播订阅", $"获取到{bUserInfo.UserName}正在直播，向群[{gid}]发送动态信息");
            await soraApi.SendGroupMessage(gid, message);
        }
    }

    private static async ValueTask GetDynamic(SoraApi              soraApi,
                                              long                 biliUser,
                                              List<long>           groupId,
                                              SubscriptionDbHelper dbHelper,
                                              IChromeService       chrome)
    {
        //获取用户信息
        (UserInfo sender, _) = await BiliApis.GetLiveUserInfo(biliUser);
        if (sender is null || sender.Code != 0)
        {
            Log.Error("BiliApi", $"无法获取用户信息[{biliUser}]");
            if (sender is not null)
                Log.Error("BiliApi[GetLiveUserInfo]", $"Api error:{sender.Message}");
            return;
        }

        (ulong dId, long dTs, _) = await BiliApis.GetLatestDynamicId(biliUser);
        if (dId == 0 || dTs == 0)
        {
            Log.Error("BiliApi", $"无法获取用户动态信息[{biliUser}]");
            return;
        }

        Log.Debug("动态获取", $"{sender.UserName}的动态获取成功");
        //检查是否是最新的
        List<long> targetGroups = groupId.Where(group => !dbHelper.IsLatestDynamic(group,
                                                                                   sender.Uid,
                                                                                   dTs.ToDateTime())).ToList();

        if (!dbHelper.UpdateDynamic(targetGroups,
                                    sender.Uid,
                                    dTs.ToDateTime()))
            Log.Error("数据库", "更新动态记录时发生了数据库错误");

        //没有群需要发送消息
        if (targetGroups.Count == 0)
        {
            Log.Debug("动态获取", $"{sender.UserName}的动态已是最新");
            return;
        }

        Log.Info("Sub", $"更新[{soraApi.GetLoginUserId()}]的动态订阅");

        SoraSegment image =
            await chrome.GetChromeXPathPic($"https://t.bilibili.com/{dId}",
                                           "//*[@id=\"app\"]/div[2]/div/div/div[1]");
        //向未发送消息的群发送消息
        foreach (long targetGroup in targetGroups)
        {
            Log.Info("动态获取", $"获取到{sender.UserName}的最新动态，向群{targetGroup}发送动态信息");
            // await soraApi.SendGroupMessage(targetGroup, $"{sender.UserName}有新动态！");
            // await soraApi.SendGroupForwardMsg(targetGroup, nodes);
            await soraApi.SendGroupMessage(targetGroup, image);
        }
    }
}