using System;
using System.IO;
using System.Linq;
using Native.Sdk.Cqp.EventArgs;
using SuiseiBot.Code.IO;
using SuiseiBot.Code.Tool.LogUtils;

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
            //检查色图文件夹大小
            long size = new DirectoryInfo(IOUtils.GetHsoPath())
                        .GetFiles("*", SearchOption.AllDirectories)
                        .Select(file => file.Length)
                        .Sum();
            ConsoleLog.Debug("Hso Dir Size",size);
            DebugEventArgs.FromGroup.SendGroupMessage($"DEBUG\r\nHso Dir Size = {size/1024.0/1024.0}MB");
#else
            DebugEventArgs.FromGroup.SendGroupMessage("哇哦");
#endif
        }
        #endregion
    }
}