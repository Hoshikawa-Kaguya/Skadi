using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using com.cbgan.SuiseiBot.Code.database;
using Native.Sdk.Cqp;
using Native.Sdk.Cqp.Enum;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Model;

namespace com.cbgan.SuiseiBot.Code.handlers
{
    internal class PCRHandler
    {
        #region 属性

        public object                  sender     { private set; get; }
        public CQGroupMessageEventArgs eventArgs  { private set; get; }
        public string                  pcrCommand { private get; set; }
        public Group                   QQgroup    { private get; set; }

        #endregion

        #region 构造函数

        public PCRHandler(object sender, CQGroupMessageEventArgs e)
        {
            this.eventArgs = e;
            this.sender    = sender;
        }

        #endregion

        public void GetChat() //消息接收并判断是否响应
        {
            if (eventArgs == null || sender == null) return;
            try
            {
                //获取第二个字符开始到空格为止的PCR命令
                pcrCommand = eventArgs.Message.Text.Substring(1).Split(' ')[0];

                //命令为空
                if (pcrCommand == "") return;
            }
            catch
            {
                //命令无法被正确解析
                return;
            }

            Group_Response();
        }

        private void Group_Response() //功能响应
        {
            QQgroup = eventArgs.FromGroup;

            //index=0为命令本身，其余为参数
            string[] commandArgs = eventArgs.Message.Text.Split(' ');

            PCRDBHelper dbAction = new PCRDBHelper(sender, eventArgs);

            int result = -2;

            switch (pcrCommand)
            {
                //参数1 服务器地区，参数2 工会名（可选，缺省为群名）
                case "建会":
                    if (checkForLength(commandArgs, 1))
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
                            QQgroup.SendGroupMessage(CQApi.CQCode_At(eventArgs.FromQQ.Id), " 公会已经创建。");
                            break;
                        case 1:
                            QQgroup.SendGroupMessage(CQApi.CQCode_At(eventArgs.FromQQ.Id), " 公会已经存在，更新了当前公会的信息。");
                            break;
                    }

                    break;
                //参数1 QQ号
                case "入会":
                    Dictionary<long,int> addedQQList= new Dictionary<long, int>();    //已经入会的QQ号列表
                    if (checkForLength(commandArgs, 1))         
                        if (eventArgs.Message.CQCodes.Count >= 1)           //如果存在AT
                        {
                            foreach (CQCode code in eventArgs.Message.CQCodes)  //检查每一个AT
                            {
                                if (code.Function.Equals(CQFunction.At)            &&
                                    code.Items.ContainsKey("qq")                   &&
                                    long.TryParse(code.Items["qq"], out long qqid) &&
                                    qqid > QQ.MinValue)
                                {
                                    //需要添加为成员的QQ号列表和对应操作的返回值
                                    addedQQList.Add(qqid, dbAction.joinGuild(qqid, eventArgs.CQApi.GetGroupMemberInfo(eventArgs.FromGroup,qqid).Nick));
                                }
                                else
                                {
                                    //有操作的QQ号非法
                                    result = -1;
                                }
                            }
                            //如果只存在需要添加的成员，而没有需要更新的成员
                            if (addedQQList.Count>0 && addedQQList.Where(x=> x.Value==1).ToList().Count==0)
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
                            result = dbAction.joinGuild(eventArgs.FromQQ, eventArgs.CQApi.GetGroupMemberInfo(eventArgs.FromGroup, eventArgs.FromQQ).Nick);
                        }

                    switch (result)
                    {
                        case -2://不可能进入，但防御性编程，需要处理
                            QQgroup.SendGroupMessage(CQApi.CQCode_At(eventArgs.FromQQ.Id), " 未定义行为，请检查代码。");
                            break;
                        case -1://一般情况下不可能非法，但也要处理
                            QQgroup.SendGroupMessage(CQApi.CQCode_At(eventArgs.FromQQ.Id), " QQ号输入非法。");
                            break;
                        case 0://只存在新添加的成员
                          
                            StringBuilder sb=new StringBuilder();
                            //at所有新添加的成员
                            foreach (long qqNumber in addedQQList.Keys)
                                sb.Append(CQApi.CQCode_At(qqNumber).ToSendString());
                            QQgroup.SendGroupMessage(CQApi.CQCode_At(eventArgs.FromQQ.Id), " 以下成员已经加入：\r\n",sb.ToString());
                           
                            break;
                        case 1://只存在需要更新的成员，目前也不可能进入了
                            QQgroup.SendGroupMessage(CQApi.CQCode_At(eventArgs.FromQQ.Id), " 成员已经存在，更新了当前成员的信息。");
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

                            QQgroup.SendGroupMessage(CQApi.CQCode_At(eventArgs.FromQQ.Id), " 有成员已经存在，以下成员已经更新：\r\n", sb2.ToString(), sb3.ToString()!=""?("\r\n以下成员已添加\r\n"+sb3.ToString()):""/*只有存在新添加成员的情况下才需要显示这一句*/);
                            break;
                    }

                    break;
                case "查看成员":
                    dbAction.showMembers();
                    break;
                //参数1 QQ号
                case "退会":
                    if (checkForLength(commandArgs, 1))
                        result = dbAction.leaveGuild(commandArgs[1]);
                    break;
                case "清空成员":
                    dbAction.emptyMember();
                    break;
                case "一键入会":
                    dbAction.allJoin();
                    break;
            }
        }


        private Boolean checkForLength(string[] args, int len)
        {
            if (args.Length < (len + 1))
            {
                QQgroup.SendGroupMessage(CQApi.CQCode_At(eventArgs.FromQQ.Id), " 请输入正确的参数个数。");
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}