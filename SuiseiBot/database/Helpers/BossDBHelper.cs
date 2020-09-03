using Native.Sdk.Cqp.EventArgs;
using SqlSugar;
using SuiseiBot.Code.SqliteTool;

namespace SuiseiBot.Code.Database.Helpers
{
    class BossDBHelper
    {
        #region 参数
        private long GroupId { set; get; } //群号
        public CQGroupMessageEventArgs EventArgs { private set; get; }
        public object Sender { private set; get; }
        private static string DBPath { set; get; } //数据库保存路径（suisei.db）
        private static string CacheDBConfigPath { set; get; }
        private static string BinPath { set; get; } //二进制文件路径
        private static string CacheDBPath { set; get; } //原boss数据库保存路径

        private static readonly string DBVersionJsonUrl = @"https://redive.estertion.win/last_version_cn.json";
        #endregion

        #region 构造函数
        // public BossDBHelper(object sender, CQGroupMessageEventArgs eventArgs)
        // {
        //     this.Sender = sender;
        //     this.EventArgs = eventArgs;
        //     this.GroupId = eventArgs.FromGroup.Id;
        //     BinPath = LocalDataIO.GetBinFilePath("BrotliParser.exe");
        //     DBPath = SugarUtils.GetDBPath(eventArgs.CQApi);
        //     CacheDBPath = SugarUtils.GetCacheDBPath(eventArgs.CQApi, "redive_cn.db");
        //     CacheDBConfigPath = LocalDataIO.GetGlobalConfigPath(eventArgs.CQApi) + "last_version_cn.json";
        // }
        #endregion

        #region 工具函数(DEBUG)
        public bool GuildExists()
        {
            bool isExists, isExists2;
            using (SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath))
            {
                isExists = dbClient.Queryable<GuildData>().Where(guild => guild.Gid == 883740678).Any();
                isExists2 = dbClient.Queryable<GuildData>().Where(guild => guild.Gid == 1146619912).Any();
            }
            return isExists || isExists2;
        }
        #endregion

        #region 操作数据库函数

        /// <summary>
        /// 检查游戏的数据库版本
        /// </summary>
        // public Func<bool> ChechRediveDBVersion =
        //     () =>
        //         JsonUtils.GetKeyData(LocalDataIO.LoadJsonFile(CacheDBConfigPath), "TruthVersions") ==
        //         JsonUtils.GetKeyData(JsonUtils.ConvertJson(NetServiceUtils.GetDataFromURL(DBVersionJsonUrl)),
        //                              "TruthVersions");

        #endregion
    }
}
