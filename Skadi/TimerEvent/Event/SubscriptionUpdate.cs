using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BilibiliApi;
using BilibiliApi.Live.Enums;
using BilibiliApi.Models;
using PuppeteerSharp;
using Skadi.Config;
using Skadi.DatabaseUtils.Helpers;
using Sora.Entities;
using Sora.Entities.Base;
using Sora.Entities.Segment;
using Sora.EventArgs.SoraEvent;
using Sora.Util;
using YukariToolBox.LightLog;

namespace Skadi.TimerEvent.Event;

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
        var moduleEnable  = loadedConfig.ModuleSwitch;
        var subscriptions = loadedConfig.SubscriptionConfig.GroupsConfig;
        //数据库
        var dbHelper = new SubscriptionDbHelper(connectEventArgs.LoginUid);
        //检查模块是否启用
        if (!moduleEnable.BiliSubscription)
            return;
        foreach (var subscription in subscriptions)
        {
            //臭DD的订阅
            foreach (var biliUser in subscription.SubscriptionId)
                await GetDynamic(connectEventArgs.SoraApi, biliUser, subscription.GroupId, dbHelper);

            //直播动态订阅
            foreach (var biliUser in subscription.LiveSubscriptionId)
                await GetLiveStatus(connectEventArgs.SoraApi, biliUser, subscription.GroupId, dbHelper);
        }
    }

    private static async ValueTask GetLiveStatus(SoraApi              soraApi,
                                                 long                 biliUser,
                                                 List<long>           groupId,
                                                 SubscriptionDbHelper dbHelper)
    {
        //数据获取
        UserInfo bUserInfo = await BiliApis.GetLiveUserInfo(biliUser);
        if (bUserInfo is null)
        {
            Log.Error("BiliApi", $"无法获取用户信息[{biliUser}]");
            return;
        }

        LiveInfo liveInfo = await BiliApis.GetLiveRoomInfo(bUserInfo.LiveId);
        if (liveInfo.Code != 0)
        {
            Log.Error("BiliApi", $"无法获取用户[{biliUser}]的直播信息\r\nmsg:{liveInfo.Message}");
            return;
        }

        //需要更新数据的群
        Dictionary<long, LiveStatusType> updateDict =
            groupId.Where(gid => dbHelper.GetLastLiveStatus(gid, biliUser) != liveInfo.LiveStatus)
                   .ToDictionary(gid => gid, _ => liveInfo.LiveStatus);

        //更新数据库
        foreach (var status in updateDict)
            if (!dbHelper.UpdateLiveStatus(status.Key, biliUser, liveInfo.LiveStatus))
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
        var message = $"{bUserInfo.UserName} 正在直播！\r\n{liveInfo.Title}"
                      + SoraSegment.Image(liveInfo.Cover)
                      + $"直播间地址:https://live.bilibili.com/{liveInfo.RoomId}";
        foreach (var gid in targetGroup)
        {
            Log.Info("直播订阅", $"获取到{bUserInfo.UserName}正在直播，向群[{gid}]发送动态信息");
            await soraApi.SendGroupMessage(gid, message);
        }
    }

    private static async ValueTask GetDynamic(SoraApi              soraApi,
                                              long                 biliUser,
                                              List<long>           groupId,
                                              SubscriptionDbHelper dbHelper)
    {
        //获取用户信息
        UserInfo sender = await BiliApis.GetLiveUserInfo(biliUser);
        (ulong dId, long dTs) = await BiliApis.GetLatestDynamicId(biliUser);
        Log.Debug("动态获取", $"{sender.UserName}的动态获取成功");
        //检查是否是最新的
        var targetGroups =
            groupId.Where(group => !dbHelper.IsLatestDynamic(group, sender.Uid, dTs.ToDateTime()))
                   .ToList();
        //没有群需要发送消息
        if (targetGroups.Count == 0)
        {
            Log.Debug("动态获取", $"{sender.UserName}的动态已是最新");
            return;
        }

        Log.Info("Sub", $"更新[{soraApi.GetLoginUserId()}]的动态订阅");

        //构建消息
        MessageBody message = new MessageBody();
        message += await GetChromePic($"https://t.bilibili.com/{dId}");
        //向未发送消息的群发送消息
        foreach (long targetGroup in targetGroups)
        {
            Log.Info("动态获取", $"获取到{sender.UserName}的最新动态，向群{targetGroup}发送动态信息");
            await soraApi.SendGroupMessage(targetGroup, message);
            if (!dbHelper.UpdateDynamic(targetGroup, sender.Uid, dTs.ToDateTime()))
                Log.Error("数据库", "更新动态记录时发生了数据库错误");
        }
    }

    private static async Task<SoraSegment> GetChromePic(string url)
    {
        Page page = await StaticVar.Chrome.NewPageAsync();
        await page.SetViewportAsync(new ViewPortOptions
        {
            Width  = 2000,
            Height = 1500
        });

        await page.GoToAsync(url);

        //动态
        ElementHandle dyElement =
            await page.QuerySelectorAsync("#app > div.content > div > div > div.bili-dyn-item__main");
        Log.Debug("Puppeteer", $"获取到动态元素[{dyElement.RemoteObject.ObjectId}]");

        string picB64 = await dyElement.ScreenshotBase64Async(new ScreenshotOptions { Type = ScreenshotType.Png });

        //关闭页面
        await page.CloseAsync();
        await page.DisposeAsync();

        SoraSegment img = SoraSegment.Image($"base64://{picB64}");
        return img;
    }
}