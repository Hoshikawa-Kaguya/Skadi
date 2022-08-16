using BilibiliApi.Live.Enums;
using SqlSugar;

namespace Skadi.DatabaseUtils;

public class Tables
{
    #region 阿B订阅数据表定义

    [SugarTable("bili_dynamic_subscription")]
    public class BiliDynamicSubscription
    {
        [SugarColumn(ColumnName = "gid", ColumnDataType = "INTEGER", IsPrimaryKey = true)]
        public long Gid { set; get; }

        [SugarColumn(ColumnName = "subscription_id", ColumnDataType = "INTEGER", IsPrimaryKey = true)]
        public long SubscriptionId { set; get; }

        [SugarColumn(ColumnName = "update_time", ColumnDataType = "VARCHAR")]
        public long UpdateTime { set; get; }
    }

    #endregion

    #region 阿B订阅数据表定义

    [SugarTable("bili_live_subscription")]
    public class BiliLiveSubscription
    {
        [SugarColumn(ColumnName = "gid", ColumnDataType = "INTEGER", IsPrimaryKey = true)]
        public long Gid { set; get; }

        [SugarColumn(ColumnName = "subscription_id", ColumnDataType = "INTEGER", IsPrimaryKey = true)]
        public long SubscriptionId { set; get; }

        [SugarColumn(ColumnName = "live_status", ColumnDataType = "INTEGER")]
        public LiveStatusType LiveStatus { set; get; }
    }

    #endregion

    /// <summary>
    /// 老色批数据库
    /// 看看谁天天看色图
    /// </summary>
    [SugarTable("hso")]
    public class HsoCount
    {
        [SugarColumn(ColumnName = "gid", ColumnDataType = "INTEGER", IsPrimaryKey = true)]
        public long Gid { set; get; }

        [SugarColumn(ColumnName = "uid", ColumnDataType = "INTEGER", IsPrimaryKey = true)]
        public long Uid { get; set; }

        [SugarColumn(ColumnName = "count", ColumnDataType = "INTEGER")]
        public int Count { get; set; }
    }

    /// <summary>
    /// 抽老婆数据
    /// </summary>
    [SugarTable("wife")]
    public class Wife
    {
        [SugarColumn(ColumnName = "uid", ColumnDataType = "INTEGER", IsPrimaryKey = true)]
        public long Uid { get; set; }

        [SugarColumn(ColumnName = "wife_uid", ColumnDataType = "INTEGER")]
        public long WifeUid { get; set; }
    }
}