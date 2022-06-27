using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AntiRain.Config;
using AntiRain.Config.ConfigModule;
using JetBrains.Annotations;
using Sora.Attributes.Command;
using Sora.Entities;
using Sora.Entities.Segment;
using Sora.Entities.Segment.DataModel;
using Sora.Enumeration;
using Sora.EventArgs.SoraEvent;
using YukariToolBox.LightLog;

namespace AntiRain.Command;

[CommandGroup]
public class Surprise
{
    #region 私有方法

    [UsedImplicitly]
    [SoraCommand(
        SourceType = SourceFlag.Group,
        CommandExpressions = new[] {"dice"})]
    public async ValueTask RandomNumber(GroupMessageEventArgs eventArgs)
    {
        eventArgs.IsContinueEventChain = false;
        if (!ConfigManager.TryGetUserConfig(eventArgs.LoginUid, out UserConfig config) &&
            !config.ModuleSwitch.HaveFun) return;
        await eventArgs.SourceGroup.SendGroupMessage(
            SoraSegment.At(eventArgs.Sender.Id) +
            "丢出了\r\n"                           +
            Random.Shared.Next(1, 6).ToString());
    }

    [UsedImplicitly]
    [SoraCommand(
        SourceType = SourceFlag.Group,
        CommandExpressions = new[] {"优质睡眠", "昏睡红茶", "昏睡套餐", "健康睡眠"})]
    public async ValueTask RedTea(GroupMessageEventArgs eventArgs)
    {
        eventArgs.IsContinueEventChain = false;
        if (!ConfigManager.TryGetUserConfig(eventArgs.LoginUid, out UserConfig config) &&
            !config.ModuleSwitch.HaveFun) return;
        await eventArgs.SourceGroup.EnableGroupMemberMute(eventArgs.Sender.Id,
            28800);
    }

    //TODO 支持图片消息
    [UsedImplicitly]
    [SoraCommand(
        SourceType = SourceFlag.Group,
        CommandExpressions = new[] {@"^选择.+(还是.+)+$"})]
    public async ValueTask Choice(GroupMessageEventArgs eventArgs)
    {
        if (eventArgs.Message.MessageBody.Count          != 1 &&
            eventArgs.Message.MessageBody[0].MessageType != SegmentType.Text) return;
        eventArgs.IsContinueEventChain = false;
        string       text    = (eventArgs.Message.MessageBody[0].Data as TextSegment)!.Content[2..].Trim();
        List<string> options = text.Split("还是").ToList();

        StringBuilder re = new StringBuilder();
        re.AppendLine("你的选项有：");
        for (int i = 0; i < options.Count; i++)
            re.AppendLine($"{i + 1}、{options[i]}");
        string result = Random.Shared.Next(0, 100) < 10 ? "我全都要" : options[Random.Shared.Next(0, options.Count)];
        re.Append($"建议你选择：{result}");
        await eventArgs.Reply(re.ToString());

        // Guid id = eventArgs.Message.MessageBody
        //                    .Where(m => m.Data is TextSegment t &&
        //                         t.Content.IndexOf("还是", StringComparison.Ordinal) != -1)
        //                    .Select(m => m.Id)
        //                    .First();
        // int index = eventArgs.Message.MessageBody.IndexOfById(id);
        // Log.Info("index", $"{index}");
        // bool selectL = Random.Shared.Next(0, 100) > 50;
        // MessageBody msg = selectL
        //     ? eventArgs.Message.MessageBody.Take(index).ToMessageBody()
        //     : eventArgs.Message.MessageBody.Skip(index).ToMessageBody();
        // string text = (msg[0].Data as TextSegment)!.Content[2..];
        // if (string.IsNullOrEmpty(text)) msg.RemoveAt(0);
        // else msg[0] = text;
        //
        // await eventArgs.Reply("选择" + msg);
    }

    #endregion
}