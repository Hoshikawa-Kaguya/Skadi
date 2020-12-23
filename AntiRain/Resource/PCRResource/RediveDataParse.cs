using System.Collections.Generic;
using AntiRain.DatabaseUtils;
using AntiRain.DatabaseUtils.Helpers.PCRDataDB;
using AntiRain.IO.Config;
using AntiRain.IO.Config.ConfigModule;
using AntiRain.Network;
using AntiRain.TypeEnum;
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
            Dictionary<Server, RediveDBVersion> cloudVers = new Dictionary<Server, RediveDBVersion>();
            //从服务器获取数据
            ConsoleLog.Info("redive数据更新检查","尝试从云端获取版本信息");
            foreach (Server server in globalConfig.ResourceConfig.PCRDatabaseArea)
            {
                //获取版本信息
                if (DownloadUtils.GetRediveVersion(server, out RediveDBVersion cloudVersion))
                    cloudVers.Add(server, cloudVersion);
            }
            //需要更新的列表
            foreach (var version in cloudVers)
            {
                RediveDBVersion localVersion = RediveDbHelper.GetVersion(version.Key);
                ConsoleLog.Debug("redive数据库版本检查",$"[{version.Key}] local:{localVersion?.Version ?? 0} cloud:{version.Value.Version}");
                //对比版本号并下载更新
                if (localVersion         != null                  &&
                    localVersion.Version >= version.Value.Version) continue;
                if (DownloadUtils.DownloadRediveDatabase(version.Key))
                {
                    bool dbsuccess = RediveDbHelper.UpdateVersionInfo(new RediveDBVersion
                    {
                        Server  = version.Key,
                        Version = version.Value.Version,
                        Hash    = version.Value.Hash
                    });
                    if(!dbsuccess) ConsoleLog.Error("redive数据库版本检查","更新数据库信息时发生未知错误");
                }
            }
            return true;
        }
        #endregion
    }
}
