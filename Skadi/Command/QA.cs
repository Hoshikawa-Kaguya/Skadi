// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using JetBrains.Annotations;
// using Skadi.Entities;
// using Skadi.Interface;
// using Skadi.Services;
// using Sora.Attributes.Command;
// using Sora.Entities;
// using Sora.Entities.Segment;
// using Sora.Entities.Segment.DataModel;
// using Sora.Enumeration;
// using Sora.Enumeration.EventParamsType;
// using Sora.EventArgs.SoraEvent;
// using YukariToolBox.LightLog;
//
// namespace Skadi.Command;
//
// /// <summary>
// /// 问答
// /// </summary>
// [CommandSeries(SeriesName = "QA")]
// public class QA
// {
//     [UsedImplicitly]
//     [SoraCommand(SourceType = SourceFlag.Group,
//                  CommandExpressions = new[] { @"^有人问[\s\S]+你答[\s\S]+$" },
//                  MatchType = MatchType.Regex,
//                  PermissionLevel = MemberRoleType.Admin)]
//     public async ValueTask CreateQuestion(GroupMessageEventArgs eventArgs)
//     {
//         if (!QaService.MessageCheck(eventArgs.Message.MessageBody))
//             return;
//         eventArgs.IsContinueEventChain = false;
//         IQaService qaService = SkadiApp.GetServices<IQaService>()
//                                        .SingleOrDefault(s => s.LoginUid == eventArgs.LoginUid);
//         if (qaService is null)
//         {
//             Log.Error("QA", "未找到QA服务");
//             await eventArgs.Reply("未找到QA服务");
//             return;
//         }
//
//         
//
//         if (QaService.MessageEqual(qMessage, aMessage))
//         {
//             await eventArgs.Reply("不可以复读,爪巴");
//             return;
//         }
//
//         Log.Info("QA", $"创建QA切片:f={qMessage.Count} b={aMessage.Count}");
//
//         //处理问题
//         int ret = qaService.AddNewQA(new QaData
//         {
//             qMsg = qMessage,
//             aMsg = aMessage,
//             GroupId = eventArgs.SourceGroup
//         });
//         switch (ret)
//         {
//             case 0:
//                 await eventArgs.Reply("我记住了！");
//                 break;
//             case -1:
//                 await eventArgs.Reply("已经有相同的问题了！");
//                 break;
//             case -2:
//                 await eventArgs.Reply("头好痒哦，感觉要长脑子了(发生错误)");
//                 break;
//         }
//     }
//
//     [UsedImplicitly]
//     [SoraCommand(SourceType = SourceFlag.Group,
//                  CommandExpressions = new[] { @"^不要回答[\s\S]+$" },
//                  MatchType = MatchType.Regex,
//                  PermissionLevel = MemberRoleType.Admin)]
//     public async ValueTask DeleteQuestion(GroupMessageEventArgs eventArgs)
//     {
//         if (!QaService.MessageCheck(eventArgs.Message.MessageBody))
//             return;
//         eventArgs.IsContinueEventChain = false;
//         IQaService qaService = SkadiApp.GetServices<IQaService>()
//                                        .SingleOrDefault(s => s.LoginUid == eventArgs.LoginUid);
//         if (qaService is null)
//         {
//             Log.Error("QA", "未找到QA服务");
//             await eventArgs.Reply("未找到QA服务");
//             return;
//         }
//
//         MessageBody question = eventArgs.Message.MessageBody;
//         string qFrontStr = (question[0].Data as TextSegment)!.Content[4..].Trim();
//         if (string.IsNullOrEmpty(qFrontStr))
//             question.RemoveAt(0);
//         else
//             question[0] = qFrontStr;
//
//         int ret = qaService.DeleteQA(question, eventArgs.SourceGroup);
//         switch (ret)
//         {
//             case 0:
//                 await eventArgs.Reply("我不再回答" + question + "了！");
//                 break;
//             case -1:
//                 await eventArgs.Reply("没有这样的问题");
//                 break;
//         }
//     }
//
//     [UsedImplicitly]
//     [SoraCommand(SourceType = SourceFlag.Group,
//                  CommandExpressions = new[] { @"^DEQA[\s\S]+$" },
//                  MatchType = MatchType.Regex,
//                  SuperUserCommand = true)]
//     public async ValueTask DeleteGlobalQuestionSu(GroupMessageEventArgs eventArgs)
//     {
//         await DeleteQuestion(eventArgs);
//     }
//
//     [UsedImplicitly]
//     [SoraCommand(SourceType = SourceFlag.Group,
//                  CommandExpressions = new[] { @"^看看有人问$" },
//                  MatchType = MatchType.Regex,
//                  PermissionLevel = MemberRoleType.Admin)]
//     public async ValueTask GetAllQuestion(GroupMessageEventArgs eventArgs)
//     {
//         eventArgs.IsContinueEventChain = false;
//         IQaService qaService = SkadiApp.GetServices<IQaService>()
//                                        .SingleOrDefault(s => s.LoginUid == eventArgs.LoginUid);
//         if (qaService is null)
//         {
//             Log.Error("QA", "未找到QA服务");
//             await eventArgs.Reply("未找到QA服务");
//             return;
//         }
//
//         List<MessageBody> qList = qaService.GetAllQA(eventArgs.SourceGroup);
//
//         if (qList.Count == 0)
//         {
//             await eventArgs.Reply("别急，还没有任何问题");
//             return;
//         }
//
//         MessageBody questions = new();
//         foreach (MessageBody msg in qList)
//         {
//             questions += msg;
//             questions += "|";
//         }
//
//         questions.RemoveAt(questions.Count - 1);
//         await eventArgs.Reply(questions);
//     }
// }