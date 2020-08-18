using com.cbgan.SuiseiBot.Code.Resource.TypeEnum.CmdType;
using Native.Sdk.Cqp;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Model;

namespace com.cbgan.SuiseiBot.Code.ChatHandlers
{
    internal class DefaultHandle
    {
        #region 属性

        public object                  sender    { private set; get; }
        public CQGroupMessageEventArgs eventArgs { private set; get; }

        #endregion

        #region 构造函数

        public DefaultHandle(object sender, CQGroupMessageEventArgs e)
        {
            this.eventArgs = e;
            this.sender    = sender;
        }

        #endregion

        #region 消息响应函数
        /// <summary>
        /// 消息接收函数
        /// 并匹配相应指令
        /// </summary>
        /// <param name="keywordType"></param>
        public void GetChat(WholeMatchCmdType keywordType) //消息接收并判断是否响应
        {
            if (eventArgs == null || sender == null) return;
            switch (keywordType)
            {
                case WholeMatchCmdType.Debug:
                    GroupResponse();
                    break;
            }
        }
        #endregion

        #region MyRegion
        /// <summary>
        /// 响应函数
        /// </summary>
        private void GroupResponse() //功能响应
        {
            string chat    = eventArgs.Message;
            Group  QQgroup = eventArgs.FromGroup;
            QQgroup.SendGroupMessage(CQApi.CQCode_UrlImage("https://ss2.bdstatic.com/70cFvnSh_Q1YnxGkpoWK1HF6hhy/it/u=2996287886,894071748&fm=26&gp=0.jpg"));
        }
        #endregion
    }
}