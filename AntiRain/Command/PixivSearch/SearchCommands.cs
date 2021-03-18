using System.Collections.Generic;
using System.Threading.Tasks;
using Sora.Attributes.Command;
using Sora.Entities;
using Sora.Entities.CQCodes;
using Sora.Entities.CQCodes.CQCodeModel;
using Sora.Enumeration;
using Sora.EventArgs.SoraEvent;
using YukariToolBox.FormatLog;

namespace AntiRain.Command.PixivSearch
{
    [CommandGroup]
    public class SearchCommands
    {
        /// <summary>
        /// 请求表
        /// </summary>
        private List<User> requestList { get; } = new();
        
        [GroupCommand(CommandExpressions = new[] {"pixiv搜图"})]
        public async ValueTask TestCommand3(GroupMessageEventArgs eventArgs)
        {
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
        public async ValueTask TestCommand4(GroupMessageEventArgs eventArgs)
        {
            if (!requestList.Exists(member => member == eventArgs.Sender)) return;
            Log.Debug("pic", $"get pic {eventArgs.Message.RawText} searching...");
            requestList.RemoveAll(user => user == eventArgs.Sender);

            await eventArgs.Reply(await SaucenaoUtils.SearchByUrl("92a805aff18cbc56c4723d7e2d5100c6892fe256",
                                                                   eventArgs.Message.GetAllImage()[0].Url, eventArgs));
        }

        [GroupCommand(CommandExpressions = new[] {@"^echo\s[\s\S]+$"},
                      MatchType = MatchType.Regex)]
        public async ValueTask TestCommand5(GroupMessageEventArgs eventArgs)
        {
            //处理开头字符串
            if (eventArgs.Message.MessageList[0].Function == CQFunction.Text)
            {
                if (eventArgs.Message.MessageList[0].CQData is Text str && str.Content.StartsWith("echo "))
                {
                    if(str.Content.Equals("echo ")) eventArgs.Message.MessageList.RemoveAt(0);
                    else eventArgs.Message.MessageList[0] = CQCode.CQText(str.Content.Substring(5));
                }
            }
            //复读
            if(eventArgs.Message.MessageList.Count != 0) await eventArgs.Reply(eventArgs.Message.MessageList);
        }
    }
}