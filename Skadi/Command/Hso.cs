using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using PyLibSharp.Requests;
using Skadi.Database.Helpers;
using Skadi.Entities.ConfigModule;
using Skadi.Interface;
using Skadi.Services;
using Skadi.Tool;
using Sora.Attributes.Command;
using Sora.Entities;
using Sora.Entities.Segment;
using Sora.Enumeration;
using Sora.EventArgs.SoraEvent;
using YukariToolBox.LightLog;
using static Skadi.Tool.CommandCdUtil;
using MatchType = Sora.Enumeration.MatchType;

namespace Skadi.Command;

[CommandSeries(SeriesName = "hso")]
public class HsoCommand
{
#region 指令响应

    /// <summary>
    /// 用于处理传入指令
    /// </summary>
    [UsedImplicitly]
    [SoraCommand(SourceType = MessageSourceMatchFlag.Group,
                 CommandExpressions = new[] { "来点色图", "来点涩图", "我要看色图" })]
    public async void HsoPic(BaseMessageEventArgs eventArgs)
    {
        eventArgs.IsContinueEventChain = false;

        IGenericStorage genericStorage = SkadiApp.GetService<IGenericStorage>();
        UserConfig      userConfig     = genericStorage.GetUserConfig(eventArgs.LoginUid);
        long            groupId        = (eventArgs as GroupMessageEventArgs)!.SourceGroup;

        if (userConfig is null)
        {
            Log.Error("Config|Hso", "无法获取用户配置文件");
            return;
        }

        if (CheckGroupBlock(userConfig, (eventArgs as GroupMessageEventArgs)!))
            return;
        if (IsInCD(groupId, eventArgs.Sender, CommandFlag.Setu, 60))
        {
            Log.Warning("HSO", $"{eventArgs.LoginUid} 正在尝试CD内再次涩涩，重置CD");
            return;
        }

        Log.Info("HSO", $"[{eventArgs.Sender.Id}]加载色图");
        //刷新数据库计数
        var hsoDbHelper = new HsoDbHelper(eventArgs.LoginUid);
        if (!hsoDbHelper.AddOrUpdate(eventArgs.Sender, groupId))
            await eventArgs.Reply("数据库错误(count)");

        Hso hso = userConfig.HsoConfig;
        Log.Debug("源", hso.Source);
        //本地模式
        if (hso.Source.Equals("Local"))
        {
            SendLocalPic(hso, (eventArgs as GroupMessageEventArgs)!);
            return;
        }

        Log.Info("NET", "尝试获取涩图");
        await eventArgs.Reply("正在获取涩图中...");

        string apiServer = hso.Source switch
                           {
                               "Wakamo" => "https://api.kosaka-wakamo.top",
                               _        => hso.Source
                           };
        
        (int code, JToken data) = await GetRandomSetuJson(apiServer, hso.CheckSSLCert);
        if (code != 200)
        {
            await eventArgs.Reply($"哇哦~发生了网络错误[{code}]，请联系机器人所在服务器管理员");
            return;
        }

        //json处理
        try
        {
            if (!int.TryParse(data["statusCode"]?.ToString() ?? "-100", out var retCode) && retCode != 200)
            {
                Log.Error("Hso",
                          retCode == -100
                              ? "Server response null message"
                              : $"Server response code {retCode}");
                await eventArgs.Reply("哇奧色图不见了\n请联系机器人服务器管理员");
                return;
            }

            if (!long.TryParse(data["data"]?[0]?["pid"]?.ToString(), out var pid)
                || !int.TryParse(data["data"]?[0]?["index"]?.ToString(), out var index))
            {
                await eventArgs.Reply("无法获取到色图信息");
                return;
            }

            //图片链接
            Log.Debug("获取到图片", $"pid:{pid}|index:{index}");
            SoraSegment image = await MediaUtil.GetPixivImage(eventArgs.LoginUid, pid, index, apiServer, hso.CheckSSLCert);
            //检查是否有设置代理
            await eventArgs.Reply(HsoMessageBuilder(data["data"]?[0], image), TimeSpan.FromSeconds(20));
        }
        catch (Exception e)
        {
            Log.Error("色图下载失败", $"网络下载数据错误\n{Log.ErrorLogBuilder(e)}");
        }
    }

