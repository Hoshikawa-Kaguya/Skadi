using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AntiRain.DatabaseUtils;
using AntiRain.DatabaseUtils.Helpers.PCRGuildBattleDB;
using AntiRain.TypeEnum;
using AntiRain.TypeEnum.CommandType;
using AntiRain.Tool;
using Sora.Entities.CQCodes;
using Sora.Entities.Info;
using Sora.Enumeration.ApiType;
using Sora.EventArgs.SoraEvent;
using YukariToolBox.FormatLog;

namespace AntiRain.ChatModule.PcrGuildBattle
{
    //TODO 等待重构
    internal class GuildManager
    {
        #region 属性

        /// <summary>
        /// 数据库实例
        /// </summary>
        private GuildManagerDBHelper DBHelper { get;     set; }
        private GroupMessageEventArgs eventArgs   { get; init; }
        private PCRGuildBattleCommand CommandType { get; set; }

        #endregion

        #region 构造函数

        internal GuildManager(GroupMessageEventArgs eventArgs, PCRGuildBattleCommand commandType)
        {
            this.DBHelper  = new GuildManagerDBHelper(eventArgs.SourceGroup);
            this.eventArgs = eventArgs;
            CommandType    = commandType;
        }

        #endregion

        #region 指令分发

        /// <summary>
        /// 公会管理指令响应函数
        /// </summary>
        public async void GuildManagerResponse() //功能响应
        {
            if (eventArgs == null) throw new ArgumentNullException(nameof(eventArgs));
            switch (CommandType)
            {
                case PCRGuildBattleCommand.CreateGuild:
                    if (!await eventArgs.AuthCheck(CommandType.ToString())) return;
                    CreateGuild();
                    break;
                case PCRGuildBattleCommand.DeleteGuild:
                    if (!await eventArgs.AuthCheck(CommandType.ToString()) || !await eventArgs.ZeroArgsCheck()) return;
                    DeleteGuild();
                    break;
                case PCRGuildBattleCommand.JoinGuild:
                    if (!await eventArgs.AuthCheck(CommandType.ToString())) return;
                    JoinGuild();
                    break;
                case PCRGuildBattleCommand.QuitGuild:
                    if (!await eventArgs.AuthCheck(CommandType.ToString())) return;
                    QuitGuild();
                    break;
                case PCRGuildBattleCommand.ListMember:
                    if (!await eventArgs.AuthCheck(CommandType.ToString()) || !await eventArgs.ZeroArgsCheck()) return;
                    ListMember();
                    break;
                case PCRGuildBattleCommand.JoinAll:
                    if (!await eventArgs.AuthCheck(CommandType.ToString()) || !await eventArgs.ZeroArgsCheck()) return;
                    JoinAll();
                    break;
                case PCRGuildBattleCommand.QuitAll:
                    if (!await eventArgs.AuthCheck(CommandType.ToString()) || !await eventArgs.ZeroArgsCheck()) return;
                    QuitAll();
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
            switch (this.DBHelper.GuildExists(eventArgs.SourceGroup))
            {
                case 1:
                    await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender),
                                                            $"此群已被标记为[{DBHelper.GetGuildInfo(eventArgs.SourceGroup).GuildName}]公会");
                    return;
            }

            //参数处理
            string[] commandArgs = eventArgs.ToCommandArgs();
            //公会名
            string guildName;
            //公会区服
            Server guildServer;
            //判断公会名
            switch (BotUtils.CheckForLength(commandArgs, 1))
            {
                case LenType.Extra:
                    if (BotUtils.CheckForLength(commandArgs, 2) == LenType.Legitimate)
                    {
                        guildName = commandArgs[2];
                    }
                    else
                    {
                        await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender), "\r\n有多余参数");
                        return;
                    }

