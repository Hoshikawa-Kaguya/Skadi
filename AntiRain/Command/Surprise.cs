using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Sora.Attributes.Command;
using Sora.Entities.MessageElement;
using Sora.Enumeration.ApiType;
using Sora.EventArgs.SoraEvent;
using YukariToolBox.FormatLog;

namespace AntiRain.Command
{
    [CommandGroup]
    public class Surprise
    {
        #region 私有方法

        [UsedImplicitly]
        [GroupCommand(CommandExpressions = new[] {"dice"})]
        public async ValueTask RandomNumber(GroupMessageEventArgs eventArgs)
        {
            Random rd = new();
            await eventArgs.SourceGroup.SendGroupMessage(CQCodes.CQAt(eventArgs.Sender.Id), "丢出了\r\n",
                                                         rd.Next(1, 6));
        }

        [UsedImplicitly]
        [GroupCommand(CommandExpressions = new[] {"优质睡眠", "昏睡红茶", "昏睡套餐", "健康睡眠"})]
        public async ValueTask RedTea(GroupMessageEventArgs eventArgs)
        {
            await eventArgs.SourceGroup.EnableGroupMemberMute(eventArgs.Sender.Id,
                                                              28800);
        }

        [UsedImplicitly]
        [GroupCommand(CommandExpressions = new[] {"抽老婆"})]
        public async ValueTask RollWife(GroupMessageEventArgs eventArgs)
        {
            var (apiStatus, memberList) = await eventArgs.SourceGroup.GetGroupMemberList();
            if (apiStatus.RetCode != ApiStatusType.OK)
            {
                Log.Error("api错误", $"api return {apiStatus}");
                return;
            }

            //删除自身和发送者
            memberList.RemoveAll(i => i.UserId == eventArgs.Sender);
            memberList.RemoveAll(i => i.UserId == eventArgs.LoginUid);

            if (memberList.Count == 0) await eventArgs.Reply("群里没人是你的老婆");

            await eventArgs.Reply("10秒后我将at一位幸运群友成为你的老婆\r\n究竟是谁会这么幸运呢");
            await Task.Delay(10000);
            var rd = new Random();
            await eventArgs.Reply(CQCodes.CQAt(memberList[rd.Next(0, memberList.Count - 1)].UserId),
                                  "\r\n恭喜成为",
                                  CQCodes.CQAt(eventArgs.Sender),
                                  "的老婆 ~");
        }

        #endregion
    }
}