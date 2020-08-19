using System.Collections.Generic;
using System.Linq;
using System.Text;
using com.cbgan.SuiseiBot.Code.ChatHandlers;
using com.cbgan.SuiseiBot.Code.Database;
using com.cbgan.SuiseiBot.Code.Resource.TypeEnum;
using com.cbgan.SuiseiBot.Code.Resource.TypeEnum.CmdType;
using com.cbgan.SuiseiBot.Code.Tool;
using Native.Sdk.Cqp;
using Native.Sdk.Cqp.Enum;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Model;

namespace com.cbgan.SuiseiBot.Code.PCRGuildManager
{
    internal static class PCRHandler
    {
        /// <summary>
        /// 公会管理指令响应函数
        /// </summary>
        /// <param name="Sender">CQSender</param>
        /// <param name="GMgrEventArgs">CQGroupMessageEventArgs</param>
        /// <param name="commandType">指令类型 [0-100]</param>
        public static void GuildMgrResponse(object Sender,CQGroupMessageEventArgs GMgrEventArgs,PCRGuildCmdType commandType) //功能响应
        {
            Group QQgroup = GMgrEventArgs.FromGroup;

            //index=0为命令本身，其余为参数
            string[] commandArgs = GMgrEventArgs.Message.Text.Split(' ');

            GuildManagerDBHelper dbAction = new GuildManagerDBHelper(Sender, GMgrEventArgs);

            int result = -2;

            switch (commandType)
            {
                //参数1 服务器地区，参数2 公会名（可选，缺省为群名）
                case PCRGuildCmdType.CreateGuild://建会
                    if (Utils.CheckForLength(commandArgs, 1) == LenType.Legitimate) 
                    {
                        if (commandArgs.Length == 3)
                        {
                            result = dbAction.createGuild(commandArgs[1], commandArgs[2]);
                        }
                        else if (commandArgs.Length == 2)
                        {
                            result = dbAction.createGuild(commandArgs[1], QQgroup.GetGroupInfo().Name);
                        }
                    }

                    switch (result)
                    {
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
                case PCRGuildCmdType.JoinGuild://入会
                    Dictionary<long,int> addedQQList= new Dictionary<long, int>();    //已经入会的QQ号列表
                    if (Utils.CheckForLength(commandArgs, 1) == LenType.Extra)
                    {
                        List<long> atQQs = Utils.GetAtList(GMgrEventArgs.Message.CQCodes,out int status);
                        result = status;
                        if (atQQs.Count == 0)//没有AT任何人，参数非法
                        {
                            PCRGuildHandle.GetIllegalArgs(GMgrEventArgs, PCRGuildCmdType.JoinGuild, "没有AT任何人");
                            return;
                        }
                        if (atQQs.Count >= 1)           //如果存在AT
                        {
                            foreach (long qqid in atQQs)  //检查每一个AT
                            {
                                //需要添加为成员的QQ号列表和对应操作的返回值
                                addedQQList.Add(qqid, dbAction.JoinToGuild(qqid,QQgroup.Id, GMgrEventArgs.CQApi.GetGroupMemberInfo(GMgrEventArgs.FromGroup, qqid).Nick));
                            }
                            //如果只存在需要添加的成员，而没有需要更新的成员
                            if (addedQQList.Count > 0 && addedQQList.Where(x => x.Value == 1).ToList().Count == 0)
                            {
                                result = 0;
                            }
                            else
                            {//否则就是既存在需要更新又存在需要添加的
                                result = 2;
                            }
                        }
                        else
                        {
                            result = dbAction.JoinToGuild(GMgrEventArgs.FromQQ,QQgroup.Id ,GMgrEventArgs.CQApi.GetGroupMemberInfo(GMgrEventArgs.FromGroup, GMgrEventArgs.FromQQ).Nick);
                        }
                    }

                    switch (result)
                    {
                        case -2://不可能进入，但防御性编程，需要处理
                            QQgroup.SendGroupMessage(CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id), " 未定义行为，请检查代码。");
                            break;
                        case -1://一般情况下不可能非法，但也要处理
                            QQgroup.SendGroupMessage(CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id), " QQ号输入非法。");
                            break;
                        case 0://只存在新添加的成员
                          
                            StringBuilder sb=new StringBuilder();
                            //at所有新添加的成员
                            foreach (long qqNumber in addedQQList.Keys)
                                sb.Append(CQApi.CQCode_At(qqNumber).ToSendString());
                            QQgroup.SendGroupMessage(CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id), " 以下成员已经加入：\r\n",sb.ToString());
                           
                            break;
                        case 1://只存在需要更新的成员，目前也不可能进入了
                            QQgroup.SendGroupMessage(CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id), " 成员已经存在，更新了当前成员的信息。");
                            break;
                        case 2://存在需要更新和/或需要添加的成员
                            StringBuilder sb2 = new StringBuilder();
                            //筛选出所有返回值为1的操作，也即更新了的成员
                            foreach (long qqNumber in addedQQList.Where(x => x.Value == 1).ToDictionary(x=>x.Key,x=>x.Value).Keys)
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

