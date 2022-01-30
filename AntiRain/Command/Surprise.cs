using System;
using System.Threading.Tasks;
using AntiRain.Config;
using JetBrains.Annotations;
using Sora.Attributes.Command;
using Sora.Entities.Segment;
using Sora.Enumeration;
using Sora.EventArgs.SoraEvent;

namespace AntiRain.Command
{
    [CommandGroup]
    public class Surprise
    {
        #region 私有方法

        [UsedImplicitly]
        [SoraCommand(
            SourceType = SourceFlag.Group, 
            CommandExpressions = new[] {"dice"})]
        public async ValueTask RandomNumber(GroupMessageEventArgs eventArgs)
        {
            if (!ConfigManager.TryGetUserConfig(eventArgs.LoginUid, out var config) &&
                !config.ModuleSwitch.HaveFun) return;
            Random rd = new();
            await eventArgs.SourceGroup.SendGroupMessage(SoraSegment.At(eventArgs.Sender.Id) + "丢出了\r\n" +
                                                         rd.Next(1, 6).ToString());
        }

        [UsedImplicitly]
        [SoraCommand(
            SourceType = SourceFlag.Group, 
            CommandExpressions = new[] {"优质睡眠", "昏睡红茶", "昏睡套餐", "健康睡眠"})]
        public async ValueTask RedTea(GroupMessageEventArgs eventArgs)
        {
            if (!ConfigManager.TryGetUserConfig(eventArgs.LoginUid, out var config) &&
                !config.ModuleSwitch.HaveFun) return;
            await eventArgs.SourceGroup.EnableGroupMemberMute(eventArgs.Sender.Id,
                                                              28800);
        }

        #endregion
    }
}