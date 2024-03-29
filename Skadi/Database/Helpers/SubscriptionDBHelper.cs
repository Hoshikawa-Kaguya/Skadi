using System;
using System.Collections.Generic;
using BilibiliApi.Live.Enums;
using Skadi.Database.SqliteTool;
using Sora.Util;
using SqlSugar;
using YukariToolBox.LightLog;

namespace Skadi.Database.Helpers;

internal class SubscriptionDbHelper
{
#region 属性

    private readonly long _loginUid; //数据库路径

#endregion

#region 构造函数

    public SubscriptionDbHelper(long loginUid)
    {
        _loginUid = loginUid;
    }

#endregion

#region 动态更新数据库记录

    /// <summary>
    /// 检查记录的动态是否为最新的
    /// </summary>
    /// <param name="groupId">群号</param>
    /// <param name="biliUserId">B站用户ID</param>
    /// <param name="updateTime">当前获取的记录</param>
    public bool IsLatestDynamic(long groupId, long biliUserId, DateTime updateTime)
    {
        try
        {
            using SqlSugarClient dbClient = SugarUtils.CreateUserDbClient(_loginUid);

            var ts = updateTime.ToTimeStamp();
            //查询是否存在相同或时间大于记录subscriptionId
            return dbClient.Queryable<Tables.BiliDynamicSubscription>()
                           .Where(lastDynamic =>
                                      lastDynamic.SubscriptionId == biliUserId
                                      && lastDynamic.Gid == groupId
                                      && lastDynamic.UpdateTime >= ts)
                           .Any();
        }
        catch (Exception e)
        {
            //数据库出错时默认输出true
            Log.Error("Database Error", Log.ErrorLogBuilder(e));
            return true;
        }
    }

    /// <summary>
    /// 更新数据库数据
    /// </summary>
    /// <returns>是否成功修改</returns>
    public bool UpdateDynamic(List<long> groupIds, long biliUserId, DateTime updateTime)
    {
        try
        {
            using SqlSugarClient dbClient = SugarUtils.CreateUserDbClient(_loginUid);
            long                 ts       = updateTime.ToTimeStamp();

            foreach (long id in groupIds)
            {
                //查找是否有历史记录
                if (!dbClient.Queryable<Tables.BiliDynamicSubscription>()
                             .Where(biliDynamic => biliDynamic.SubscriptionId == biliUserId && biliDynamic.Gid == id)
                             .Any())
                {
                    //没有记录插入新行
                    dbClient.Insertable(new Tables.BiliDynamicSubscription
                    {
                        Gid            = id,
                        SubscriptionId = biliUserId,
                        UpdateTime     = updateTime.ToTimeStamp()
                    }).ExecuteCommand();
                    continue;
                }

                //有记录更新时间
                dbClient.Updateable<Tables.BiliDynamicSubscription>(newBiliDynamic =>
                                                                        newBiliDynamic.UpdateTime == ts)
                        .Where(biliDynamic => biliDynamic.SubscriptionId == biliUserId && biliDynamic.Gid == id)
                        .ExecuteCommandHasChange();
            }

            return true;
        }
        catch (Exception e)
        {
            Log.Error("Database Error", Log.ErrorLogBuilder(e));
            return false;
        }
    }

    /// <summary>
    /// 更新数据库数据
    /// </summary>
    /// <param name="groupId"></param>
    /// <param name="biliUserId"></param>
    /// <param name="updateTime"></param>
    /// <returns>是否成功修改</returns>
    public bool UpdateDynamic(long groupId, long biliUserId, long updateTime)
    {
        try
        {
            using SqlSugarClient dbClient = SugarUtils.CreateUserDbClient(_loginUid);
            //查找是否有历史记录
            if (!dbClient.Queryable<Tables.BiliDynamicSubscription>()
                         .Where(biliDynamic => biliDynamic.SubscriptionId == biliUserId && biliDynamic.Gid == groupId)
                         .Any())
                //没有记录插入新行
                return
                    dbClient.Insertable(new Tables.BiliDynamicSubscription
                    {
                        Gid            = groupId,
                        SubscriptionId = biliUserId,
                        UpdateTime     = updateTime
                    }).ExecuteCommand()
                    > 0;
            //有记录更新时间
            return
                dbClient.Updateable<Tables.BiliDynamicSubscription>(newBiliDynamic =>
                                                                        newBiliDynamic.UpdateTime == updateTime)
                        .Where(biliDynamic => biliDynamic.SubscriptionId == biliUserId && biliDynamic.Gid == groupId)
                        .ExecuteCommandHasChange();
        }
        catch (Exception e)
        {
            Log.Error("Database Error", Log.ErrorLogBuilder(e));
            return false;
        }
    }

#endregion

#region 直播订阅数据库

    /// <summary>
    /// 获取最新的直播状态
    /// </summary>
    /// <param name="groupId">群号</param>
    /// <param name="biliUserId">B站用户ID</param>
    /// <returns>直播间状态</returns>
    public LiveStatusType GetLastLiveStatus(long groupId, long biliUserId)
    {
        using SqlSugarClient dbClient = SugarUtils.CreateUserDbClient(_loginUid);
        //查找是否有历史记录
        if (dbClient.Queryable<Tables.BiliLiveSubscription>()
                    .Where(biliLive => biliLive.SubscriptionId == biliUserId && biliLive.Gid == groupId)
                    .Any())
            return dbClient.Queryable<Tables.BiliLiveSubscription>()
                           .Where(biliLive => biliLive.SubscriptionId == biliUserId && biliLive.Gid == groupId)
                           .Select(biliLive => biliLive.LiveStatus)
                           .First();

        return LiveStatusType.Unknown;
    }

    /// <summary>
    /// 更新直播状态
    /// </summary>
    /// <param name="groupId">群号</param>
    /// <param name="biliUserId">B站用户ID</param>
    /// <param name="newStatus">新状态</param>
    public bool UpdateLiveStatus(long groupId, long biliUserId, LiveStatusType newStatus)
    {
        using SqlSugarClient dbClient = SugarUtils.CreateUserDbClient(_loginUid);
        //查找是否有历史记录
        if (!dbClient.Queryable<Tables.BiliLiveSubscription>()
                     .Where(biliLive => biliLive.SubscriptionId == biliUserId && biliLive.Gid == groupId)
                     .Any())
            //没有记录插入新行
            return dbClient.Insertable(new Tables.BiliLiveSubscription
                   {
                       Gid            = groupId,
                       SubscriptionId = biliUserId,
                       LiveStatus     = newStatus
                   }).ExecuteCommand()
                   > 0;
        //有记录更新时间
        return dbClient.Updateable<Tables.BiliLiveSubscription>(biliLive =>
                                                                    biliLive.LiveStatus == newStatus)
                       .Where(biliDynamic => biliDynamic.SubscriptionId == biliUserId && biliDynamic.Gid == groupId)
                       .ExecuteCommandHasChange();
    }

#endregion
}