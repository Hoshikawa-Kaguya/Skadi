using System;
using System.Globalization;
using System.IO;
using com.cbgan.SuiseiBot.Code.Tool;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SuiseiBot.IO
{
    internal static class IOUtils
    {
        #region IO工具
        /// <summary>
        /// 获取错误报告路径
        /// </summary>
        public static string GetCrashLogPath()
        {
            string path = $@"{Directory.GetCurrentDirectory()}\crash".Replace('\\', '/');
            //检查目录是否存在，不存在则新建一个
            Directory.CreateDirectory(path);
            return path;
        }

        //TODO 改用自己的文件存储方式
        // /// <summary>
        // /// 获取配置文件路径
        // /// </summary>
        // public static string GetUserConfigPath(CQApi cqApi)
        // {
        //     Directory.CreateDirectory($@"{cqApi.AppDirectory}\configs\{cqApi.GetLoginQQ()}\");
        //     return $@"{cqApi.AppDirectory}\configs\{cqApi.GetLoginQQ()}\";
        // }
        //
        // /// <summary>
        // /// 获取全局配置文件路径
        // /// </summary>
        // public static string GetGlobalConfigPath(CQApi cqApi)
        // {
        //     Directory.CreateDirectory($@"{cqApi.AppDirectory}\configs\global\");
        //     return $@"{cqApi.AppDirectory}\configs\global\";
        // }

        /// <summary>
        /// 创建错误报告文件
        /// </summary>
        /// <param name="errorMessage">错误信息</param>
        public static void CrashLogGen(string errorMessage)
        {
            string fileName = $"{DateTime.Now.ToString(CultureInfo.CurrentCulture).Replace('/', '-').Replace(':','-')}.log";
            using StreamWriter streamWriter = File.CreateText($@"{GetCrashLogPath()}/{fileName}");
            streamWriter.Write(errorMessage);
        }
        #endregion

        #region 文件读取工具
        /// <summary>
        /// 读取Json文件并返回为一个JObject
        /// </summary>
        /// <param name="jsonPath">json文件路径</param>
        /// <returns>保存整个文件信息的JObject</returns>
        public static JObject LoadJsonFile(string jsonPath)
        {
            try
            {
                StreamReader jsonFile = File.OpenText(jsonPath);
                JsonTextReader reader = new JsonTextReader(jsonFile);
                JObject jsonObject = (JObject)JToken.ReadFrom(reader);
                return jsonObject;
            }
            catch (Exception e)
            {
                ConsoleLog.Error("IO ERROR", $"读取文件{jsonPath}时出错，错误：\n{ConsoleLog.ErrorLogBuilder(e)}");
                return null;
            }
        }

        #endregion

        #region 文件处理工具
        //TODO 转为内嵌代码，不再调用外部工具
        /// <summary>
        /// 解压程序，解压出的文件和原文件同路径
        /// </summary>
        /// <param name="LocalDBPath">数据文件路径</param>
        /// <param name="BinPath">二进制执行文件路径</param>
        public static void decompressDBFile(string LocalDBPath, string BinPath)
        {
            string InputFile = LocalDBPath + "redive_cn.db.br";
            string outputFilePath = LocalDBPath;
            string outputFileName = "redive_cn.db";

            if (!File.Exists(outputFilePath + outputFileName))
            {
                try
                {
                    System.Diagnostics.Process.Start(BinPath, "-bd " + InputFile + " " + outputFilePath + " " + outputFileName);
                    //GC.Collect();
                }
                catch (Exception e)
                {
                    ConsoleLog.Error("BOSS信息数据库", $"BOSS信息数据库解压错误，请检查文件路径 错误:\n{e}");
                }
            }
        }
        #endregion
    }
}
