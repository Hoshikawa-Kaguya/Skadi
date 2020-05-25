using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.cbgan.SuiseiBot.Code
{
    internal class ConsoleLog
    {
        /// <summary>
        /// 向控制台发送Info信息
        /// </summary>
        /// <param name="type">信息类型</param>
        /// <param name="message">信息内容</param>
        public static void Info(string type,string message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"[{DateTime.Now}][{type}]INFO:{message}");
        }
        /// <summary>
        /// 向控制台发送Warning信息
        /// </summary>
        /// <param name="type">信息类型</param>
        /// <param name="message">信息内容</param>
        public static void Warning(string type, string message)
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
        public static void Error(string type, string message)
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
        public static void Debug(string type, string message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"[{DateTime.Now}][{type}][");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"WARNINIG");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("]");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"{message}");
        }
    }
}