                    break;
                case LenType.Legitimate:
                    var groupInfo = await eventArgs.SourceGroup.GetGroupInfo();
                    if (groupInfo.apiStatus != APIStatusType.OK)
                    {
                        Log.Error("API error", $"api ret code {(int) groupInfo.apiStatus}");
                        await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender), "\r\nAPI调用错误请重试");
                        return;
                    }

                    guildName = groupInfo.groupInfo.GroupName;
                    break;
                default:
                    return;
            }

            //判断区服
            if (Enum.IsDefined(typeof(Server), commandArgs[1]))
            {
                guildServer = (Server) Enum.Parse(typeof(Server), commandArgs[1]);
                if (guildServer != Server.CN)
                {
                    await eventArgs.SourceGroup.SendGroupMessage("暂不支持国服以外的服务器");
                    return;
                }
            }
            else
            {
                await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender),
                                                        "弟啊，你哪个服务器的");
                return;
            }

            switch (this.DBHelper.CreateGuild(guildServer, guildName, eventArgs.SourceGroup))
            {
                case -1:
                    await BotUtils.DatabaseFailedTips(eventArgs);
                    break;
                case 0:
                    await eventArgs.SourceGroup.SendGroupMessage($"公会[{guildName}]已创建");
                    break;
                default:
                    await eventArgs.SourceGroup.SendGroupMessage("发生了未知错误");
                    break;
            }
        }

        /// <summary>
        /// 删除公会
        /// </summary>
        private async void DeleteGuild()
        {
            //判断公会是否存在
            switch (DBHelper.GuildExists(eventArgs.SourceGroup))
            {
                case 0:
                    await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender), "\r\n此群并未标记为公会");
                    return;
                case -1:
                    await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender), "\r\nERROR\r\n数据库错误");
                    return;
            }

            //获取当前群公会名
            string guildName = DBHelper.GetGuildName(eventArgs.SourceGroup);
            //删除公会
            await eventArgs.SourceGroup.SendGroupMessage(DBHelper.DeleteGuild(eventArgs.SourceGroup)
                                                   ? $" 公会[{guildName}]已被删除。"
                                                   : $" 公会[{guildName}]删除失败，数据库错误。");
        }

        /// <summary>
        /// 加入公会
        /// </summary>
        private async void JoinGuild()
        {
            //检查公会是否存在
            if (DBHelper.GuildExists(eventArgs.SourceGroup) != 1)
            {
                await eventArgs.SourceGroup.SendGroupMessage("该群未创建公会");
                return;
            }

            //参数处理
            string[] commandArgs = eventArgs.ToCommandArgs();

            //处理需要加入的成员列表
            List<long> joinList = new();
            switch (BotUtils.CheckForLength(commandArgs, 1))
            {
                case LenType.Illegal:
                    joinList.Add(eventArgs.Sender);
                    break;
                case LenType.Extra:
                case LenType.Legitimate:
                    joinList.AddRange(eventArgs.Message.GetAllAtList());
                    if (joinList.Count == 0)
                    {
                        await eventArgs.SourceGroup.SendGroupMessage("没有At任何成员");
                        return;
                    }

                    break;
            }

            //检查列表中是否有机器人
            if (joinList.Any(member => member == eventArgs.LoginUid))
            {
                joinList.Remove(eventArgs.LoginUid);
                await eventArgs.SourceGroup.SendGroupMessage("不要在成员中At机器人啊kora");
                if (joinList.Count == 0) return;
            }

            Log.Debug("Guild Mgr", $"Get join list count={joinList.Count}");
            //从API获取成员信息cao 
            Log.Debug("Guild Mgr", "Get group member infos");
            var (apiStatus, groupMemberList) = await eventArgs.SourceGroup.GetGroupMemberList();
            if (apiStatus != APIStatusType.OK)
            {
                Log.Error("API error", $"api ret code {(int) apiStatus}");
                await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender), "\r\nAPI调用错误请重试");
                return;
            }

            //加入待加入的成员
            Dictionary<long, int> databaseRet = new();
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
                                DBHelper.JoinGuild(member, eventArgs.SourceGroup, memberName));
            }

            //构建格式化信息
            List<CQCode> responseMsg = new();
            if (databaseRet.Any(ret => ret.Value == 0)) //成员成功加入
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

            if (databaseRet.Any(ret => ret.Value == 1)) //成员已存在
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

            if (databaseRet.Any(ret => ret.Value == -1)) //数据库错误
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
            await eventArgs.SourceGroup.SendGroupMessage(responseMsg);
        }

        /// <summary>
        /// 加入除机器人外的所有成员
        /// 没有人数限制
        /// </summary>
        private async void JoinAll()
        {
            //检查公会是否存在
            if (DBHelper.GuildExists(eventArgs.SourceGroup) != 1)
            {
                await eventArgs.SourceGroup.SendGroupMessage("该群未创建公会");
                return;
            }

            //获取所有成员的信息
            var (apiStatus, groupMemberList) = await eventArgs.SourceGroup.GetGroupMemberList();
            if (apiStatus != APIStatusType.OK)
            {
                Log.Error("API error", $"api ret code {(int) apiStatus}");
                await eventArgs.SourceGroup.SendGroupMessage(CQCode.CQAt(eventArgs.Sender), "\r\nAPI调用错误请重试");
                return;
            }

            //移除机器人的成员信息
            groupMemberList.RemoveAt(groupMemberList.FindIndex(member => member.UserId == eventArgs.LoginUid));
            //添加成员到公会
            Dictionary<long, int> databaseRet = new();
            foreach (GroupMemberInfo member in groupMemberList)
            {
                //获取群成员名
                string memberName = groupMemberList.Any(memberInfo => memberInfo.UserId == member.UserId)
                    ? groupMemberList.Where(memberInfo => memberInfo.UserId == member.UserId)
                                     .Select(memberInfo =>
                                                 string.IsNullOrEmpty(memberInfo.Card)
                                                     ? memberInfo.Nick
                                                     : memberInfo.Card)
                                     .First()
                    : "N/A";
                //添加成员
                databaseRet.Add(member.UserId,
                                DBHelper.JoinGuild(member.UserId, eventArgs.SourceGroup, memberName));
            }

            //构建格式化信息
            List<CQCode> responseMsg = new();
            if (databaseRet.Any(ret => ret.Value == 0)) //成员成功加入
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

            if (databaseRet.Any(ret => ret.Value == 1)) //成员已存在
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

            if (databaseRet.Any(ret => ret.Value == -1)) //数据库错误
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
            await eventArgs.SourceGroup.SendGroupMessage(responseMsg);
        }

        /// <summary>
        /// 退会
        /// </summary>
        private async void QuitGuild()
        {
            //检查公会是否存在
            if (DBHelper.GuildExists(eventArgs.SourceGroup) != 1)
            {
                await eventArgs.SourceGroup.SendGroupMessage("该群未创建公会");
                return;
            }

            //参数处理
            string[] commandArgs = eventArgs.ToCommandArgs();
            //获取成员参数
            List<long> quitList = new();
            switch (BotUtils.CheckForLength(commandArgs, 1))
            {
                case LenType.Illegal:
                    quitList.Add(eventArgs.Sender);
                    break;
                case LenType.Legitimate: //只有单一成员时需判断uid参数
                    quitList.AddRange(eventArgs.Message.GetAllAtList());
                    if (quitList.Count == 0)
                    {
                        if (long.TryParse(commandArgs[1], out long uid))
                        {
                            quitList.Add(uid);
                        }
                        else
                        {
                            await eventArgs.SourceGroup.SendGroupMessage("没有At任何成员");
                            return;
                        }
                    }

                    break;
                case LenType.Extra:
                    quitList.AddRange(eventArgs.Message.GetAllAtList());
                    if (quitList.Count == 0)
                    {
                        await eventArgs.SourceGroup.SendGroupMessage("没有At任何成员");
                        return;
                    }

                    break;
            }

            //检查列表中是否有机器人
            if (quitList.Any(member => member == eventArgs.LoginUid))
            {
                quitList.Remove(eventArgs.LoginUid);
                await eventArgs.SourceGroup.SendGroupMessage("不要在成员中At机器人啊kora");
                if (quitList.Count == 0) return;
            }

            Log.Debug("Guild Mgr", $"Get quit list count={quitList.Count}");
            Dictionary<long, int> databaseRet = new();
            //删除退会成员
            foreach (long member in quitList)
            {
                databaseRet.Add(member,
                                DBHelper.QuitGuild(member, eventArgs.SourceGroup));
            }

            //构建格式化信息
            List<CQCode> responseMsg = new();
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
            await eventArgs.SourceGroup.SendGroupMessage(responseMsg);
        }

        /// <summary>
        /// 清空公会成员
        /// </summary>
        private async void QuitAll()
        {
            //检查公会是否存在
            if (DBHelper.GuildExists(eventArgs.SourceGroup) != 1)
            {
                await eventArgs.SourceGroup.SendGroupMessage("该群未创建公会");
                return;
            }

            Log.Debug("database", $"Quit guild[{eventArgs.SourceGroup.Id}] all members");
            //清空成员
            switch (DBHelper.QuitAll(eventArgs.SourceGroup))
            {
                case 0:
                    await eventArgs.SourceGroup.SendGroupMessage("公会成员已全部清空");
                    break;
                case 1:
                    await eventArgs.SourceGroup.SendGroupMessage("DB ERROR:公会不存在");
                    Log.Error("database", $"guild {eventArgs.SourceGroup.Id} not found");
                    break;
                case -1:
                    await BotUtils.DatabaseFailedTips(eventArgs);
                    break;
                default:
                    await eventArgs.SourceGroup.SendGroupMessage("发生了未知错误");
                    Log.Error("Guild Mgr", "清空成员时发生了未知错误");
                    break;
            }
        }

        /// <summary>
        /// 列出所有成员
        /// </summary>
        private async void ListMember()
        {
            //检查公会是否存在
            if (DBHelper.GuildExists(eventArgs.SourceGroup) != 1)
            {
                await eventArgs.SourceGroup.SendGroupMessage("该群未创建公会");
                return;
            }

            //获取群成员数
            int memberCount = DBHelper.GetMemberCount(eventArgs.SourceGroup);
            //判断数据库错误和空公会
            switch (memberCount)
            {
                case -1:
                    await BotUtils.DatabaseFailedTips(eventArgs);
                    return;
                case 0:
                    await eventArgs.SourceGroup.SendGroupMessage("公会并没有成员");
                    return;
            }

            //获取公会成员表
            List<MemberInfo> guildMembers = DBHelper.GetAllMembersInfo(eventArgs.SourceGroup);
            //获取公会名
            string guildName = DBHelper.GetGuildName(eventArgs.SourceGroup);
            //检查数据库错误
            if (guildName == null || guildMembers == null) //数据库错误
            {
                await BotUtils.DatabaseFailedTips(eventArgs);
                return;
            }

            //构建消息文本
            StringBuilder sendMsg = new();
            sendMsg.Append($"公会[{guildName}]成员列表\r\n{memberCount}/30\r\n==================\r\n     UID     |   昵称");
            foreach (MemberInfo guildMember in guildMembers)
            {
                sendMsg.Append("\r\n");
                sendMsg.Append(guildMember.Uid);
                sendMsg.Append(" | ");
                sendMsg.Append(guildMember.Name);
            }

            //发送消息
            await eventArgs.SourceGroup.SendGroupMessage(sendMsg.ToString());
        }

        #endregion

        #region 私有方法

        #endregion
    }
}