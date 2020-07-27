using System;
using System.Net;
using System.Threading;
using com.cbgan.SuiseiBot.Code.Tool;

namespace com.cbgan.SuiseiBot.Code.Network
{
    class DataUpdate
    {
        #region 网络文件IO
        /// <summary>
        /// 从网络URL下载文件保存到本地
        /// </summary>
        /// <param name="url">目标URL</param>
        /// <param name="receivePath">接收文件的地址</param>
        /// <param name="re"></param>
        /// <returns>返回1表示成功</returns>
        public static int DownloadFileFromURL(string url, string receivePath)
        {
            try
            {
                ConsoleLog.Info("网络文件下载", "开始从网络下载文件");
                Thread downloadThread = new Thread((obj) =>
                                                   {
                                                       WebClient client = new WebClient();
                                                       client.DownloadFile(url, receivePath + System.IO.Path.GetFileName(url));
                                                   });
                downloadThread.Start();
                ConsoleLog.Info("网络文件下载", "网络下载数据成功");
                return 1;
            }
            catch (Exception e)
            {
                ConsoleLog.Error("网络Boss数据库", $"网络下载数据错误\n{e}");
                throw;
            }
        }

        #endregion

    }
}
