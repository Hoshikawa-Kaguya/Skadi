using com.cbgan.SuiseiBot.Code.IO;
using com.cbgan.SuiseiBot.Code.SqliteTool;
using com.cbgan.SuiseiBot.Code.Tool;
using Native.Sdk.Cqp.EventArgs;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Data.SqlClient;
using System.Data.SQLite;
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
        public readonly static string BossTableName = "boss_info";  //公会数据库表名
        public readonly static string PeriodTableName = "clan_battle_period";  //Boss期表名
        public readonly static string PhaseTableName = "clan_battle_map_data";  //Boss阶段表名
        public readonly static string BossGroupTableName = "clan_battle_boss_group";  //Boss组表名
        public readonly static string WaveTableName = "wave_group_data";  //BossWave表名
        public readonly static string EnemyPropertyTableName = "enemy_parameter";  //Boss属性表名
        public readonly static string EnemyCommentTableName = "unit_enemy_data";  //Boss描述表名
        //public readonly static string MemberTableName = "member"; //成员数据库表名
        private static string DBPath;//数据库保存路径（suisei.db）
        private static string BinPath;//二进制文件路径
        private static string LocalDBPath;//原boss数据库保存路径
        #endregion

        #region 构造函数
        public BossDBHelper(object sender, CQGroupMessageEventArgs eventArgs)
        {
            this.Sender = sender;
            this.EventArgs = eventArgs;
            this.GroupId = eventArgs.FromGroup.Id;
            BinPath = LocalDataIO.GetBinFilePath(eventArgs.CQApi, @"BrotliParser.exe");
            DBPath = SugarUtils.GetDBPath(eventArgs.CQApi);
            LocalDBPath = SugarUtils.GetLocalPath(eventArgs.CQApi);
            decompressDBFile();
        }
        #endregion

        #region 辅助数据结构
        private readonly string[] periodColName = new string[] { "clan_battle_id", "start_time" };

        private readonly string[] phaseColName = new string[] { "clan_battle_id" };

        private readonly string[] groupColName = new string[] { "clan_battle_boss_group_id" };

        private readonly string[] waveColName = new string[] { "wave_group_id"};

        private readonly string[] enemyColName = new string[] { "enemy_id" };

        private readonly string[] unitColName = new string[] { "unit_id" };
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
                    //GC.Collect();
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
                    LoadBossInfo();
                    return 0;
                }
            }
        }

        private int LoadBossInfo()
        {
            string BossDBPath = LocalDBPath + @"redive_cn.db";
            try
            {
                SQLiteHelper dbHelper = new SQLiteHelper(BossDBPath);//TODO 改用ORM
                dbHelper.OpenDB();

                List<string> periods = dbHelper.GetIdInTime(PeriodTableName, new string[] { "clan_battle_id", "start_time" }, 6);
                if ((periods == null)||(periods.Count != phaseColName.Length)) //是否有符合要求的会战信息
                {
                    //未找到，输出错误信息
                    dbHelper.CloseDB();
                    ConsoleLog.Error("会战管理数据库", "未找到符合要求的BOSS信息，请检查会战开放日期");
                    return -1;
                }
                else //找到，进行下一步查询，只要找到Period，那么下面的所有查询都能实现，因此不考虑找不到的情况
                {
                    SQLiteDataReader dr =  dbHelper.FindRow(PhaseTableName, phaseColName, periods.ToArray());
                    List<List<string>> phases = new List<List<string>>();
                    while (dr.Read())
                    {
                        List<string> row = new List<string>();
                        row.Add(dr["clan_battle_boss_group_id"].ToString());
                        row.Add(dr["lap_num_from"].ToString());
                        row.Add(dr["lap_num_to"].ToString());
                        phases.Add(row);
                    }


                    List<List<List<string>>> enemies = new List<List<List<string>>>();

                    int rowIdx = 0;
                    foreach(var row in phases)
                    {
                        List<List<string>> groups = new List<List<string>>();
                        SQLiteDataReader groupdr = dbHelper.FindRow(BossGroupTableName, groupColName, new string[] { phases[rowIdx][0] });
                        int order = 1;
                        while(groupdr.Read())
                        {
                            List<string> enemy = new List<string>();
                            enemy.Add($"{rowIdx+1}");
                            enemy.Add(phases[rowIdx][1]);
                            enemy.Add(phases[rowIdx][2]);
                            enemy.Add(groupdr["scale_ratio"].ToString());
                            enemy.Add($"{order++}");
                            enemy.Add(groupdr["wave_group_id"].ToString());
                            SQLiteDataReader wavedr = dbHelper.FindRow(WaveTableName, waveColName, new string[] { enemy[enemy.Count - 1] });
                            enemy.RemoveAt(enemy.Count - 1);
                            enemy.Add(wavedr["enemy_id_1"].ToString());
                            groups.Add(enemy);
                        }
                        rowIdx++;
                    }

                    foreach(var groups in enemies)
                    {
                        foreach(var enemy in groups)
                        {
                            SQLiteDataReader enemydr = dbHelper.FindRow(EnemyPropertyTableName, waveColName, new string[] { enemy[enemy.Count - 1] });
                            enemy.RemoveAt(enemy.Count - 1);
                            while(enemydr.Read())
                            {
                                enemy.Add(enemydr["name"].ToString());
                                enemy.Add(enemydr["hp"].ToString());
                                enemy.Add(enemydr["atk"].ToString());
                                enemy.Add(enemydr["magic_str"].ToString());
                                enemy.Add(enemydr["def"].ToString());
                                enemy.Add(enemydr["magic_def"].ToString());
                                enemy.Add(enemydr["unit_id"].ToString());
                                SQLiteDataReader unitdr = dbHelper.FindRow(WaveTableName, waveColName, new string[] { enemy[enemy.Count - 1] });
                                enemy.RemoveAt(enemy.Count - 1);
                                enemy.Add(unitdr["comment"].ToString());
                            }

                        }
                    }


                    //这里需要把enemies按行添加到数据库中

                    dbHelper.CloseDB();
                    ConsoleLog.Info("会战管理数据库", "会战BOSS信息加载完成");
                    return 0;
                }
            }
            catch (Exception)
            {
                ConsoleLog.Error("会战管理数据库","会战BOSS信息加载出错，请检查");
                throw;
                
            }
        }
        #endregion
    }
}
