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
        public long QQID { private set; get; }          //QQ号
        public long GroupId { private set; get; }       //群号
        public long TriggerTime { private set; get; }  //触发时间戳
        public CQGroupMessageEventArgs SuiseiGroupMessageEventArgs { private set; get; }
        public CQAppEnableEventArgs SuiseiAppEnableEventArgs { private set; get; }
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
            this.TriggerTime = Utils.GetNowTimeStamp();
            DBPath = System.IO.Directory.GetCurrentDirectory() + "\\data\\" + eventArgs.CQApi.GetLoginQQ() + "\\suisei.db";
        }
        /// <summary>
        /// 在插件启用时使用
        /// </summary>
        /// <param name="eventArgs">CQAppEnableEventArgs类</param>
        private SuiseiDBHandle(){}
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
                    "INTEGER NOT NULL" 
        };
        public readonly static string[] PrimaryColName = {//主键名
                    "uid",          //用户QQ
                    "gid"          //用户所在群号
        };
        #endregion

        /// <summary>
        /// 触发用户签到后修改数据库
        /// 并判断是否增加好感度
        /// </summary>
        /// <returns>状态值
        /// 大于0   签到成功且为当前好感度
        /// 0       初见
        /// -1      签到失败（今天已经签到过）
        /// </returns>
        public int SignIn()
        {
            string[] UserID = //用户标识
            {
                QQID.ToString(),                    //用户QQ
                GroupId.ToString(),                 //用户所在群号
            };
            int statusValue = 0;
            SQLiteHelper dbHelper = new SQLiteHelper(DBPath);
            dbHelper.OpenDB();
            SuiseiGroupMessageEventArgs.CQLog.Debug("数据库", "开始修改数据库");
            if (Convert.ToBoolean(dbHelper.GetCount(TableName, PrimaryColName, UserID))) //查找是否有记录
            {
                SuiseiGroupMessageEventArgs.CQLog.Debug("数据库", "有记录");
                //获取用户的数据
                SQLiteDataReader DBReader = dbHelper.FindRow(TableName, PrimaryColName, UserID);
                //初始化变量
                Dictionary<string, long> user_data = DBDataReader(DBReader);
                //获取当前好感度
                long FavorRate = 0;
                user_data.TryGetValue("favor_rate", out FavorRate);
                dbHelper.UpdateData(TableName, "favor_rate", (FavorRate + 1).ToString(), PrimaryColName, UserID);
                dbHelper.UpdateData(TableName, "use_date", Utils.GetNowTimeStamp().ToString(), PrimaryColName, UserID);
                //DateTime LastUseTime = Utils.TimeStampToDateTime(Convert.ToInt64(DBReader["use_date"]));//获取上次调用时间
                dbHelper.CloseDB();
            }
            else                                                             //未找到签到记录
            {
                SuiseiGroupMessageEventArgs.CQLog.Debug("数据库", "无记录");
                string[] UserInitData = //创建用户初始化数据数组
                {
                    QQID.ToString(),                    //用户QQ
                    GroupId.ToString(),                 //用户所在群号
                    "0",                                //好感度
                    Utils.GetNowTimeStamp().ToString()  //签到时间
                };
                dbHelper.InsertRow(TableName, ColName, UserInitData);//向数据库写入新数据
                dbHelper.CloseDB();
            }
            return statusValue;
        }

        private Dictionary<string,long> DBDataReader(SQLiteDataReader dbReader)
        {
            Dictionary<string, long> valuePairs = new Dictionary<string, long>();
            while (dbReader.Read())
            {
                valuePairs.Add("favor_rate", Convert.ToInt64(dbReader["favor_rate"]));
                valuePairs.Add("use_date", Convert.ToInt64(dbReader["use_date"]));
            }
            return valuePairs;
        }
    }
}
