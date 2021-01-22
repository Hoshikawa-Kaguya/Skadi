using AntiRain.DatabaseUtils.SqliteTool;
using AntiRain.TypeEnum;
using SqlSugar;

namespace AntiRain.DatabaseUtils.Helpers.PCRDataDB
{
    /// <summary>
    /// 游戏数据数据库
    /// </summary>
    internal class RediveDBHelper
    {
        #region 属性
        /// <summary>
        /// 数据库路径
        /// </summary>
        private readonly string ResDBPath;
        #endregion

        #region 构造函数
        internal RediveDBHelper()
        {
            this.ResDBPath = SugarUtils.GetDataDBPath(SugarUtils.GlobalResDBName);
        }
        #endregion

        #region 公有方法
        /// <summary>
        /// 获取数据库版本号
        /// </summary>
        /// <param name="server">区服</param>
        internal RediveDBVersion GetVersion(Server server)
        {
            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(ResDBPath);
            return dbClient.Queryable<RediveDBVersion>()
                           .InSingle(server);
        }

        /// <summary>
        /// 更新数据库版本信息
        /// </summary>
        /// <param name="newVersion">新版本信息</param>
        internal bool UpdateVersionInfo(RediveDBVersion newVersion)
        {
            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(ResDBPath);
            //查找版本信息是否存在
            if (dbClient.Queryable<RediveDBVersion>().Where(ver => ver.Server == newVersion.Server).Any())
            {
                return dbClient.Updateable(newVersion).ExecuteCommandHasChange();
            }
            else
            {
                return dbClient.Insertable(newVersion).ExecuteCommand() > 0;
            }
        }
        #endregion
    }
}
