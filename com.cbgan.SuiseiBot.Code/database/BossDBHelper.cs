using com.cbgan.SuiseiBot.Code.SqliteTool;
using com.cbgan.SuiseiBot.Code.Tool;
using Native.Sdk.Cqp.EventArgs;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.cbgan.SuiseiBot.Code.Database
{
    class BossDBHelper
    {
        #region 参数
        private long GroupId { set; get; } //群号
        private string[] GuildId { set; get; } //公会信息
        public CQGroupMessageEventArgs EventArgs { private set; get; }
        public object Sender { private set; get; }
        private SqlSugarClient OriginDBClient;
        public readonly static string GuildTableName = "guild";  //公会数据库表名
        public readonly static string MemberTableName = "member"; //成员数据库表名
        private static string DBPath;//数据库保存路径
        private static string BinPath;//二进制文件路径
        private static string LocalDBPath;//
        #endregion

        #region 构造函数
        public BossDBHelper(object sender, CQGroupMessageEventArgs eventArgs)
        {
            this.Sender = sender;
            this.EventArgs = eventArgs;
            this.GroupId = eventArgs.FromGroup.Id;
            BinPath = SugarUtils.GetBinFilePath(eventArgs.CQApi, @"BrotliParser.exe");
            DBPath = SugarUtils.GetDBPath(eventArgs.CQApi);
            LocalDBPath = SugarUtils.GetLocalPath(eventArgs.CQApi);
            decompressDBFile();
        }
        #endregion

        #region 工具函数
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
        public static void decompressDBFile()
        {
            string InputFile = LocalDBPath + @"redive_cn.db.br";
            string outputFilePath = LocalDBPath;
            string outputFileName = @"redive_cn.db";

            if (!File.Exists(outputFilePath + outputFileName))
            {
                try
                {
                    System.Diagnostics.Process.Start(BinPath, InputFile+" "+ outputFilePath + " " + outputFileName);
                }
                catch
                {
                    ConsoleLog.Error("BOSS信息数据库","BOSS信息数据库解压错误，请检查文件路径");
                }
            }
        }

        public int StartLoadBoss()
        {
            using (SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath))
            {
                if (SugarUtils.TableExists<BossInfo>(dbClient,
                                                        $"{SugarTableUtils.GetTableName<BossInfo>()}_{GroupId}"))
                {
                    ConsoleLog.Error("会战管理数据库", "Boss信息表已存在，请检查是否结束上一次会战");
                    return -1;
                }
                else
                {
                    SugarUtils.CreateTable<BossInfo>(dbClient,
                                                        $"{SugarTableUtils.GetTableName<BossInfo>()}_{GroupId}");
                    ConsoleLog.Info("会战管理数据库", "开始加载新一期会战数据");

                    //根据PhaseInfoTable的信息创建


                    return 0;
                }
            }
        }
        #endregion
    }
}
