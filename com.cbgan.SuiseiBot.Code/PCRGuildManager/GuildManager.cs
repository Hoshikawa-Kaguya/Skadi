
using SuiseiBot.PCRHandle;
using SuiseiBot.Database.Helpers;
using SuiseiBot.TypeEnum;
using SuiseiBot.TypeEnum.CmdType;
using SuiseiBot.Tool;
using SuiseiBot.Tool.Log;
using Native.Sdk.Cqp;
using Native.Sdk.Cqp.Enum;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Model;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SuiseiBot.PCRGuildManager
{
    internal static class PCRHandler
    {
        /// <summary>
        /// 公会管理指令响应函数
        /// </summary>
        /// <param name="Sender">CQSender</param>
        /// <param name="GMgrEventArgs">CQGroupMessageEventArgs</param>
        /// <param name="commandType">指令类型 [0-100]</param>
        public static void GuildMgrResponse(object Sender, CQGroupMessageEventArgs GMgrEventArgs,
                                            PCRGuildCmdType commandType) //功能响应
        {
            Group QQgroup = GMgrEventArgs.FromGroup;

            //index=0为命令本身，其余为参数
            string[] commandArgs = GMgrEventArgs.Message.Text.Split(' ');

            GuildManagerDBHelper dbAction = new GuildManagerDBHelper(Sender, GMgrEventArgs);

            int result = -2;


            QQGroupMemberType fromQQMemberType = QQgroup.GetGroupMemberInfo(GMgrEventArgs.FromQQ.Id).MemberType;
            //指示是否是管理员操作的
            bool isAdminAction = (fromQQMemberType == QQGroupMemberType.Manage ||
                                  fromQQMemberType == QQGroupMemberType.Creator);
            switch (commandType)
            {
                //参数1 服务器地区，参数2 公会名（可选，缺省为群名）
                case PCRGuildCmdType.CreateGuild: //建会
                    if (!isAdminAction)
                    {
                        QQgroup.SendGroupMessage(CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id),
                                                 " 你没有权限这样做~");
                        ConsoleLog.Warning($"会战[群:{QQgroup.Id}]", $"群成员{QQgroup.GetGroupMemberInfo(GMgrEventArgs.FromQQ.Id).Nick}正在尝试执行指令{commandType}");
                        return;
                    }

                    if (Utils.CheckForLength(commandArgs, 1, QQgroup, GMgrEventArgs.FromQQ.Id) == LenType.Legitimate)
                    {
                        if (commandArgs.Length == 3)
                        {
                            result = dbAction.createGuild(commandArgs[1], commandArgs[2], QQgroup.Id);
                        }
                        else if (commandArgs.Length == 2)
                        {
                            result = dbAction.createGuild(commandArgs[1], QQgroup.GetGroupInfo().Name, QQgroup.Id);
                        }
                    }

                    switch (result)
                    {
                        case -1:
                            QQgroup.SendGroupMessage(
                                                     CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id),
                                                     $" 公会[{GMgrEventArgs.CQApi.GetGroupInfo(GMgrEventArgs.FromGroup.Id).Name}]创建失败：数据库错误。");
                            break;
                        case 0:
                            QQgroup.SendGroupMessage(
                                                     CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id),
                                                     $" 公会[{GMgrEventArgs.CQApi.GetGroupInfo(GMgrEventArgs.FromGroup.Id).Name}]已经创建。");
                            break;
                        case 1:
                            QQgroup.SendGroupMessage(CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id), " 公会已经存在，更新了当前公会的信息。");
                            break;
                    }

                    break;
                //参数1 QQ号
                case PCRGuildCmdType.JoinGuild: //入会
                    if (!isAdminAction)
                    {
                        QQgroup.SendGroupMessage(CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id),
                                                 " 你没有权限这样做~");
                        ConsoleLog.Warning($"会战[群:{QQgroup.Id}]", $"群成员{QQgroup.GetGroupMemberInfo(GMgrEventArgs.FromQQ.Id).Nick}正在尝试执行指令{commandType}");
                        return;
                    }

                    Dictionary<long, int> addedQQList = new Dictionary<long, int>(); //已经入会的QQ号列表
                    var checkRet = Utils.CheckForLength(commandArgs, 1, QQgroup, GMgrEventArgs.FromQQ.Id);
                    if (checkRet == LenType.Extra || checkRet == LenType.Legitimate)
                    {
                        List<long> atQQs = Utils.GetAtList(GMgrEventArgs.Message.CQCodes, out int status);
                        result = status;
                        if (atQQs.Count == 0) //没有AT任何人，参数非法
                        {
                            PCRGuildHandle.GetIllegalArgs(GMgrEventArgs, PCRGuildCmdType.JoinGuild, "没有AT任何人");
                            return;
                        }

                        if (atQQs.Count >= 1) //如果存在AT
                        {
                            foreach (long qqid in atQQs) //检查每一个AT
                            {
                                //需要添加为成员的QQ号列表和对应操作的返回值
                                addedQQList.Add(qqid,
                                                dbAction.JoinToGuild(qqid, QQgroup.Id,
                                                                     Utils.getNick(GMgrEventArgs
                                                                         .CQApi
                                                                         .GetGroupMemberInfo(GMgrEventArgs.FromGroup,
                                                                             qqid))));
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
                            result = dbAction.JoinToGuild(GMgrEventArgs.FromQQ, QQgroup.Id,
                                                          Utils.getNick(GMgrEventArgs
                                                                        .CQApi
                                                                        .GetGroupMemberInfo(GMgrEventArgs.FromGroup,
                                                                            GMgrEventArgs.FromQQ)));
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
                            QQgroup.SendGroupMessage(CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id), " 未定义行为，请检查代码。");
                            break;
                        case -1: //一般情况下不可能非法，但也要处理
                            QQgroup.SendGroupMessage(CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id), " 操作过程中数据库出现错误。");
                            StringBuilder sb11 = new StringBuilder();
                            //筛选出所有返回值为1的操作，也即更新了的成员
                            foreach (long qqNumber in addedQQList
                                                      .Where(x => x.Value == 1)
                                                      .ToDictionary(x => x.Key, x => x.Value)
                                                      .Keys)
                                sb11.Append(CQApi.CQCode_At(qqNumber).ToSendString());

                            StringBuilder sb12 = new StringBuilder();
                            //如果有操作返回值为0，说明存在新添加的成员
                            if (addedQQList.Where(x => x.Value == 0).ToDictionary(x => x.Key, x => x.Value).Count >
                                0)
                            {
                                foreach (long qqNumber in addedQQList
                                                          .Where(x => x.Value == 0)
                                                          .ToDictionary(x => x.Key, x => x.Value).Keys)
                                    sb12.Append(CQApi.CQCode_At(qqNumber).ToSendString());
                            }
                            StringBuilder sb13 = new StringBuilder();
                            if (addedQQList.Where(x => x.Value == -1).ToDictionary(x => x.Key, x => x.Value).Count >
                                0)
                            {
                                foreach (long qqNumber in addedQQList
                                                          .Where(x => x.Value == -1)
                                                          .ToDictionary(x => x.Key, x => x.Value).Keys)
                                    sb13.Append(CQApi.CQCode_At(qqNumber).ToSendString());
                            }

                            if (sb11.ToString() != "" || sb12.ToString() != "" || sb13.ToString() != "")
                            {
                                QQgroup.SendGroupMessage(CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id), " 结果如下，",
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
                            foreach (long qqNumber in addedQQList.Keys)
                                sb.Append(CQApi.CQCode_At(qqNumber).ToSendString());
                            QQgroup.SendGroupMessage(CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id), " 以下成员已经加入：\r\n",
                                                     sb.ToString());

                            break;
                        case 1: //只存在需要更新的成员，目前也不可能进入了
                            QQgroup.SendGroupMessage(CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id), " 成员已经存在，更新了当前成员的信息。");
                            break;
                        case 2: //存在需要更新和/或需要添加的成员
                            StringBuilder sb2 = new StringBuilder();
                            //筛选出所有返回值为1的操作，也即更新了的成员
                            foreach (long qqNumber in addedQQList
                                                      .Where(x => x.Value == 1).ToDictionary(x => x.Key, x => x.Value)
                                                      .Keys)
                                sb2.Append(CQApi.CQCode_At(qqNumber).ToSendString());

                            StringBuilder sb3 = new StringBuilder();
                            //如果有操作返回值为0，说明存在新添加的成员
                            if (addedQQList.Where(x => x.Value == 0).ToDictionary(x => x.Key, x => x.Value).Count > 0)
                            {
                                foreach (long qqNumber in addedQQList
                                                          .Where(x => x.Value == 0)
                                                          .ToDictionary(x => x.Key, x => x.Value).Keys)
                                    sb3.Append(CQApi.CQCode_At(qqNumber).ToSendString());
                            }

                            QQgroup.SendGroupMessage(CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id), " 有成员已经存在，",
                                                     sb2.ToString() != ""
                                                         ? ("以下成员已经更新：\r\n" + sb2.ToString())
                                                         : "",
                                                     sb3.ToString() != ""
                                                         ? ("\r\n以下成员已添加\r\n" + sb3.ToString())
                                                         : "" /*只有存在新添加成员的情况下才需要显示这一句*/);
                            break;
                    }

                    break;
                case PCRGuildCmdType.ListMember: //查看成员
                    var list = dbAction.ShowMembers(QQgroup.Id);
                    if (list.Any())
                    {
                        StringBuilder sb                 = new StringBuilder();
                        double        maxLenghtOfQQ      = 0; //最长的QQ号长度，用于Pad对齐
                        double        maxLenghtOfNick    = 0; //最长的昵称长度，用于Pad对齐
                        int           maxLenghtOfQQint   = 0; //最长的QQ号长度，用于Pad对齐
                        int           maxLenghtOfNickint = 0; //最长的昵称长度，用于Pad对齐
                        list.ForEach(i =>
                                     {
                                         if (Utils.getQQStrLength(i.Uid.ToString()) > maxLenghtOfQQ)
                                         {
                                             maxLenghtOfQQ = Utils.getQQStrLength(i.Uid.ToString());
                                         }

                                         if (Utils.getQQStrLength(i.NickName.ToString()) > maxLenghtOfNick)
                                         {
                                             maxLenghtOfNick = Utils.getQQStrLength(i.NickName.ToString());
                                         }

                                         if (i.Uid.ToString().Length > maxLenghtOfQQint)
                                         {
                                             maxLenghtOfQQint = i.Uid.ToString().Length;
                                         }

                                         if (i.NickName.ToString().Length > maxLenghtOfNickint)
                                         {
                                             maxLenghtOfNickint = i.NickName.ToString().Length;
                                         }
                                     });
                        maxLenghtOfQQ++;
                        maxLenghtOfQQ++;
                        list.ForEach(i => sb.Append("\n"     + Utils.PadRightQQ(i.Uid.ToString(), maxLenghtOfQQ) +
                                                    "  |   " +
                                                    i.NickName));

                        string listHeader = "\n\t" + dbAction.getGuildName(QQgroup.Id);
                        listHeader += "\n\t公会成员列表";
                        listHeader += "\n".PadRight(maxLenghtOfNickint + maxLenghtOfQQint + 6, '=');
                        listHeader += "\n" + Utils.PadRightQQ("QQ号", maxLenghtOfQQ) + "  |   昵称";
                        QQgroup.SendGroupMessage(CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id), listHeader, sb.ToString());
                    }
                    else
                    {
                        QQgroup.SendGroupMessage(CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id), " 公会暂无成员噢~");
                    }

                    break;
                //参数1 QQ号
                case PCRGuildCmdType.QuitGuild: //退会
                    if (!isAdminAction)
                    {
                        QQgroup.SendGroupMessage(CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id),
                                                 " 你没有权限这样做~");
                        ConsoleLog.Warning($"会战[群:{QQgroup.Id}]", $"群成员{QQgroup.GetGroupMemberInfo(GMgrEventArgs.FromQQ.Id).Nick}正在尝试执行指令{commandType}");
                        return;
                    }

                    Dictionary<long, int> deletedQQList = new Dictionary<long, int>(); //已经入会的QQ号列表
                    var lengthRet = Utils.CheckForLength(commandArgs, 1, QQgroup, GMgrEventArgs.FromQQ.Id);
                    if (lengthRet == LenType.Legitimate || lengthRet == LenType.Extra)
                    {
                        List<long> atQQs = Utils.GetAtList(GMgrEventArgs.Message.CQCodes, out int status);
                        result = status;
                        if (atQQs.Count == 0) //没有AT任何人，参数非法
                        {
                            PCRGuildHandle.GetIllegalArgs(GMgrEventArgs, PCRGuildCmdType.QuitGuild, "没有AT任何人");
                            return;
                        }

                        if (atQQs.Count >= 1) //如果存在AT
                        {
                            foreach (long qqid in atQQs) //检查每一个AT
                            {
                                //需要添加移除成员的操作的返回值
                                deletedQQList.Add(qqid, dbAction.LeaveGuild(qqid, QQgroup.Id));
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
                        else //自分の退会
                        {
                            result = dbAction.LeaveGuild(GMgrEventArgs.FromQQ, QQgroup.Id);
                        }
                    }

                    if (deletedQQList.Where(x => x.Value == -1).ToList().Count > 0)
                    {
                        result = -1;
                    }

                    switch (result)
                    {
                        case -2: //不可能进入，但防御性编程，需要处理
                            QQgroup.SendGroupMessage(CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id), " 未定义行为，请检查代码。");
                            break;
                        case -1: //数据库出错
                            QQgroup.SendGroupMessage(CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id), " 操作过程中数据库出现错误。");
                            StringBuilder sb11 = new StringBuilder();
                            //筛选出所有返回值为1的操作，也即未在公会的成员
                            foreach (long qqNumber in deletedQQList
                                                      .Where(x => x.Value == 1)
                                                      .ToDictionary(x => x.Key, x => x.Value)
                                                      .Keys)
                                sb11.Append(CQApi.CQCode_At(qqNumber).ToSendString());

                            StringBuilder sb12 = new StringBuilder();
                            //如果有操作返回值为0，说明是成功退会的
                            if (deletedQQList.Where(x => x.Value == 0).ToDictionary(x => x.Key, x => x.Value).Count >
                                0)
                            {
                                foreach (long qqNumber in deletedQQList
                                                          .Where(x => x.Value == 0)
                                                          .ToDictionary(x => x.Key, x => x.Value).Keys)
                                    sb12.Append(CQApi.CQCode_At(qqNumber).ToSendString());
                            }
                            StringBuilder sb13 = new StringBuilder();
                            if (deletedQQList.Where(x => x.Value == -1).ToDictionary(x => x.Key, x => x.Value).Count >
                                0)
                            {
                                foreach (long qqNumber in deletedQQList
                                                          .Where(x => x.Value == -1)
                                                          .ToDictionary(x => x.Key, x => x.Value).Keys)
                                    sb13.Append(CQApi.CQCode_At(qqNumber).ToSendString());
                            }

                            if (sb11.ToString() != "" || sb12.ToString() != "" || sb13.ToString() != "")
                            {
                                QQgroup.SendGroupMessage(CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id), " 结果如下，",
                                                         sb11.ToString() != ""
                                                             ? ("以下成员未在公会：\r\n" + sb11.ToString())
                                                             : "",
                                                         sb12.ToString() != ""
                                                             ? ("\r\n以下成员成功退会\r\n" + sb12.ToString())
                                                             : "" /*只有存在新添加成员的情况下才需要显示这一句*/,
                                                         sb13.ToString() != ""
                                                             ? ("\r\n以下成员操作时发生错误\r\n" + sb13.ToString())
                                                             : ""
                                                         );
                            }

                            break;
                        case 0: //QQ号全部成功退会
                            //如果是自己退会
                            if (deletedQQList.Count == 1 && deletedQQList.First().Key == GMgrEventArgs.FromQQ.Id)
                            {
                                QQgroup.SendGroupMessage(CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id), " 你已退会");
                            }
                            else
                            {
                                StringBuilder sb = new StringBuilder();
                                //at所有退会的成员
                                foreach (long qqNumber in deletedQQList.Keys)
                                    sb.Append(CQApi.CQCode_At(qqNumber).ToSendString());
                                QQgroup.SendGroupMessage(CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id), " 以下成员已经退会：\r\n",
                                                         sb.ToString());
                            }


                            break;
                        case 1: //自己退会但并不在公会中
                            QQgroup.SendGroupMessage(CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id), " 你并不在公会中，无法退会");
                            break;
                        case 2: //存在不在公会里的成员
                            StringBuilder sb2 = new StringBuilder();
                            //筛选出所有返回值为0的操作，也即成功退会的
                            foreach (long qqNumber in deletedQQList
                                                      .Where(x => x.Value == 0).ToDictionary(x => x.Key, x => x.Value)
                                                      .Keys)
                                sb2.Append(CQApi.CQCode_At(qqNumber).ToSendString());

                            StringBuilder sb3 = new StringBuilder();
                            //如果有操作返回值为1，说明存在并不在公会里的成员
                            if (deletedQQList.Where(x => x.Value == 1).ToDictionary(x => x.Key, x => x.Value).Count > 0)
                            {
                                foreach (long qqNumber in deletedQQList
                                                          .Where(x => x.Value == 1)
                                                          .ToDictionary(x => x.Key, x => x.Value).Keys)
                                    sb3.Append(CQApi.CQCode_At(qqNumber).ToSendString());
                            }

                            QQgroup.SendGroupMessage(CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id),
                                                     " 部分成员并未在公会中，",
                                                     sb2.ToString() != ""
                                                         ? ("以下成员已经退会：\r\n" + sb2.ToString())
                                                         : "",
                                                     sb3.ToString() != ""
                                                         ? ("\r\n以下成员并未在公会中\r\n" + sb3.ToString())
                                                         : "");
                            break;
                    }

                    break;
                case PCRGuildCmdType.QuitAll: //清空成员
                    if (!isAdminAction)
                    {
                        QQgroup.SendGroupMessage(CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id),
                                                 " 你没有权限这样做~");
                        ConsoleLog.Warning($"会战[群:{QQgroup.Id}]", $"群成员{QQgroup.GetGroupMemberInfo(GMgrEventArgs.FromQQ.Id).Nick}正在尝试执行指令{commandType}");
                        return;
                    }

                    int retCode = dbAction.EmptyMember(QQgroup.Id);
                    switch (retCode)
                    {
                        case -1:
                            QQgroup.SendGroupMessage(CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id),
                                                     " 数据库发生错误~");
                            break;
                        case 0:
                            QQgroup.SendGroupMessage(CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id),
                                                     " 清空成功~");
                            break;
                        case 1:
                            QQgroup.SendGroupMessage(CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id),
                                                     " 该群并没有建立公会或公会里没有任何成员~");
                            break;
                        case 2:
                            QQgroup.SendGroupMessage(CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id),
                                                     " 清空时发生错误！");
                            break;
                        default:
                            QQgroup.SendGroupMessage(CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id),
                                                     " 未知错误！返回值异常！");
                            break;
                    }

                    break;
                case PCRGuildCmdType.JoinAll: //一键入会
                    if (!isAdminAction)
                    {
                        QQgroup.SendGroupMessage(CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id),
                                                 " 你没有权限这样做~");
                        return;
                    }

                    Dictionary<long, int> addedQQsList = new Dictionary<long, int>(); //已经入会的QQ号列表
                    foreach (GroupMemberInfo member in QQgroup.GetGroupMemberList())
                    {
                        //需要添加为成员的QQ号列表和对应操作的返回值
                        addedQQsList.Add(member.QQ.Id,
                                         dbAction.JoinToGuild(member.QQ.Id, QQgroup.Id, Utils.getNick(member)));
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
                            QQgroup.SendGroupMessage(CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id), " 未定义行为，请检查代码。");
                            break;
                        case -1: //数据库出错
                            QQgroup.SendGroupMessage(CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id), " 操作过程中数据库出现错误。");
                            StringBuilder sb11 = new StringBuilder();
                            //筛选出所有返回值为1的操作，也即更新了的成员
                            foreach (long qqNumber in addedQQsList
                                                      .Where(x => x.Value == 1)
                                                      .ToDictionary(x => x.Key, x => x.Value)
                                                      .Keys)
                                sb11.Append(CQApi.CQCode_At(qqNumber).ToSendString());

                            StringBuilder sb12 = new StringBuilder();
                            //如果有操作返回值为0，说明存在新添加的成员
                            if (addedQQsList.Where(x => x.Value == 0).ToDictionary(x => x.Key, x => x.Value).Count >
                                0)
                            {
                                foreach (long qqNumber in addedQQsList
                                                          .Where(x => x.Value == 0)
                                                          .ToDictionary(x => x.Key, x => x.Value).Keys)
                                    sb12.Append(CQApi.CQCode_At(qqNumber).ToSendString());
                            }
                            StringBuilder sb13 = new StringBuilder();
                            if (addedQQsList.Where(x => x.Value == -1).ToDictionary(x => x.Key, x => x.Value).Count >
                                0)
                            {
                                foreach (long qqNumber in addedQQsList
                                                          .Where(x => x.Value == -1)
                                                          .ToDictionary(x => x.Key, x => x.Value).Keys)
                                    sb13.Append(CQApi.CQCode_At(qqNumber).ToSendString());
                            }

                            if (sb11.ToString() != "" || sb12.ToString() != "" || sb13.ToString() != "")
                            {
                                QQgroup.SendGroupMessage(CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id), " 结果如下，",
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
                                sb.Append(CQApi.CQCode_At(qqNumber).ToSendString());
                            QQgroup.SendGroupMessage(CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id), " 以下成员已经加入：\r\n",
                                                     sb.ToString());

                            break;
                        case 1: //只存在需要更新的成员，目前也不可能进入了
                            QQgroup.SendGroupMessage(CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id),
                                                     " 成员已经存在，更新了当前成员的信息。");
                            break;
                        case 2: //存在需要更新和/或需要添加的成员
                            StringBuilder sb2 = new StringBuilder();
                            //筛选出所有返回值为1的操作，也即更新了的成员
                            foreach (long qqNumber in addedQQsList
                                                      .Where(x => x.Value == 1)
                                                      .ToDictionary(x => x.Key, x => x.Value)
                                                      .Keys)
                                sb2.Append(CQApi.CQCode_At(qqNumber).ToSendString());

                            StringBuilder sb3 = new StringBuilder();
                            //如果有操作返回值为0，说明存在新添加的成员
                            if (addedQQsList.Where(x => x.Value == 0).ToDictionary(x => x.Key, x => x.Value).Count >
                                0)
                            {
                                foreach (long qqNumber in addedQQsList
                                                          .Where(x => x.Value == 0)
                                                          .ToDictionary(x => x.Key, x => x.Value).Keys)
                                    sb3.Append(CQApi.CQCode_At(qqNumber).ToSendString());
                            }

                            QQgroup.SendGroupMessage(CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id), " 有成员已经存在，",
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
                    PCRGuildHandle.GetUnknowCommand(GMgrEventArgs);
                    ConsoleLog.Warning($"会战[群:{QQgroup.Id}]", $"接到未知指令{commandType}");
                    break;
            }
        }
    }
}