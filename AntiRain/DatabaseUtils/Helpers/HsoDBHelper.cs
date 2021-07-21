using System;
using System.Collections.Generic;
using AntiRain.DatabaseUtils.SqliteTool;
using SqlSugar;
using YukariToolBox.FormatLog;
using static AntiRain.DatabaseUtils.Tables;

namespace AntiRain.DatabaseUtils.Helpers
{
    internal class HsoDBHelper
    {
        #region 属性

        private readonly string DBPath; //数据库路径

        #endregion

        #region 构造函数

        public HsoDBHelper(long uid)
        {
            DBPath = SugarUtils.GetDBPath(uid.ToString());
        }

        #endregion

        #region 数据更新

        /// <summary>
        /// 又有新的lsp来了
        /// </summary>
        /// <param name="userId">uid</param>
        /// <param name="groupId">gid</param>
        public bool AddOrUpdate(long userId, long groupId)
        {
            try
            {
                using var dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                var countData = dbClient.Queryable<HsoCount>()
                                        .First(member => member.Gid == groupId &&
                                                         member.Uid == userId);
                //查找是否存在
                if (countData != null)
                {
                    return dbClient.Updateable<HsoCount>(newCount => newCount.Count == countData.Count + 1)
                                   .Where(member => member.Gid == groupId &&
                                                    member.Uid == userId)
                                   .ExecuteCommandHasChange();
                }

                //没有记录则插入新纪录
                return
                    dbClient.Insertable(new HsoCount()
                    {
                        Gid   = groupId,
                        Uid   = userId,
                        Count = 1
                    }).ExecuteCommand() > 0;
            }
            catch (Exception e)
            {
                Log.Error("Database Error", $"update lsp record error\r\n{Log.ErrorLogBuilder(e)}");
                return false;
            }
        }

        /// <summary>
        /// 获取群色批榜
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="rankList">色批榜</param>
        public bool GetGroupRank(long groupId, out List<HsoCount> rankList)
        {
            try
            {
                using var dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                rankList = dbClient.Queryable<HsoCount>()
                                   .Where(group => group.Gid == groupId)
                                   .OrderBy(c => c.Count, OrderByType.Desc)
                                   .ToList();
                return true;
            }
            catch (Exception e)
            {
                Log.Error("Database Error", $"update lsp record error\r\n{Log.ErrorLogBuilder(e)}");
                rankList = null;
                return false;
            }
        }

        #endregion
    }
}