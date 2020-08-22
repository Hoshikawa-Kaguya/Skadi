using System;
using com.cbgan.SuiseiBot.Code.SqliteTool;
using com.cbgan.SuiseiBot.Code.Tool;
using com.cbgan.SuiseiBot.Code.Tool.Log;
using Native.Sdk.Cqp.EventArgs;
using SqlSugar;

namespace com.cbgan.SuiseiBot.Code.Database.Helpers
{
    internal class SuiseiDBHelper
    {
        #region 参数
        private long QQID { set; get; }             //QQ号
        private long GroupId { set; get; }          //群号
        private int CurrentFavorRate { set; get; }  //当前的好感度
        private static string DBPath { set; get; }  //数据库路径
        public long TriggerTime { set; get; }      //触发时间戳
        public bool IsExists { set; get; }          //是否存在上一次的记录
        public SuiseiData UserData { set; get; }    //用户数据
        public CQGroupMessageEventArgs SuiseiGroupMessageEventArgs { private set; get; }
        public object Sender { private set; get; }
        #endregion

        #region 构造函数
        /// <summary>
        /// 在接受到群消息时使用
        /// </summary>
        /// <param name="sender">sender object</param>
        /// <param name="eventArgs">CQAppEnableEventArgs类</param>
        public SuiseiDBHelper(object sender, CQGroupMessageEventArgs eventArgs)
        {
            this.QQID = eventArgs.FromQQ.Id;
            this.GroupId = eventArgs.FromGroup.Id;
            this.Sender = sender;
            this.SuiseiGroupMessageEventArgs = eventArgs;
            this.TriggerTime = Utils.GetTodayStamp();//触发日期
            DBPath = SugarUtils.GetDBPath(eventArgs.CQApi.GetLoginQQ().Id.ToString());
        }
        #endregion

        #region Suisei数据库操作方法
        /// <summary>
        /// 触发后查找数据库返回读取到的值
        /// </summary>
        /// <returns>状态值
        /// 包含当前好感度和调用数据的Dictionary
        /// "favor_rate":当前的好感度[int]
        /// "use_date":上次调用时间戳[long]
        /// </returns>
        public void SignIn()
        {
            try
            {
                using SqlSugarClient SQLiteClient = SugarUtils.CreateSqlSugarClient(DBPath);
                if (Convert.ToBoolean(
                                      SQLiteClient.Queryable<SuiseiData>().Where(user => user.Uid == QQID && user.Gid == GroupId).Count()
                                     )) //查找是否有记录
                {
                    //查询数据库数据
                    UserData = SQLiteClient.Queryable<SuiseiData>()
                                           .Where(userInfo => userInfo.Uid == QQID && userInfo.Gid == GroupId)
                                           .First();
                    IsExists         = true;
                    CurrentFavorRate = UserData.FavorRate; //更新当前好感值
                }
                else //未找到签到记录
                {
                    UserData = new SuiseiData //创建用户初始化数据
                    {
                        Uid       = QQID,       //用户QQ
                        Gid       = GroupId,    //用户所在群号
                        FavorRate = 0,          //好感度
                        ChatDate  = TriggerTime //签到时间
                    };
                    IsExists         = false;
                    CurrentFavorRate = 0;
                }
            }
            catch (Exception e)
            {
                SuiseiGroupMessageEventArgs.FromGroup.SendGroupMessage($"数据库出现错误\n请向管理员反馈此错误\n{e}");
                ConsoleLog.Error("suisei签到", $"数据库出现错误\n{e}");
            }
        }

        /// <summary>
        /// 更新当前的好感度
        /// </summary>
        public void FavorRateUp()
        {
            try
            {
                //更新好感度数据
                this.CurrentFavorRate++;
                UserData.FavorRate = CurrentFavorRate;  //更新好感度
                using SqlSugarClient SQLiteClient = SugarUtils.CreateSqlSugarClient(DBPath);
                //判断用户记录是否已经存在
                if (IsExists) //已存在则更新数据
                {
                    UserData.ChatDate = TriggerTime; //更新触发时间
                    SQLiteClient.Updateable(UserData).ExecuteCommand();
                }
                else //不存在插入新行
                {
                    SQLiteClient.Insertable(UserData).ExecuteCommand(); //向数据库写入新数据
                }
            }
            catch (Exception e)
            {
                SuiseiGroupMessageEventArgs.FromGroup.SendGroupMessage($"数据库出现错误\n请向管理员反馈此错误\n{e}");
                ConsoleLog.Error("suisei签到", $"数据库出现错误\n{e}");
            }
        }
        #endregion
    }
}
