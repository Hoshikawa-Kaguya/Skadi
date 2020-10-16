using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sora.EventArgs.SoraEvent;

namespace SuiseiBot.BotInterface
{
    /// <summary>
    /// 初始化事件
    /// </summary>
    internal static class InitalizationEvent
    {
        internal static ValueTask Initalization(object sender, ConnectEventArgs connectEvent)
        {
            return ValueTask.CompletedTask;
        }
    }
}
