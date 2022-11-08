using System.Threading.Tasks;
using Sora.Entities.Segment;
using Sora.EventArgs.SoraEvent;
using static Skadi.Tool.CommandCdUtil;

namespace Skadi.ServerInterface;

internal static class GroupPokeEvent
{
    internal static async ValueTask GroupPokeEventParse(object sender, GroupPokeEventArgs eventArgs)
    {
        if (eventArgs.TargetUser == eventArgs.LoginUid)
        { 
            if (!IsInCD(eventArgs.SourceGroup, eventArgs.SendUser, CommandFlag.GroupPoke))
                await eventArgs.SourceGroup.SendGroupMessage(SoraSegment.At(eventArgs.SendUser)
                                                             + "\r\n你今晚必被爽哥杀害\r\n"
                                                             + SoraSegment
                                                                 .Image("https://i.loli.net/2020/10/20/zWPyocxFEVp2tDT.jpg"));
            else
                await eventArgs.SourceGroup.SendGroupMessage("再戳？再戳把你牙拔了当球踢");
        }
    }
}