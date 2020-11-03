using System;
using System.Collections.Generic;
using System.Linq;
using AntiRain.DatabaseUtils.Helpers.PCRGuildBattleDB;
using AntiRain.Resource.TypeEnum;
using AntiRain.Resource.TypeEnum.CommandType;
using AntiRain.Tool;
using Sora.Entities.CQCodes;
using Sora.Enumeration.ApiEnum;
using Sora.EventArgs.SoraEvent;
using Sora.Tool;

namespace AntiRain.ChatModule.PcrGuildBattle
{
    internal class GuildManager : BaseManager
    {
        #region 属性
        /// <summary>
        /// 数据库实例
        /// </summary>
        private GuildManagerDBHelper DBHelper { get; set; }
        #endregion

        #region 构造函数
        internal GuildManager(GroupMessageEventArgs messageEventArgs, PCRGuildBattleCommand commandType) : base(messageEventArgs, commandType)
        {
            this.DBHelper = new GuildManagerDBHelper(messageEventArgs);
        }
        #endregion

        #region 指令分发
        /// <summary>
        /// 公会管理指令响应函数
        /// </summary>
        public async void GuildManagerResponse() //功能响应
        {
            if (MessageEventArgs == null) throw new ArgumentNullException(nameof(MessageEventArgs));
            switch (CommandType)
            {
                case PCRGuildBattleCommand.CreateGuild:
                    if(!await AuthCheck()) return;
                    CreateGuild();
                    break;
                case PCRGuildBattleCommand.DeleteGuild:
                    if(!await AuthCheck() || !await ZeroArgsCheck()) return;
                    DeleteGuild();
                    break;
                case PCRGuildBattleCommand.JoinGuild:
                    if(!await AuthCheck()) return;
                    JoinGuild();
                    break;
            }
        }
        #endregion

        #region 指令
        /// <summary>
        /// 创建公会
        /// </summary>
        private async void CreateGuild()
        {
            //检查群是否已经被标记为公会
            switch (this.DBHelper.GuildExists())
            {
                case 1:
                    await base.SourceGroup.SendGroupMessage(CQCode.CQAt(base.Sender),
                                                            $"此群已被标记为[{DBHelper.GetGuildInfo(base.SourceGroup).GuildName}]公会");
                    return;
            }
            //公会名
            string guildName;
            //公会区服
            Server guildServer;
            //判断公会名
            switch (BotUtils.CheckForLength(this.CommandArgs,1))
            {
                case LenType.Extra:
                    if (BotUtils.CheckForLength(this.CommandArgs, 2) == LenType.Legitimate)
                    {
                        guildName = CommandArgs[2];
                    }
                    else
                    {
                        await base.SourceGroup.SendGroupMessage(CQCode.CQAt(base.Sender), "\r\n有多余参数");
                        return;
                    }
                    break;
                case LenType.Legitimate:
                    var groupInfo = await base.SourceGroup.GetGroupInfo();
                    if (groupInfo.apiStatus != APIStatusType.OK)
                    {
                        ConsoleLog.Error("API error",$"api ret code {(int) groupInfo.apiStatus}");
                        await base.SourceGroup.SendGroupMessage(CQCode.CQAt(base.Sender), "\r\nAPI调用错误请重试");
                        return;
                    }
                    guildName = groupInfo.groupInfo.GroupName;
                    break;
                default:
                    return;
            }
            //判断区服
            if (Enum.IsDefined(typeof(Server), CommandArgs[1]))
            {
                guildServer = (Server) Enum.Parse(typeof(Server), CommandArgs[1]);
                if (guildServer != Server.CN)
                {
                    await base.SourceGroup.SendGroupMessage("暂不支持国服以外的服务器");
                    return;
                }
            }
            else
            {
                await base.SourceGroup.SendGroupMessage(CQCode.CQAt(base.Sender),
                                               "弟啊，你哪个服务器的");
                return;
            }

            switch (this.DBHelper.CreateGuild(guildServer, guildName, base.SourceGroup))
            {
                case -1:
                    await base.SourceGroup.SendGroupMessage("错误:数据库错误");
                    break;
                case 0:
                    await base.SourceGroup.SendGroupMessage($"公会[{guildName}]已创建");
                    break;
                default:
                    await base.SourceGroup.SendGroupMessage("发生了未知错误");
                    break;
            }
        }

