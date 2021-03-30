using System;
using AntiRain.TypeEnum.CommandType;
using Sora.EventArgs.SoraEvent;
using YukariToolBox.FormatLog;

namespace AntiRain.ChatModule.PcrGuildBattle
{
    //TODO 扬了这玩意
    internal class PcrGuildBattleChatHandle
    {
        #region 属性

        public  object                Sender            { private set; get; }
        public  GroupMessageEventArgs PCRGuildEventArgs { private set; get; }
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
                //公会管理指令
                if (CommandType > 0 && (int) CommandType < 100)
                {
                    GuildManager guildManager = new(PCRGuildEventArgs, CommandType);
                    guildManager.GuildManagerResponse();
                }
                //出刀管理指令
                else if ((int) CommandType > 100 && (int) CommandType < 200)
                {
                    GuildBattleManager battleManager = new(PCRGuildEventArgs, CommandType);
                    battleManager.GuildBattleResponse();
                }
            }
            catch (Exception e)
            {
                //命令无法被正确解析
                Log.Error("PCR公会管理", $"指令解析发生错误\n{e}");
            }
        }

        #endregion
    }
}