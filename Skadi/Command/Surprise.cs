using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Skadi.Entities.ConfigModule;
using Skadi.Interface;
using Sora.Attributes.Command;
using Sora.Entities.Segment;
using Sora.Entities.Segment.DataModel;
using Sora.Enumeration;
using Sora.EventArgs.SoraEvent;

namespace Skadi.Command;

[CommandSeries]
public class Surprise
{
#region 私有方法

    [UsedImplicitly]
    [SoraCommand(SourceType = MessageSourceMatchFlag.Group,
                 CommandExpressions = new[] { "dice" })]
    public async ValueTask RandomNumber(BaseMessageEventArgs eventArgs)
    {
        eventArgs.IsContinueEventChain = false;
        IGenericStorage genericStorage = SkadiApp.GetService<IGenericStorage>();
        UserConfig      userConfig     = genericStorage.GetUserConfig(eventArgs.LoginUid);
        if (userConfig is null || !userConfig.ModuleSwitch.HaveFun)
            return;
        await (eventArgs as GroupMessageEventArgs)!.SourceGroup.SendGroupMessage(SoraSegment.At(eventArgs.Sender.Id)
                                                                                 + "丢出了\r\n"
                                                                                 + Random.Shared.Next(1, 6).ToString());
    }

    [UsedImplicitly]
    [SoraCommand(SourceType = MessageSourceMatchFlag.Group,
                 CommandExpressions = new[] { "优质睡眠", "昏睡红茶", "昏睡套餐", "健康睡眠" })]
    public async ValueTask RedTea(BaseMessageEventArgs eventArgs)
    {
        eventArgs.IsContinueEventChain = false;
        IGenericStorage genericStorage = SkadiApp.GetService<IGenericStorage>();
        UserConfig      userConfig     = genericStorage.GetUserConfig(eventArgs.LoginUid);
        if (userConfig is null || !userConfig.ModuleSwitch.HaveFun)
            return;
        await (eventArgs as GroupMessageEventArgs)!.SourceGroup.EnableGroupMemberMute(eventArgs.Sender.Id,
                                                                                      28800);
    }

    [UsedImplicitly]
    [SoraCommand(SourceType = MessageSourceMatchFlag.Group,
                 CommandExpressions = new[] { @"^选择.+(还是.+)+$" },
                 MatchType = MatchType.Regex)]
    public async ValueTask Choice(BaseMessageEventArgs eventArgs)
    {
        if (eventArgs.Message.MessageBody.Count != 1
            && eventArgs.Message.MessageBody[0].MessageType != SegmentType.Text)
            return;
        eventArgs.IsContinueEventChain = false;
        string       text    = (eventArgs.Message.MessageBody[0].Data as TextSegment)!.Content[2..].Trim();
        List<string> options = text.Split("还是").ToList();

        StringBuilder re = new();
        re.AppendLine("你的选项有：");
        for (int i = 0; i < options.Count; i++)
            re.AppendLine($"{i + 1}、{options[i]}");
        string result = Random.Shared.Next(0, 100) < 10 ? "我全都要" : options[Random.Shared.Next(0, options.Count)];
        re.Append($"建议你选择：{result}");
        await eventArgs.Reply(re.ToString());
    }

#endregion
}