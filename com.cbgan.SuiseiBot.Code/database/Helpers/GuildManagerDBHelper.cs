using SuiseiBot.SqliteTool;
using Native.Sdk.Cqp.EventArgs;
using SqlSugar;
using System;
using System.Collections.Generic;

namespace SuiseiBot.Database.Helpers
{
    internal class GuildManagerDBHelper
    {
        #region 参数

        private long QQID    { set; get; } //QQ号
        private long GroupId { set; get; } //群号

        private string[] GuildId { set; get; } //公会信息

        public                 CQGroupMessageEventArgs EventArgs { private set; get; }
        public                 object                  Sender    { private set; get; }
        public readonly static string                  GuildTableName  = "guild";  //公会数据库表名
        public readonly static string                  MemberTableName = "member"; //成员数据库表名
        private static         string                  DBPath;                     //数据库路径

        #endregion

        #region 构造函数

        /// <summary>
        /// 在接受到群消息时使用
        /// </summary>
        /// <param name="sender">sender object</param>
        /// <param name="eventArgs">CQAppEnableEventArgs类</param>
        /// <param name="time">触发时间</param>
        public GuildManagerDBHelper(object sender, CQGroupMessageEventArgs eventArgs)
        {
            this.QQID      = eventArgs.FromQQ.Id;
            this.GroupId   = eventArgs.FromGroup.Id;
            this.Sender    = sender;
            this.EventArgs = eventArgs;
            GuildId = new string[] //公会信息
            {
                GroupId.ToString(), //公会所在群号
            };
            DBPath = SugarUtils.GetDBPath(eventArgs.CQApi.GetLoginQQ().Id.ToString());
        }

        #endregion

        #region PCR数据表的定义

        //公会表
        public readonly static string[] GColName =
        {
            //字段名
            "gid",   //公会所在的QQ群号
            "name",  //公会名
            "server" //公会所在区服
        };

        public readonly static string[] GColType =
        {
            //字段类型
            "INTEGER NOT NULL",
            "VARCHAR NOT NULL",
            "VARCHAR NOT NULL"
        };

        public readonly static string[] GPrimaryColName =
        {
            //主键名
            "gid" //公会所在的QQ群号
        };

        //成员表
        public readonly static string[] MColName =
        {
            //字段名
            "uid",  //成员的QQ号
            "gid",  //公会所在的QQ群号
            "name", //成员昵称
        };

        public readonly static string[] MColType =
        {
            //字段类型
            "INTEGER NOT NULL",
            "INTEGER NOT NULL",
            "VARCHAR NOT NULL"
        };

        public readonly static string[] MPrimaryColName =
        {
            //主键名
            "uid", //成员的QQ号
            "gid"  //公会所在的QQ群号
        };

        #endregion

        #region 查询函数

        public string getGuildName(long groupid)
        {
            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
            var                  data     = dbClient.Queryable<GuildData>().Where(i => i.Gid == groupid);
            if (data.Any())
            {
                return data.First().GuildName;
            }
            else
            {
                return "公会不存在";
            }
        }

        #endregion

        #region 指令响应函数

