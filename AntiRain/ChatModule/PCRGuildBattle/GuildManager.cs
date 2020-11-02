using System;
using AntiRain.DatabaseUtils.Helpers.PCRGuildBattleDB;
using AntiRain.Resource.TypeEnum;
using AntiRain.Resource.TypeEnum.CommandType;
using AntiRain.Tool;
using Sora.Entities.CQCodes;
using Sora.Enumeration.ApiEnum;
using Sora.EventArgs.SoraEvent;
using Sora.Tool;

namespace AntiRain.ChatModule.PcrGuildBattle
{
    internal class GuildManager : BaseManager
    {
        #region 属性
        /// <summary>
        /// 数据库实例
        /// </summary>
        private GuildManagerDBHelper DBHelper { get; set; }
        #endregion

        #region 构造函数
        internal GuildManager(GroupMessageEventArgs messageEventArgs, PCRGuildBattleCommand commandType) : base(messageEventArgs, commandType)
        {
            this.DBHelper = new GuildManagerDBHelper(messageEventArgs);
        }
        #endregion

        #region 指令分发
        /// <summary>
        /// 公会管理指令响应函数
        /// </summary>
        public void GuildManagerResponse() //功能响应
        {
            if (base.MessageEventArgs == null) throw new ArgumentNullException(nameof(base.MessageEventArgs));
            switch (CommandType)
            {
                case PCRGuildBattleCommand.CreateGuild:
                    CreateGuild();
                    break;
                case PCRGuildBattleCommand.DeleteGuild:
                    break;
            }
        }
        #endregion

        #region 指令
        /// <summary>
        /// 创建公会
        /// </summary>
        private async void CreateGuild()
        {
            //检查群是否已经被标记为公会
            switch (this.DBHelper.GuildExists())
            {
                case 1:
                    await base.SourceGroup.SendGroupMessage(CQCode.CQAt(base.Sender),
                                                            $"此群已被标记为[{DBHelper.GetGuildInfo(base.SourceGroup).GuildName}]公会");
                    return;
            }
            //公会名
            string guildName;
            //公会区服
            Server guildServer;
            //判断公会名
            switch (BotUtils.CheckForLength(this.CommandArgs,1))
            {
                case LenType.Extra:
                    if (BotUtils.CheckForLength(this.CommandArgs, 2) == LenType.Legitimate)
                    {
                        guildName = CommandArgs[2];
                    }
                    else
                    {
                        await base.SourceGroup.SendGroupMessage(CQCode.CQAt(base.Sender), "\r\n有多余参数");
                        return;
                    }
                    break;
                case LenType.Legitimate:
                    var groupInfo = await base.SourceGroup.GetGroupInfo();
                    if (groupInfo.apiStatus != APIStatusType.OK)
                    {
                        ConsoleLog.Error("API error",$"api ret code {(int) groupInfo.apiStatus}");
                        await base.SourceGroup.SendGroupMessage(CQCode.CQAt(base.Sender), "\r\nAPI调用错误请重试");
                        return;
                    }
                    guildName = groupInfo.groupInfo.GroupName;
                    break;
                default:
                    return;
            }
            //判断区服
            if (Enum.IsDefined(typeof(Server), CommandArgs[1]))
            {
                guildServer = (Server) Enum.Parse(typeof(Server), CommandArgs[1]);
                if (guildServer != Server.CN)
                {
                    await base.SourceGroup.SendGroupMessage("暂不支持国服以外的服务器");
                    return;
                }
            }
            else
            {
                await base.SourceGroup.SendGroupMessage(CQCode.CQAt(base.Sender),
                                               "弟啊，你哪个服务器的");
                return;
            }

            switch (this.DBHelper.CreateGuild(guildServer, guildName, base.SourceGroup))
            {
                case -1:
                    await base.SourceGroup.SendGroupMessage("错误:数据库错误");
                    break;
                case 0:
                    await base.SourceGroup.SendGroupMessage($"公会[{guildName}]已创建");
                    break;
                default:
                    await base.SourceGroup.SendGroupMessage("发生了未知错误");
                    break;
            }
        }
        #endregion

        #region 私有方法
        #endregion
    }
}
