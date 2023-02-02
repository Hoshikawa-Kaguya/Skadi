using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Skadi.Entities;
using Skadi.Interface;
using Skadi.Services;
using Sora.Attributes.Command;
using Sora.Entities;
using Sora.Entities.Segment;
using Sora.Entities.Segment.DataModel;
using Sora.Enumeration;
using Sora.Enumeration.EventParamsType;
using Sora.EventArgs.SoraEvent;
using YukariToolBox.LightLog;

namespace Skadi.Command;

/// <summary>
/// 问答
/// </summary>
[CommandSeries(SeriesName = "QA")]
public class QA
{
    [UsedImplicitly]
    [SoraCommand(SourceType = SourceFlag.Group,
                 CommandExpressions = new[] { @"^有人问[\s\S]+你答[\s\S]+$" },
                 MatchType = MatchType.Regex,
                 PermissionLevel = MemberRoleType.Admin)]
    public async ValueTask CreateQuestion(GroupMessageEventArgs eventArgs)
    {
        if (!QaService.MessageCheck(eventArgs.Message.MessageBody))
            return;
        eventArgs.IsContinueEventChain = false;
        IQaService qaService = StaticStuff.ServiceProvider.GetServices<IQaService>()
                                          .SingleOrDefault(s => s.LoginUid == eventArgs.LoginUid);
        if (qaService is null)
        {
            Log.Error("QA", "未找到QA服务");
            await eventArgs.Reply("未找到QA服务");
            return;
        }

        //查找分割点
        Guid backSegmentId = eventArgs.Message.MessageBody
                                      .Where(s =>
                                                 s.Data is TextSegment t
                                                 && t.Content.IndexOf("你答", StringComparison.Ordinal) != -1)
                                      .Select(s => s.Id)
                                      .FirstOrDefault();
        if (backSegmentId == Guid.Empty)
            return;
        eventArgs.IsContinueEventChain = false;
        int nextMsgIndex = eventArgs.Message.MessageBody.IndexOfById(backSegmentId);

        //切片预处理
        MessageBody fMessage = new(eventArgs.Message.MessageBody.Take(nextMsgIndex == 0 ? 1 : nextMsgIndex).ToList());
        if (fMessage[0].Data is not TextSegment srcFSegment)
            return;
        MessageBody bMessage = new(eventArgs.Message.MessageBody.Skip(nextMsgIndex).ToList());
        if (bMessage[0].Data is not TextSegment srcBSegment)
            return;

        //问题消息切片
        if (srcFSegment.Content.Equals("有人问"))
        {
            fMessage.RemoveAt(0);
        }
        else
        {
            int qEndIndex = srcFSegment.Content.IndexOf("你答", StringComparison.Ordinal);
            string msg = qEndIndex != -1
                ? srcFSegment.Content.Substring(3, qEndIndex - 3).Trim()
                : srcFSegment.Content[3..].Trim();

            if (!string.IsNullOrEmpty(msg))
            {
                SoraSegment qSegment = SoraSegment.Text(msg);
                fMessage[0] = qSegment;
            }
            else
            {
                fMessage.RemoveAt(0);
            }
        }

        if (!srcBSegment.Content.Equals("你答") && srcBSegment.Content.EndsWith("你答") && nextMsgIndex != 0)
            fMessage.Add(srcBSegment.Content[..^2]);

        //回答消息切片
        if (srcBSegment.Content.EndsWith("你答"))
        {
            bMessage.RemoveAt(0);
        }
        else
        {
            int    aStartIndex = srcBSegment.Content.IndexOf("你答", StringComparison.Ordinal);
            string msg         = srcBSegment.Content[(aStartIndex + 2)..].Trim();

            if (!string.IsNullOrEmpty(msg))
            {
                SoraSegment aSegment = SoraSegment.Text(msg);
                bMessage[0] = aSegment;
            }
            else
            {
                bMessage.RemoveAt(0);
            }
        }

        if (QaService.MessageEqual(fMessage, bMessage))
        {
            await eventArgs.Reply("不可以复读,爪巴");
            return;
        }

        Log.Info("QA", $"创建QA切片:f={fMessage.Count} b={bMessage.Count}");

        //处理问题
        int ret = qaService.AddNewQA(new QaData
        {
            qMsg    = fMessage,
            aMsg    = bMessage,
            GroupId = eventArgs.SourceGroup
        });
        switch (ret)
        {
            case 0:
                await eventArgs.Reply("我记住了！");
                break;
            case -1:
                await eventArgs.Reply("已经有相同的问题了！");
                break;
            case -2:
                await eventArgs.Reply("头好痒哦，感觉要长脑子了(发生错误)");
                break;
        }
    }

    [UsedImplicitly]
    [SoraCommand(SourceType = SourceFlag.Group,
                 CommandExpressions = new[] { @"^不要回答[\s\S]+$" },
                 MatchType = MatchType.Regex,
                 PermissionLevel = MemberRoleType.Admin)]
    public async ValueTask DeleteQuestion(GroupMessageEventArgs eventArgs)
    {
        if (!QaService.MessageCheck(eventArgs.Message.MessageBody))
            return;
        eventArgs.IsContinueEventChain = false;
        IQaService qaService = StaticStuff.ServiceProvider.GetServices<IQaService>()
                                          .SingleOrDefault(s => s.LoginUid == eventArgs.LoginUid);
        if (qaService is null)
        {
            Log.Error("QA", "未找到QA服务");
            await eventArgs.Reply("未找到QA服务");
            return;
        }

        MessageBody question  = eventArgs.Message.MessageBody;
        string      qFrontStr = (question[0].Data as TextSegment)!.Content[4..].Trim();
        if (string.IsNullOrEmpty(qFrontStr))
            question.RemoveAt(0);
        else
            question[0] = qFrontStr;

        int ret = qaService.DeleteQA(question, eventArgs.SourceGroup);
        switch (ret)
        {
            case 0:
                await eventArgs.Reply("我不再回答" + question + "了！");
                break;
            case -1:
                await eventArgs.Reply("没有这样的问题");
                break;
        }
    }

    [UsedImplicitly]
    [SoraCommand(SourceType = SourceFlag.Group,
                 CommandExpressions = new[] { @"^DEQA[\s\S]+$" },
                 MatchType = MatchType.Regex,
                 SuperUserCommand = true)]
    public async ValueTask DeleteGlobalQuestionSu(GroupMessageEventArgs eventArgs)
    {
        await DeleteQuestion(eventArgs);
    }

    [UsedImplicitly]
    [SoraCommand(SourceType = SourceFlag.Group,
                 CommandExpressions = new[] { @"^看看有人问$" },
                 MatchType = MatchType.Regex,
                 PermissionLevel = MemberRoleType.Admin)]
    public async ValueTask GetAllQuestion(GroupMessageEventArgs eventArgs)
    {
        eventArgs.IsContinueEventChain = false;
        IQaService qaService = StaticStuff.ServiceProvider.GetServices<IQaService>()
                                          .SingleOrDefault(s => s.LoginUid == eventArgs.LoginUid);
        if (qaService is null)
        {
            Log.Error("QA", "未找到QA服务");
            await eventArgs.Reply("未找到QA服务");
            return;
        }

        List<MessageBody> qList = qaService.GetAllQA(eventArgs.SourceGroup);

        if (qList.Count == 0)
        {
            await eventArgs.Reply("别急，还没有任何问题");
            return;
        }

        MessageBody questions = new();
        foreach (MessageBody msg in qList)
        {
            questions += msg;
            questions += "|";
        }

        questions.RemoveAt(questions.Count - 1);
        await eventArgs.Reply(questions);
    }
}