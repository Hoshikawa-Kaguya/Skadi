using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sora.Entities;
using Sora.Entities.CQCodes;
using Sora.Entities.Info;
using Sora.Enumeration.ApiEnum;
using Sora.Enumeration.EventParamsType;
using Sora.EventArgs.SoraEvent;
using Sora.Tool;
using SuiseiBot.DatabaseUtils.Helpers.PCRGuildBattleDB;
using SuiseiBot.Resource.TypeEnum;
using SuiseiBot.Resource.TypeEnum.CommandType;
using SuiseiBot.Tool;

namespace SuiseiBot.ChatModule.PCRGuildBattle
{
    internal static class GuildManager
    {
        /// <summary>
        /// 公会管理指令响应函数
        /// </summary>
        /// <param name="Sender">CQSender</param>
        /// <param name="GMgrEventArgs">CQGroupMessageEventArgs</param>
        /// <param name="commandType">指令类型</param>
        public static async void GuildMgrResponse(object Sender, GroupMessageEventArgs GMgrEventArgs,
                                            PCRGuildBattleCommand commandType) //功能响应
        {
            Group QQGroup = GMgrEventArgs.SourceGroup;

            //index=0为命令本身，其余为参数
            string[] commandArgs = GMgrEventArgs.Message.RawText.Trim().Split(' ');

            GuildManagerDBHelper dbAction = new GuildManagerDBHelper(GMgrEventArgs);

            int result = -2;


            (APIStatusType apiStatus, GroupMemberInfo memberInfo) = await QQGroup.GetGroupMemberInfo(GMgrEventArgs.Sender.Id);
            if (apiStatus != APIStatusType.OK)
            {
                ConsoleLog.Error("Guild Manager",$"API error code={apiStatus}");
            }
            //指示是否是管理员操作的
            bool isAdminAction = (memberInfo.Role == MemberRoleType.Admin ||
                                  memberInfo.Role == MemberRoleType.Owner);
            switch (commandType)
            {
                case PCRGuildBattleCommand.DeleteGuild://删除公会
                    if(SuiseiUtils.CheckForLength(commandArgs, 0) != LenType.Legitimate) return;
                    switch (dbAction.GuildExists())
                    {
                        case 0:
                            await QQGroup.SendGroupMessage(CQCode.CQAt(GMgrEventArgs.Sender.Id),
                                                     "此群并未标记为公会");
                            return;
                        case -1:
                            await QQGroup.SendGroupMessage(CQCode.CQAt(GMgrEventArgs.Sender.Id),
                                                     "\r\nERROR",
                                                     "\r\n数据库错误");
                            return;
                    }
                    string guildName1 = dbAction.GetGuildName(GMgrEventArgs.SourceGroup.Id);
                    await QQGroup.SendGroupMessage(dbAction.DeleteGuild(GMgrEventArgs.SourceGroup.Id)
                                                 ? $" 公会[{guildName1}]已被删除。"
                                                 : $" 公会[{guildName1}]删除失败，数据库错误。");
                    break;

                //参数1 服务器地区，参数2 公会名（可选，缺省为群名）
                case PCRGuildBattleCommand.CreateGuild: //建会
                    (APIStatusType apiStatus2, GroupInfo groupInfo) = await QQGroup.GetGroupInfo();
                    string guildName2 = apiStatus2 == APIStatusType.OK ? groupInfo.GroupName : "unknown_group_name";
                    if (!isAdminAction)
                    {
                        await QQGroup.SendGroupMessage(CQCode.CQAt(GMgrEventArgs.Sender.Id),
                                                 " 你没有权限这样做~");
                        ConsoleLog.Warning($"会战[群:{QQGroup.Id}]", $"群成员{GMgrEventArgs.SenderInfo.Nick}正在尝试执行指令{commandType}");
                        return;
                    }
                    //检查群是否已经被标记为公会
                    switch (dbAction.GuildExists())
                    {
                        case -1:
                            await QQGroup.SendGroupMessage(CQCode.CQAt(GMgrEventArgs.Sender.Id),
                                                     "\r\nERROR",
                                                     "\r\n数据库错误");
                            return;
                    }

                    if (SuiseiUtils.CheckForLength(commandArgs, 1) != LenType.Illegal)
                    {
                        Server guildServer;
                        //检查输入服务器代号是否合法
                        if (Enum.IsDefined(typeof(Server), commandArgs[1]))
                        {
                            guildServer = (Server)Enum.Parse(typeof(Server),commandArgs[1]);
                        }
                        else
                        {
                            await QQGroup.SendGroupMessage(CQCode.CQAt(GMgrEventArgs.Sender.Id),
                                                     "弟啊，你哪个服务器的");
                            return;
                        }

                        if (guildServer != Server.CN)
                        {
                            await QQGroup.SendGroupMessage("暂不支持国服以外的服务器");
                            return;
                        }
                        //根据输入参数建立公会
                        switch (SuiseiUtils.CheckForLength(commandArgs, 2))
                        {
                            case LenType.Legitimate://参数中有公会名
                                guildName2 = commandArgs[2];
                                result    = dbAction.CreateGuild(guildServer, guildName2, QQGroup.Id);
                                break;
                            case LenType.Illegal: //参数中没有公会名
                                result = dbAction.CreateGuild(guildServer, guildName2, QQGroup.Id);
                                break;
                            case LenType.Extra:
                                await QQGroup.SendGroupMessage(CQCode.CQAt(GMgrEventArgs.Sender.Id),
                                                         "参数过多");
                                return;
                        }
                    }
                    else
                    {
                        await QQGroup.SendGroupMessage(CQCode.CQAt(GMgrEventArgs.Sender.Id),
                                                 "弟啊，你哪个服务器的");
                        return;
                    }

                    switch (result)
                    {
                        case -1:
                            await QQGroup.SendGroupMessage($" 公会[{guildName2}]创建失败：数据库错误。");
                            break;
                        case 0:
                            await QQGroup.SendGroupMessage($" 公会[{guildName2}]已经创建。");
                            break;
                        case 1:
                            await QQGroup.SendGroupMessage("公会已经存在，更新了当前公会的信息。");
                            break;
                    }

                    break;
                //参数1 QQ号
                case PCRGuildBattleCommand.JoinGuild: //入会
                    if (!isAdminAction)
                    {
                        await QQGroup.SendGroupMessage(CQCode.CQAt(GMgrEventArgs.Sender.Id),
                                                 " 你没有权限这样做~");
                        ConsoleLog.Warning($"会战[群:{QQGroup.Id}]", $"群成员{GMgrEventArgs.SenderInfo.Nick}正在尝试执行指令{commandType}");
                        return;
                    }

                    Dictionary<long, int> addedQQList = new Dictionary<long, int>(); //已经入会的QQ号列表
                    var checkRet = SuiseiUtils.CheckForLength(commandArgs, 1, QQGroup, GMgrEventArgs.Sender.Id);
                    if (checkRet == LenType.Extra || checkRet == LenType.Legitimate)
                    {
                        List<long> atQQs = GMgrEventArgs.Message.GetAllAtList();
                        if (atQQs.Count == 0) //没有AT任何人，参数非法
                        {
                            await QQGroup.SendGroupMessage(CQCode.CQAt(GMgrEventArgs.Sender.Id),
                                                     "没有AT任何人");
                            return;
                        }

                        if (atQQs.Count >= 1) //如果存在AT
                        {
                            foreach (long qqid in atQQs) //检查每一个AT
                            {
                                //需要添加为成员的QQ号列表和对应操作的返回值
                                addedQQList.Add(qqid,
                                                await dbAction.JoinToGuild(qqid, QQGroup.Id,
                                                                     GMgrEventArgs.SenderInfo.Nick));
                            }

                            //如果只存在需要添加的成员，而没有需要更新的成员
                            if (addedQQList.Count > 0 && addedQQList.Where(x => x.Value == 1).ToList().Count == 0)
                            {
                                result = 0;
                            }
                            else
                            {
                                //否则就是既存在需要更新又存在需要添加的
                                result = 2;
                            }
                        }
                        else
                        {
                            result = await dbAction.JoinToGuild(GMgrEventArgs.Sender.Id, QQGroup.Id,
                                                          GMgrEventArgs.SenderInfo.Nick);
                        }
                    }
                    else
                    {
                        return;
                    }
                    if (addedQQList.Where(x => x.Value == -1).ToList().Count > 0)
                    {
                        result = -1;
                    }
                    switch (result)
                    {
                        case -2: //不可能进入，但防御性编程，需要处理
                            await QQGroup.SendGroupMessage(CQCode.CQAt(GMgrEventArgs.Sender.Id), " 未定义行为，请检查代码。");
                            break;
                        case -1: //一般情况下不可能非法，但也要处理
                            await QQGroup.SendGroupMessage(CQCode.CQAt(GMgrEventArgs.Sender.Id), " 操作过程中数据库出现错误。");
                            List<CQCode>  msg11 = new List<CQCode>();
                            //筛选出所有返回值为1的操作，也即更新了的成员
                            foreach (long qqNumber in addedQQList
                                                      .Where(x => x.Value == 1)
                                                      .ToDictionary(x => x.Key, x => x.Value)
                                                      .Keys)
                                msg11.Add(CQCode.CQAt(qqNumber));

                            List<CQCode>  msg12 = new List<CQCode>();
                            //如果有操作返回值为0，说明存在新添加的成员
                            if (addedQQList.Where(x => x.Value == 0).ToDictionary(x => x.Key, x => x.Value).Count >
                                0)
                            {
                                foreach (long qqNumber in addedQQList
                                                          .Where(x => x.Value == 0)
                                                          .ToDictionary(x => x.Key, x => x.Value).Keys)
                                    msg12.Add(CQCode.CQAt(qqNumber));
                            }
                            List<CQCode>  msg13 = new List<CQCode>();
                            if (addedQQList.Where(x => x.Value == -1).ToDictionary(x => x.Key, x => x.Value).Count >
                                0)
                            {
                                foreach (long qqNumber in addedQQList
                                                          .Where(x => x.Value == -1)
                                                          .ToDictionary(x => x.Key, x => x.Value).Keys)
                                    msg13.Add(CQCode.CQAt(qqNumber));
                            }

                            if (msg11.Count != 0)
                            {
                                msg11.Insert(0, CQCode.CQText("以下成员已经更新：\r\n"));
                                await QQGroup.SendGroupMessage(CQCode.CQAt(GMgrEventArgs.Sender.Id), msg11);
                            }

                            if (msg12.Count != 0)
                            {
                                msg12.Insert(0, CQCode.CQText("\r\n以下成员已添加：\r\n"));
                                await QQGroup.SendGroupMessage(CQCode.CQAt(GMgrEventArgs.Sender.Id), msg12);
                            }

                            if (msg13.Count != 0)
                            {
                                msg13.Insert(0, CQCode.CQText("\r\n以下成员操作时发生错误：\r\n"));
                                await QQGroup.SendGroupMessage(CQCode.CQAt(GMgrEventArgs.Sender.Id), msg13);
                            }
                            break;
                        case 0: //只存在新添加的成员
                            List<CQCode>  msg = new List<CQCode>();
                            //at所有新添加的成员
                            foreach (long qqNumber in addedQQList.Keys)
                                msg.Add(CQCode.CQAt(qqNumber));
                            await QQGroup.SendGroupMessage(CQCode.CQAt(GMgrEventArgs.Sender.Id), " 以下成员已经加入：\r\n",
                                                     msg);

                            break;
                        case 1: //只存在需要更新的成员，目前也不可能进入了
                            await QQGroup.SendGroupMessage(CQCode.CQAt(GMgrEventArgs.Sender.Id), " 成员已经存在，更新了当前成员的信息。");
                            break;
                        case 2: //存在需要更新和/或需要添加的成员
                            List<CQCode>  msg2 = new List<CQCode>();
                            //筛选出所有返回值为1的操作，也即更新了的成员
                            foreach (long qqNumber in addedQQList
                                                      .Where(x => x.Value == 1).ToDictionary(x => x.Key, x => x.Value)
                                                      .Keys)
                                msg2.Add(CQCode.CQAt(qqNumber));

                            List<CQCode>  msg3 = new List<CQCode>();
                            //如果有操作返回值为0，说明存在新添加的成员
                            if (addedQQList.Where(x => x.Value == 0).ToDictionary(x => x.Key, x => x.Value).Count > 0)
                            {
                                foreach (long qqNumber in addedQQList
                                                          .Where(x => x.Value == 0)
                                                          .ToDictionary(x => x.Key, x => x.Value).Keys)
                                    msg3.Add(CQCode.CQAt(qqNumber));
                            }

                            await QQGroup.SendGroupMessage(CQCode.CQAt(GMgrEventArgs.Sender.Id), " 有成员已经存在，");

                            if (msg2.Count != 0)
                            {
                                await QQGroup.SendGroupMessage("以下成员已经更新：\r\n", msg2);
                            }

                            if (msg3.Count != 0)
                            {
                                await QQGroup.SendGroupMessage("\r\n以下成员已添加：\r\n", msg3);
                            }
                            break;
                    }

                    break;
                case PCRGuildBattleCommand.ListMember: //查看成员
                    var list = dbAction.ShowMembers(QQGroup.Id);
                    if (list.Any())
                    {
                        StringBuilder sb                 = new StringBuilder();
                        double        maxLenghtOfQQ      = 0; //最长的QQ号长度，用于Pad对齐
                        double        maxLenghtOfNick    = 0; //最长的昵称长度，用于Pad对齐
                        int           maxLenghtOfQQint   = 0; //最长的QQ号长度，用于Pad对齐
                        int           maxLenghtOfNickint = 0; //最长的昵称长度，用于Pad对齐
                        list.ForEach(async i =>
                                     {
                                         await GMgrEventArgs.SourceGroup.GetGroupMemberInfo(i.Uid);
                                         (APIStatusType status, GroupMemberInfo groupMemberInfo) = await GMgrEventArgs.SourceGroup.GetGroupMemberInfo(i.Uid);
                                         if (status != APIStatusType.OK)
                                         {
                                             ConsoleLog.Error("API Error",$"API ret error {status}");
                                             return;
                                         }
                                         if (SuiseiUtils.GetQQStrLength(i.Uid.ToString()) > maxLenghtOfQQ)
                                         {
                                             maxLenghtOfQQ = SuiseiUtils.GetQQStrLength(i.Uid.ToString());
                                         }

                                         if (SuiseiUtils.GetQQStrLength(groupMemberInfo.Nick) > maxLenghtOfNick)
                                         {
                                             maxLenghtOfNick = SuiseiUtils.GetQQStrLength(groupMemberInfo.Nick);
                                         }

                                         if (i.Uid.ToString().Length > maxLenghtOfQQint)
                                         {
                                             maxLenghtOfQQint = i.Uid.ToString().Length;
                                         }

                                         if (groupMemberInfo.Nick.Length > maxLenghtOfNickint)
                                         {
                                             maxLenghtOfNickint = groupMemberInfo.Nick.Length;
                                         }
                                     });
                        maxLenghtOfQQ++;
                        maxLenghtOfQQ++;
                        list.ForEach(async i =>
                                     {
                                         (APIStatusType status, GroupMemberInfo member) = await GMgrEventArgs.SoraApi.GetGroupMemberInfo(i.Gid, i.Uid);
                                         if (status != APIStatusType.OK)
                                         {
                                             ConsoleLog.Error("API Error",$"API ret error {status}");
                                             return;
                                         }
                                         
                                         sb.Append("\n"     + SuiseiUtils.PadRightQQ(i.Uid.ToString(), maxLenghtOfQQ) +
                                                   "  |   " +
                                                   member.Nick);
                                     });

                        string listHeader = "\n\t" + dbAction.GetGuildName(QQGroup.Id);
                        listHeader += "\n\t公会成员列表";
                        listHeader += "\n".PadRight(maxLenghtOfNickint + maxLenghtOfQQint + 6, '=');
                        listHeader += "\n" + SuiseiUtils.PadRightQQ("QQ号", maxLenghtOfQQ) + "  |   昵称";
                        await QQGroup.SendGroupMessage(CQCode.CQAt(GMgrEventArgs.Sender.Id), listHeader, sb.ToString());
                    }
                    else
                    {
                        await QQGroup.SendGroupMessage(CQCode.CQAt(GMgrEventArgs.Sender.Id), " 公会暂无成员噢~");
                    }

                    break;
                //参数1 QQ号
                case PCRGuildBattleCommand.QuitGuild: //退会
                    if (!isAdminAction)
                    {
                        await QQGroup.SendGroupMessage(CQCode.CQAt(GMgrEventArgs.Sender.Id),
                                                 " 你没有权限这样做~");
                        ConsoleLog.Warning($"会战[群:{QQGroup.Id}]", $"群成员{GMgrEventArgs.SenderInfo.Nick}正在尝试执行指令{commandType}");
                        return;
                    }

                    Dictionary<long, int> deletedQQList = new Dictionary<long, int>(); //已经入会的QQ号列表
                    var lengthRet = SuiseiUtils.CheckForLength(commandArgs, 1, QQGroup, GMgrEventArgs.Sender.Id);
                    if (lengthRet == LenType.Legitimate || lengthRet == LenType.Extra)
                    {
                        List<long> atQQs = GMgrEventArgs.Message.GetAllAtList();
                        if (atQQs.Count == 0) //没有AT任何人，参数非法
                        {
                            await QQGroup.SendGroupMessage(CQCode.CQAt(GMgrEventArgs.Sender.Id),
                                                     "没有AT任何人");
                            return;
                        }

                        if (atQQs.Count >= 1) //如果存在AT
                        {
                            foreach (long qqid in atQQs) //检查每一个AT
                            {
                                //需要添加移除成员的操作的返回值
                                deletedQQList.Add(qqid, dbAction.LeaveGuild(qqid, QQGroup.Id));
                            }

                            //如果全部删除成功
                            if (deletedQQList.Count > 0 && deletedQQList.Where(x => x.Value == 1).ToList().Count == 0)
                            {
                                result = 0;
                            }
                            else
                            {
                                //否则就是有些QQ号并不存在
                                result = 2;
                            }
                        }

                   
                        if (deletedQQList.Where(x => x.Value == -1).ToList().Count > 0)
                        {
                            result = -1;
                        }

                        switch (result)
                        {
                            case -2: //不可能进入，但防御性编程，需要处理
                            {
                                await QQGroup.SendGroupMessage(CQCode.CQAt(GMgrEventArgs.Sender.Id), " 未定义行为，请检查代码。");
                                break;
                            }
                            case -1: //数据库出错
                            {
                                await QQGroup.SendGroupMessage(CQCode.CQAt(GMgrEventArgs.Sender.Id), " 操作过程中数据库出现错误。");
                                StringBuilder sb11 = new StringBuilder();
                                //筛选出所有返回值为1的操作，也即未在公会的成员
                                foreach (long qqNumber in deletedQQList
                                                          .Where(x => x.Value == 1)
                                                          .ToDictionary(x => x.Key, x => x.Value)
                                                          .Keys)
                                    sb11.Append(CQCode.CQAt(qqNumber));

                                StringBuilder sb12 = new StringBuilder();
                                //如果有操作返回值为0，说明是成功退会的
                                if (deletedQQList.Where(x => x.Value == 0).ToDictionary(x => x.Key, x => x.Value).Count >
                                    0)
                                {
                                    foreach (long qqNumber in deletedQQList
                                                              .Where(x => x.Value == 0)
                                                              .ToDictionary(x => x.Key, x => x.Value).Keys)
                                        sb12.Append(CQCode.CQAt(qqNumber));
                                }
                                StringBuilder sb13 = new StringBuilder();
                                if (deletedQQList.Where(x => x.Value == -1).ToDictionary(x => x.Key, x => x.Value).Count >
                                    0)
                                {
                                    foreach (long qqNumber in deletedQQList
                                                              .Where(x => x.Value == -1)
                                                              .ToDictionary(x => x.Key, x => x.Value).Keys)
                                        sb13.Append(CQCode.CQAt(qqNumber));
                                }

                                if (sb11.ToString() != "" || sb12.ToString() != "" || sb13.ToString() != "")
                                {
                                    await QQGroup.SendGroupMessage(CQCode.CQAt(GMgrEventArgs.Sender.Id), " 结果如下，",
                                                                   sb11.ToString() != ""
                                                                       ? ("以下成员未在公会：\r\n" + sb11)
                                                                       : "",
                                                                   sb12.ToString() != ""
                                                                       ? ("\r\n以下成员成功退会\r\n" + sb12)
                                                                       : "" /*只有存在新添加成员的情况下才需要显示这一句*/,
                                                                   sb13.ToString() != ""
                                                                       ? ("\r\n以下成员操作时发生错误\r\n" + sb13)
                                                                       : ""
                                                                  );
                                }

                                break;
                            }
                            case 0: //QQ号全部成功退会
                            {
                                //如果是自己退会
                                if (deletedQQList.Count == 1 && deletedQQList.First().Key == GMgrEventArgs.Sender.Id)
                                {
                                    await QQGroup.SendGroupMessage(CQCode.CQAt(GMgrEventArgs.Sender.Id), " 你已退会");
                                }
                                else
                                {
                                    StringBuilder sb = new StringBuilder();
                                    //at所有退会的成员
                                    foreach (long qqNumber in deletedQQList.Keys)
                                        sb.Append(CQCode.CQAt(qqNumber));
                                    await QQGroup.SendGroupMessage(CQCode.CQAt(GMgrEventArgs.Sender.Id), " 以下成员已经退会：\r\n",
                                                                   sb.ToString());
                                }


                                break;
                            }
                            case 1: //自己退会但并不在公会中
                            {
                                await QQGroup.SendGroupMessage(CQCode.CQAt(GMgrEventArgs.Sender.Id), " 你并不在公会中，无法退会");
                                break;
                            }
                            case 2: //存在不在公会里的成员
                            {
                                StringBuilder sb2 = new StringBuilder();
                                //筛选出所有返回值为0的操作，也即成功退会的
                                foreach (long qqNumber in deletedQQList
                                                          .Where(x => x.Value == 0).ToDictionary(x => x.Key, x => x.Value)
                                                          .Keys)
                                    sb2.Append(CQCode.CQAt(qqNumber));

                                StringBuilder sb3 = new StringBuilder();
                                //如果有操作返回值为1，说明存在并不在公会里的成员
                                if (deletedQQList.Where(x => x.Value == 1).ToDictionary(x => x.Key, x => x.Value).Count > 0)
                                {
                                    foreach (long qqNumber in deletedQQList
                                                              .Where(x => x.Value == 1)
                                                              .ToDictionary(x => x.Key, x => x.Value).Keys)
                                        sb3.Append(CQCode.CQAt(qqNumber));
                                }

                                await QQGroup.SendGroupMessage(CQCode.CQAt(GMgrEventArgs.Sender.Id),
                                                               " 部分成员并未在公会中，",
                                                               sb2.ToString() != ""
                                                                   ? ("以下成员已经退会：\r\n" + sb2)
                                                                   : "",
                                                               sb3.ToString() != ""
                                                                   ? ("\r\n以下成员并未在公会中\r\n" + sb3)
                                                                   : "");
                                break;
                            }
                        }
                    }
                    break;
                case PCRGuildBattleCommand.QuitAll: //清空成员
                    if (!isAdminAction)
                    {
                        await QQGroup.SendGroupMessage(CQCode.CQAt(GMgrEventArgs.Sender.Id),
                                                 " 你没有权限这样做~");
                        ConsoleLog.Warning($"会战[群:{QQGroup.Id}]", $"群成员{GMgrEventArgs.SenderInfo.Nick}正在尝试执行指令{commandType}");
                        return;
                    }

                    int retCode = dbAction.EmptyMember(QQGroup.Id);
                    switch (retCode)
                    {
                        case -1:
                            await QQGroup.SendGroupMessage(CQCode.CQAt(GMgrEventArgs.Sender.Id),
                                                     " 数据库发生错误~");
                            break;
                        case 0:
                            await QQGroup.SendGroupMessage(CQCode.CQAt(GMgrEventArgs.Sender.Id),
                                                     " 清空成功~");
                            break;
                        case 1:
                            await QQGroup.SendGroupMessage(CQCode.CQAt(GMgrEventArgs.Sender.Id),
                                                     " 该群并没有建立公会或公会里没有任何成员~");
                            break;
                        case 2:
                            await QQGroup.SendGroupMessage(CQCode.CQAt(GMgrEventArgs.Sender.Id),
                                                     " 清空时发生错误！");
                            break;
                        default:
                            await QQGroup.SendGroupMessage(CQCode.CQAt(GMgrEventArgs.Sender.Id),
                                                     " 未知错误！返回值异常！");
                            break;
                    }

                    break;
                case PCRGuildBattleCommand.JoinAll: //一键入会
                    if (!isAdminAction)
                    {
                        await QQGroup.SendGroupMessage(CQCode.CQAt(GMgrEventArgs.Sender.Id),
                                                 " 你没有权限这样做~");
                        return;
                    }

                    Dictionary<long, int> addedQQsList = new Dictionary<long, int>(); //已经入会的QQ号列表
                    var memberList = await QQGroup.GetGroupMemberList();
                    if (memberList.apiStatus != APIStatusType.OK)
                    {
                        ConsoleLog.Error("API Error",$"API ret error {memberList.apiStatus}");
                        return;
                    }
                    foreach (GroupMemberInfo member in memberList.groupMemberList)
                    {
                        //需要添加为成员的QQ号列表和对应操作的返回值
                        addedQQsList.Add(member.UserId,
                                         await dbAction.JoinToGuild(member.UserId, QQGroup.Id, SuiseiUtils.getNick(member)));
                    }

                    //如果只存在需要添加的成员，而没有需要更新的成员
                    if (addedQQsList.Count > 0 && addedQQsList.Where(x => x.Value == 1).ToList().Count == 0)
                    {
                        result = 0;
                    }
                    else
                    {
                        //否则就是既存在需要更新又存在需要添加的
                        result = 2;
                    }

                    if (addedQQsList.Where(x => x.Value == -1).ToList().Count > 0)
                    {
                        result = -1;
                    }

                    switch (result)
                    {
                        case -2: //不可能进入，但防御性编程，需要处理
                            await QQGroup.SendGroupMessage(CQCode.CQAt(GMgrEventArgs.Sender.Id), " 未定义行为，请检查代码。");
                            break;
                        case -1: //数据库出错
                            await QQGroup.SendGroupMessage(CQCode.CQAt(GMgrEventArgs.Sender.Id), " 操作过程中数据库出现错误。");
                            StringBuilder sb11 = new StringBuilder();
                            //筛选出所有返回值为1的操作，也即更新了的成员
                            foreach (long qqNumber in addedQQsList
                                                      .Where(x => x.Value == 1)
                                                      .ToDictionary(x => x.Key, x => x.Value)
                                                      .Keys)
                                sb11.Append(CQCode.CQAt(qqNumber));

                            StringBuilder sb12 = new StringBuilder();
                            //如果有操作返回值为0，说明存在新添加的成员
                            if (addedQQsList.Where(x => x.Value == 0).ToDictionary(x => x.Key, x => x.Value).Count >
                                0)
                            {
                                foreach (long qqNumber in addedQQsList
                                                          .Where(x => x.Value == 0)
                                                          .ToDictionary(x => x.Key, x => x.Value).Keys)
                                    sb12.Append(CQCode.CQAt(qqNumber));
                            }
                            StringBuilder sb13 = new StringBuilder();
                            if (addedQQsList.Where(x => x.Value == -1).ToDictionary(x => x.Key, x => x.Value).Count >
                                0)
                            {
                                foreach (long qqNumber in addedQQsList
                                                          .Where(x => x.Value == -1)
                                                          .ToDictionary(x => x.Key, x => x.Value).Keys)
                                    sb13.Append(CQCode.CQAt(qqNumber));
                            }

                            if (sb11.ToString() != "" || sb12.ToString() != "" || sb13.ToString() != "")
                            {
                                await QQGroup.SendGroupMessage(CQCode.CQAt(GMgrEventArgs.Sender.Id), " 结果如下，",
                                                         sb11.ToString() != ""
                                                             ? ("以下成员已经更新：\r\n" + sb11.ToString())
                                                             : "",
                                                         sb12.ToString() != ""
                                                             ? ("\r\n以下成员已添加\r\n" + sb12.ToString())
                                                             : "" /*只有存在新添加成员的情况下才需要显示这一句*/,
                                                         sb13.ToString() != ""
                                                             ? ("\r\n以下成员操作时发生错误\r\n" + sb13.ToString())
                                                             : ""
                                                         );
                            }

                            break;
                        case 0: //只存在新添加的成员

                            StringBuilder sb = new StringBuilder();
                            //at所有新添加的成员
                            foreach (long qqNumber in addedQQsList.Keys)
                                sb.Append(CQCode.CQAt(qqNumber));
                            await QQGroup.SendGroupMessage(CQCode.CQAt(GMgrEventArgs.Sender.Id), " 以下成员已经加入：\r\n",
                                                     sb.ToString());

                            break;
                        case 1: //只存在需要更新的成员，目前也不可能进入了
                            await QQGroup.SendGroupMessage(CQCode.CQAt(GMgrEventArgs.Sender.Id),
                                                     " 成员已经存在，更新了当前成员的信息。");
                            break;
                        case 2: //存在需要更新和/或需要添加的成员
                            StringBuilder sb2 = new StringBuilder();
                            //筛选出所有返回值为1的操作，也即更新了的成员
                            foreach (long qqNumber in addedQQsList
                                                      .Where(x => x.Value == 1)
                                                      .ToDictionary(x => x.Key, x => x.Value)
                                                      .Keys)
                                sb2.Append(CQCode.CQAt(qqNumber));

                            StringBuilder sb3 = new StringBuilder();
                            //如果有操作返回值为0，说明存在新添加的成员
                            if (addedQQsList.Where(x => x.Value == 0).ToDictionary(x => x.Key, x => x.Value).Count >
                                0)
                            {
                                foreach (long qqNumber in addedQQsList
                                                          .Where(x => x.Value == 0)
                                                          .ToDictionary(x => x.Key, x => x.Value).Keys)
                                    sb3.Append(CQCode.CQAt(qqNumber));
                            }

                            await QQGroup.SendGroupMessage(CQCode.CQAt(GMgrEventArgs.Sender.Id), " 有成员已经存在，",
                                                     sb2.ToString() != ""
                                                         ? ("以下成员已经更新：\r\n" + sb2.ToString())
                                                         : "",
                                                     sb3.ToString() != ""
                                                         ? ("\r\n以下成员已添加\r\n" + sb3.ToString())
                                                         : "" /*只有存在新添加成员的情况下才需要显示这一句*/);
                            break;
                    }


                    break;

                default: //不可能发生，防御性处理
                    ConsoleLog.Warning($"会战[群:{QQGroup.Id}]", $"接到未知指令{commandType}");
                    break;
            }
        }
    }
}
