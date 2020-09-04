using System;
using Native.Sdk.Cqp;
using SqlSugar;
using SuiseiBot.Code.SqliteTool;
using SuiseiBot.Code.Tool;

namespace SuiseiBot.Code.Database.Helpers
{
    internal class SubscriptionDBHelper
    {
        #region 属性
        private readonly string DBPath;//数据库路径
        #endregion

        #region 构造函数
        public SubscriptionDBHelper(CQApi api)
        {
            DBPath = SugarUtils.GetDBPath(api.GetLoginQQ().Id.ToString());
        }
        #endregion

        #region 动态更新数据库记录
        /// <summary>
        /// 检查记录的动态是否为最新的
        /// </summary>
        /// <returns></returns>
        public bool IsLatest(long groupId, long biliUserId, DateTime updateTime)
        {
            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
            //查询是否存在相同记录subscriptionId
            return dbClient.Queryable<BiliSubscription>()
                           .Where(currDynamic => currDynamic.SubscriptionId == biliUserId &&
                                                 currDynamic.Gid            == groupId    &&
                                                 currDynamic.UpdateTime     == Utils.DateTimeToTimeStamp(updateTime))
                           .Any();
        }

        public int Update(long groupId, long biliUserId, DateTime updateTime)
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
                    }).ExecuteCommand();
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
                            .ExecuteCommand();
            }
        }
        #endregion
    }
}
