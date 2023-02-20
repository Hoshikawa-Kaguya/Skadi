using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Skadi.Interface;
using Sora.Attributes.Command;
using Sora.Entities;
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
        eventArgs.IsContinueEventChain = false;
        IQaService qaService = SkadiApp.GetService<IQaService>();
        if (qaService is null)
        {
            await eventArgs.Reply("QA服务错误");
            Log.Error("QA", "未找到QA服务");
            return;
        }

        bool success =
            await qaService.AddNewQA(eventArgs.LoginUid, eventArgs.SourceGroup, eventArgs.Message.MessageBody);
        if (success)
            await eventArgs.Reply("我记住了！");
        else
            await eventArgs.Reply("已经有相同问题了！");
    }

    [UsedImplicitly]
    [SoraCommand(SourceType = SourceFlag.Group,
                 CommandExpressions = new[] { @"^不要回答[\s\S]+$" },
                 MatchType = MatchType.Regex,
                 PermissionLevel = MemberRoleType.Admin)]
    public async ValueTask DeleteQuestion(GroupMessageEventArgs eventArgs)
    {
        eventArgs.IsContinueEventChain = false;
        IQaService qaService = SkadiApp.GetService<IQaService>();
        if (qaService is null)
        {
            await eventArgs.Reply("QA服务错误");
            Log.Error("QA", "未找到QA服务");
            return;
        }

        bool success =
            await qaService.DeleteQA(eventArgs.LoginUid, eventArgs.SourceGroup, eventArgs.Message.MessageBody);
        if (success)
            await eventArgs.Reply("我不再回答这个问题了！");
        else
            await eventArgs.Reply("没有这样的问题");
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
        IQaService qaService = SkadiApp.GetService<IQaService>();
        if (qaService is null)
        {
            await eventArgs.Reply("QA服务错误");
            Log.Error("QA", "未找到QA服务");
            return;
        }

        List<MessageBody> qList = qaService.GetAllQA(eventArgs.LoginUid, eventArgs.SourceGroup);

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