        /// <summary>
        /// 删除公会
        /// </summary>
        private async void DeleteGuild()
        {
            //判断公会是否存在
            switch (DBHelper.GuildExists())
            {
                case 0:
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(Sender), "\r\n此群并未标记为公会");
                    return;
                case -1:
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(Sender), "\r\nERROR\r\n数据库错误");
                    return;
            }
            //获取当前群公会名
            string guildName = DBHelper.GetGuildName(SourceGroup);
            //删除公会
            await SourceGroup.SendGroupMessage(DBHelper.DeleteGuild(SourceGroup)
                                               ? $" 公会[{guildName}]已被删除。"
                                               : $" 公会[{guildName}]删除失败，数据库错误。");
        }

        private async void JoinGuild()
        {
            //检查公会是否存在
            if (DBHelper.GuildExists() != 1)
            {
                await SourceGroup.SendGroupMessage("该群未创建公会");
                return;
            }
            List<long> joinList = new List<long>();
            switch (BotUtils.CheckForLength(CommandArgs,1))
            {
                case LenType.Illegal:
                    joinList.Add(Sender);
                    break;
                case LenType.Extra:case LenType.Legitimate:
                    joinList.AddRange(MessageEventArgs.Message.GetAllAtList());
                    if (joinList.Count == 0)
                    {
                        await SourceGroup.SendGroupMessage("没有At任何成员");
                        return;
                    }
                    break;
            }
            ConsoleLog.Debug("Guild Mgr",$"Get join list count={joinList.Count}");
            Dictionary<long,int> databaseRet = new Dictionary<long, int>();
            //加入待加入的成员
            foreach (long member in joinList)
            {
                databaseRet.Add(member,
                                await DBHelper.JoinToGuild(member, SourceGroup,
                                                           string.IsNullOrEmpty(MessageEventArgs
                                                                                    .SenderInfo.Card)
                                                               ? MessageEventArgs.SenderInfo.Nick
                                                               : MessageEventArgs.SenderInfo.Card));
            }
            List<CQCode> responseMsg = new List<CQCode>();
            //构建格式化信息
            if (databaseRet.Any(ret => ret.Value == 0))
            {
                responseMsg.Add(CQCode.CQText("以下成员已加入:"));
                foreach (long member in databaseRet.Where(member => member.Value == 0)
                                                 .Select(member => member.Key)
                                                 .ToList())
                {
                    responseMsg.Add(CQCode.CQText("\r\n"));
                    responseMsg.Add(CQCode.CQAt(member));
                }
            }
            if (databaseRet.Any(ret => ret.Value == 1))
            {
                if (responseMsg.Count != 0) responseMsg.Add(CQCode.CQText("\r\n"));
                responseMsg.Add(CQCode.CQText("以下成员已在公会中，仅更新信息:"));
                foreach (long member in databaseRet.Where(member => member.Value == 1)
                                                   .Select(member => member.Key)
                                                   .ToList())
                {
                    responseMsg.Add(CQCode.CQText("\r\n"));
                    responseMsg.Add(CQCode.CQAt(member));
                }
            }
            if (databaseRet.Any(ret => ret.Value == -1))
            {
                if (responseMsg.Count != 0) responseMsg.Add(CQCode.CQText("\r\n"));
                responseMsg.Add(CQCode.CQText("以下成员在加入时发生错误:"));
                foreach (long member in databaseRet.Where(member => member.Value == -1)
                                                   .Select(member => member.Key)
                                                   .ToList())
                {
                    responseMsg.Add(CQCode.CQText("\r\n"));
                    responseMsg.Add(CQCode.CQAt(member));
                }
            }
            //发送信息
            await SourceGroup.SendGroupMessage(responseMsg);
        }
        #endregion

        #region 私有方法
        #endregion
    }
}