    [UsedImplicitly]
    [SoraCommand(SourceType = MessageSourceMatchFlag.Group,
                 CommandExpressions = new[] { @"^让我康康[0-9]+$" },
                 MatchType = MatchType.Regex)]
    public async void HsoPicIndexSearchAll(BaseMessageEventArgs eventArgs)
    {
        eventArgs.IsContinueEventChain = false;
        long pid = Convert.ToInt64(eventArgs.Message.RawText[4..]);
        Log.Info("让我康康", $"[{eventArgs.Sender.Id}]加载图片:{pid}");
        await eventArgs.Reply("什么，有好康的");

        //TODO
        List<SoraSegment> images = []; // await MediaUtil.GetMultiPixivImage(eventArgs.LoginUid, pid);
        // ReSharper disable once AsyncVoidLambda
        images.ForEach(async i =>
        {
            await eventArgs.Reply(i);
            await Task.Delay(500);
        });
    }

    [UsedImplicitly]
    [SoraCommand(SourceType = MessageSourceMatchFlag.Group,
                 CommandExpressions = new[] { @"^让我康康[0-9]+\s[0-9]+$" },
                 MatchType = MatchType.Regex)]
    public async void HsoPicIndexSearch(BaseMessageEventArgs eventArgs)
    {
        eventArgs.IsContinueEventChain = false;
        var  picInfos = eventArgs.Message.RawText.Split(' ');
        long pid      = Convert.ToInt64(picInfos[0][4..]);
        int  index    = Convert.ToInt32(picInfos[1]);
        Log.Info("让我康康", $"加载图片:{pid}-{index}");
        await eventArgs.Reply("什么，有好康的");

        //TODO
        SoraSegment image = "shit"; //await MediaUtil.GetPixivImage(eventArgs.LoginUid, pid, index);
        await eventArgs.Reply(image, TimeSpan.FromSeconds(20));
    }

    [UsedImplicitly]
    [SoraCommand(SourceType = MessageSourceMatchFlag.Group,
                 CommandExpressions = new[] { "来点色批" },
                 MatchType = MatchType.Full)]
    public static async void HsoRank(BaseMessageEventArgs eventArgs)
    {
        eventArgs.IsContinueEventChain = false;
        var hsoDbHelper = new HsoDbHelper(eventArgs.LoginUid);
        if (!hsoDbHelper.GetGroupRank((eventArgs as GroupMessageEventArgs)!.SourceGroup, out var rankList))
        {
            await eventArgs.Reply("数据库错误(count)");
            return;
        }

        if (rankList == null || rankList.Count == 0)
        {
            await eventArgs.Reply("你群连个色批都没有");
        }
        else
        {
            var message = new MessageBody { "让我康康到底谁最能冲\r\n" };
            foreach (var count in rankList)
                message.AddRange(count.Uid.ToAt() + $"冲了{count.Count}次" + "\r\n");

            //删去多余的换行
            message.RemoveAt(message.Count - 1);
            await eventArgs.Reply(message);
        }
    }

    [UsedImplicitly]
    [SoraCommand(SourceType = MessageSourceMatchFlag.Group,
                 CommandExpressions = new[] { @"^AD[0-9]+\s[0-9]+$" })]
    public async void HsoAddPic(BaseMessageEventArgs eventArgs)
    {
        eventArgs.IsContinueEventChain = false;
        //判断权限
        if (!eventArgs.IsSuperUser)
        {
            await eventArgs.Reply("权限不足拒绝执行");
            return;
        }

        string[] picInfos = eventArgs.Message.RawText.Split(' ');
        string   picId    = picInfos[0][2..];
        string   picIndex = picInfos[1];
        Log.Info("cloud database", $"[{eventArgs.Sender.Id}]正在添加图片:{picId}-{picIndex}");
        await eventArgs.Reply($"Adding[{picId}]...");
        //读取用户配置
        IGenericStorage genericStorage = SkadiApp.GetService<IGenericStorage>();
        UserConfig      userConfig     = genericStorage.GetUserConfig(eventArgs.LoginUid);
        if (userConfig is null)
        {
            Log.Error("Config|Hso", "无法获取用户配置文件");
            await eventArgs.Reply("Try load user config error");
            return;
        }

        if (string.IsNullOrEmpty(userConfig.HsoConfig.YukariApiKey))
        {
            Log.Error("apikey", "apikey is null");
            await eventArgs.Reply("error:apikey is null");
            return;
        }

        JToken resData;
        try
        {
            var res =
                await Requests.PostAsync("https://api.yukari.one/setu/illust",
                                         new ReqParams
                                         {
                                             PostJson = new
                                             {
                                                 apikey = userConfig.HsoConfig.YukariApiKey,
                                                 pid    = picId,
                                                 index  = picIndex
                                             },
                                             Timeout                   = 10000,
                                             IsThrowErrorForStatusCode = false,
                                             IsThrowErrorForTimeout    = false
                                         });

            if (res is null)
            {
                Log.Error("net", "net error");
                await eventArgs.Reply("error:net error");
                return;
            }

            if (res.StatusCode != HttpStatusCode.OK)
            {
                string message = res.Json()?["message"]?.ToString() ?? "unknown";
                Log.Error("api error", $"{res.StatusCode}|{message}");
                await eventArgs.Reply($"error:api err\r\n{message}");
                return;
            }

            resData = res.Json();
        }
        catch (Exception e)
        {
            Log.Error(e, "HsoAddPic", "cannot add pic");
            await eventArgs.Reply("出错了，寄");
            return;
        }

        if (resData == null)
        {
            Log.Error("api error", "api error (null response)");
            await eventArgs.Reply("api error (null response)");
            return;
        }

        if (Convert.ToInt32(resData["code"]) != 0)
        {
            Log.Error("api error", $"api error (code:{resData["code"]})]");
            await eventArgs.Reply($"api error (code:{resData["code"]})]\r\n{resData["message"]}");
            return;
        }

        Log.Info("pic upload", "pic upload success");
        await eventArgs.Reply($"success[{picId}]");
    }

#endregion

#region 私有方法

