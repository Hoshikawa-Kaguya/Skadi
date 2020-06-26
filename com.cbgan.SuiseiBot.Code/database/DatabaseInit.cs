using com.cbgan.SuiseiBot.Code.SqliteTool;
using com.cbgan.SuiseiBot.Code.Tool;
using Native.Sdk.Cqp.EventArgs;
using SqlSugar;
using System.IO;

namespace com.cbgan.SuiseiBot.Code.Database
{
    internal static class DatabaseInit//数据库初始化类
    {
        public static void Init(CQAppEnableEventArgs e)
        {
            string DBPath = System.IO.Directory.GetCurrentDirectory() + "\\data\\" + e.CQApi.GetLoginQQ() + "\\suisei.db";
            SqlSugarClient dbClient = new SqlSugarClient(new ConnectionConfig()
            {
                ConnectionString = $"DATA SOURCE={DBPath}",
                DbType = DbType.Sqlite,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });
            if (!File.Exists(DBPath))//查找数据文件
            {
                ConsoleLog.Warning("数据库初始化", "未找到数据库文件，创建新的数据库");
                SugarUtils.CreateNewSQLiteDBFile(dbClient);
            }
            dbClient.Open();//开始数据库连接
            if (!SugarUtils.TableExists<SuiseiData>(dbClient)) //彗酱数据库初始化
            {
                ConsoleLog.Warning("数据库初始化", "未找到慧酱数据表 - 创建一个新表");
                SugarUtils.CreateTable<SuiseiData>(dbClient);
            }
            if (!SugarUtils.TableExists<GuildData>(dbClient)) //公会数据库初始化
            {
                ConsoleLog.Warning("数据库初始化", "未找到公会表数据表 - 创建一个新表");
                SugarUtils.CreateTable<GuildData>(dbClient);
            }
            if (!SugarUtils.TableExists<MemberData>(dbClient)) //公会成员数据库初始化
            {
                ConsoleLog.Warning("数据库初始化", "未找到成员表数据表 - 创建一个新表");
                SugarUtils.CreateTable<MemberData>(dbClient);
            }
        }
    }
}
