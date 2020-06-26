using System;
using SqlSugar;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.cbgan.SuiseiBot.Code.Database
{
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

    /// <summary>
    /// 用于存放公会信息的表定义
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
        public string FavorRate { get; set; }
        //签到时间(使用时间戳）
        [SugarColumn(ColumnName = "use_date", ColumnDataType = "INTEGER")]
        public string ChatDate { get; set; }
    }
}
