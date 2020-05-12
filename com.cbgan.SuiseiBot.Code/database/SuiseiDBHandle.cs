using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Interface;
using Native.Sdk.Cqp.Model;
using Native.Sdk.Cqp;
using System.IO;

namespace com.cbgan.SuiseiBot.Code.database
{
    internal class SuiseiDBHandle
    {
        #region 参数
        private long QQID { set; get; }             //QQ号
        private long GroupId { set; get; }          //群号
        public int CurrentFavorRate { set; get; }   //当前的好感度
        private DateTime TriggerTime { set; get; }  //触发时间戳
        private string[] UserID { set; get; }       //用户信息
        public CQGroupMessageEventArgs SuiseiGroupMessageEventArgs { private set; get; }
        public object Sender { private set; get; }
        public readonly static string TableName = "suisei";//数据库表名
        private static string DBPath;//数据库路径
        #endregion

        #region 构造函数
        /// <summary>
        /// 在接受到群消息时使用
        /// </summary>
        /// <param name="sender">sender object</param>
        /// <param name="eventArgs">CQAppEnableEventArgs类</param>
        /// <param name="time">触发时间</param>
        public SuiseiDBHandle(object sender, CQGroupMessageEventArgs eventArgs)
        {
            this.QQID = eventArgs.FromQQ.Id;
            this.GroupId = eventArgs.FromGroup.Id;
            this.Sender = sender;
            this.SuiseiGroupMessageEventArgs = eventArgs;
            this.TriggerTime = DateTime.Today;//触发日期
            UserID = new string[] //用户信息
            {
                QQID.ToString(),                    //用户QQ
                GroupId.ToString(),                 //用户所在群号
            };
            DBPath = System.IO.Directory.GetCurrentDirectory() + "\\data\\" + eventArgs.CQApi.GetLoginQQ() + "\\suisei.db";
        }
        #endregion

        #region Suisei互动数据表的定义
        public readonly static string[] ColName = {//字段名
                    "uid",          //用户QQ
                    "gid",          //用户所在群号
                    "favor_rate",   //好感度（大概
                    "use_date"     //签到时间(使用时间戳）
        };

        public readonly static string[] ColType = {//字段类型
                    "INTEGER NOT NULL",
                    "INTEGER NOT NULL",
                    "INTEGER NOT NULL",
                    "TEXT NOT NULL"
        };
        public readonly static string[] PrimaryColName = {//主键名
                    "uid",          //用户QQ
                    "gid"          //用户所在群号
        };
        #endregion

        /// <summary>
        /// 触发后查找数据库返回读取到的值
        /// </summary>
        /// <returns>状态值
        /// 包含当前好感度和调用数据的Dictionary
        /// "favor_rate":当前的好感度[int]
        /// "use_date":上次调用时间[DateTime]
        /// "isExists":是否存在上一次的记录
        /// </returns>
        public Dictionary<string,string> SignIn()
        {
            try
            {
                SQLiteHelper dbHelper = new SQLiteHelper(DBPath);
                dbHelper.OpenDB();
                if (Convert.ToBoolean(dbHelper.GetCount(TableName, PrimaryColName, UserID))) //查找是否有记录
                {
                    //获取用户的数据
                    SQLiteDataReader DBReader = dbHelper.FindRow(TableName, PrimaryColName, UserID);
                    //初始化变量 获取当前好感度
                    Dictionary<string, string> user_data = DBDataReader(DBReader);
                    dbHelper.CloseDB();
                    user_data.Add("isExists", "true");
                    user_data.TryGetValue("favor_rate", out string favorRate);
                    this.CurrentFavorRate = Convert.ToInt32(favorRate);//更新当前好感值
                    return user_data;
                }
                else                                                             //未找到签到记录
                {
                    string[] UserInitData = //创建用户初始化数据数组
                    {
                    QQID.ToString(),                    //用户QQ
                    GroupId.ToString(),                 //用户所在群号
                    "0",                                //好感度
                    TriggerTime.ToString()              //签到时间
                };
                    dbHelper.InsertRow(TableName, ColName, UserInitData);//向数据库写入新数据
                    dbHelper.CloseDB();
                    Dictionary<string, string> user_data = new Dictionary<string, string>();
                    user_data.Add("favor_rate", "0");
                    user_data.Add("use_date", TriggerTime.ToString());
                    user_data.Add("isExists", "false");
                    this.CurrentFavorRate = 0;
                    return user_data;
                }
            }
            catch (Exception){throw;}
        }

        /// <summary>
        /// 更新当前的好感度
        /// </summary>
        /// <returns>返回成功标准</returns>
        public bool FavorRateUp()
        {
            try
            {
                SQLiteHelper dbHelper = new SQLiteHelper(DBPath);
                dbHelper.OpenDB();
                //更新好感度数据
                this.CurrentFavorRate++;
                dbHelper.UpdateData(TableName, "favor_rate", CurrentFavorRate.ToString(), PrimaryColName, UserID);
                dbHelper.UpdateData(TableName, "use_date", TriggerTime.ToString(), PrimaryColName, UserID);
                dbHelper.CloseDB();
            }
            catch (Exception){throw; }
            return true;
        }

        /// <summary>
        /// 读取SQLiteDataReader中的第一行数据
        /// 其他数据丢弃
        /// </summary>
        /// <param name="dbReader"></param>
        /// <returns>返回当前读取到的第一行数据</returns>
        private Dictionary<string,string> DBDataReader(SQLiteDataReader dbReader)
        {
            Dictionary<string, string> valuePairs = new Dictionary<string, string>();
            dbReader.Read();
            //写入键值对
            valuePairs.Add("favor_rate", dbReader["favor_rate"].ToString());
            valuePairs.Add("use_date", dbReader["use_date"].ToString());
            while (dbReader.Read()) ;//丢弃数据
            return valuePairs;
        }
    }
}
