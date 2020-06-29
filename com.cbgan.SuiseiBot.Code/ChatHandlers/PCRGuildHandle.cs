using com.cbgan.SuiseiBot.Code.PCRGuildManager;
using com.cbgan.SuiseiBot.Code.Resource;
using com.cbgan.SuiseiBot.Code.Tool;
using Native.Sdk.Cqp;
using Native.Sdk.Cqp.EventArgs;
using System;

namespace com.cbgan.SuiseiBot.Code.ChatHandlers
{
    internal class PCRGuildHandle
    {
        #region 属性
        public object Sender { private set; get; }
        public CQGroupMessageEventArgs PCRGuildEventArgs { private set; get; }
        public string PCRGuildCommand { private get; set; }
        private PCRGuildCommandType CommandType { get; set; }
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
                GuildCommand.GuildCommands.TryGetValue(PCRGuildCommand, out PCRGuildCommandType commandType);
                this.CommandType = commandType;
                //未知指令
                if (commandType == 0)
                {
                    GetUnknowCommand(PCRGuildEventArgs);
                    ConsoleLog.Info("PCR公会管理", commandType == 0 ? "解析到未知指令" : $"解析指令{CommandType}");
                    return;
                }
                //公会管理指令
                if (commandType > 0 && (int)commandType < 100)
                {
                    PCRHandler.GuildMgrResponse(Sender, PCRGuildEventArgs, CommandType);
                }
                //出刀管理指令
                else if ((int)commandType > 100 && (int)commandType < 200)
                {
                    throw new NotImplementedException();
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
        /// <param name="e"></param>
        public static void GetIllegalArgs(CQGroupMessageEventArgs e, PCRGuildCommandType commandType)
        {
            ConsoleLog.Warning("PCR公会管理", "非法参数");
            string helpString = " ";//GetCommandHelp();
            e.FromGroup.SendGroupMessage(
                CQApi.CQCode_At(e.FromQQ.Id), 
                "\n非法参数请重新输入指令" +
                "\n指令帮助：" +
                $"\n{helpString}");
        }
        #endregion

        #region 辅助函数
        /// <summary>
        /// 检查指令长度
        /// </summary>
        /// <param name="args">指令数组</param>
        /// <param name="len">目标长度</param>
        /// <param name="e">CQGroupMessageEventArgs</param>
        /// <returns>长度合法性</returns>
        public static bool CheckForLength(string[] args, int len, CQGroupMessageEventArgs e)
        {
            if (args.Length < (len + 1))
            {
                e.FromGroup.SendGroupMessage(CQApi.CQCode_At(e.FromQQ.Id), "\n请输入正确的参数个数。");
                return false;
            }
            else
            {
                return true;
            }
        }


        /// <summary>
        /// 获取对应指令的帮助文本
        /// </summary>
        /// <returns>帮助文本</returns>
        public static string GetCommandHelp(PCRGuildCommandType commandType)
        {
            //TODO 帮助文本库
            throw new NotImplementedException();
        }
        #endregion
    }
}
