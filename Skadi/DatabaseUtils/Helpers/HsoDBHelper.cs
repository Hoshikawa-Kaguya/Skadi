using System;
using System.Collections.Generic;
using Skadi.DatabaseUtils.SqliteTool;
using SqlSugar;
using YukariToolBox.LightLog;

namespace Skadi.DatabaseUtils.Helpers
{
    internal class HsoDbHelper
    {
        #region 属性

        private readonly string _dbPath; //数据库路径

        #endregion

        #region 构造函数

        public HsoDbHelper(long uid)
        {
            _dbPath = SugarUtils.GetDbPath(uid.ToString());
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
                using var dbClient = SugarUtils.CreateSqlSugarClient(_dbPath);
                var countData = dbClient.Queryable<Tables.HsoCount>()
                                        .First(member => member.Gid == groupId &&
                                                         member.Uid == userId);
                //查找是否存在
                if (countData != null)
                {
                    return dbClient.Updateable<Tables.HsoCount>(newCount => newCount.Count == countData.Count + 1)
                                   .Where(member => member.Gid == groupId &&
                                                    member.Uid == userId)
                                   .ExecuteCommandHasChange();
                }

                //没有记录则插入新纪录
                return
                    dbClient.Insertable(new Tables.HsoCount()
                    {
                        Gid   = groupId,
                        Uid   = userId,
                        Count = 1
                    }).ExecuteCommand() > 0;
            }
            catch (Exception e)
            {
                Log.Error(e, "HsoDatabase", "update lsp record error");
                return false;
            }
        }

        /// <summary>
        /// 获取群色批榜
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="rankList">色批榜</param>
        public bool GetGroupRank(long groupId, out List<Tables.HsoCount> rankList)
        {
            try
            {
                using var dbClient = SugarUtils.CreateSqlSugarClient(_dbPath);
                rankList = dbClient.Queryable<Tables.HsoCount>()
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