using System;
using AntiRain.DatabaseUtils.SqliteTool;
using AntiRain.DatabaseUtils.Tables;
using Sora.Tool;
using SqlSugar;

namespace AntiRain.DatabaseUtils.Helpers
{
    internal class SubscriptionDBHelper
    {
        #region 属性
        private readonly string DBPath;//数据库路径
        #endregion

        #region 构造函数
        public SubscriptionDBHelper(long uid)
        {
            DBPath = SugarUtils.GetDBPath(uid.ToString());
        }
        #endregion

        #region 动态更新数据库记录
        /// <summary>
        /// 检查记录的动态是否为最新的
        /// </summary>
        /// <returns></returns>
        public bool IsLatest(long groupId, long biliUserId, DateTime updateTime)
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                //查询是否存在相同记录subscriptionId
                return dbClient.Queryable<BiliSubscription>()
                               .Where(currDynamic => currDynamic.SubscriptionId == biliUserId &&
                                                     currDynamic.Gid            == groupId    &&
                                                     currDynamic.UpdateTime     == Utils.DateTimeToTimeStamp(updateTime))
                               .Any();
            }
            catch (Exception e)
            {
                //数据库出错时默认输出true
                ConsoleLog.Error("Database Error",ConsoleLog.ErrorLogBuilder(e));
                return true;
            }
        }

        /// <summary>
        /// 更新数据库数据
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="biliUserId"></param>
        /// <param name="updateTime"></param>
        /// <returns>是否成功修改</returns>
        public bool Update(long groupId, long biliUserId, DateTime updateTime)
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                //查找是否有历史记录
                if (!dbClient.Queryable<BiliSubscription>()
                             .Where(biliDynamic => biliDynamic.SubscriptionId == biliUserId &&
                                                   biliDynamic.Gid            == groupId)
                             .Any())
                {
                    //没有记录插入新行
                    return
                        dbClient.Insertable(new BiliSubscription()
                        {
                            Gid            = groupId,
                            SubscriptionId = biliUserId,
                            UpdateTime     = Utils.DateTimeToTimeStamp(updateTime)
                        }).ExecuteCommand() > 0;
                }
                else
                {
                    //有记录更新时间
                    return
                        dbClient.Updateable<BiliSubscription>(newBiliDynamic =>
                                                                  newBiliDynamic.UpdateTime ==
                                                                  Utils.DateTimeToTimeStamp(updateTime))
                                .Where(biliDynamic => biliDynamic.SubscriptionId == biliUserId &&
                                                      biliDynamic.Gid            == groupId)
                                .ExecuteCommandHasChange();
                }
            }
            catch (Exception e)
            {
                ConsoleLog.Error("Database Error",ConsoleLog.ErrorLogBuilder(e));
                return false;
            }
        }

        //TODO 获取到未知动态时更新数据库
        #endregion
    }
}
