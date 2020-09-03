using com.cbgan.SuiseiBot.Code.Database;
using com.cbgan.SuiseiBot.Code.Database.Helpers;
using com.cbgan.SuiseiBot.Code.Resource.TypeEnum.CmdType;
using com.cbgan.SuiseiBot.Code.Tool.Log;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Model;

namespace com.cbgan.SuiseiBot.Code.ChatHandle
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

        #region  消息响应函数
        /// <summary>
        /// 在收到消息后获取数据库数据并判断是否需要修改数据库
        /// 指令分发
        /// </summary>
        public void GetChat(WholeMatchCmdType cmdType)
        {
            switch (cmdType)
            {
                case WholeMatchCmdType.Suisei_SignIn:
                    SignIn();
                    break;
                default:
                    break;
            }
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 签到
        /// </summary>
        private void SignIn()
        {
            ConsoleLog.Info("收到消息", "慧酱签到");
            SuiseiDBHelper suiseiDB = new SuiseiDBHelper(Sender, SuiseiEventArgs);
            suiseiDB.SignIn();
            SuiseiData userData = suiseiDB.UserData; //数据库查询到的用户数据
            //签到成功判断
            if (userData.ChatDate == suiseiDB.TriggerTime && suiseiDB.IsExists) //今天已经签到过了
            {
                QQGroup.SendGroupMessage("neeeeeeee\nmooooooo\n今天已经贴过了");
            }
            else //签到
            {
                suiseiDB.FavorRateUp();
                QQGroup.SendGroupMessage("奇怪的好感度增加了！\n当前好感度为：", userData.FavorRate);
            }
        }
        #endregion
    }
}
