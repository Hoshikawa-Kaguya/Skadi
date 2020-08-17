using com.cbgan.SuiseiBot.Code.Resource.Enum;
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
        /// </summary>
        /// <param name="keywordType"></param>
        public void GetChat(WholeMatchCmdType keywordType) //消息接收并判断是否响应
        {
            if (eventArgs == null || sender == null) return;
            if (keywordType == WholeMatchCmdType.Debug)
            {
                GroupResponse();
            }
        }

        /// <summary>
        /// 响应函数
        /// </summary>
        private void GroupResponse() //功能响应
        {
            string chat    = eventArgs.Message;
            Group  QQgroup = eventArgs.FromGroup;
            //QQgroup.SendGroupMessage(CQApi.CQCode_Image())
        }
        #endregion
    }
}