    /// <summary>
    /// 获取随机图片信息
    /// </summary>
    private static async ValueTask<(int code, JToken json)> GetRandomSetuJson(string server, bool checkSsl)
    {
        try
        {
            //向服务器发送请求
            var reqResponse = await Requests.GetAsync($"{server}/setu",
                                                      new ReqParams
                                                      {
                                                          Timeout        = 3000,
                                                          isCheckSSLCert = checkSsl
                                                      });
            if (reqResponse.StatusCode != HttpStatusCode.OK)
            {
                Log.Error("Net", $"return code {(int)reqResponse.StatusCode}");
                return ((int)reqResponse.StatusCode, null);
            }

            return (200, reqResponse.Json());
        }
        catch (Exception e)
        {
            Log.Error("网络发生错误",
                      $"{Log.ErrorLogBuilder(e)}\r\n\r\n{PyLibSharp.Requests.Utils.GetInnerExceptionMessages(e)}");
            return (-1, null);
        }
    }

    private static async void SendLocalPic(Hso hso, GroupMessageEventArgs eventArgs)
    {
        var picNames = Directory.GetFiles(GenericStorage.GetHsoPath());
        if (picNames.Length == 0)
        {
            await eventArgs.Reply("机器人管理者没有在服务器上塞色图\r\n你去找他要啦!");
            return;
        }

        var randFile     = new Random();
        var localPicPath = $"{picNames[randFile.Next(0, picNames.Length - 1)]}";
        Log.Debug("发送图片", localPicPath);
        await eventArgs.Reply(hso.CardImage
                                  ? SoraSegment.CardImage(localPicPath)
                                  : SoraSegment.Image(localPicPath));
    }

    /// <summary>
    /// 构建发送用的消息段
    /// </summary>
    /// <param name="picInfo">图片信息Json</param>
    /// <param name="image">图片</param>
    private static MessageBody HsoMessageBuilder(JToken picInfo, SoraSegment image)
    {
        var textBuilder = new StringBuilder();
        textBuilder.AppendLine();
        textBuilder.AppendLine(picInfo["title"]?.ToString());
        textBuilder.Append($"pid:{picInfo["pid"]}");
        if (picInfo["index"] != null)
        {
            textBuilder.Append(" - ");
            textBuilder.Append(picInfo["index"]);
        }

        textBuilder.AppendLine();
        textBuilder.Append($"作者:{picInfo["author"]}");

        //构建消息
        MessageBody msg = new()
        {
            image,
            textBuilder.ToString()
        };
        return msg;
    }

    /// <summary>
    /// 检查群是否被屏蔽
    /// </summary>
    /// <param name="config">配置文件</param>
    /// <param name="eventArgs">事件参数</param>
    private static bool CheckGroupBlock(UserConfig config, GroupMessageEventArgs eventArgs)
    {
        return config.HsoConfig.GroupBlock.Any(gid => gid == eventArgs.SourceGroup);
    }

#endregion
}