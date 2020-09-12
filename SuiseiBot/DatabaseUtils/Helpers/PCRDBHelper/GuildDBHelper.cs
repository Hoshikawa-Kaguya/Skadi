using System;
using Native.Sdk.Cqp.EventArgs;
using SqlSugar;
using SuiseiBot.Code.SqliteTool;
using SuiseiBot.Code.Tool.LogUtils;

namespace SuiseiBot.Code.DatabaseUtils.Helpers.PCRDBHelper
{
    internal class GuildDBHelper
    {
        #region 属性
        protected CQGroupMessageEventArgs GuildEventArgs { set; get; }
        protected string                  DBPath         { get; set; }
        #endregion

        #region 通用查询函数
        /// <summary>
        /// 检查公会是否存在
        /// </summary>
        public bool GuildExists()
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                return dbClient.Queryable<GuildInfo>().Where(guild => guild.Gid == GuildEventArgs.FromGroup.Id).Any();
            }
            catch (Exception e)
            {
                ConsoleLog.Error("Database error",ConsoleLog.ErrorLogBuilder(e));
                return false;
            }
        }

        public string GetGuildName(long groupid)
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                var                  data     = dbClient.Queryable<GuildInfo>().Where(i => i.Gid == groupid);
                if (data.Any())
                {
                    return data.First().GuildName;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                ConsoleLog.Error("Database error",ConsoleLog.ErrorLogBuilder(e));
                return null;
            }
        }

        public int GetMemberCount(long gid)
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                return dbClient.Queryable<MemberInfo>().Where(guild => guild.Gid == gid).Count();
            }
            catch (Exception e)
            {
                ConsoleLog.Error("Database error",ConsoleLog.ErrorLogBuilder(e));
                return -1;
            }
        }

        /// <summary>
        /// 检查公会是否有这个成员
        /// </summary>
        /// <param name="uid">QQ号</param>
        public bool CheckMemberExists(long uid)
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                return dbClient.Queryable<MemberInfo>()
                               .Where(i => i.Uid == uid && i.Gid == GuildEventArgs.FromGroup.Id)
                               .Any();
            }
            catch (Exception e)
            {
                ConsoleLog.Error("Database error",ConsoleLog.ErrorLogBuilder(e));
                return false;
            }
        }
        /// <summary>
        /// 获取成员信息
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public MemberInfo GetMemberInfo(long uid)
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                return dbClient.Queryable<MemberInfo>()
                               .Where(i => i.Uid == uid && i.Gid == GuildEventArgs.FromGroup.Id)
                               .First();
            }
            catch (Exception e)
            {
                ConsoleLog.Error("Database error",ConsoleLog.ErrorLogBuilder(e));
                return null;
            }
        }

        public GuildInfo GetGuildInfo(long gid)
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                return dbClient.Queryable<GuildInfo>()
                               .InSingle(GuildEventArgs.FromGroup.Id); //单主键查询
            }
            catch (Exception e)
            {
                ConsoleLog.Error("Database error",ConsoleLog.ErrorLogBuilder(e));
                return null;
            }
        }
        #endregion
    }
}
