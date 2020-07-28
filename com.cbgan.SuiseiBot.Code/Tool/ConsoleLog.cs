using System;
using System.Runtime.InteropServices;

namespace com.cbgan.SuiseiBot.Code.Tool
{
    internal class ConsoleLog
    {
        #region 控制台调用函数
        /// <summary>
        /// 打开控制台
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AllocConsole();
        #endregion

        #region 格式化控制台Log函数
        /// <summary>
        /// 向控制台发送Info信息
        /// </summary>
        /// <param name="type">信息类型</param>
        /// <param name="message">信息内容</param>
        public static void Info(object type, object message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"[{DateTime.Now}][{type}][INFO]{message}");
        }
        /// <summary>
        /// 向控制台发送Warning信息
        /// </summary>
        /// <param name="type">信息类型</param>
        /// <param name="message">信息内容</param>
        public static void Warning(object type, object message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"[{DateTime.Now}][{type}][");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"WARNINIG");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("]");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{message}");
        }
        /// <summary>
        /// 向控制台发送Error信息
        /// </summary>
        /// <param name="type">信息类型</param>
        /// <param name="message">信息内容</param>
        public static void Error(object type, object message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"[{DateTime.Now}][{type}][");
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.Write($"ERROR");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("]");
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine($"{message}");
        }
        /// <summary>
        /// 向控制台发送Debug信息
        /// </summary>
        /// <param name="type">信息类型</param>
        /// <param name="message">信息内容</param>
        public static void Debug(object type, object message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"[{DateTime.Now}][{type}][");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"DEBUG");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("]");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"{message}");
        }
        #endregion
    }
}
