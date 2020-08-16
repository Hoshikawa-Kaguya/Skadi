using System.Collections.Generic;
using System.Linq;
using System.Text;
using com.cbgan.SuiseiBot.Code.ChatHandlers;
using com.cbgan.SuiseiBot.Code.Database;
using com.cbgan.SuiseiBot.Code.Resource;
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
        public static void GuildMgrResponse(object Sender,CQGroupMessageEventArgs GMgrEventArgs,PCRGuildCommandType commandType) //功能响应
        {
            Group QQgroup = GMgrEventArgs.FromGroup;

            //index=0为命令本身，其余为参数
            string[] commandArgs = GMgrEventArgs.Message.Text.Split(' ');

            GuildManagerDBHelper dbAction = new GuildManagerDBHelper(Sender, GMgrEventArgs);

            int result = -2;

            switch (commandType)
            {
                //参数1 服务器地区，参数2 公会名（可选，缺省为群名）
                case PCRGuildCommandType.CreateGuild://建会
                    if (Utils.CheckForLength(commandArgs, 1)) 
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
                case PCRGuildCommandType.JoinGuild://入会
                    Dictionary<long,int> addedQQList= new Dictionary<long, int>();    //已经入会的QQ号列表
                    if (Utils.CheckForLength(commandArgs, 1))
                    {
                        if (GMgrEventArgs.Message.CQCodes.Count == 0)//没有AT任何人，参数非法
                        {
                            PCRGuildHandle.GetIllegalArgs(GMgrEventArgs, PCRGuildCommandType.JoinGuild, "没有AT任何人");
                            return;
                        }
                        if (GMgrEventArgs.Message.CQCodes.Count >= 1)           //如果存在AT
                        {
                            foreach (CQCode code in GMgrEventArgs.Message.CQCodes)  //检查每一个AT
                            {
                                if (code.Function.Equals(CQFunction.At) &&
                                    code.Items.ContainsKey("qq") &&
                                    long.TryParse(code.Items["qq"], out long qqid) &&
                                    qqid > QQ.MinValue)
                                {
                                    //需要添加为成员的QQ号列表和对应操作的返回值
                                    addedQQList.Add(qqid, dbAction.JoinToGuild(qqid, GMgrEventArgs.CQApi.GetGroupMemberInfo(GMgrEventArgs.FromGroup, qqid).Nick));
                                }
                                else
                                {
                                    //有操作的QQ号非法
                                    result = -1;
                                }
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
                            result = dbAction.JoinToGuild(GMgrEventArgs.FromQQ, GMgrEventArgs.CQApi.GetGroupMemberInfo(GMgrEventArgs.FromGroup, GMgrEventArgs.FromQQ).Nick);
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
                case PCRGuildCommandType.ListMember://查看成员
                    dbAction.ShowMembers();
                    break;
                //参数1 QQ号
                case PCRGuildCommandType.QuitGuild://退会
                    if (Utils.CheckForLength(commandArgs, 1)) 
                        result = dbAction.LeaveGuild(commandArgs[1]);
                    break;
                case PCRGuildCommandType.QuitAll://清空成员
                    dbAction.EmptyMember();
                    break;
                case PCRGuildCommandType.JoinAll://一键入会
                    dbAction.AllJoin();
                    break;

                default://不可能发生，防御性处理
                    PCRGuildHandle.GetUnknowCommand(GMgrEventArgs);
                    break;
            }
        }
    }
}