using System;
using System.Collections.Generic;
using Native.Sdk.Cqp.EventArgs;
using SqlSugar;
using SuiseiBot.Code.Resource.TypeEnum;
using SuiseiBot.Code.SqliteTool;
using SuiseiBot.Code.Tool.LogUtils;

namespace SuiseiBot.Code.DatabaseUtils.Helpers
{
    internal class GuildManagerDBHelper
    {
        //TODO 创建公会时同时写入会战相关表格
        #region 参数
        public         CQGroupMessageEventArgs EventArgs { private set; get; }
        public         object                  Sender    { private set; get; }
        private static string                  DBPath; //数据库路径

        #endregion

        #region 构造函数

        /// <summary>
        /// 在接受到群消息时使用
        /// </summary>
        /// <param name="sender">sender object</param>
        /// <param name="eventArgs">CQAppEnableEventArgs类</param>
        public GuildManagerDBHelper(object sender, CQGroupMessageEventArgs eventArgs)
        {
            this.Sender    = sender;
            this.EventArgs = eventArgs;
            DBPath         = SugarUtils.GetDBPath(eventArgs.CQApi.GetLoginQQ().Id.ToString());
        }

        #endregion

        #region PCR数据表的定义

        //公会表
        public readonly static string[] GColName =
        {
            //字段名
            "gid",   //公会所在的QQ群号
            "name",  //公会名
            "server" //公会所在区服
        };

        public readonly static string[] GColType =
        {
            //字段类型
            "INTEGER NOT NULL",
            "VARCHAR NOT NULL",
            "VARCHAR NOT NULL"
        };

        public readonly static string[] GPrimaryColName =
        {
            //主键名
            "gid" //公会所在的QQ群号
        };

        //成员表
        public readonly static string[] MColName =
        {
            //字段名
            "uid",  //成员的QQ号
            "gid",  //公会所在的QQ群号
            "name", //成员昵称
        };

        public readonly static string[] MColType =
        {
            //字段类型
            "INTEGER NOT NULL",
            "INTEGER NOT NULL",
            "VARCHAR NOT NULL"
        };

        public readonly static string[] MPrimaryColName =
        {
            //主键名
            "uid", //成员的QQ号
            "gid"  //公会所在的QQ群号
        };

        #endregion

        #region 查询函数

        public string GetGuildName(long groupid)
        {
            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
            var                  data     = dbClient.Queryable<GuildData>().Where(i => i.Gid == groupid);
            if (data.Any())
            {
                return data.First().GuildName;
            }
            else
            {
                return "公会不存在";
            }
        }

        #endregion

        #region 指令响应函数

        /// <summary>
        /// 移除所有成员
        /// </summary>
        /// <param name="groupid">公会所在群号</param>
        /// <returns>状态值
        /// 0：正常移除
        /// 1：公会不存在
        /// -1：删除时发生错误
        /// </returns>
        public int EmptyMember(long groupid)
        {
            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
            var                  data     = dbClient.Queryable<MemberData>().Where(i => i.Gid == groupid);
            if (data.Any())
            {
                if (dbClient.Deleteable<MemberData>().Where(i => i.Gid == groupid).ExecuteCommandHasChange())
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
        public int LeaveGuild(long qqid, long groupid)
        {
            int                  retCode  = -1;
            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
            if (dbClient.Queryable<MemberData>().Where(i => i.Uid == qqid && i.Gid == groupid).Any())
            {
                retCode = dbClient.Deleteable<MemberData>().Where(i => i.Uid == qqid && i.Gid == groupid)
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

        public List<MemberData> ShowMembers(long groupid)
        {
            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
            return dbClient.Queryable<MemberData>().Where(i => i.Gid == groupid).ToList();
        }

        /// <summary>
        /// 添加一名成员
        /// </summary>
        /// <param name="qqid">成员QQ号</param>
        /// <param name="groupid">成员所在群号</param>
        /// <param name="nickName">成员昵称</param>
        /// <returns>状态值
        /// 0：正常添加
        /// 1：该成员已存在，更新信息
        /// -1：数据库出错
        /// </returns>
        public int JoinToGuild(long qqid, long groupid, string nickName)
        {
            try
            {
                int                  retCode  = -1;
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                var data = new MemberData()
                {
                    NickName = nickName,
                    Uid      = qqid,
                    Gid      = groupid
                };
                if (dbClient.Queryable<MemberData>().Where(i => i.Uid == qqid && i.Gid == groupid).Any())
                {
                    retCode = dbClient.Updateable(data)
                                      .Where(i => i.Uid == qqid && i.Gid == groupid)
                                      .ExecuteCommandHasChange()
                        ? 1
                        : -1;
                }
                else
                {
                    retCode = dbClient.Insertable(data).ExecuteCommand() > 0 ? 0 : -1;
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
                int                  retCode  = -1;
                long                 initHP   = GetInitBossHP(gArea);
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                var data = new GuildData()
                {
                    GuildName  = gName,
                    ServerArea = gArea,
                    Gid        = gId
                };
                //更新信息时不需要更新公会战信息
                if (dbClient.Queryable<GuildData>().Where(i => i.Gid == gId).Any())
                {
                    retCode = dbClient.Updateable(data)
                                      .Where(i => i.Gid == gId)
                                      .ExecuteCommandHasChange()
                        ? 1
                        : -1;
                }
                else
                {
                    var statusData = new GuildBattleStatus
                    {
                        BossPhase = 1,
                        Gid       = gId,
                        HP        = initHP,
                        InBattle  = false,
                        Order     = 1,
                        Round     = 1,
                        TotalHP   = initHP
                    };
                    retCode = dbClient.Insertable(statusData).ExecuteCommand() > 0 ? 0 : -1;
                    retCode = dbClient.Insertable(data).ExecuteCommand()       > 0 ? 0 : -1;
                }

                return retCode;
            }
            catch (Exception e)
            {
                ConsoleLog.Error("Database",ConsoleLog.ErrorLogBuilder(e));
                return -1;
            }
        }

        //TODO 解散公会
        #endregion

        #region 私有方法
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