using Native.Sdk.Cqp;
using Native.Sdk.Cqp.EventArgs;
using SuiseiBot.Code.Resource.TypeEnum.CmdType;

namespace SuiseiBot.Code.ChatHandle
{
    internal class DefaultHandle
    {
        #region 属性

        public object                  Sender    { private set; get; }
        public CQGroupMessageEventArgs DebugEventArgs { private set; get; }

        #endregion

        #region 构造函数

        public DefaultHandle(object sender, CQGroupMessageEventArgs e)
        {
            this.DebugEventArgs = e;
            this.Sender    = sender;
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
            if (DebugEventArgs == null || Sender == null) return;
            switch (keywordType)
            {
                case WholeMatchCmdType.Debug:
                    Test();
                    break;
            }
        }
        #endregion

        #region DEBUG
        /// <summary>
        /// 响应函数
        /// </summary>
        public void Test() //功能响应
        {
            //此区域代码均只用于测试
#if DEBUG
            DebugEventArgs.FromGroup.SendGroupMessage(CQApi.CQCode_Image("/hso/75603102_p0.png"));
#else
            DebugEventArgs.FromGroup.SendGroupMessage("哇哦");
#endif
        }
        #endregion
    }
}