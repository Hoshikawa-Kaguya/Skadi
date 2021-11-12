using System.Threading.Tasks;
using AntiRain.TypeEnum;
using Sora.Entities.Segment;
using Sora.Enumeration.EventParamsType;
using Sora.EventArgs.SoraEvent;
using YukariToolBox.FormatLog;

namespace AntiRain.Tool
{
    internal static class SessionUtil
    {
        #region 基础方法

        internal static bool IsAdminSession(this GroupMessageEventArgs eventArgs) =>
            eventArgs.SenderInfo.Role == MemberRoleType.Admin ||
            eventArgs.SenderInfo.Role == MemberRoleType.Owner;

        /// <summary>
        /// 零参数指令的参数检查
        /// 同时检查成员是否存在
        /// </summary>
        /// <returns>
        /// <para><see langword="true"/> 指令合法</para>
        /// <para><see langword="false"/> 有多余参数</para>
        /// </returns>
        internal static async ValueTask<bool> ZeroArgsCheck(this GroupMessageEventArgs eventArgs)
        {
            //检查参数
            switch (await BotUtils.CheckForLength(eventArgs.ToCommandArgs(), 0))
            {
                case LenType.Extra:
                    await eventArgs.SourceGroup.SendGroupMessage(SoraSegment.At(eventArgs.Sender.Id) +
                                                                 "\r\n听不见！重来！（有多余参数）");
                    return false;
                case LenType.Legitimate:
                    return true;
                default:
                    await eventArgs.SourceGroup.SendGroupMessage(SoraSegment.At(eventArgs.Sender.Id) +
                                                                 "发生未知错误，请联系机器人管理员");
                    Log.Error("Unknown error", "LenType");
                    return false;
            }
        }

        internal static string[] ToCommandArgs(this GroupMessageEventArgs eventArgs) =>
            eventArgs.Message.RawText.Trim().Split(' ');

        /// <summary>
        /// 权限检查/越权警告
        /// </summary>
        internal static async Task<bool> AuthCheck(this GroupMessageEventArgs eventArgs, string cmdTypeStr)
        {
            if (eventArgs.IsAdminSession()) return true;
            else
            {
                await eventArgs.SourceGroup.SendGroupMessage(SoraSegment.At(eventArgs.Sender.Id) +
                                                             " 你没有执行此指令的权限");
                Log.Warning($"会战[群:{eventArgs.SourceGroup.Id}]", $"群成员{eventArgs.SenderInfo.Nick}正在尝试执行指令{cmdTypeStr}");
                return false;
            }
        }

        #endregion
    }
}