                            QQgroup.SendGroupMessage(CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id), " 有成员已经存在，以下成员已经更新：\r\n", sb2.ToString(), sb3.ToString()!=""?("\r\n以下成员已添加\r\n"+sb3.ToString()):""/*只有存在新添加成员的情况下才需要显示这一句*/);
                            break;
                    }

                    break;
                case PCRGuildCmdType.ListMember://查看成员
                    dbAction.ShowMembers();
                    break;
                //参数1 QQ号
                case PCRGuildCmdType.QuitGuild://退会
                    Dictionary<long, int> deletedQQList = new Dictionary<long, int>();    //已经入会的QQ号列表
                    if (Utils.CheckForLength(commandArgs, 1) == LenType.Extra)
                    {
                        List<long> atQQs = Utils.GetAtList(GMgrEventArgs.Message.CQCodes, out int status);
                        result = status;
                        if (atQQs.Count == 0)//没有AT任何人，参数非法
                        {
                            PCRGuildHandle.GetIllegalArgs(GMgrEventArgs, PCRGuildCmdType.QuitGuild, "没有AT任何人");
                            return;
                        }
                        if (atQQs.Count >= 1)           //如果存在AT
                        {
                            foreach (long qqid in atQQs)  //检查每一个AT
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
                            {//否则就是有些QQ号并不存在
                                result = 2;
                            }
                        }
                        else //自分の退会
                        {
                            result = dbAction.LeaveGuild(GMgrEventArgs.FromQQ, QQgroup.Id);
                        }
                    }

                    switch (result)
                    {
                        case -2://不可能进入，但防御性编程，需要处理
                            QQgroup.SendGroupMessage(CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id), " 未定义行为，请检查代码。");
                            break;
                        case -1://一般情况下不可能非法，但也要处理
                            QQgroup.SendGroupMessage(CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id), " QQ号输入非法。");
                            break;
                        case 0://QQ号全部成功退会
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
                                QQgroup.SendGroupMessage(CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id), " 以下成员已经退会：\r\n", sb.ToString());
                            }
                       

                            break;
                        case 1://自己退会但并不在公会中
                            QQgroup.SendGroupMessage(CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id), " 你并不在公会中，无法退会");
                            break;
                        case 2://存在不在公会里的成员
                            StringBuilder sb2 = new StringBuilder();
                            //筛选出所有返回值为0的操作，也即成功退会的
                            foreach (long qqNumber in deletedQQList.Where(x => x.Value == 0).ToDictionary(x => x.Key, x => x.Value).Keys)
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

                            QQgroup.SendGroupMessage(CQApi.CQCode_At(GMgrEventArgs.FromQQ.Id), " 部分成员并未在公会中，以下成员已经退会：\r\n", sb2.ToString(), sb3.ToString() != "" ? ("\r\n以下成员并未在公会中\r\n" + sb3.ToString()) : "");
                            break;
                    }
                    break;
                case PCRGuildCmdType.QuitAll://清空成员
                    dbAction.EmptyMember();
                    break;
                case PCRGuildCmdType.JoinAll://一键入会
                    dbAction.AllJoin();
                    break;

                default://不可能发生，防御性处理
                    PCRGuildHandle.GetUnknowCommand(GMgrEventArgs);
                    break;
            }
        }
    }
}