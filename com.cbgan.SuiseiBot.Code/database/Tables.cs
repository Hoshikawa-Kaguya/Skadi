using com.cbgan.SuiseiBot.Code.Resource.TypeEnum;
using SqlSugar;

namespace com.cbgan.SuiseiBot.Code.Database
{
    #region 成员表定义
    /// <summary>
    /// 用于存放成员信息的表定义
    /// </summary>
    [SugarTable("member", "guild members data table")]
    internal class MemberData
    {
        //成员QQ
        [SugarColumn(ColumnName = "uid", ColumnDataType = "INTEGER", IsPrimaryKey = true)]
        public long Uid { get; set; }
        //成员所在群号
        [SugarColumn(ColumnName = "gid", ColumnDataType = "INTEGER", IsPrimaryKey = true)]
        public long Gid { get; set; }
        //成员昵称
        [SugarColumn(ColumnName = "name", ColumnDataType = "VARCHAR")]
        public string NickName { get; set; }
    }
    #endregion

    #region 公会表定义
    /// <summary>
    /// 用于存放公会信息的表定义
    /// </summary>
    [SugarTable("guild", "guild data table")]
    internal class GuildData
    {
        //公会所在的QQ群号
        [SugarColumn(ColumnName = "gid", ColumnDataType = "INTEGER", IsPrimaryKey = true)]
        public long Gid { get; set; }
        //公会名
        [SugarColumn(ColumnName = "name", ColumnDataType = "VARCHAR")]
        public string GuildName { get; set; }
        //公会所在区服
        [SugarColumn(ColumnName = "server", ColumnDataType = "VARCHAR")]
        public string ServerArea { get; set; }
    }
    #endregion

    #region 彗酱签到表定义
    /// <summary>
    /// 用于存放彗酱信息的表定义
    /// </summary>
    [SugarTable("suisei", "suisei data table")]
    internal class SuiseiData
    {
        //用户QQ
        [SugarColumn(ColumnName = "uid", ColumnDataType = "INTEGER", IsPrimaryKey = true)]
        public long Uid { get; set; }
        //用户所在群号
        [SugarColumn(ColumnName = "gid", ColumnDataType = "INTEGER", IsPrimaryKey = true)]
        public long Gid { get; set; }
        //好感度（大概
        [SugarColumn(ColumnName = "favor_rate", ColumnDataType = "INTEGER")]
        public int FavorRate { get; set; }
        //签到时间(使用时间戳）
        [SugarColumn(ColumnName = "use_date", ColumnDataType = "INTEGER")]
        public long ChatDate { get; set; }
    }
    #endregion

    #region 出刀记录表定义
    [SugarTable("guildbattle")]
    internal class GuildBattle
    {
        //记录编号[自增]
        [SugarColumn(ColumnName = "bid" , ColumnDataType = "INTEGER",IsIdentity = true)]
        public int Bid { get; set; }
        //用户QQ号
        [SugarColumn(ColumnName = "uid",ColumnDataType = "INTEGER")]
        public long Uid { get; set; }
        //记录产生时间
        [SugarColumn(ColumnName = "time",ColumnDataType = "INTEGER")]
        public long Time { get; set; }
        //boss的代号
        [SugarColumn(ColumnName = "boss",ColumnDataType = "VARCHAR")]
        public string BossID { get; set; }
        //伤害数值
        [SugarColumn(ColumnName = "dmg",ColumnDataType = "INTEGER")]
        public long Damage { get; set; }
        //出刀类型标记
        [SugarColumn(ColumnName = "flag",ColumnDataType = "INTEGER")]
        public int Flag { get; set; }
    }
    #endregion

    #region 状态表定义
    [SugarTable("member_status")]
    internal class MemberStatus
    {
        //用户所在群号，同时也是公会标识
        [SugarColumn(ColumnName = "gid",ColumnDataType = "INTEGER",IsPrimaryKey = true)]
        public long Gid { get; set; }
        //用户的QQ号
        [SugarColumn(ColumnName = "uid",ColumnDataType = "INTEGER",IsPrimaryKey = true)]
        public long Uid { get; set; }
        //用户状态修改时间
        [SugarColumn(ColumnName = "time",ColumnDataType = "INTEGER")]
        public long Time { get; set; }
        //用户状态标志
        [SugarColumn(ColumnName = "flag",ColumnDataType = "INTEGER")]
        public int Flag { get; set; }
        //状态描述（可空，需按照文档进行修改）
        [SugarColumn(ColumnName = "info",ColumnDataType = "VARCHAR",IsNullable = true)]
        public string Info { get; set; }
        //当日SL标记
        [SugarColumn(ColumnName = "sl",ColumnDataType = "INTEGER")]
        public int SL { get; set; }
    }
    #endregion

