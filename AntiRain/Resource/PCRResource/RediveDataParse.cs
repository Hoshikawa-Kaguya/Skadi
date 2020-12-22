using System;
using System.Collections.Generic;
using System.Net;
using AntiRain.DatabaseUtils;
using AntiRain.DatabaseUtils.Helpers.PCRDataDB;
using AntiRain.IO.Config;
using AntiRain.IO.Config.ConfigModule;
using AntiRain.TypeEnum;
using Newtonsoft.Json.Linq;
using PyLibSharp.Requests;
using Sora.Tool;

namespace AntiRain.Resource.PCRResource
{
    /// <summary>
    /// redive数据版本更新及处理
    /// </summary>
    internal class RediveDataParse
    {
        #region 属性
        internal RediveDBHelper RediveDbHelper { get; set; }
        /// <summary>
        /// 全局配置文件
        /// </summary>
        private readonly GlobalConfig globalConfig;
        #endregion

        #region 构造函数
        internal RediveDataParse()
        {
            ConfigManager configManager = new ConfigManager();
            configManager.LoadGlobalConfig(out globalConfig);
            RediveDbHelper = new RediveDBHelper();
        }
        #endregion

        #region 公有方法
        internal bool UpdateRediveData()
        {
            Dictionary<Server, ReqResponse> res = new Dictionary<Server, ReqResponse>();
            //从服务器获取数据
            ConsoleLog.Info("redive数据更新检查","尝试从云端获取版本信息");
            foreach (Server server in globalConfig.ResourceConfig.PCRDatabaseArea)
            {
                try
                {
                    switch (server)
                    {
                        case Server.JP:
                            res.Add(Server.JP, Requests.Get("https://api.redive.lolikon.icu/json/lastver_jp.json",
                                                            new ReqParams {Timeout = 5000}));
                            break;
                        case Server.CN:
                            res.Add(Server.CN, Requests.Get("https://api.redive.lolikon.icu/json/lastver_cn.json",
                                                            new ReqParams {Timeout = 5000}));
                            break;
                        case Server.TW:
                            res.Add(Server.TW, Requests.Get("https://api.redive.lolikon.icu/json/lastver_tw.json",
                                                            new ReqParams {Timeout = 5000}));
                            break;
                        default:
                            ConsoleLog.Error("区服标识错误","不存在的区服标识");
                            break;
                    }
                }
                catch (Exception e)
                {
                    ConsoleLog.Error("redive数据更新",$"获取版本号发生错误{ConsoleLog.ErrorLogBuilder(e)}");
                    return false;
                }
            }
            //需要更新的列表
            foreach (var version in res)
            {
                if (version.Value.StatusCode != HttpStatusCode.OK) continue;
                RediveDBVersion localVersion = RediveDbHelper.GetVersion(version.Key);
                JToken          versionData  = version.Value.Json();
                ConsoleLog.Debug("redive数据库版本检查",$"[{version.Key}] local:{localVersion?.Version ?? 0} cloud:{versionData["TruthVersion"]}");
                //对比版本号
                if (localVersion == null || localVersion.Version < Convert.ToInt64(versionData["TruthVersion"]))
                {
                    //TODO 下载数据库更新并解压
                    //TODO 数据库报错
                    //更新数据库记录
                    RediveDbHelper.UpdateVersionInfo(new RediveDBVersion
                    {
                        Server  = version.Key,
                        Version = Convert.ToInt64(versionData["TruthVersion"]),
                        Hash    = versionData["hash"]?.ToString()
                    });
                }
            }
            return false;
        }
        #endregion
    }
}
