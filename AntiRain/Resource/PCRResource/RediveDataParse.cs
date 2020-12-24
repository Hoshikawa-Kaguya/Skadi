using System;
using AntiRain.DatabaseUtils;
using AntiRain.DatabaseUtils.Helpers.PCRDataDB;
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
        #endregion

        #region 构造函数
        internal RediveDataParse()
        {
            RediveDbHelper = new RediveDBHelper();
        }
        #endregion

        #region 公有方法
        internal bool UpdateRediveData()
        {
            //从服务器获取数据
            ConsoleLog.Info("redive数据更新检查","尝试从云端获取版本信息");
            //更新数据库
            foreach (Server server in Enum.GetValues<Server>())
            {
                if (!DownloadUtils.GetRediveVersion(server, out RediveDBVersion cloudVersion)) continue;
                RediveDBVersion localVersion = RediveDbHelper.GetVersion(server);
                ConsoleLog.Debug("redive数据库版本检查",$"[{server}] local:{localVersion?.Version ?? 0} cloud:{cloudVersion.Version}");
                //对比版本号并下载更新
                if (localVersion         != null                  &&
                    localVersion.Version >= cloudVersion.Version) continue;
                if (!DownloadUtils.DownloadRediveDatabase(server)) continue;
                bool dbsuccess = RediveDbHelper.UpdateVersionInfo(new RediveDBVersion
                {
                    Server  = server,
                    Version = cloudVersion.Version,
                    Hash    = cloudVersion.Hash
                });
                if(!dbsuccess) ConsoleLog.Error("redive数据库版本检查","更新数据库信息时发生未知错误");
                GC.Collect();
            }
            return true;
        }
        #endregion
    }
}
