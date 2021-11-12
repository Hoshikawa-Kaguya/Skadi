using System;
using System.Threading.Tasks;
using AntiRain.Config;
using JetBrains.Annotations;
using Sora.Attributes.Command;
using Sora.Entities.Segment;
using Sora.Enumeration;
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
            if (!ConfigManager.TryGetUserConfig(eventArgs.LoginUid, out var config) &&
                !config.ModuleSwitch.HaveFun) return;
            Random rd = new();
            await eventArgs.SourceGroup.SendGroupMessage(SoraSegment.At(eventArgs.Sender.Id) + "丢出了\r\n" +
                                                         rd.Next(1, 6).ToString());
        }

        [UsedImplicitly]
        [GroupCommand(CommandExpressions = new[] {"优质睡眠", "昏睡红茶", "昏睡套餐", "健康睡眠"})]
        public async ValueTask RedTea(GroupMessageEventArgs eventArgs)
        {
            if (!ConfigManager.TryGetUserConfig(eventArgs.LoginUid, out var config) &&
                !config.ModuleSwitch.HaveFun) return;
            await eventArgs.SourceGroup.EnableGroupMemberMute(eventArgs.Sender.Id,
                                                              28800);
        }

        [UsedImplicitly]
        [GroupCommand(CommandExpressions = new[] {"抽老婆"})]
        public async ValueTask RollWife(GroupMessageEventArgs eventArgs)
        {
            if (!ConfigManager.TryGetUserConfig(eventArgs.LoginUid, out var config) &&
                !config.ModuleSwitch.HaveFun) return;
            var (apiStatus, memberList) = await eventArgs.SourceGroup.GetGroupMemberList();
            if (apiStatus.RetCode != ApiStatusType.OK)
            {
                Log.Error("api错误", $"api return {apiStatus}");
                return;
            }

            //删除自身和发送者
            memberList.RemoveAll(i => i.UserId == eventArgs.Sender);
            memberList.RemoveAll(i => i.UserId == eventArgs.LoginUid);
            memberList.RemoveAll(i => i.Sex    == eventArgs.SenderInfo.Sex);

            if (memberList.Count == 0) await eventArgs.Reply("群里没人是你的老婆");

            await
                eventArgs.Reply($"10秒后我将at一位幸运群友成为你的{(eventArgs.SenderInfo.Sex == Sex.Female ? "老公" : "老婆")}\r\n究竟是谁会这么幸运呢");
            await Task.Delay(10000);
            var rd = new Random();
            await eventArgs.Reply(SoraSegment.At(memberList[rd.Next(0, memberList.Count - 1)].UserId) +
                                  "\r\n恭喜成为"                                                          +
                                  SoraSegment.At(eventArgs.Sender)                                    +
                                  $"的{(eventArgs.SenderInfo.Sex == Sex.Female ? "老公" : "老婆")} ~");
        }

        #endregion
    }
}