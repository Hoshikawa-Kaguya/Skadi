using com.cbgan.SuiseiBot.Code.Tool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace com.cbgan.SuiseiBot.Code.IO
{
    internal static class NetDataIO
    {
        #region 网络文件IO
        public static int DownloadFileFromURL(string url, string receivePath, string re)
        {
            try
            {
                ConsoleLog.Info("网络Boss数据库", "开始从网络下载数据");
                Thread downloadThread = new Thread((obj) =>
                {
                    WebClient client = new WebClient();
                    client.DownloadFile(url, receivePath + System.IO.Path.GetFileName(url));
                });
                downloadThread.Start();
                ConsoleLog.Info("网络Boss数据库", "网络下载数据成功");
                return 1;
            }
            catch (Exception e)
            {
                ConsoleLog.Error("网络Boss数据库", "网络下载数据错误");
                throw;
            }
        }

        #endregion
    }
}
