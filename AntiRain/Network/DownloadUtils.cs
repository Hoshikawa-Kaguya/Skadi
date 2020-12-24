using System;
using System.Net;
using AntiRain.DatabaseUtils.SqliteTool;
using AntiRain.DatabaseUtils.Tables;
using AntiRain.IO;
using AntiRain.Tool;
using AntiRain.TypeEnum;
using Newtonsoft.Json.Linq;
using PyLibSharp.Requests;
using Sora.Tool;

namespace AntiRain.Network
{
    internal static class DownloadUtils
    {
        /// <summary>
        /// 获取数据库版本信息
        /// </summary>
        /// <param name="server">区服</param>
        /// <param name="version">版本信息</param>
        /// <returns>获取是否成功</returns>
        internal static bool GetRediveVersion(Server server, out RediveDBVersion version)
        {
            ReqResponse response;
            try
            {
                //请求不同区服的版本信息
                switch (server)
                {
                    case Server.JP:
                        response = Requests.Get("https://api.redive.lolikon.icu/json/lastver_jp.json",
                                              new ReqParams {Timeout = 5000});
                        break;
                    case Server.CN:
                        response = Requests.Get("https://api.redive.lolikon.icu/json/lastver_cn.json",
                                                new ReqParams {Timeout = 5000});
                        break;
                    case Server.TW:
                        response = Requests.Get("https://api.redive.lolikon.icu/json/lastver_tw.json",
                                                new ReqParams {Timeout = 5000});
                        break;
                    default:
                        ConsoleLog.Error("区服标识错误",$"不存在的区服标识[{server}]");
                        version = null;
                        return false;
                }
                //判断返回状态码
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    ConsoleLog.Error("redive数据更新",$"获取[{server}]版本信息失败[{(int)response.StatusCode} {response.StatusCode}]");
                    version = null;
                    return false;
                }
            }
            catch (Exception e)
            {
                ConsoleLog.Error("redive数据更新",$"获取[{server}]版本号发生错误{ConsoleLog.ErrorLogBuilder(e)}");
                version = null;
                return false;
            }
            //反序列化版本信息
            JToken verInfo = response.Json();
            version = new RediveDBVersion
            {
                Server  = server,
                Version = Convert.ToInt64(verInfo["TruthVersion"]),
                Hash    = verInfo["hash"]?.ToString()
            };
            return true;
        }

        /// <summary>
        /// 下载最新的数据库并解压
        /// </summary>
        /// <param name="server">区服</param>
        /// <returns>获取是否成功</returns>
        internal static bool DownloadRediveDatabase(Server server)
        {
            ReqResponse response;
            string      databaseName;
            try
            {
                ConsoleLog.Info("数据下载",$"正在下载{server}数据库");
                switch (server)
                {
                    case Server.JP:
                        response = Requests.Get("https://api.redive.lolikon.icu/br/redive_jp.db.br",
                                                new ReqParams {Timeout = 5000});
                        databaseName = SugarUtils.GameDBNameJP;
                        break;
                    case Server.CN:
                        response = Requests.Get("https://api.redive.lolikon.icu/br/redive_cn.db.br",
                                                new ReqParams{Timeout = 5000});
                        databaseName = SugarUtils.GameDBNameCN;
                        break;
                    case Server.TW:
                        response = Requests.Get("https://api.redive.lolikon.icu/br/redive_tw.db.br",
                                                new ReqParams {Timeout = 5000});
                        databaseName = SugarUtils.GameDBNameTW;
                        break;
                    default:
                        ConsoleLog.Error("区服标识错误",$"不存在的区服标识[{server}]");
                        return false;
                }
                //判断返回状态码
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    ConsoleLog.Error("redive数据更新",$"获取[{server}]数据库失败[{(int)response.StatusCode} {response.StatusCode}]");
                    return false;
                }
            }
            catch (Exception e)
            {
                ConsoleLog.Error("redive数据更新",$"获取[{server}]数据库发生错误{ConsoleLog.ErrorLogBuilder(e)}");
                return false;
            }
            ConsoleLog.Info("数据下载",$"下载{server}数据库成功");
            ConsoleLog.Info("数据下载",$"正在解压{server}数据库");
            //解压数据并保存
            return IOUtils.Bytes2File(BotUtils.BrotliDecompress(response.Content),
                                      SugarUtils.GetDataDBPath(databaseName));
        }
    }
}