    #region 阿B订阅数据表定义
    [SugarTable("bili_subscription")]
    internal class BiliSubscription
    {
        [SugarColumn(ColumnName = "gid", ColumnDataType = "INTEGER", IsPrimaryKey = true)]
        public long Gid { set; get; }
        [SugarColumn(ColumnName = "subscription_id", ColumnDataType = "INTEGER", IsPrimaryKey = true)]
        public long SubscriptionId { set; get; }
        [SugarColumn(ColumnName = "update_time", ColumnDataType = "VARCHAR")]
        public long UpdateTime { set; get; }
    }
    #endregion
    
#region 会战Boss相关数据

    #region 每个公会的会战数据
    [SugarTable("guild_battle_data")]
    internal class GuildBattleData
    {
        /// <summary>
        /// 公会所属群号
        /// </summary>
        [SugarColumn(ColumnName = "gid",ColumnDataType = "INTEGER",IsPrimaryKey = true)]
        public long Gid { get; set; }

        /// <summary>
        /// 当前公会正在进行的会战数据id（与ClanBattleInfo的会战ID相同）
        /// boss血量数据均由此ID进行查找
        /// </summary>
        [SugarColumn(ColumnName = "clan_battle_id",ColumnDataType = "INTEGER")]
        public long ClanBattleId { get; set; }

        /// <summary>
        /// 当前boss的血量
        /// </summary>
        [SugarColumn(ColumnName = "hp",ColumnDataType = "INTEGER")]
        public long HP { get; set; }

        /// <summary>
        /// 当前boss的总血量
        /// </summary>
        [SugarColumn(ColumnName = "total_hp",ColumnDataType = "INTEGER")]
        public long TotalHP { get; set; }

        /// <summary>
        /// 当前公会所在周目
        /// </summary>
        [SugarColumn(ColumnName = "round",ColumnDataType = "INTEGER")]
        public int Round { get; set; }
    }
    #endregion

    #region 会战数据表定义
    [SugarTable("clan_battle_info")]
    internal class ClanBattleInfo
    {
        /// <summary>
        /// 会战ID
        /// </summary>
        [SugarColumn(ColumnName = "clan_battle_id", ColumnDataType = "INTEGER", IsIdentity = true)]
        public long ClanBattleId { get; set; }

        /// <summary>
        /// 当前会战在远程数据库中的id
        /// 为空时则是本地数据
        /// </summary>
        [SugarColumn(ColumnName = "clan_battle_id_cloud", ColumnDataType = "INTEGER", IsNullable = true)]
        public long? ClanBattleIdCloud { get; set; }

        /// <summary>
        /// server用于标识当前boss组信息的所在区服
        /// </summary>
        [SugarColumn(ColumnName = "server", ColumnDataType = "INTEGER")]
        public Server ServerId { set; get; }

        /// <summary>
        /// 当前会战数据的更新时间
        /// </summary>
        [SugarColumn(ColumnName = "update_time", ColumnDataType = "INTEGER")]
        public long UpdateTime { get; set; }

        #region 一阶段

        /// <summary>
        /// 用于记录一阶段boss组所属分组的id
        /// </summary>
        [SugarColumn(ColumnName = "boss_group_id", ColumnDataType = "INTEGER")]
        public long BossGroupId1 { get; set; }

        /// <summary>
        /// 用于记录一阶段boss组所属分组在远程数据库中的id
        /// 为空时则是本地数据
        /// </summary>
        [SugarColumn(ColumnName = "boss_group_id_cloud", ColumnDataType = "INTEGER", IsNullable = true)]
        public long? GroupIdCloud1 { get; set; }

        /// <summary>
        /// 用于记录一阶段boss组该阶段的周目数
        /// -1则为无限周目数，不再进入下一阶段
        /// </summary>
        [SugarColumn(ColumnName = "round", ColumnDataType = "INTEGER")]
        public int Round1 { get; set; }

        #endregion

        #region 二阶段

        /// <summary>
        /// 用于记录二阶段boss组所属分组的id
        /// </summary>
        [SugarColumn(ColumnName = "boss_group_id", ColumnDataType = "INTEGER")]
        public long BossGroupId2 { get; set; }

        /// <summary>
        /// 用于记录二阶段boss组所属分组在远程数据库中的id
        /// 为空时则是本地数据
        /// </summary>
        [SugarColumn(ColumnName = "boss_group_id_cloud", ColumnDataType = "INTEGER", IsNullable = true)]
        public long? GroupIdCloud2 { get; set; }

        /// <summary>
        /// 用于记录二阶段boss组该阶段的周目数
        /// -1则为无限周目数，不再进入下一阶段
        /// </summary>
        [SugarColumn(ColumnName = "round", ColumnDataType = "INTEGER")]
        public int Round2 { get; set; }

        #endregion

        //第三/四阶段均为可空字段，当不存在三/四阶段时需要置空

        #region 三阶段

