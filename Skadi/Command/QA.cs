using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Skadi.Entities;
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
    private struct QABuf
    {
        internal MessageBody msg;
        internal Guid        cmdId;
        internal long        gid;
    }

    public void QaInit(long loginUid)
    {
        Log.Info("QA", "QA初始化");
        List<QaData> qaMsg = StaticStuff.QaConfig.GetAllQA();
        foreach (QaData data in qaMsg)
            RegisterNewQaCommand(data.qMsg, data.aMsg, data.GroupId);
        Log.Info("QA", $"加载了{qaMsg.Count}条QA");
    }

    private readonly List<QABuf> _commandGuids = new();

    [UsedImplicitly]
    [SoraCommand(SourceType = SourceFlag.Group,
                 CommandExpressions = new[] { @"^有人问[\s\S]+你答[\s\S]+$" },
                 MatchType = MatchType.Regex,
                 PermissionLevel = MemberRoleType.Admin)]
    public async ValueTask GetGlobalQuestion(GroupMessageEventArgs eventArgs)
    {
        if (!MessageCheck(eventArgs.Message.MessageBody))
            return;
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

        if (MessageEqual(fMessage, bMessage))
        {
            await eventArgs.Reply("不可以复读,爪巴");
            return;
        }

        //处理问题
        RegisterNewQaCommand(fMessage, bMessage, eventArgs.SourceGroup);
        StaticStuff.QaConfig.AddNewQA(new QaData
        {
            qMsg    = fMessage,
            aMsg    = bMessage,
            GroupId = eventArgs.SourceGroup
        });
        await eventArgs.Reply("我记住了！");
    }

    [UsedImplicitly]
    [SoraCommand(SourceType = SourceFlag.Group,
                 CommandExpressions = new[] { @"^不要回答[\s\S]+$" },
                 MatchType = MatchType.Regex,
                 PermissionLevel = MemberRoleType.Admin)]
    public async ValueTask DeleteGlobalQuestion(GroupMessageEventArgs eventArgs)
    {
        if (!MessageCheck(eventArgs.Message.MessageBody))
            return;
        eventArgs.IsContinueEventChain = false;

        MessageBody question  = eventArgs.Message.MessageBody;
        string      qFrontStr = (question[0].Data as TextSegment)!.Content[4..].Trim();
        if (string.IsNullOrEmpty(qFrontStr))
            question.RemoveAt(0);
        else
            question[0] = qFrontStr;

        //查找相同问题的指令
        List<QABuf> qaBufs = _commandGuids
                             .Where(s => MessageEqual(s.msg, question) && eventArgs.SourceGroup == s.gid)
                             .ToList();
        if (qaBufs.Count == 0)
        {
            await eventArgs.Reply("没有这样的问题");
        }
        else
        {
            foreach (QABuf buf in qaBufs)
            {
                //取消注册指令
                StaticStuff.CommandManager.DeleteDynamicCommand(buf.cmdId);
                //删除数据文件中的qa数据
                StaticStuff.QaConfig.DeleteQA(buf.msg);
                //删除缓存中的qa
                _commandGuids.RemoveAll(s => MessageEqual(s.msg, question));
            }

            await eventArgs.Reply("我不再回答" + question + "了");
        }
    }

    [UsedImplicitly]
    [SoraCommand(SourceType = SourceFlag.Group,
                 CommandExpressions = new[] { @"^DEQA[\s\S]+$" },
                 MatchType = MatchType.Regex,
                 SuperUserCommand = true)]
    public async ValueTask DeleteGlobalQuestionSu(GroupMessageEventArgs eventArgs)
    {
        await DeleteGlobalQuestion(eventArgs);
    }

    [UsedImplicitly]
    [SoraCommand(SourceType = SourceFlag.Group,
                 CommandExpressions = new[] { @"^看看有人问$" },
                 MatchType = MatchType.Regex,
                 PermissionLevel = MemberRoleType.Admin)]
    public async ValueTask GetAllQuestion(GroupMessageEventArgs eventArgs)
    {
        MessageBody questions = new MessageBody();
        List<MessageBody> groupQuestion =
            _commandGuids.Where(c => c.gid == eventArgs.SourceGroup)
                         .Select(c => c.msg)
                         .ToList();
        List<MessageBody> temp = new();

        if (groupQuestion.Count == 0)
        {
            await eventArgs.Reply("别急，还没有任何问题");
            return;
        }

        foreach (MessageBody msg in groupQuestion)
        {
            if (temp.Any(i => MessageEqual(i, msg)))
                continue;
            questions.AddRange(msg);
            questions.Add("|");
            temp.Add(msg);
        }

        questions.RemoveAt(questions.Count - 1);
        await eventArgs.Reply(questions);
    }

    public void RegisterNewQaCommand(MessageBody qMsg, MessageBody aMsg, long group)
    {
        Guid cmdId =
            StaticStuff.CommandManager.RegisterGroupDynamicCommand(args => MessageEqual(args.Message.MessageBody,
                                                                                        qMsg),
                                                                   async e =>
                                                                   {
                                                                       e.IsContinueEventChain = false;
                                                                       await e.Reply(aMsg);
                                                                   },
                                                                   "qa_global",
                                                                   MemberRoleType.Member,
                                                                   false,
                                                                   0,
                                                                   new[] { group });

        _commandGuids.Add(new QABuf
        {
            msg   = qMsg,
            cmdId = cmdId,
            gid   = group
        });
    }

    public static bool MessageCheck(MessageBody message)
    {
        if (message is null)
            return false;
        bool check = true;
        foreach (SoraSegment segment in message)
            check &= segment.MessageType is
                SegmentType.Text or SegmentType.At or SegmentType.Face or SegmentType.Image;
        return check;
    }

    public static bool MessageEqual(MessageBody srcMsg, MessageBody rxMsg)
    {
        if (rxMsg is null)
            return false;
        if (!MessageCheck(rxMsg) || srcMsg.Count != rxMsg.Count)
            return false;

        for (int i = 0; i < srcMsg.Count; i++)
            switch (srcMsg[i].MessageType)
            {
                case SegmentType.Text:
                    if ((srcMsg[i].Data as TextSegment)!.Content
                        != ((rxMsg[i].Data as TextSegment)?.Content ?? string.Empty))
                        return false;
                    break;
                case SegmentType.Image:
                    if ((srcMsg[i].Data as ImageSegment)!.ImgFile != (rxMsg[i].Data as ImageSegment)?.ImgFile)
                        return false;
                    break;
                case SegmentType.At:
                    if ((srcMsg[i].Data as AtSegment)!.Target != (rxMsg[i].Data as AtSegment)?.Target)
                        return false;
                    break;
                case SegmentType.Face:
                    if ((srcMsg[i].Data as FaceSegment)!.Id != (rxMsg[i].Data as FaceSegment)?.Id)
                        return false;
                    break;
                default:
                    return false;
            }

        return true;
    }
}