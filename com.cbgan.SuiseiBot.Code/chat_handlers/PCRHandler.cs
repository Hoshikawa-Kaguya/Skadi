using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using com.cbgan.SuiseiBot.Code.chat_handlers;
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
                pcrCommand = eventArgs.Message.ToString().Substring(1).Split(' ')[0];

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
            string[] commandArgs = eventArgs.Message.ToString().Split(' ');

            PCRDBHelper dbAction = new PCRDBHelper(sender, eventArgs);

            int result = -2;

            switch (pcrCommand)
            {
                //参数1 服务器地区，参数2 工会名（可选，缺省为群名）
                case "建会":
                    if (checkForLength(commandArgs, 1))
                    {
                        if (commandArgs.Length == 2)
                        {
                            result = dbAction.createGuild(commandArgs[1], commandArgs[2]);
                        }
                        else if (commandArgs.Length == 1)
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
                //参数1 昵称
                case "入会":
                    List<CQCode> addedQQList=new List<CQCode>();    //已经入会的QQ号列表的AT值
                    if (checkForLength(commandArgs, 1))
                        if (eventArgs.Message.CQCodes.Count >= 1)
                        {
                            foreach (CQCode code in eventArgs.Message.CQCodes)
                            {
                                if (code.Function.Equals(CQFunction.At)            &&
                                    code.Items.ContainsKey("qq")                   &&
                                    long.TryParse(code.Items["qq"], out long qqid) &&
                                    qqid < QQ.MinValue)
                                {
                                    addedQQList.Add(CQApi.CQCode_At(qqid));
                                    result = dbAction.joinGuild(qqid, commandArgs[2]);
                                }
                                else
                                {
                                    result = -1;
                                }
                            }
                        }
                        else if (commandArgs.Length == 1)
                        {
                            result = dbAction.joinGuild(eventArgs.FromQQ, commandArgs[1]);
                        }

                    switch (result)
                    {
                        case -2:
                            QQgroup.SendGroupMessage(CQApi.CQCode_At(eventArgs.FromQQ.Id), " 未定义行为，请检查代码。");
                            break;
                        case -1:
                            QQgroup.SendGroupMessage(CQApi.CQCode_At(eventArgs.FromQQ.Id), " QQ号输入非法。");
                            break;
                        case 0:
                          
                            QQgroup.SendGroupMessage(CQApi.CQCode_At(eventArgs.FromQQ.Id), " 以下成员已经加入：\r\n",addedQQList.ToArray());
                            break;
                        case 1:
                            QQgroup.SendGroupMessage(CQApi.CQCode_At(eventArgs.FromQQ.Id), " 成员已经存在，更新了当前成员的信息。");
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

                default:
                    if (GuildBattleManagerHandle.TryParseCommand(pcrCommand))
                    {
                        GuildBattleManagerHandle guildBattle = new GuildBattleManagerHandle(sender, eventArgs);
                    }
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