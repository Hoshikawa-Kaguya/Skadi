using System.Data.Entity.Core.Metadata.Edm;
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

    #region BOSS信息表定义
    [SugarTable("boss_info")]
    internal class BossInfo
    {
        //enemy_id用于标识boss借以寻找enemy_parameter
        [SugarColumn(ColumnName = "eid", ColumnDataType = "INTEGER", IsPrimaryKey = true)]
        public long Eid { get; set; }
        //unit_id用于标识boss借以寻找unit_skill_data与unit_enemy_data获取boss技能属性
        [SugarColumn(ColumnName = "uid", ColumnDataType = "INTEGER", IsPrimaryKey = true)]
        public long Uid { get; set; }
        //boss属性：名称
        [SugarColumn(ColumnName = "name", ColumnDataType = "VARCHAR",IsPrimaryKey = true)]
        public string Name { get; set; }
        //boss属性：血量
        [SugarColumn(ColumnName = "hp", ColumnDataType = "INTEGER", IsPrimaryKey = true)]
        public int hp { get; set; }
        //boss属性：物攻
        [SugarColumn(ColumnName = "atk", ColumnDataType = "INTEGER", IsPrimaryKey = true)]
        public int ATK { get; set; }
        //boss属性：法攻
        [SugarColumn(ColumnName = "magic_atk", ColumnDataType = "INTEGER", IsPrimaryKey = true)]
        public int MagicATK { get; set; }
        //boss属性：物防
        [SugarColumn(ColumnName = "def", ColumnDataType = "INTEGER", IsPrimaryKey = true)]
        public int DEF { get; set; }
        //boss属性：法防
        [SugarColumn(ColumnName = "magic_def", ColumnDataType = "INTEGER", IsPrimaryKey = true)]
        public int MagicDEF { get; set; }
    }
    #endregion

    #region BOSS组信息表表定义
    [SugarTable("phase_info")]
    internal class PhaseInfo
    {
        //Phase_id用于标识boss借以寻找enemy_parameter
        [SugarColumn(ColumnName = "pid", ColumnDataType = "INTEGER", IsPrimaryKey = true)]
        public long Pid { get; set; }
        //enemy_id1用于标识一王
        [SugarColumn(ColumnName = "eid1", ColumnDataType = "INTEGER", IsPrimaryKey = true)]
        public long Eid1 { get; set; }
        //enemy_id1用于标识二王
        [SugarColumn(ColumnName = "eid2", ColumnDataType = "INTEGER", IsPrimaryKey = true)]
        public long Eid2 { get; set; }
        //enemy_id3用于标识三王
        [SugarColumn(ColumnName = "eid3", ColumnDataType = "INTEGER", IsPrimaryKey = true)]
        public long Eid3 { get; set; }
        //enemy_id4用于标识四王
        [SugarColumn(ColumnName = "eid4", ColumnDataType = "INTEGER", IsPrimaryKey = true)]
        public long Eid4 { get; set; }
        //enemy_id5用于标识一王
        [SugarColumn(ColumnName = "eid5", ColumnDataType = "INTEGER", IsPrimaryKey = true)]
        public long Eid5 { get; set; }
    }
    #endregion
}
