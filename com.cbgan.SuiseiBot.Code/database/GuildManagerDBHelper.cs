using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Native.Sdk.Cqp.EventArgs;

namespace com.cbgan.SuiseiBot.Code.database
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
            DBPath = System.IO.Directory.GetCurrentDirectory() + "\\data\\" + eventArgs.CQApi.GetLoginQQ() +
                     "\\suisei.db";
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


        public void allJoin()
        {
            throw new NotImplementedException();
        }

        public void emptyMember()
        {
            throw new NotImplementedException();
        }

        public int leaveGuild(string qq)
        {
            throw new NotImplementedException();
        }

        public void showMembers()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 添加一名成员
        /// </summary>
        /// <param name="qqid">成员QQ号</param>
        /// <param name="nickName">成员昵称</param>
        /// <returns>状态值
        /// 0：正常添加
        /// 1：该成员已存在，更新信息
        /// </returns>
        public int joinGuild(long qqid, string nickName)
        {
            try
            {
                SQLiteHelper dbHelper = new SQLiteHelper(DBPath);
                dbHelper.OpenDB();
                string[] memberKey =
                    {qqid.ToString(), GuildId[0]};

                if (Convert.ToBoolean(dbHelper.GetCount(MemberTableName, MPrimaryColName, memberKey))) //查找是否有记录
                {
                    //已存在，则更新信息
                    dbHelper.UpdateData(MemberTableName, "name", nickName, MPrimaryColName, memberKey);
                    dbHelper.CloseDB();
                    return 1;
                }
                else //未找到，初次创建
                {
                    string[] memberInfo =
                    {
                        qqid.ToString(), //用户QQ号
                        GuildId[0],      //用户所在QQ群号
                        nickName         //用户昵称
                    };
                    dbHelper.InsertRow(MemberTableName, MColName, memberInfo); //向数据库写入新数据
                    dbHelper.CloseDB();
                    return 0;
                }
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
        /// </returns>
        public int createGuild(string gArea, string gName)
        {
            try
            {
                SQLiteHelper dbHelper = new SQLiteHelper(DBPath);
                dbHelper.OpenDB();
                if (Convert.ToBoolean(dbHelper.GetCount(GuildTableName, GPrimaryColName, GuildId))) //查找是否有记录
                {
                    //已存在，则更新信息
                    dbHelper.UpdateData(GuildTableName, "name", gName, GPrimaryColName, GuildId);
                    dbHelper.UpdateData(GuildTableName, "server", gArea, GPrimaryColName, GuildId);
                    dbHelper.CloseDB();
                    return 1;
                }
                else //未找到，初次创建
                {
                    string[] GuildInitData = //创建用户初始化数据数组
                    {
                        GroupId.ToString(), //所在群号
                        gName,              //公会名
                        gArea               //所在区服
                    };
                    dbHelper.InsertRow(GuildTableName, GColName, GuildInitData); //向数据库写入新数据
                    dbHelper.CloseDB();
                    return 0;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

    }
}