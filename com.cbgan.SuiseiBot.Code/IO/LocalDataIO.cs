using com.cbgan.SuiseiBot.Code.Tool;
using Native.Sdk.Cqp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.cbgan.SuiseiBot.Code.IO
{
    internal static class LocalDataIO
    {
        #region IO工具
        /// <summary>
        /// 获取数据文件路径
        /// </summary>
        public static Func<CQApi, string, string> GetBinFilePath = (cqApi, filename) => $@"{Directory.GetCurrentDirectory()}\bin\{filename}";

        /// <summary>
        /// 获取数据文件路径
        /// </summary>
        public static Func<string> GetBinPath = () => $@"{Directory.GetCurrentDirectory()}\bin\";

        /// <summary>
        /// 获取数据文件路径
        /// </summary>
        public static Func<CQApi, string, string> GetLocalFilePath = (cqApi, filename) => $@"{Directory.GetCurrentDirectory()}\data\{cqApi.GetLoginQQ()}\{filename}";
        #endregion

        #region 文件读取工具
        /// <summary>
        /// 读取Json文件并返回为一个JObject
        /// </summary>
        /// <param name="jsonPath">json文件路径</param>
        /// <param name="jsonName">json文件名称</param>
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
                ConsoleLog.Error("IO ERROR",$"读取文件{jsonPath}时出错，错误：\n{e}");
                throw e;
            }
        }

        #endregion

        #region 文件处理工具
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
                catch(Exception e)
                {
                    ConsoleLog.Error("BOSS信息数据库", $"BOSS信息数据库解压错误，请检查文件路径 错误:\n{e}");
                }
            }
        }
        #endregion
    }
}
