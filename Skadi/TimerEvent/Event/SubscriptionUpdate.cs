using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BilibiliApi;
using BilibiliApi.Live.Enums;
using BilibiliApi.Models;
using Newtonsoft.Json.Linq;
using PuppeteerSharp;
using Skadi.Config;
using Skadi.Config.ConfigModule;
using Skadi.DatabaseUtils.Helpers;
using Sora;
using Sora.Entities.Base;
using Sora.Entities.Segment;
using Sora.Entities.Segment.DataModel;
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
        ModuleSwitch            moduleEnable  = loadedConfig.ModuleSwitch;
        List<GroupSubscription> subscriptions = loadedConfig.SubscriptionConfig.GroupsConfig;
        //数据库
        var dbHelper = new SubscriptionDbHelper(connectEventArgs.LoginUid);
        //检查模块是否启用
        if (!moduleEnable.BiliSubscription)
            return;
        if (!SoraServiceFactory.TryGetApi(connectEventArgs.LoginUid, out SoraApi api))
        {
            Log.Error("SoraApi", $"无法获取账号[{connectEventArgs.LoginUid}]API实例");
            return;
        }

        foreach (var subscription in subscriptions)
        {
            //臭DD的订阅
            foreach (var biliUser in subscription.SubscriptionId)
                await GetDynamic(api,
                                 biliUser,
                                 subscription.GroupId,
                                 dbHelper);

            //直播动态订阅
            foreach (var biliUser in subscription.LiveSubscriptionId)
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
        (UserInfo sender, _) = await BiliApis.GetLiveUserInfo(biliUser);
        if (sender is null || sender.Code != 0)
        {
            Log.Error("BiliApi", $"无法获取用户信息[{biliUser}]");
            if (sender is not null)
                Log.Error("BiliApi[GetLiveUserInfo]", $"Api error:{sender.Message}");
            return;
        }

        (ulong dId, long dTs, JToken dyJson) = await BiliApis.GetLatestDynamicId(biliUser);
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
        //没有群需要发送消息
        if (targetGroups.Count == 0)
        {
            Log.Debug("动态获取", $"{sender.UserName}的动态已是最新");
            return;
        }

        Log.Info("Sub", $"更新[{soraApi.GetLoginUserId()}]的动态订阅");

        //构建消息
        List<CustomNode> nodes = new()
        {
            //动态渲染图
            new CustomNode(sender.UserName,
                           114514,
                           await GetChromePic($"https://t.bilibili.com/{dId}"))
        };

        //纯文本内容
        if (dyJson.SelectToken("modules.module_dynamic.desc.text") is JValue textDetail)
        {
            nodes.Add(new CustomNode(sender.UserName,
                                     114514,
                                     "动态内容:"));
            nodes.Add(new CustomNode(sender.UserName,
                                     114514,
                                     textDetail.Value<string>() ?? string.Empty));
        }

        //图片内容
        if (dyJson.SelectToken("modules.module_dynamic.major.draw.items") is JArray { HasValues: true } picDetail)
        {
            nodes.Add(new CustomNode(sender.UserName,
                                     114514,
                                     "动态图片:"));
            nodes.AddRange(picDetail.Select(item => new CustomNode(sender.UserName,
                                                                   114514,
                                                                   SoraSegment.Image(item.Value<string>("src")))));
        }

        //向未发送消息的群发送消息
        foreach (long targetGroup in targetGroups)
        {
            Log.Info("动态获取", $"获取到{sender.UserName}的最新动态，向群{targetGroup}发送动态信息");
            await soraApi.SendGroupMessage(targetGroup, $"{sender.UserName}有新动态！");
            await soraApi.SendGroupForwardMsg(targetGroup, nodes);
            if (!dbHelper.UpdateDynamic(targetGroup,
                                        sender.Uid,
                                        dTs.ToDateTime()))
                Log.Error("数据库", "更新动态记录时发生了数据库错误");
        }
    }

    private static async Task<SoraSegment> GetChromePic(string url)
    {
        Page   page = await StaticVar.Chrome.NewPageAsync();
        string dId  = Path.GetFileName(url);
        Log.Debug("动态ID", dId);
        await page.SetViewportAsync(new ViewPortOptions
        {
            Width  = 2000,
            Height = 1500
        });

        await page.GoToAsync(url);

        //动态
        //await page.QuerySelectorAsync("#app > div.content > div > div > div.bili-dyn-item__main");
        ElementHandle dyElement = await page.WaitForXPathAsync("//*[@id=\"app\"]/div[2]/div/div/div[1]");

        if (dyElement is null)
        {
            Log.Debug($"动态{dId}", "无法获取动态内容");
            return "404";
        }

        Log.Debug($"动态{dId}", $"获取到动态元素[{dyElement.RemoteObject.ObjectId}]");

        string picB64 = await dyElement.ScreenshotBase64Async(new ScreenshotOptions { Type = ScreenshotType.Png });

        //关闭页面
        await page.CloseAsync();
        await page.DisposeAsync();

        SoraSegment img = SoraSegment.Image($"base64://{picB64}");
        return img;
    }
}