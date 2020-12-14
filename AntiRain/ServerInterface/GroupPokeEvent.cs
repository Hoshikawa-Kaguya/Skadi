using System.Threading.Tasks;
using AntiRain.Tool;
using Sora.Entities.CQCodes;
using Sora.EventArgs.SoraEvent;

namespace AntiRain.ServerInterface
{
    internal static class GroupPokeEvent
    {
        internal static async ValueTask GroupPokeEventParse(object sender, GroupPokeEventArgs groupPokeEventArgs)
        {
            if (groupPokeEventArgs.TargetUser == groupPokeEventArgs.LoginUid &&
                !CheckInCD.isInCD(groupPokeEventArgs.SourceGroup, groupPokeEventArgs.SendUser))
            {
                await groupPokeEventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(groupPokeEventArgs.SendUser),
                                                                      "\r\n你今晚必被爽哥杀害\r\n",
                                                                      CQCode.CQImage("https://i.loli.net/2020/10/20/zWPyocxFEVp2tDT.jpg"));
            }
            else
            {
                await groupPokeEventArgs.SourceGroup.SendGroupMessage("再戳？再戳把你牙拔了当球踢");
            }
        }
    }
}