        /// <summary>
        /// 移除所有成员
        /// </summary>
        /// <param name="groupid">公会所在群号</param>
        /// <returns>状态值
        /// 0：正常移除
        /// 1：公会不存在
        /// -1：删除时发生错误
        /// </returns>
        public int EmptyMember(long groupid)
        {
            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
            var                  data     = dbClient.Queryable<MemberData>().Where(i => i.Gid == groupid);
            if (data.Any())
            {
                if (dbClient.Deleteable<MemberData>().Where(i => i.Gid == groupid).ExecuteCommandHasChange())
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            else
            {
                return 1;
            }
        }

        /// <summary>
        /// 移除一名成员
        /// </summary>
        /// <param name="qqid">成员QQ号</param>
        /// <param name="groupid">成员所在群号</param>
        /// <returns>状态值
        /// 0：正常移除
        /// 1：该成员并不在公会内
        /// -1：数据库出错
        /// </returns>
        public int LeaveGuild(long qqid, long groupid)
        {
            int                  retCode  = -1;
            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
            if (dbClient.Queryable<MemberData>().Where(i => i.Uid == qqid && i.Gid == groupid).Any())
            {
                retCode = dbClient.Deleteable<MemberData>().Where(i => i.Uid == qqid && i.Gid == groupid)
                                  .ExecuteCommandHasChange()
                    ? 0
                    : -1;
            }
            else
            {
                retCode = 1;
            }

            return retCode;
        }

        public List<MemberData> ShowMembers(long groupid)
        {
            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
            return dbClient.Queryable<MemberData>().Where(i => i.Gid == groupid).ToList();
        }

        /// <summary>
        /// 添加一名成员
        /// </summary>
        /// <param name="qqid">成员QQ号</param>
        /// <param name="groupid">成员所在群号</param>
        /// <param name="nickName">成员昵称</param>
        /// <returns>状态值
        /// 0：正常添加
        /// 1：该成员已存在，更新信息
        /// -1：数据库出错
        /// </returns>
        public int JoinToGuild(long qqid, long groupid, string nickName)
        {
            try
            {
                int                  retCode  = -1;
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                var data = new MemberData()
                {
                    NickName = nickName,
                    Uid      = qqid,
                    Gid      = groupid
                };
                if (dbClient.Queryable<MemberData>().Where(i => i.Uid == qqid && i.Gid == groupid).Any())
                {
                    retCode = dbClient.Updateable<MemberData>(data).Where(i => i.Uid == qqid && i.Gid == groupid)
                                      .ExecuteCommandHasChange()
                        ? 1
                        : -1;
                }
                else
                {
                    retCode = dbClient.Insertable<MemberData>(data).ExecuteCommand() > 0 ? 0 : -1;
                }


                // if (Convert.ToBoolean(dbHelper.GetCount(MemberTableName, MPrimaryColName, memberKey))) //查找是否有记录
                // {
                //     //已存在，则更新信息
                //     dbHelper.UpdateData(MemberTableName, "name", nickName, MPrimaryColName, memberKey);
                //     dbHelper.CloseDB();
                //     return 1;
                // }
                // else //未找到，初次创建
                // {
                //     MemberStatus member = new MemberStatus()//写入新的状态数据
                //     {
                //         Gid  = GroupId,
                //         Uid  = qqid,
                //         Time = Utils.GetNowTimeStamp(),
                //         Flag = 0,
                //         Info = null,
                //         SL   = 0
                //     };
                //     dbClient.Insertable(member).ExecuteCommand();
                //
                //     string[] memberInfo =
                //     {
                //         qqid.ToString(), //用户QQ号
                //         GuildId[0],      //用户所在QQ群号
                //         nickName         //用户昵称
                //     };
                //     dbHelper.InsertRow(MemberTableName, MColName, memberInfo); //向数据库写入新数据
                //     dbHelper.CloseDB();
                //     return 0;
                // }
                return retCode;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 初次创建公会
        /// </summary>
        /// <param name="gArea">公会所在区域</param>
        /// <param name="gName">公会名称</param>
        /// <returns>状态值
        /// 0：正常创建
        /// 1：该群公会已存在，更新信息
        /// -1:数据库出错
        /// </returns>
        public int createGuild(string gArea, string gName, long gId)
        {
            try
            {
                int                  retCode  = -1;
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                var data = new GuildData()
                {
                    GuildName  = gName,
                    ServerArea = gArea,
                    Gid        = gId
                };
                if (dbClient.Queryable<GuildData>().Where(i => i.Gid == gId).Any())
                {
                    retCode = dbClient.Updateable<GuildData>(data)
                                      .Where(i => i.Gid == gId)
                                      .ExecuteCommandHasChange()?1:-1;
                }
                else
                {
                    retCode = dbClient.Insertable<GuildData>(data).ExecuteCommand()>0?0:-1;
                }

                //TODO 改用ORM
                // SQLiteHelper dbHelper = new SQLiteHelper(DBPath);
                // dbHelper.OpenDB();
                // if (Convert.ToBoolean(dbHelper.GetCount(GuildTableName, GPrimaryColName, GuildId))) //查找是否有记录
                // {
                //     //已存在，则更新信息
                //     dbHelper.UpdateData(GuildTableName, "name", gName, GPrimaryColName, GuildId);
                //     dbHelper.UpdateData(GuildTableName, "server", gArea, GPrimaryColName, GuildId);
                //     dbHelper.CloseDB();
                //     return 1;
                // }
                // else //未找到，初次创建
                // {
                //     string[] GuildInitData = //创建用户初始化数据数组
                //     {
                //         GroupId.ToString(), //所在群号
                //         gName,              //公会名
                //         gArea               //所在区服
                //     };
                //     dbHelper.InsertRow(GuildTableName, GColName, GuildInitData); //向数据库写入新数据
                //     dbHelper.CloseDB();
                //     return 0;
                // }
                return retCode;
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion
    }
}