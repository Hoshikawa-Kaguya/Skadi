﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PyLibSharp.Requests;
using Sora.Attributes.Command;
using Sora.Entities.Segment;
using Sora.Enumeration;
using Sora.EventArgs.SoraEvent;

namespace Skadi.Command;

[CommandSeries(SeriesName = "MoeGoe")]
public class MoeGoe
{
    [UsedImplicitly]
    [SoraCommand(SourceType = MessageSourceMatchFlag.Group,
                 CommandExpressions = new[] { @"^宁宁说.+$" },
                 MatchType = MatchType.Regex)]
    public async ValueTask NeneSpeak(BaseMessageEventArgs eventArgs)
    {
        string text = eventArgs.Message.RawText[3..];
        await GetVoice(text, 0, eventArgs);
    }

    [UsedImplicitly]
    [SoraCommand(SourceType = MessageSourceMatchFlag.Group,
                 CommandExpressions = new[] { @"^咩咕噜说.+$" },
                 MatchType = MatchType.Regex)]
    public async ValueTask MeguruSpeak(BaseMessageEventArgs eventArgs)
    {
        string text = eventArgs.Message.RawText[3..];
        await GetVoice(text, 1, eventArgs);
    }

    [UsedImplicitly]
    [SoraCommand(SourceType = MessageSourceMatchFlag.Group,
                 CommandExpressions = new[] { @"^芳乃说.+$" },
                 MatchType = MatchType.Regex)]
    public async ValueTask YoshinoSpeak(BaseMessageEventArgs eventArgs)
    {
        string text = eventArgs.Message.RawText[3..];
        if (text is null) return;
        eventArgs.IsContinueEventChain = false;
        await GetVoice(text, 2, eventArgs);
    }

    [UsedImplicitly]
    [SoraCommand(SourceType = MessageSourceMatchFlag.Group,
                 CommandExpressions = new[] { @"^茉子说.+$" },
                 MatchType = MatchType.Regex)]
    public async ValueTask MakoSpeak(BaseMessageEventArgs eventArgs)
    {
        string text = eventArgs.Message.RawText[3..];
        await GetVoice(text, 3, eventArgs);
    }

    [UsedImplicitly]
    [SoraCommand(SourceType = MessageSourceMatchFlag.Group,
                 CommandExpressions = new[] { @"^丛雨说.+$" },
                 MatchType = MatchType.Regex)]
    public async ValueTask MurasameSpeak(BaseMessageEventArgs eventArgs)
    {
        string text = eventArgs.Message.RawText[3..];
        await GetVoice(text, 4, eventArgs);
    }

    [UsedImplicitly]
    [SoraCommand(SourceType = MessageSourceMatchFlag.Group,
                 CommandExpressions = new[] { @"^小春说.+$" },
                 MatchType = MatchType.Regex)]
    public async ValueTask KoharuSpeak(BaseMessageEventArgs eventArgs)
    {
        string text = eventArgs.Message.RawText[3..];
        await GetVoice(text, 5, eventArgs);
    }

    [UsedImplicitly]
    [SoraCommand(SourceType = MessageSourceMatchFlag.Group,
                 CommandExpressions = new[] { @"^七海说.+$" },
                 MatchType = MatchType.Regex)]
    public async ValueTask NanamiSpeak(BaseMessageEventArgs eventArgs)
    {
        string text = eventArgs.Message.RawText[3..];
        await GetVoice(text, 6, eventArgs);
    }

    private async ValueTask GetVoice(string text, int id, BaseMessageEventArgs eventArgs)
    {
        ReqResponse response = await Requests.GetAsync("https://moegoe.azurewebsites.net/api/speak",
                                                       new ReqParams
                                                       {
                                                           Params = new Dictionary<string, string>
                                                           {
                                                               { "text", text },
                                                               { "id", id.ToString() }
                                                           },
                                                           Timeout                   = 60000,
                                                           IsThrowErrorForStatusCode = false,
                                                           IsThrowErrorForTimeout    = false
                                                       });
        if (response.StatusCode != HttpStatusCode.OK)
            return;
        await eventArgs.Reply(SoraSegment.Record($"base64://{Convert.ToBase64String(response.Content)}"));
        eventArgs.IsContinueEventChain = false;
    }
}