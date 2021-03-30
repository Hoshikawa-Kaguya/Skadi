using System;
using JetBrains.Annotations;
using Sora.Attributes.Command;
using Sora.Entities.CQCodes;
using Sora.EventArgs.SoraEvent;

namespace AntiRain.Command
{
    [CommandGroup]
    internal class Surprise
    {
        #region 私有方法

        [UsedImplicitly]
        [GroupCommand(CommandExpressions = new[] {"dice"})]
        private async void RandomNumber(GroupMessageEventArgs eventArgs)
        {
            Random randomGen = new();
            await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender.Id), "丢出了\r\n",
                                                         randomGen.Next(1, 6));
        }

        [UsedImplicitly]
        [GroupCommand(CommandExpressions = new[] {"优质睡眠", "昏睡红茶", "昏睡套餐", "健康睡眠"})]
        private async void RedTea(GroupMessageEventArgs eventArgs)
        {
            await eventArgs.SourceGroup.EnableGroupMemberMute(eventArgs.Sender.Id,
                                                              28800);
        }

        #endregion
    }
}