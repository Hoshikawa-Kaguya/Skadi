﻿using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AntiRain.Tool;
using BilibiliApi.Video;
using BilibiliApi.Video.Models;
using JetBrains.Annotations;
using SixLabors.ImageSharp;
using Sora.Attributes.Command;
using Sora.Entities;
using Sora.Entities.Segment;
using Sora.Enumeration;
using Sora.EventArgs.SoraEvent;
using YukariToolBox.LightLog;
using MatchType = Sora.Enumeration.MatchType;

namespace AntiRain.Command;

/// <summary>
/// BV/AV号解析
/// </summary>
[CommandGroup]
public static class BlibiliVideo
{
    [UsedImplicitly]
    [SoraCommand(
        SourceType = SourceFlag.Group,
        CommandExpressions = new[] {@"^BV1[1-9A-NP-Za-km-z]{9}$", @"^AV[1-9][0-9]*$" },
        MatchType = MatchType.Regex)]
    public static async ValueTask BiliVideoGet(GroupMessageEventArgs eventArgs)
    {
        await VideoInfoId(eventArgs);
    }

    [UsedImplicitly]
    [SoraCommand(
        SourceType = SourceFlag.Group,
        CommandExpressions = new[] {@"https://b23\.tv/[a-zA-Z0-9]+"},
        MatchType = MatchType.Regex,
        Priority = 0)]
    public static async ValueTask VideoInfoByShortUrl(GroupMessageEventArgs eventArgs)
    {
        //获取短链
        var    urlRegex    = new Regex(@"https://b23\.tv/[a-zA-Z0-9]+");
        string videoUrlStr = urlRegex.Match(eventArgs.Message.RawText).Value;
        if (string.IsNullOrEmpty(videoUrlStr)) return;
        //网络请求获取跳转地址
        var                 handler  = new HttpClientHandler {AllowAutoRedirect = false};
        var                 client   = new HttpClient(handler);
        HttpResponseMessage response = await client.GetAsync(videoUrlStr);
        //解析id
        var    idRegex    = new Regex(@"(?:BV|bv|AV|av)[a-zA-Z0-9]+");
        string videoIdStr = idRegex.Match(response.Headers.Location?.ToString() ?? string.Empty).Value;
        if (string.IsNullOrEmpty(videoIdStr))
        {
            Log.Error("Mini App", "解析ID为空");
            return;
        }

        //获取视频信息
        VideoInfo videoInfo = VideoApis.GetVideoInfo(videoIdStr);
        if (videoInfo.Code != 0)
        {
            await eventArgs.Reply($"API发生错误({videoInfo.Code})\r\nmessage:{videoInfo.Message}");
            return;
        }

        //判断小程序
        if (eventArgs.Message.IsCodeCard())
            await eventArgs.Reply(SoraSegment.Image("a4afbbbbf9f5224771a39b1b3cbc402d.image"));
        //发送视频信息
        await eventArgs.Reply(GenReplyMessage(videoInfo));
    }

    private static async ValueTask VideoInfoId(GroupMessageEventArgs eventArgs)
    {
        VideoInfo videoInfo = VideoApis.GetVideoInfo(eventArgs.Message.RawText);
        if (videoInfo.Code != 0)
        {
            await eventArgs.Reply($"API发生错误({videoInfo.Code})\r\nmessage:{videoInfo.Message}");
            return;
        }

        //发送视频信息
        await eventArgs.Reply(GenReplyMessage(videoInfo));
    }

    private static MessageBody GenReplyMessage(VideoInfo info)
    {
        var messageBuilder = new StringBuilder();
        messageBuilder.AppendLine($"简介:{info.Desc}");
        messageBuilder.AppendLine($"UP:{info.AuthName}");
        messageBuilder.AppendLine($"https://space.bilbili.com/{info.AuthUid}\r\n");
        messageBuilder.AppendLine($"投稿时间:{info.PublishTime:yyyy-MM-dd HH:mm:ss}");

        string b64Pic =
            MediaUtil.DrawTextImage(messageBuilder.ToString(), Color.Black, Color.White);

        var sendMessage = new MessageBody
        {
            $"Bilibili视频解析\r\n[{info.Bid}(av{info.Aid})]\r\n",
            $"标题:{info.Title}",
            SoraSegment.Image($"{info.CoverUrl}"),
            SoraSegment.Image($"base64://{b64Pic}"),
            $"Link:https://b23.tv/{info.Bid}"
        };

        return sendMessage;
    }
}