using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using com.cbgan.SuiseiBot.Code.Database;
using com.cbgan.SuiseiBot.Code.Tool;
using Native.Sdk.Cqp.Enum;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Interface;
using Native.Sdk.Cqp.Model;

namespace com.cbgan.SuiseiBot.Code.ChatHandlers
{
    internal class SuiseiHanlde
    {
        #region 属性
        public object Sender { private set; get; }
        public Group QQGroup { private set; get; }
        public CQGroupMessageEventArgs SuiseiEventArgs { private set; get; }
        #endregion

        #region 构造函数
        public SuiseiHanlde(object sender, CQGroupMessageEventArgs e)
        {
            this.SuiseiEventArgs = e;
            this.Sender = sender;
            this.QQGroup = SuiseiEventArgs.FromGroup;
        }
        #endregion

        /// <summary>
        /// 在收到消息后获取数据库数据并判断是否需要修改数据库
        /// </summary>
        public void GetChat()
        {
            ConsoleLog.Info("收到消息", "慧酱签到");
            SuiseiDBHelper suiseiDB = new SuiseiDBHelper(Sender, SuiseiEventArgs);
            Dictionary<string, string> GetUserData = suiseiDB.SignIn();
            //获取调用时间
            GetUserData.TryGetValue("use_date", out string LastUseDateString);
            //获取是否是第一次调用
            GetUserData.TryGetValue("isExists", out string isExists);
            DateTime LastUseDate = Utils.TimeStampToDateTime(Convert.ToInt64(LastUseDateString));
            if (DateTime.Today.Equals(LastUseDate) && isExists.Equals("true")) //今天已经签到过了
            {
                QQGroup.SendGroupMessage("neeeeeeee\nmooooooo\n今天已经贴过了");
            }
            else//签到
            {
                GetUserData.TryGetValue("favor_rate", out string FavorRateString);
                int FavorRate = Convert.ToInt32(FavorRateString);
                suiseiDB.FavorRateUp();
                QQGroup.SendGroupMessage("奇怪的好感度增加了！\n当前好感度为：", FavorRate + 1);
            }
            SuiseiEventArgs.Handler = true;
        }
    }
}
