using System;
using System.Linq;
using System.Threading.Tasks;
using AntiRain.Tool;
using JetBrains.Annotations;
using Sora.Attributes.Command;
using Sora.Entities;
using Sora.Enumeration;
using Sora.Enumeration.ApiType;
using Sora.EventArgs.SoraEvent;
using YukariToolBox.LightLog;
using static AntiRain.Tool.CommandCdUtil;

namespace AntiRain.Command.ImageSearch
{
    /// <summary>
    /// 搜图指令
    /// </summary>
    [CommandGroup]
    public static class SearchCommands
    {
        [UsedImplicitly]
        [SoraCommand(
            SourceType = SourceFlag.Group, 
            CommandExpressions = new[] { "搜图" })]
        public static async ValueTask SearchRequest(GroupMessageEventArgs eventArgs)
        {
            if (CommandCdUtil.IsInCD(eventArgs.SourceGroup, eventArgs.Sender, CommandFlag.PicSearch))
            {
                await eventArgs.Reply("你冲的太快了，要坏掉了(请等待CD冷却)");
                return;
            }

            await eventArgs.Reply("图呢(请在1分钟内发送图片)");

            var imgArgs =
                await eventArgs.WaitForNextMessageAsync(@"^\[CQ:image,file=[a-z0-9]+\.image,subType=[0-9]+\]$",
                                                        MatchType.Regex, TimeSpan.FromMinutes(1));
            if(imgArgs == null)
            {
                await eventArgs.Reply("连图都没有真是太逊了");
                return;
            }
            Log.Debug("pic", $"get pic {imgArgs.Message.RawText} searching...");
            //发送图片
            (ApiStatus apiStatus, _) =
                await eventArgs.Reply(await SaucenaoApi.SearchByUrl("92a805aff18cbc56c4723d7e2d5100c6892fe256",
                                                                    imgArgs.Message.GetAllImage().ToList()[0].Url,
                                                                    imgArgs.LoginUid),
                                      TimeSpan.FromSeconds(15));
            if (apiStatus.RetCode != ApiStatusType.Ok)
            {
                await eventArgs.Reply("图被夹了，你找服务器要去");
            }
        }
    }
}