using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Sora.Attributes.Command;
using Sora.Enumeration;
using Sora.Enumeration.ApiType;
using Sora.EventArgs.SoraEvent;
using YukariToolBox.FormatLog;
using static AntiRain.Tool.CheckInCD;

namespace AntiRain.Command.PixivSearch
{
    /// <summary>
    /// 搜图指令
    /// </summary>
    [CommandGroup]
    public class SearchCommands
    {
        /// <summary>
        /// 调用CD记录
        /// </summary>
        private static Dictionary<CheckUser, DateTime> users { get; } = new();

        /// <summary>
        /// 请求表
        /// </summary>
        private HashSet<(long uid, long gid)> requestList { get; } = new();

        [UsedImplicitly]
        [GroupCommand(CommandExpressions = new[] { "pixiv搜图" })]
        public async ValueTask SearchRequest(GroupMessageEventArgs eventArgs)
        {
            if (users.IsInCD(eventArgs.SourceGroup, eventArgs.Sender))
            {
                await eventArgs.Reply("你冲的太快了，要坏掉了(请等待CD冷却)");
                return;
            }

            await eventArgs.Reply("图呢(请在1分钟内发送图片)");
            requestList.Add((eventArgs.Sender, eventArgs.SourceGroup));

            var imgArgs =
                await eventArgs.WaitForNextMessageAsync(@"^\[CQ:image,file=[a-z0-9]+\.image,subType=[0-9]+\]$",
                                                        MatchType.Regex, TimeSpan.FromMinutes(1));
            if(imgArgs == null)
            {
                await eventArgs.Reply("连图都没有真是太逊了");
                requestList.Remove((eventArgs.Sender, eventArgs.SourceGroup));
                return;
            }
            Log.Debug("pic", $"get pic {imgArgs.Message.RawText} searching...");
            //发送图片
            var messageInfo =
                await eventArgs.Reply(await SaucenaoUtils.SearchByUrl("92a805aff18cbc56c4723d7e2d5100c6892fe256",
                                                                      imgArgs.Message.GetAllImage().ToList()[0].Url,
                                                                      imgArgs.Sender, imgArgs.LoginUid),
                                      TimeSpan.FromSeconds(15));
            if (messageInfo.apiStatus.RetCode != ApiStatusType.OK)
            {
                await eventArgs.Reply("图被夹了，你找服务器要去");
            }
        }
    }
}