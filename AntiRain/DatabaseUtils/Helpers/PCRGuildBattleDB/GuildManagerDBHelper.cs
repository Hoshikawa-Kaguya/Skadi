using System;
using AntiRain.DatabaseUtils.SqliteTool;
using AntiRain.DatabaseUtils.Tables;
using AntiRain.TypeEnum;
using AntiRain.TypeEnum.GuildBattleType;
using Sora.EventArgs.SoraEvent;
using Sora.Tool;
using SqlSugar;

namespace AntiRain.DatabaseUtils.Helpers.PCRGuildBattleDB
{
    internal class GuildManagerDBHelper : BaseGuildBattleDBHelper
    {
        #region 构造函数
        /// <summary>
        /// 在接受到群消息时使用
        /// </summary>
        /// <param name="guildEventArgs">CQAppEnableEventArgs类</param>
        public GuildManagerDBHelper(GroupMessageEventArgs guildEventArgs) : base(guildEventArgs)
        {
        }
        #endregion

        #region 指令
        /// <summary>
        /// 移除所有成员
        /// </summary>
        /// <param name="groupid">公会所在群号</param>
        /// <returns>状态值
        /// 0：正常移除
        /// 1：公会不存在
        /// -1：删除时发生错误
        /// </returns>
        public int QuitAll(long groupid)
        {
            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
            var                  data     = dbClient.Queryable<MemberInfo>().Where(i => i.Gid == groupid);
            if (data.Any())
            {
                if (dbClient.Deleteable<MemberInfo>().Where(i => i.Gid == groupid).ExecuteCommandHasChange())
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            else
            {
                return 1;
            }
        }

        /// <summary>
        /// 移除一名成员
        /// </summary>
        /// <param name="qqid">成员QQ号</param>
        /// <param name="groupid">成员所在群号</param>
        /// <returns>状态值
        /// 0：正常移除
        /// 1：该成员并不在公会内
        /// -1：数据库出错
        /// </returns>
        public int QuitGuild(long qqid, long groupid)
        {
            int                  retCode;
            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
            if (dbClient.Queryable<MemberInfo>().Where(i => i.Uid == qqid && i.Gid == groupid).Any())
            {
                retCode = dbClient.Deleteable<MemberInfo>().Where(i => i.Uid == qqid && i.Gid == groupid)
                                  .ExecuteCommandHasChange()
                    ? 0
                    : -1;
            }
            else
            {
                retCode = 1;
            }

            return retCode;
        }

        /// <summary>
        /// 添加一名成员
        /// </summary>
        /// <param name="uid">成员QQ号</param>
        /// <param name="groupid">成员所在群号</param>
        /// <param name="name">成员昵称</param>
        /// <returns>状态值
        /// <para><see langword="0"/> 正常添加</para>
        /// <para><see langword="1"/> 该成员已存在，更新信息</para>
        /// <para><see langword="-1"/> 数据库出错/API错误</para>
        /// </returns>
        public int JoinGuild(long uid, long groupid, string name)
        {
            try
            {
                //读取数据库
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                //已存在成员则更新
                if (dbClient.Queryable<MemberInfo>().Any(i => i.Uid == uid && i.Gid == groupid))
                {
                    var data = new MemberInfo()
                    {
                        Uid  = uid,
                        Gid  = groupid,
                        Name = name
                    };
                    return dbClient.Updateable(data)
                                   .Where(i => i.Uid == uid && i.Gid == groupid)
                                   .ExecuteCommandHasChange()
                        ? 1
                        : -1;
                }
                //加入成员并写入成员信息
                else
                {
                    var memberStatus = new MemberInfo
                    {
                        Flag = FlagType.IDLE,
                        Gid  = groupid,
                        Info = null,
                        SL   = 0,
                        Time = Utils.GetNowTimeStamp(),
                        Uid  = uid,
                        Name = name
                    };
                    //成员信息
                    return dbClient.Insertable(memberStatus).ExecuteCommand() > 0 ? 0 : -1;
                }
            }
            catch (Exception e)
            {
                ConsoleLog.Error("Database",ConsoleLog.ErrorLogBuilder(e));
                return -1;
            }
        }

        /// <summary>
        /// 初次创建公会
        /// </summary>
        /// <param name="gArea">公会所在区域</param>
        /// <param name="gName">公会名称</param>
        /// <param name="gId">公会所在群号</param>
        /// <returns>状态值
        /// 0：正常创建
        /// 1：该群公会已存在，更新信息
        /// -1:数据库出错
        /// </returns>
        public int CreateGuild(Server gArea, string gName, long gId)
        {
            try
            {
                int                  retCode;
                long                 initHP   = GetInitBossHP(gArea);
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                //更新信息时不需要更新公会战信息
                if (dbClient.Queryable<GuildInfo>().Where(i => i.Gid == gId).Any())
                {
                    var data = new GuildInfo()
                    {
                        GuildName = gName,
                        ServerId  = gArea,
                        Gid       = gId
                    };
                    retCode = dbClient.Updateable(data)
                                      .UpdateColumns(guildInfo =>
                                                         new {guildInfo.GuildName, guildInfo.ServerId, guildInfo.Gid})
                                      .Where(i => i.Gid == gId)
                                      .ExecuteCommandHasChange()
                        ? 1
                        : -1;
                }
                else
                {
                    //会战进度表
                    var bossStatusData = new GuildInfo
                    {
                        BossPhase = 1,
                        Gid       = gId,
                        HP        = initHP,
                        InBattle  = false,
                        Order     = 1,
                        Round     = 1,
                        TotalHP   = initHP,
                        GuildName = gName,
                        ServerId  = gArea
                    };
                    retCode = dbClient.Insertable(bossStatusData).ExecuteCommand() > 0 ? 0 : -1;
                }

                return retCode;
            }
            catch (Exception e)
            {
                ConsoleLog.Error("Database",ConsoleLog.ErrorLogBuilder(e));
                return -1;
            }
        }

        /// <summary>
        /// 删除公会
        /// </summary>
        /// <param name="gid">公会群的群号</param>
        /// <returns>数据库是否成功运行</returns>
        public bool DeleteGuild(long gid)
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                bool deletGuildInfo = dbClient.Deleteable<GuildInfo>().Where(guild => guild.Gid == gid)
                                              .ExecuteCommandHasChange();
                bool deletMemberInfo = true;
                if (dbClient.Queryable<MemberInfo>().Where(guild => guild.Gid == gid).Count() > 0)
                {
                    deletMemberInfo = dbClient.Deleteable<MemberInfo>().Where(member => member.Gid == gid)
                                              .ExecuteCommandHasChange();
                }
                
                return deletMemberInfo && deletGuildInfo;
            }
            catch (Exception e)
            {
                ConsoleLog.Error("Database",ConsoleLog.ErrorLogBuilder(e));
                return false;
            }
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 获取对应区服的boss初始化HP
        /// </summary>
        /// <param name="server">区服</param>
        private long GetInitBossHP(Server server)
        {
            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
            return dbClient.Queryable<GuildBattleBoss>()
                           .Where(i => i.ServerId == server
                                    && i.Phase    == 1
                                    && i.Order    == 1)
                           .Select(i=>i.HP)
                           .First();
        }
        #endregion
    }
}
