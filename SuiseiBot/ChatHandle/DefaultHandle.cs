using Native.Sdk.Cqp.EventArgs;

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
        public void GetChat() //消息接收并判断是否响应
        {
            if (DebugEventArgs == null || Sender == null) return;
            if (DebugEventArgs.Message.Text.StartsWith("echo"))
            {
                Echo();
            }
            else
            {
                Test();
            }
        }
        #endregion/

        #region DEBUG
        /// <summary>
        /// echo打印函数
        /// </summary>
        private void Echo()
        {
            if (DebugEventArgs.Message.Text.Length > 5)
            {
                DebugEventArgs.FromGroup.SendGroupMessage(DebugEventArgs.Message.Text.Substring(5));
            }
        }

        /// <summary>
        /// 响应函数
        /// </summary>
        private void Test() //功能响应
        {
            //此区域代码均只用于测试
#if DEBUG
            //目前测试得到mirai发向pixiv.cat的请求依旧返回403
            DebugEventArgs.FromGroup.SendGroupMessage("[CQ:image,url=https://pixiv.cat/69168247.png]");
#else
            DebugEventArgs.FromGroup.SendGroupMessage("哇哦");
#endif
        }
        #endregion
    }
}