using System.Collections.Generic;
using SqlSugar;
using SuiseiBot.Code.Resource.TypeEnum;
using SuiseiBot.Code.Resource.TypeEnum.GuildBattleType;

namespace SuiseiBot.Code.DatabaseUtils
{
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

    #region 会战数据

    #region 出刀记录表定义
    [SugarTable("guildbattle")]
    internal class GuildBattle
    {
        /// <summary>
        /// 记录编号[自增]
        /// </summary>
        [SugarColumn(ColumnName = "aid" , ColumnDataType = "INTEGER",IsIdentity = true,IsPrimaryKey = true)]
        public int Aid { get; set; }
        /// <summary>
        /// 用户QQ号
        /// </summary>
        [SugarColumn(ColumnName = "uid",ColumnDataType = "INTEGER")]
        public long Uid { get; set; }
        /// <summary>
        /// 记录产生时间
        /// </summary>
        [SugarColumn(ColumnName = "time",ColumnDataType = "INTEGER")]
        public long Time { get; set; }
        /// <summary>
        /// 周目数
        /// </summary>
        [SugarColumn(ColumnName = "round",ColumnDataType = "INTEGER")]
        public int Round { get; set; }
        /// <summary>
        /// boss的序号
        /// </summary>
        [SugarColumn(ColumnName = "order_num",ColumnDataType = "INTEGER")]
        public int Order { get; set; }
        /// <summary>
        /// 伤害数值
        /// </summary>
        [SugarColumn(ColumnName = "dmg",ColumnDataType = "INTEGER")]
        public long Damage { get; set; }
        /// <summary>
        /// 出刀类型标记
        /// </summary>
        [SugarColumn(ColumnName = "flag",ColumnDataType = "INTEGER")]
        public AttackType Attack { get; set; }
    }
    #endregion

    #region 成员表定义
    [SugarTable("member")]
    internal class MemberInfo
    {
        /// <summary>
        /// 用户所在群号，同时也是公会标识
        /// </summary>
        [SugarColumn(ColumnName = "gid",ColumnDataType = "INTEGER",IsPrimaryKey = true)]
        public long Gid { get; set; }
        /// <summary>
        /// 用户的QQ号
        /// </summary>
        [SugarColumn(ColumnName = "uid",ColumnDataType = "INTEGER",IsPrimaryKey = true)]
        public long Uid { get; set; }
        /// <summary>
        /// 成员名
        /// </summary>
        [SugarColumn(ColumnName = "name",ColumnDataType = "VARCHAR")]
        public string Name { get; set; }
        /// <summary>
        /// 用户状态修改时间
        /// </summary>
        [SugarColumn(ColumnName = "time",ColumnDataType = "INTEGER")]
        public long Time { get; set; }
        /// <summary>
        /// 用户状态标志
        /// </summary>
        [SugarColumn(ColumnName = "flag",ColumnDataType = "INTEGER")]
        public FlagType Flag { get; set; }
        /// <summary>
        /// 状态描述（可空，需按照文档进行修改）
        /// </summary>
        [SugarColumn(ColumnName = "info",ColumnDataType = "VARCHAR",IsNullable = true)]
        public string Info { get; set; }
        /// <summary>
        /// 当日SL标记,使用时间戳存储产生时间
        /// </summary>
        [SugarColumn(ColumnName = "sl",ColumnDataType = "INTEGER")]
        public long SL { get; set; }
    }
    #endregion

