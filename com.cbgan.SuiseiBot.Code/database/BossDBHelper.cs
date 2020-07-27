using com.cbgan.SuiseiBot.Code.IO;
using com.cbgan.SuiseiBot.Code.Network;
using com.cbgan.SuiseiBot.Code.SqliteTool;
using Native.Sdk.Cqp.EventArgs;
using Newtonsoft.Json.Linq;
using SqlSugar;

namespace com.cbgan.SuiseiBot.Code.Database
{
    class BossDBHelper
    {
        #region 参数
        private long GroupId { set; get; } //群号
        public CQGroupMessageEventArgs EventArgs { private set; get; }
        public object Sender { private set; get; }
        //public readonly static string MemberTableName = "member"; //成员数据库表名
        private static string DBPath;//数据库保存路径（suisei.db）
        private static string BinPath;//二进制文件路径
        private static string LocalDBPath;//原boss数据库保存路径

        private static readonly string DBVersionJsonUrl = @"https://redive.estertion.win/last_version_cn.json";
        #endregion

        #region 构造函数
        public BossDBHelper(object sender, CQGroupMessageEventArgs eventArgs)
        {
            this.Sender = sender;
            this.EventArgs = eventArgs;
            this.GroupId = eventArgs.FromGroup.Id;
            BinPath = LocalDataIO.GetBinFilePath(eventArgs.CQApi, "BrotliParser.exe");
            DBPath = SugarUtils.GetDBPath(eventArgs.CQApi);
            LocalDBPath = SugarUtils.GetCacheDBPath(eventArgs.CQApi, "redive_cn.db");
        }
        #endregion

<<<<<<< HEAD
        #region 工具函数(DEBUG)
=======
        #region 辅助数据结构
        private readonly string[] periodColName = new string[] { "clan_battle_id", "start_time" };

        private readonly string[] phaseColName = new string[] { "clan_battle_id" };

        private readonly string[] groupColName = new string[] { "clan_battle_boss_group_id" };

        private readonly string[] waveColName = new string[] { "wave_group_id" };

        private readonly string[] enemyColName = new string[] { "enemy_id" };

        private readonly string[] unitColName = new string[] { "unit_id" };
        #endregion

        #region 工具函数
>>>>>>> origin/watremons
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

        public bool ChechDBVersion()
        {
            string localVersion = JsonUtils.GetKeyData(LocalDataIO.LoadJsonFile(LocalDBPath, @"last_version_cn.json"), "TruthVersions");
            string latestVersion = JsonUtils.GetKeyData(JsonUtils.ConvertJson(NetServiceUtils.GetDataFromURL(DBVersionJsonUrl)), "TruthVersions");
            if (localVersion == latestVersion)
            {
                return true;
            }
            else
            {
                return false;
            }

        }
        #endregion
    }
}
