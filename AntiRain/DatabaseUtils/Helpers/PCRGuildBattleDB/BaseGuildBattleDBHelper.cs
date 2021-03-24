using System;
using System.Collections.Generic;
using AntiRain.DatabaseUtils.SqliteTool;
using SqlSugar;
using YukariToolBox.FormatLog;
using YukariToolBox.Time;

namespace AntiRain.DatabaseUtils.Helpers.PCRGuildBattleDB
{
    /// <summary>
    /// 公会数据库基类
    /// </summary>
    internal abstract class BaseGuildBattleDBHelper
    {
        #region 属性

        protected string DBPath { get; set; }

        #endregion

        #region 构造函数

        /// <summary>
        /// 基类构造函数
        /// </summary>
        /// <param name="loginUid">登录账号ID</param>
        protected BaseGuildBattleDBHelper(long loginUid)
        {
            DBPath = SugarUtils.GetDBPath(loginUid.ToString());
        }

        #endregion

        #region 通用查询函数

        /// <summary>
        /// 检查公会是否存在
        /// </summary>
        /// <returns>
        /// <para><see langword="1"/> 公会存在</para>
        /// <para><see langword="0"/> 公会不存在</para>
        /// <para><see langword="-1"/> 数据库错误</para>
        /// </returns>
        public int GuildExists(long groupId)
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                return dbClient.Queryable<GuildInfo>().Where(guild => guild.Gid == groupId).Any()
                    ? 1
                    : 0;
            }
            catch (Exception e)
            {
                Log.Error("Database error", Log.ErrorLogBuilder(e));
                return -1;
            }
        }

        /// <summary>
        /// 获取公会名
        /// </summary>
        /// <param name="groupid"></param>
        /// <returns>
        /// <para>公会名</para>
        /// <para><see langword="空字符串"/> 公会不存在</para>
        /// <para><see langword="null"/> 数据库错误</para>
        /// </returns>
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
                    return string.Empty;
                }
            }
            catch (Exception e)
            {
                Log.Error("Database error", Log.ErrorLogBuilder(e));
                return null;
            }
        }

        /// <summary>
        /// 获取公会成员数
        /// </summary>
        /// <param name="gid">公会群号</param>
        /// <returns>
        /// <para>成员数</para>
        /// <para><see langword="-1"/> 数据库错误</para>
        /// </returns>
        public int GetMemberCount(long gid)
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                return dbClient.Queryable<MemberInfo>().Where(guild => guild.Gid == gid).Count();
            }
            catch (Exception e)
            {
                Log.Error("Database error", Log.ErrorLogBuilder(e));
                return -1;
            }
        }

        /// <summary>
        /// 检查公会是否有这个成员
        /// </summary>
        /// <param name="uid">QQ号</param>
        /// <param name="groupId">公会群号</param>
        /// <returns>
        /// <para><see langword="1"/> 存在</para>
        /// <para><see langword="0"/> 不存在</para>
        /// <para><see langword="-1"/> 数据库错误</para>
        /// </returns>
        public int CheckMemberExists(long uid, long groupId)
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                return dbClient.Queryable<MemberInfo>()
                               .Where(i => i.Uid == uid && i.Gid == groupId)
                               .Any()
                    ? 1
                    : 0;
            }
            catch (Exception e)
            {
                Log.Error("Database error", Log.ErrorLogBuilder(e));
                return -1;
            }
        }

        /// <summary>
        /// 获取成员信息
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="groupId"></param>
        /// <returns>
        /// <para>成员信息</para>
        /// <para><see langword="null"/> 数据库错误</para>
        /// </returns>
        public MemberInfo GetMemberInfo(long uid, long groupId)
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                return dbClient.Queryable<MemberInfo>()
                               .Where(i => i.Uid == uid && i.Gid == groupId)
                               .First();
            }
            catch (Exception e)
            {
                Log.Error("Database error", Log.ErrorLogBuilder(e));
                return null;
            }
        }

        /// <summary>
        /// 获取所有成员信息
        /// </summary>
        /// <param name="gid">群号</param>
        /// <returns>
        /// <para>成员信息</para>
        /// <para><see langword="null"/> 数据库错误</para>
        /// </returns>
        public List<MemberInfo> GetAllMembersInfo(long gid)
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                return dbClient.Queryable<MemberInfo>()
                               .Where(group => group.Gid == gid)
                               .OrderBy(member => member.Uid)
                               .ToList();
            }
            catch (Exception e)
            {
                Log.Error("Database error", Log.ErrorLogBuilder(e));
                return null;
            }
        }

        /// <summary>
        /// 获取公会信息
        /// </summary>
        /// <param name="gid"></param>
        /// <returns>
        /// <para>成员信息</para>
        /// <para><see langword="null"/> 数据库错误</para>
        /// </returns>
        public GuildInfo GetGuildInfo(long gid)
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                return dbClient.Queryable<GuildInfo>()
                               .InSingle(gid); //单主键查询
            }
            catch (Exception e)
            {
                Log.Error("Database error", Log.ErrorLogBuilder(e));
                return null;
            }
        }

        /// <summary>
        /// 获取状态的刷新时间
        /// </summary>
        /// <param name="uid">uid</param>
        /// <param name="groupId">群号</param>
        /// <param name="time">刷新时间</param>
        /// <returns>成功与失败</returns>
        public bool GetStatusUpdateTime(long uid, long groupId, out DateTime time)
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                time = dbClient.Queryable<MemberInfo>()
                               .Where(member => member.Uid == uid && member.Gid == groupId)
                               .First().Time.ToDateTime();
                return true;
            }
            catch (Exception e)
            {
                Log.Error("Database error", Log.ErrorLogBuilder(e));
                time = new DateTime(0);
                return false;
            }
        }

        #endregion
    }
}