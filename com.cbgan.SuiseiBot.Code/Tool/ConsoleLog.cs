using System;
using System.Text;
using System.Threading.Tasks;

namespace com.cbgan.SuiseiBot.Code.Tool
{
    /// <summary>
    /// 用来格式化输出的控制台Log的通用代码
    /// by 饼干（摸了
    /// </summary>
    internal class ConsoleLog
    {
        #region 格式化错误Log

        public static string ErrorLogBuilder(Exception e)
        {
            StringBuilder errorMessageBuilder = new StringBuilder();
            errorMessageBuilder.Append("\r\n");
            errorMessageBuilder.Append("==============ERROR==============\r\n");
            errorMessageBuilder.Append("Error:");
            errorMessageBuilder.Append(e.GetType().FullName);
            errorMessageBuilder.Append("\r\n\r\n");
            errorMessageBuilder.Append("Message:");
            errorMessageBuilder.Append(e.Message);
            errorMessageBuilder.Append("\r\n\r\n");
            errorMessageBuilder.Append("Stack Trace:\r\n");
            errorMessageBuilder.Append(e.StackTrace);
            errorMessageBuilder.Append("\r\n");
            errorMessageBuilder.Append("=================================\r\n");
            return errorMessageBuilder.ToString();
        }

        #endregion

        #region 格式化控制台Log函数

        /// <summary>
        /// 向控制台发送Info信息
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="message">信息内容</param>
        public static void Info(object type, object message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"[{DateTime.Now}][INFO][{type}]{message}");
        }

        /// <summary>
        /// 向控制台发送Warning信息
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="message">信息内容</param>
        public static void Warning(object type, object message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"[{DateTime.Now}][");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("WARNINIG");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"][{type}]");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{message}");
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// 向控制台发送Error信息
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="message">信息内容</param>
        public static void Error(object type, object message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"[{DateTime.Now}][");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("ERROR");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"][{type}]");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// 向控制台发送Fatal信息
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="message">信息内容</param>
        public static void Fatal(object type, object message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"[{DateTime.Now}][");
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.Write("FATAL");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"][{type}]");
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// 向控制台发送Debug信息
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="message">信息内容</param>
        public static void Verbose(object type, object message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"[{DateTime.Now}][");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("Verbose");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"][{type}]");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// 向控制台发送Debug信息
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="message">信息内容</param>
        public static void Debug(object type, object message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"[{DateTime.Now}][");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("DEBUG");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"][{type}]");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        #endregion
    }
}