using com.cbgan.SuiseiBot.Code.Tool;
using Native.Sdk.Cqp;
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
        public static Func<CQApi, string, string> GetBinFilePath = (cqApi, filename) => $"{Directory.GetCurrentDirectory()}\\bin\\{filename}";

        /// <summary>
        /// 解压程序
        /// </summary>
        /// <param name="LocalDBPath"></param>
        /// <param name="BinPath"></param>
        public static void decompressDBFile(string LocalDBPath, string BinPath)
        {
            string InputFile = LocalDBPath + @"redive_cn.db.br";
            string outputFilePath = LocalDBPath;
            string outputFileName = @"redive_cn.db";

            if (!File.Exists(outputFilePath + outputFileName))
            {
                try
                {
                    System.Diagnostics.Process.Start(BinPath, "-bd "+ InputFile + " " + outputFilePath + " " + outputFileName);
                    //GC.Collect();
                }
                catch
                {
                    ConsoleLog.Error("BOSS信息数据库", "BOSS信息数据库解压错误，请检查文件路径");
                }
            }
        }
        #endregion
    }
}
