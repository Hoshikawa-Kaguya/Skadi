using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AntiRain.Tool;
using Sora.Entities.Segment;
using Sora.EventArgs.SoraEvent;
using static AntiRain.Tool.CheckInCD;

namespace AntiRain.ServerInterface
{
    internal static class GroupPokeEvent
    {
        /// <summary>
        /// 调用CD记录
        /// </summary>
        private static Dictionary<CheckUser, DateTime> Users { get; set; } = new();

        internal static async ValueTask GroupPokeEventParse(object sender, GroupPokeEventArgs groupPokeEventArgs)
        {
            if (groupPokeEventArgs.TargetUser == groupPokeEventArgs.LoginUid)
            {
                if (!Users.IsInCD(groupPokeEventArgs.SourceGroup, groupPokeEventArgs.SendUser))
                    await groupPokeEventArgs.SourceGroup.SendGroupMessage(SoraSegment.At(groupPokeEventArgs.SendUser) +
                                                                          "\r\n你今晚必被爽哥杀害\r\n"                         +
                                                                          SoraSegment
                                                                              .Image("https://i.loli.net/2020/10/20/zWPyocxFEVp2tDT.jpg"));
                else
                    await groupPokeEventArgs.SourceGroup.SendGroupMessage("再戳？再戳把你牙拔了当球踢");
            }
        }
    }
}