        /// <summary>
        /// 用于记录三阶段boss组所属分组的id
        /// </summary>
        [SugarColumn(ColumnName = "boss_group_id", ColumnDataType = "INTEGER", IsNullable = true)]
        public long? BossGroupId3 { get; set; }

        /// <summary>
        /// 用于记录三阶段boss组所属分组在远程数据库中的id
        /// 为空时则是本地数据
        /// </summary>
        [SugarColumn(ColumnName = "boss_group_id_cloud", ColumnDataType = "INTEGER", IsNullable = true)]
        public long? GroupIdCloud3 { get; set; }

        /// <summary>
        /// 用于记录三阶段boss组该阶段的周目数
        /// -1则为无限周目数，不再进入下一阶段
        /// </summary>
        [SugarColumn(ColumnName = "round", ColumnDataType = "INTEGER", IsNullable = true)]
        public int? Round3 { get; set; }

        #endregion

        #region 四阶段

        /// <summary>
        /// 用于记录三阶段boss组所属分组的id
        /// </summary>
        [SugarColumn(ColumnName = "boss_group_id", ColumnDataType = "INTEGER", IsNullable = true)]
        public long? BossGroupId4 { get; set; }

        /// <summary>
        /// 用于记录三阶段boss组所属分组在远程数据库中的id
        /// 为空时则是本地数据
        /// </summary>
        [SugarColumn(ColumnName = "boss_group_id_cloud", ColumnDataType = "INTEGER", IsNullable = true)]
        public long? GroupIdCloud4 { get; set; }

        /// <summary>
        /// 用于记录三阶段boss组该阶段的周目数
        /// -1则为无限周目数，不再进入下一阶段
        /// </summary>
        [SugarColumn(ColumnName = "round", ColumnDataType = "INTEGER", IsNullable = true)]
        public int? Round4 { get; set; }

        #endregion
    }
    #endregion

    #region BOSS信息表定义
    [SugarTable("clan_battle_boss_info")]
    internal class BossInfo
    {
        /// <summary>
        /// GroupId用于记录该boss所属分组的id
        /// </summary>
        [SugarColumn(ColumnName = "boss_group_id", ColumnDataType = "INTEGER", IsPrimaryKey = true)]
        public long BossGroupId { get; set; }

        /// <summary>
        /// GroupIdCloud用于记录当前boss组所属分组在远程数据中的id
        /// 为空时则是本地数据
        /// </summary>
        [SugarColumn(ColumnName = "boss_group_id_cloud", ColumnDataType = "INTEGER", IsNullable = true)]
        public long? GroupIdCloud { get; set; }

        /// <summary>
        /// order用于标识boss的序号(1-5)
        /// </summary>
        [SugarColumn(ColumnName = "order_num", ColumnDataType = "INTEGER", IsPrimaryKey = true)]
        public int Order { get; set; }

        /// <summary>
        /// server用于标识boss信息的所在区服
        /// </summary>
        [SugarColumn(ColumnName = "server", ColumnDataType = "INTEGER", IsPrimaryKey = true)]
        public Server ServerId { set; get; }

        /// <summary>
        /// scale用于记录boss属性强化倍率
        /// </summary>
        [SugarColumn(ColumnName = "scale", ColumnDataType = "FLOAT", IsNullable = true)]
        public float? Scale { get; set; }

        /// <summary>
        /// boss属性：名称
        /// </summary>
        [SugarColumn(ColumnName = "name", ColumnDataType = "VARCHAR", IsNullable = true)]
        public string Name { get; set; }

        /// <summary>
        /// boss属性：血量
        /// </summary>
        [SugarColumn(ColumnName = "hp", ColumnDataType = "INTEGER")]
        public long HP { get; set; }

        /// <summary>
        /// boss属性：物攻
        /// </summary>
        [SugarColumn(ColumnName = "atk", ColumnDataType = "INTEGER", IsNullable = true)]
        public int? ATK { get; set; }

        /// <summary>
        /// boss属性：法攻
        /// </summary>
        [SugarColumn(ColumnName = "magic_atk", ColumnDataType = "INTEGER", IsNullable = true)]
        public int? MagicATK { get; set; }

        /// <summary>
        /// boss属性：物防
        /// </summary>
        [SugarColumn(ColumnName = "def", ColumnDataType = "INTEGER", IsNullable = true)]
        public int? DEF { get; set; }

        /// <summary>
        /// boss属性：法防
        /// </summary>
        [SugarColumn(ColumnName = "magic_def", ColumnDataType = "INTEGER", IsNullable = true)]
        public int? MagicDEF { get; set; }

        /// <summary>
        /// 用于记录boss描述
        /// </summary>
        [SugarColumn(ColumnName = "comment", ColumnDataType = "VARCHAR", IsNullable = true)]
        public string Comment { get; set; }
    }
#endregion

#endregion
}
