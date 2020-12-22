using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AntiRain.TypeEnum;
using AntiRain.TypeEnum.CommandType;
using AntiRain.Tool;
using Sora.Entities;
using Sora.Entities.CQCodes;
using Sora.Enumeration.EventParamsType;
using Sora.EventArgs.SoraEvent;
using Sora.Tool;

namespace AntiRain.ChatModule.PcrGuildBattle
{
    internal abstract class BaseManager
    {
        #region 属性
        protected GroupMessageEventArgs MessageEventArgs { get; set; }
        protected Group                 SourceGroup      { get; set; }
        protected User                  Sender           { get; set; }
        protected string[]              CommandArgs      { get; set; }
        protected bool                  IsAdmin          { get; set; }
        protected PCRGuildBattleCommand CommandType      { get; set; }
        #endregion

        #region 构造函数

        internal BaseManager(GroupMessageEventArgs messageEventArgs, PCRGuildBattleCommand commandType)
        {
            this.MessageEventArgs = messageEventArgs;
            this.SourceGroup      = messageEventArgs.SourceGroup;
            this.Sender           = messageEventArgs.Sender;
            this.CommandArgs      = messageEventArgs.Message.RawText.Trim().Split(' ');
            this.IsAdmin = messageEventArgs.SenderInfo.Role == MemberRoleType.Admin ||
                           messageEventArgs.SenderInfo.Role == MemberRoleType.Owner;
            this.CommandType = commandType;
        }
        #endregion

        #region 基类管理方法
        /// <summary>
        /// 零参数指令的参数检查
        /// 同时检查成员是否存在
        /// </summary>
        /// <returns>
        /// <para><see langword="true"/> 指令合法</para>
        /// <para><see langword="false"/> 有多余参数</para>
        /// </returns>
        internal async ValueTask<bool> ZeroArgsCheck()
        {
            //检查参数
            switch (BotUtils.CheckForLength(CommandArgs,0))
            {
                case LenType.Extra:
                    await MessageEventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(MessageEventArgs.Sender.Id),
                                                                        "\r\n听不见！重来！（有多余参数）");
                    return false;
                case LenType.Legitimate:
                    return true;
                default:
                    await MessageEventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(MessageEventArgs.Sender.Id),
                                                                        "发生未知错误，请联系机器人管理员");
                    ConsoleLog.Error("Unknown error","LenType");
                    return false;
            }
        }

        /// <summary>
        /// 从消息的CQ码中获取用户ID（单CQ码）
        /// </summary>
        internal long GetUidInAt()
        {
            List<long> AtUserList = MessageEventArgs.Message.GetAllAtList();
            return AtUserList.Any() ? AtUserList.First() : -1;
        }

        /// <summary>
        /// 权限检查/越权警告
        /// </summary>
        internal async Task<bool> AuthCheck()
        {
            if (IsAdmin) return true;
            else
            {
                await SourceGroup.SendGroupMessage(CQCode.CQAt(Sender.Id),
                                                   " 你没有执行此指令的权限");
                ConsoleLog.Warning($"会战[群:{SourceGroup.Id}]", $"群成员{MessageEventArgs.SenderInfo.Nick}正在尝试执行指令{CommandType}");
                return false;
            }
        }
        #endregion
    }
}
