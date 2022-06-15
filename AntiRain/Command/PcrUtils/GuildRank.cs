using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AntiRain.Config;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PyLibSharp.Requests;
using Sora.Attributes.Command;
using Sora.Enumeration;
using Sora.EventArgs.SoraEvent;
using Sora.Util;
using YukariToolBox.LightLog;

namespace AntiRain.Command.PcrUtils;

[CommandGroup]
public static class GuildRank
{
    #region 查询指令

    /// <summary>
    /// 镜华站查询
    /// </summary>
    [UsedImplicitly]
    [SoraCommand(
        SourceType = SourceFlag.Group,
        CommandExpressions = new[] {@"^镜华排名\S*$"},
        MatchType = MatchType.Regex)]
    public static async ValueTask KyoukaRank(GroupMessageEventArgs eventArgs)
    {
        eventArgs.IsContinueEventChain = false;
        if (!ConfigManager.TryGetUserConfig(eventArgs.LoginUid, out var config) &&
            !config.ModuleSwitch.PcrGuildRank) return;
        //网络响应
        JToken response;
        //获取公会名
        string guildName = eventArgs.Message.RawText[4..];

        Log.Debug("guild rank", $"get guild name[{guildName}]");
        if (string.IsNullOrEmpty(guildName))
        {
            await eventArgs.Reply("此群未被记录为公会\r\n请建会后再查询或输入公会名进行查询");
            return;
        }

        //获取网络响应
        try
        {
            //获取查询结果
            Log.Info("NET", $"尝试查询[{guildName}]会站排名");
            await eventArgs.Reply($"正在查询公会[{guildName}]的排名...");
            var reqResponse =
                await Requests.PostAsync("https://service-kjcbcnmw-1254119946.gz.apigw.tencentcs.com/name/0",
                    new ReqParams
                    {
                        Timeout = 3000,
                        PostJson = new JObject
                        {
                            ["clanName"] = guildName,
                            ["history"]  = 0
                        },
                        Header = new Dictionary<HttpRequestHeader, string>
                        {
                            {
                                HttpRequestHeader.Referer,
                                "https://kyouka.kengxxiao.com/"
                            }
                        },
                        CustomHeader = new Dictionary<string, string>
                        {
                            {"Custom-Source", "AntiRainBot"},
                            {"Origin", "https://kyouka.kengxxiao.com/"}
                        }
                    });
            //判断响应
            if (reqResponse.StatusCode != HttpStatusCode.OK)
            {
                await
                    eventArgs
                       .Reply($"哇哦~发生了网络错误，请联系机器人所在服务器管理员\r\n{reqResponse.StatusCode}({(int) reqResponse.StatusCode})");
                Log.Error("网络发生错误", $"Code[{reqResponse.StatusCode}({(int) reqResponse.StatusCode})]");
                //阻止下一步处理
                return;
            }

            //读取返回值
            response = reqResponse.Json();
            //判断空数据
            if (response == null || !response.Any())
            {
                await eventArgs.Reply("发生了未知错误，请请向开发者反馈问题");
                Log.Error("JSON数据读取错误", "从网络获取的文本为空");
                return;
            }

            Log.Info("获取JSON成功", response.ToString(Formatting.None));
        }
        catch (Exception e)
        {
            await eventArgs.Reply($"哇哦~发生了网络错误，请联系机器人所在服务器管理员\n{e.Message}");
            Log.Error(e, "KyoukaRank", "在获取排名时发生错误");
            //阻止下一步处理
            return;
        }

        //JSON数据处理
        try
        {
            if (response["full"] == null)
            {
                await eventArgs.Reply("发生了未知错误，请请向开发者反馈问题");
                Log.Error("JSON数据读取错误", "从网络获取的JSON格式可能有问题");
                return;
            }

            //在有查询结果时查找值
            if (!response["full"]?.ToString().Equals("0") ?? false)
            {
                if (!response["full"]?.ToString().Equals("1") ?? false)
                    await eventArgs.Reply("查询到多个公会，可能存在重名或关键词错误");
                Log.Info("JSON处理成功", "向用户发送数据");
                long.TryParse(response["ts"]?.ToString() ?? "0", out long updateTimeStamp);
                await eventArgs.Reply("查询成功！\n"                               +
                    $"公会:{guildName}\n"                                       +
                    $"排名:{response["data"]?[0]?["rank"]}\n"                   +
                    $"总分数:{response["data"]?[0]?["damage"]}\n"                +
                    $"会长:{response["data"]?[0]?["leader_name"]}\n"            +
                    $"数据更新时间:{updateTimeStamp.ToDateTime():MM-dd HH:mm:ss}\n" +
                    "如果查询到的信息有误，有可能关键词错误或公会排名在20060之后");
            }
            else
            {
                await eventArgs.Reply("未找到任意公会\n请检查是否查询的错误的公会名或公会排名在20060之后");
                Log.Info("JSON处理成功", "所查询列表为空");
            }
        }
        catch (Exception e)
        {
            await eventArgs.Reply($"在处理数据时发生了错误，请请向开发者反馈问题\n{e.Message}");
            Log.Error(e, "KyoukaRank", "JSON数据读取错误");
        }
    }

    #endregion
}