    #region 公会表定义
    [SugarTable("guild")]
    internal class GuildInfo
    {
        /// <summary>
        /// 公会所属群号
        /// </summary>
        [SugarColumn(ColumnName = "gid",ColumnDataType = "INTEGER",IsPrimaryKey = true)]
        public long Gid { get; set; }
        /// <summary>
        /// 公会名
        /// </summary>
        [SugarColumn(ColumnName = "name", ColumnDataType = "VARCHAR")]
        public string GuildName { get; set; }
        /// <summary>
        /// 公会所在区服
        /// </summary>
        [SugarColumn(ColumnName = "server", ColumnDataType = "INTEGER")]
        public Server ServerId { get; set; }
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
        /// <summary>
        /// 当前所在boss序号
        /// </summary>
        [SugarColumn(ColumnName = "order_num",ColumnDataType = "INTEGER")]
        public int Order { get; set; }
        /// <summary>
        /// 当前boss阶段
        /// </summary>
        [SugarColumn(ColumnName = "boss_phase",ColumnDataType = "INTEGER")]
        public int BossPhase { get; set; }
        /// <summary>
        /// 公会是否在会战
        /// </summary>
        [SugarColumn(ColumnName = "in_battle",ColumnDataType = "INTEGER")]
        public bool InBattle { get; set; }
    }
    #endregion

    #region Boss数据
    [SugarTable("guild_battle_boss")]
    internal class GuildBattleBoss
    {
        /// <summary>
        /// boss的区服
        /// </summary>
        [SugarColumn(ColumnName = "server",ColumnDataType = "INTEGER",IsPrimaryKey = true)]
        public Server ServerId { set; get; }

        /// <summary>
        /// Boss序号
        /// </summary>
        [SugarColumn(ColumnName = "order_num",ColumnDataType = "INTEGER",IsPrimaryKey = true)]
        public int Order { set; get; }

        /// <summary>
        /// 阶段
        /// </summary>
        [SugarColumn(ColumnName = "phase",ColumnDataType = "INTEGER",IsPrimaryKey = true)]
        public int Phase { set; get; }

        /// <summary>
        /// 进入下一阶段的所需的周目数
        /// </summary>
        [SugarColumn(ColumnName = "round",ColumnDataType = "INTEGER")]
        public int Round { set; get; }

        /// <summary>
        /// boss的血量
        /// </summary>
        [SugarColumn(ColumnName = "hp",ColumnDataType = "INTEGER")]
        public long HP { set; get; }

        public static List<GuildBattleBoss> GetInitBossInfos()
        {
            List<GuildBattleBoss> initInfos = new List<GuildBattleBoss>();

            #region 一阶段
            initInfos.Add(new GuildBattleBoss
            {
                ServerId = Server.CN,
                HP       = 6000000,
                Order    = 1,
                Phase    = 1,
                Round    = 1
            });
            initInfos.Add(new GuildBattleBoss
            {
                ServerId = Server.CN,
                HP       = 8000000,
                Order    = 2,
                Phase    = 1,
                Round    = 1
            });
            initInfos.Add(new GuildBattleBoss
            {
                ServerId = Server.CN,
                HP       = 10000000,
                Order    = 3,
                Phase    = 1,
                Round    = 1
            });
            initInfos.Add(new GuildBattleBoss
            {
                ServerId = Server.CN,
                HP       = 12000000,
                Order    = 4,
                Phase    = 1,
                Round    = 1
            });
            initInfos.Add(new GuildBattleBoss
            {
                ServerId = Server.CN,
                HP       = 20000000,
                Order    = 5,
                Phase    = 1,
                Round    = 1
            });
            #endregion
            
            #region 二阶段
            initInfos.Add(new GuildBattleBoss
            {
                ServerId = Server.CN,
                HP       = 6000000,
                Order    = 1,
                Phase    = 2,
                Round    = -1
            });
            initInfos.Add(new GuildBattleBoss
            {
                ServerId = Server.CN,
                HP       = 8000000,
                Order    = 2,
                Phase    = 2,
                Round    = -1
            });
            initInfos.Add(new GuildBattleBoss
            {
                ServerId = Server.CN,
                HP       = 10000000,
                Order    = 3,
                Phase    = 2,
                Round    = -1
            });
            initInfos.Add(new GuildBattleBoss
            {
                ServerId = Server.CN,
                HP       = 12000000,
                Order    = 4,
                Phase    = 2,
                Round    = -1
            });
            initInfos.Add(new GuildBattleBoss
            {
                ServerId = Server.CN,
                HP       = 20000000,
                Order    = 5,
                Phase    = 2,
                Round    = -1
            });
            #endregion

            return initInfos;
        }
    }
    #endregion

    #endregion
}
