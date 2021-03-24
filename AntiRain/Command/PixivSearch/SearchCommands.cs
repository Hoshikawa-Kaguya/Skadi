using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sora.Attributes.Command;
using Sora.Entities;
using Sora.Enumeration;
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
        private static Dictionary<CheckUser, DateTime> users { get; set; } = new();

        /// <summary>
        /// 请求表
        /// </summary>
        private List<User> requestList { get; } = new();

        [GroupCommand(CommandExpressions = new[] {"pixiv搜图"})]
        public async ValueTask SearchRequest(GroupMessageEventArgs eventArgs)
        {
            if (users.IsInCD(eventArgs.SourceGroup, eventArgs.Sender))
            {
                await eventArgs.Reply("你冲的太快了，要坏掉了(请等待CD冷却)");
                return;
            }

            if (requestList.Exists(member => member == eventArgs.Sender))
            {
                await eventArgs.Reply("dnmd图呢");
                return;
            }

            await eventArgs.Reply("图呢");
            requestList.Add(eventArgs.Sender);
        }

        [GroupCommand(CommandExpressions = new[] {@"^\[CQ:image,file=[a-z0-9]+\.image\]$"},
                      MatchType          = MatchType.Regex)]
        public async ValueTask PicParse(GroupMessageEventArgs eventArgs)
        {
            if (!requestList.Exists(member => member == eventArgs.Sender)) return;
            Log.Debug("pic", $"get pic {eventArgs.Message.RawText} searching...");
            requestList.RemoveAll(user => user == eventArgs.Sender);

            await eventArgs.Reply(await SaucenaoUtils.SearchByUrl("92a805aff18cbc56c4723d7e2d5100c6892fe256",
                                                                  eventArgs.Message.GetAllImage()[0].Url, eventArgs));
        }
    }
}