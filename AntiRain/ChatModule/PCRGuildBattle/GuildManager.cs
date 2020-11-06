using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AntiRain.DatabaseUtils;
using AntiRain.DatabaseUtils.Helpers.PCRGuildBattleDB;
using AntiRain.Resource.TypeEnum;
using AntiRain.Resource.TypeEnum.CommandType;
using AntiRain.Tool;
using Sora.Entities.Base;
using Sora.Entities.CQCodes;
using Sora.Entities.Info;
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
                case PCRGuildBattleCommand.QuitGuild:
                    if(!await AuthCheck()) return;
                    QuitGuild();
                    break;
                case PCRGuildBattleCommand.ListMember:
                    if (!await AuthCheck() || !await ZeroArgsCheck()) return;
                    ListMember();
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

        /// <summary>
        /// 加入公会
        /// </summary>
        private async void JoinGuild()
        {
            //检查公会是否存在
            if (DBHelper.GuildExists() != 1)
            {
                await SourceGroup.SendGroupMessage("该群未创建公会");
                return;
            }
            //处理需要加入的成员列表
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
            //检查列表中是否有机器人
            if (joinList.Any(member => member == MessageEventArgs.LoginUid))
            {
                joinList.Remove(MessageEventArgs.LoginUid);
                await SourceGroup.SendGroupMessage("不要在成员中At机器人啊kora");
                if(joinList.Count == 0) return;
            }
            ConsoleLog.Debug("Guild Mgr",$"Get join list count={joinList.Count}");
            //从API获取成员信息
            ConsoleLog.Debug("Guild Mgr","Get group member infos");
            var (apiStatus, groupMemberList) = await SourceGroup.GetGroupMemberList();
            if (apiStatus != APIStatusType.OK)
            {
                ConsoleLog.Error("API error",$"api ret code {(int) apiStatus}");
                await SourceGroup.SendGroupMessage(CQCode.CQAt(Sender), "\r\nAPI调用错误请重试");
                return;
            }
            //加入待加入的成员
            Dictionary<long,int> databaseRet = new Dictionary<long, int>();
            foreach (long member in joinList)
            {
                //获取群成员名
                string memberName = groupMemberList.Any(memberInfo => memberInfo.UserId == member)
                    ? groupMemberList.Where(memberInfo => memberInfo.UserId == member)
                                     .Select(memberInfo =>
                                                 string.IsNullOrEmpty(memberInfo.Card)
                                                     ? memberInfo.Nick
                                                     : memberInfo.Card)
                                     .First()
                    : "N/A";
                //添加成员
                databaseRet.Add(member,
                                DBHelper.JoinGuild(member, SourceGroup, memberName));
            }
            List<CQCode> responseMsg = new List<CQCode>();
            //构建格式化信息
            if (databaseRet.Any(ret => ret.Value == 0))//成员成功加入
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
            if (databaseRet.Any(ret => ret.Value == 1))//成员已存在
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
            if (databaseRet.Any(ret => ret.Value == -1))//数据库错误
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

        /// <summary>
        /// 退会
        /// </summary>
        private async void QuitGuild()
        {
            List<long> quitList = new List<long>();
            //获取成员参数
            switch (BotUtils.CheckForLength(CommandArgs,1))
            {
                case LenType.Illegal:
                    quitList.Add(Sender);
                    break;
                case LenType.Legitimate: //只有单一成员时需判断uid参数
                    quitList.AddRange(MessageEventArgs.Message.GetAllAtList());
                    if (quitList.Count == 0)
                    {
                        if (long.TryParse(CommandArgs[1], out long uid))
                        {
                            quitList.Add(uid);
                        }
                        else
                        {
                            await SourceGroup.SendGroupMessage("没有At任何成员");
                            return;
                        }
                    }
                    break;
                case LenType.Extra:
                    quitList.AddRange(MessageEventArgs.Message.GetAllAtList());
                    if (quitList.Count == 0)
                    {
                        await SourceGroup.SendGroupMessage("没有At任何成员");
                        return;
                    }
                    break;
            }
            //检查列表中是否有机器人
            if (quitList.Any(member => member == MessageEventArgs.LoginUid))
            {
                quitList.Remove(MessageEventArgs.LoginUid);
                await SourceGroup.SendGroupMessage("不要在成员中At机器人啊kora");
                if(quitList.Count == 0) return;
            }
            ConsoleLog.Debug("Guild Mgr",$"Get quit list count={quitList.Count}");
            Dictionary<long,int> databaseRet = new Dictionary<long, int>();
            //删除退会成员
            foreach (long member in quitList)
            {
                databaseRet.Add(member,
                                DBHelper.QuitGuild(member, SourceGroup));
            }
            List<CQCode> responseMsg = new List<CQCode>();
            //构建格式化信息
            if (databaseRet.Any(ret => ret.Value == 0))
            {
                responseMsg.Add(CQCode.CQText("以下成员已退出:"));
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
                responseMsg.Add(CQCode.CQText("以下成员并不在公会中:"));
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
                responseMsg.Add(CQCode.CQText("以下成员在退出时发生错误:"));
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

        private async void ListMember()
        {
            //获取群成员数
            int memberCount = DBHelper.GetMemberCount(SourceGroup);
            //判断数据库错误和空公会
            switch (memberCount)
            {
                case -1:
                    DBMsgUtils.DatabaseFailedTips(base.MessageEventArgs);
                    return;
                case 0:
                    await SourceGroup.SendGroupMessage("公会并没有成员");
                    return;
            }
            //获取公会成员表
            List<MemberInfo> guildMembers = DBHelper.GetAllMembersInfo(SourceGroup);
            //获取公会名
            string guildName = DBHelper.GetGuildName(SourceGroup);
            //检查数据库错误
            if (guildName == null || guildMembers == null)//数据库错误
            {
                DBMsgUtils.DatabaseFailedTips(base.MessageEventArgs);
                return;
            }
            //构建消息文本
            StringBuilder sendMsg = new StringBuilder();
            sendMsg.Append($"公会[{guildName}]成员列表\r\n{memberCount}/30\r\n==================\r\n     UID     |   昵称");
            foreach (MemberInfo guildMember in guildMembers)
            {
                sendMsg.Append("\r\n");
                sendMsg.Append(guildMember.Uid);
                sendMsg.Append(" | ");
                sendMsg.Append(guildMember.Name);
            }
            //发送消息
            await SourceGroup.SendGroupMessage(sendMsg.ToString());
        }
        #endregion

        #region 私有方法
        #endregion
    }
}
