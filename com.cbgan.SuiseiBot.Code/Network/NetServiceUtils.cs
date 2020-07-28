using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using com.cbgan.SuiseiBot.Code.Tool;

namespace com.cbgan.SuiseiBot.Code.Network
{
    internal class NetServiceUtils
    {
        #region 网络数据请求工具
        /// <summary>
        /// 从指定Url获取byte数据并转为string输出
        /// </summary>
        /// <param name="url">目标url</param>
        /// <returns>返回一个string</returns>
        public static string GetDataFromURL(string url)
        {
            string pageString;
            WebClient webClient = new WebClient
            {
                Credentials = CredentialCache.DefaultCredentials
            };
            try
            {
                Byte[] pageData = webClient.DownloadData(url);
                MemoryStream ms = new MemoryStream(pageData);

                using (StreamReader sr = new StreamReader(ms,Encoding.GetEncoding("GB2312")))
                {
                    pageString = sr.ReadLine();
                }
            }
            catch (Exception e)
            {
                ConsoleLog.Error("网络线程错误",$"下载文件时发生错误\n{e}");
                throw e;
            }
            return pageString;
        }

        #endregion
    }
}
