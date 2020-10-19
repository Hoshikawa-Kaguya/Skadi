using System;
using Sora.EventArgs.SoraEvent;
using Sora.Tool;
using SuiseiBot.Resource.TypeEnum.CommandType;

namespace SuiseiBot.ChatModule.PCRGuildBattle
{
    internal class PcrGuildBattleChatHandle
    {
        #region 属性
        public  object                Sender            { private set; get; }
        public  GroupMessageEventArgs PCRGuildEventArgs { private set; get; }
        public  string                PCRGuildCommand   { private get; set; }
        private PCRGuildBattleCommand CommandType       { get;         set; }
        #endregion

        #region 构造函数
        public PcrGuildBattleChatHandle(object sender, GroupMessageEventArgs e, PCRGuildBattleCommand commandType)
        {
            this.PCRGuildEventArgs = e;
            this.Sender            = sender;
            this.CommandType       = commandType;
        }
        #endregion

        #region 消息解析函数
        public void GetChat() //消息接收并判断是否响应
        {
            try
            {
                //获取第二个字符开始到空格为止的PCR命令
                PCRGuildCommand  = PCRGuildEventArgs.Message.RawText.Substring(1).Split(' ')[0];
                //公会管理指令
                if (CommandType > 0 && (int)CommandType < 100)
                {
                    GuildManager.GuildMgrResponse(Sender, PCRGuildEventArgs, CommandType);
                }
                //出刀管理指令
                else if ((int)CommandType > 100 && (int)CommandType < 200)
                {
                    GuildBattleManager battleManager = new GuildBattleManager(PCRGuildEventArgs, CommandType);
                    battleManager.GuildBattleResponse();
                }
            }
            catch(Exception e)
            {
                //命令无法被正确解析
                ConsoleLog.Error("PCR公会管理", $"指令解析发生错误\n{e}");
            }
        }
        #endregion
    }
}
