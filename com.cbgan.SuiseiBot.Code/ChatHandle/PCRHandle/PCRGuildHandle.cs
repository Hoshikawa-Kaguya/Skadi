using System;
using SuiseiBot.PCRGuildManager;
using SuiseiBot.CommandHelp;
using SuiseiBot.Commands;
using SuiseiBot.TypeEnum.CmdType;
using SuiseiBot.Tool.Log;
using Native.Sdk.Cqp;
using Native.Sdk.Cqp.EventArgs;

namespace SuiseiBot.PCRHandle
{
    internal class PCRGuildHandle
    {
        #region 属性
        public object Sender { private set; get; }
        public CQGroupMessageEventArgs PCRGuildEventArgs { private set; get; }
        public string PCRGuildCommand { private get; set; }
        private PCRGuildCmdType CommandType { get; set; }
        #endregion

        #region 构造函数
        public PCRGuildHandle(object sender, CQGroupMessageEventArgs e)
        {
            this.PCRGuildEventArgs = e;
            this.Sender = sender;
        }
        #endregion

        #region 消息解析函数
        public void GetChat() //消息接收并判断是否响应
        {
            try
            {
                //获取第二个字符开始到空格为止的PCR命令
                PCRGuildCommand = PCRGuildEventArgs.Message.Text.Substring(1).Split(' ')[0];
                //获取指令类型
                PCRGuildCmd.PCRGuildCommands.TryGetValue(PCRGuildCommand, out PCRGuildCmdType commandType);
                this.CommandType = commandType;
                //未知指令
                if (CommandType == 0)
                {
                    GetUnknowCommand(PCRGuildEventArgs);
                    ConsoleLog.Info("PCR公会管理", CommandType == 0 ? "解析到未知指令" : $"解析指令{CommandType}");
                    return;
                }
                //公会管理指令
                if (CommandType > 0 && (int)CommandType < 100)
                {
                    PCRHandler.GuildMgrResponse(Sender, PCRGuildEventArgs, CommandType);
                }
                //出刀管理指令
                else if ((int)CommandType > 100 && (int)CommandType < 200)
                {
                    GuildBattleManager.GuildBattleResponse(Sender,PCRGuildEventArgs, CommandType);
                }
                //Boss数据相关指令
                else if ((int)CommandType > 200 && (int)CommandType < 300)
                {
                    BossManager.BossResponse(Sender, PCRGuildEventArgs, CommandType);
                }
            }
            catch(Exception e)
            {
                //命令无法被正确解析
                ConsoleLog.Error("PCR公会管理", $"指令解析发生错误\n{e}");
                return;
            }
        }
        #endregion

        #region 非法指令响应
        /// <summary>
        /// 得到未知指令时的响应
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">CQGroupMessageEventArgs</param>
        public static void GetUnknowCommand(CQGroupMessageEventArgs e)
        {
            ConsoleLog.Warning("PCR公会管理", "未知指令");
            e.FromGroup.SendGroupMessage(CQApi.CQCode_At(e.FromQQ.Id), "\n未知指令");
        }

        /// <summary>
        /// 存在非法参数时的响应
        /// </summary>
        /// <param name="e">CQGroupMessageEventArgs</param>
        /// <param name="commandType">指令类型</param>
        /// <param name="errDescription">错误描述</param>
        public static void GetIllegalArgs(CQGroupMessageEventArgs e, PCRGuildCmdType commandType, string errDescription)
        {
            ConsoleLog.Warning("PCR公会管理", "非法参数");
            e.FromGroup.SendGroupMessage(
                                         CQApi.CQCode_At(e.FromQQ.Id),
                                         "\n非法参数请重新输入指令" +
                                         $"\n错误：{errDescription}" +
                                         $"\n指令帮助：{GetCommandHelp(commandType)}");
        }
        #endregion

        #region 辅助函数
        /// <summary>
        /// 获取对应指令的帮助文本
        /// </summary>
        /// <returns>帮助文本</returns>
        public static string GetCommandHelp(PCRGuildCmdType commandType)
        {
            GuildCommandHelp.HelpText.TryGetValue(commandType, out string helptext);
            if (string.IsNullOrEmpty(helptext)) helptext = "该指令还在开发中，请询问机器人维护者或者开发者";
            return helptext;
        }
        #endregion
    }
}
