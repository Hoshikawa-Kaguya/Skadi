using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text;
using SqlSugar;

namespace SuiseiBot.SqliteTool
{
    /// <summary>
    /// SQLite数据库ORM工具类
    /// 用于完成对数据库的基本操作
    /// </summary>
    internal static class SugarUtils
    {
        #region IO辅助函数
        /// <summary>
        /// 获取应用数据库的绝对路径
        /// </summary>
        public static string GetDBPath(string dirName = null)
        {
            StringBuilder dbPath = new StringBuilder();
#if DEBUG
            dbPath.Append(Environment.GetEnvironmentVariable("DebugDataPath"));
#else
            dbPath.Append(Environment.CurrentDirectory);
#endif
            dbPath.Append("/data");
            //自定义二级文件夹
            if (!string.IsNullOrEmpty(dirName)) dbPath.Append($"/{dirName}");
            //检查目录是否存在，不存在则新建一个
            Directory.CreateDirectory(dbPath.ToString());
            dbPath.Append("/data.db");
            return dbPath.ToString();
        }

        /// <summary>
        /// 获取目标数据库的绝对路径
        /// </summary>
        public static string GetCacheDBPath(string dbFileName)
        {
            StringBuilder dbPath = new StringBuilder();
#if DEBUG
            dbPath.Append(Environment.GetEnvironmentVariable("DebugDataPath"));
#else
            dbPath.Append(Environment.CurrentDirectory);
#endif
            dbPath.Append("/cache");
            //检查目录是否存在，不存在则新建一个
            Directory.CreateDirectory(dbPath.ToString());
            dbPath.Append($"/{dbFileName}");
            return dbPath.ToString();
        }
        #endregion

        #region 表辅助函数
        /// <summary>
        /// 删除表
        /// </summary>
        /// <typeparam name="TableClass">自定义表格类</typeparam>
        /// <param name="sugarClient">SqlSugarClient</param>
        /// <param name="tableName">表名，为空值则为默认值</param>
        /// <returns>影响的记录数</returns>
        public static int DeletTable<TableClass>(SqlSugarClient sugarClient, string tableName = null)
        {
            if (sugarClient == null) throw new NullReferenceException("null SqlSugarClient");
            using IDbCommand cmd = sugarClient.Ado.Connection.CreateCommand();
            //检查表名
            if (string.IsNullOrEmpty(tableName)) tableName = SugarTableUtils.GetTableName<TableClass>();
            cmd.CommandText = $"DROP TABLE {tableName}";
            //检查数据库链接
            sugarClient.Ado.CheckConnection();
            int ret = cmd.ExecuteNonQuery();
            if (sugarClient.CurrentConnectionConfig.IsAutoCloseConnection) sugarClient.Close();
            return ret;
        }

        /// <summary>
        /// 创建新表，返回影响的记录数
        /// 本方法只用于创建包含联合主键的表
        /// </summary>
        /// <typeparam name="TableClass">自定义表格类</typeparam>
        /// <param name="sugarClient">SqlSugarClient</param>
        /// <param name="tableName">表名，为空值则为默认值</param>
        /// <returns>影响的记录数</returns>
        public static int CreateTable<TableClass>(SqlSugarClient sugarClient, string tableName = null)
        {
            if (sugarClient == null) throw new NullReferenceException("null SqlSugarClient");
            using IDbCommand cmd = sugarClient.Ado.Connection.CreateCommand();
            //检查表名
            if (string.IsNullOrEmpty(tableName)) tableName = SugarTableUtils.GetTableName<TableClass>();
            //写入创建新表指令
            cmd.CommandText = $"CREATE TABLE {tableName} (";
            PropertyInfo[] properties   = typeof(TableClass).GetProperties();
            int            i            = 0;
            List<string>   primaryKeys  = new List<string>();
            bool           haveIdentity = false;
            foreach (PropertyInfo colInfo in properties)
            {
                i++;
                //写入字段信息
                cmd.CommandText +=
                    $"{SugarColUtils.GetColName(colInfo)} "    +
                    $"{SugarColUtils.GetColType(colInfo)} "    +
                    $"{SugarColUtils.ColIsNullable(colInfo)} " +
                    $"{SugarColUtils.ColIsIdentity(colInfo)}";
                if (i != properties.Length) cmd.CommandText += ",";
                if (SugarColUtils.ColIsPrimaryKey(colInfo) && string.IsNullOrEmpty(SugarColUtils.ColIsIdentity(colInfo))
                ) primaryKeys.Add(SugarColUtils.GetColName(colInfo));
                if(!string.IsNullOrEmpty(SugarColUtils.ColIsIdentity(colInfo))) haveIdentity = true;
            }

            if (primaryKeys.Count != 0 && !haveIdentity) //当有多主键时
            {
                cmd.CommandText +=
                    $",PRIMARY KEY({string.Join(",", primaryKeys)})";
            }
            cmd.CommandText += ")";
            //检查数据库链接
            sugarClient.Ado.CheckConnection();
            int ret = cmd.ExecuteNonQuery();
            if (!sugarClient.CurrentConnectionConfig.IsAutoCloseConnection) sugarClient.Close();
            return ret;
        }

        /// <summary>
        /// 查找是否存在同名表
        /// </summary>
        /// <typeparam name="TableClass">自定义表格类</typeparam>
        /// <param name="sugarClient">SqlSugarClient</param>
        /// <param name="tableName">表名可为空</param>
        /// <returns>返回True/False代表是否存在</returns>
        public static bool TableExists<TableClass>(SqlSugarClient sugarClient, string tableName = null)
        {
            if (sugarClient == null) throw new NullReferenceException("null SqlSugarClient");
            //检查表名
            if (string.IsNullOrEmpty(tableName)) tableName = SugarTableUtils.GetTableName<TableClass>();
            //获取所有表的信息

            //ORM的返回值会返回不存在的表，暂时弃用
            // List<DbTableInfo> tableInfos = sugarClient.DbMaintenance.GetTableInfoList();
            // return tableInfos.Exists(table => table.Name == tableName);

            //检查数据库链接
            sugarClient.Ado.CheckConnection();
            using IDbCommand cmd = sugarClient.Ado.Connection.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = '{tableName}'";
            return Convert.ToBoolean(cmd.ExecuteScalar());
        }
        #endregion

        #region 简单辅助函数
        /// <summary>
        /// 执行sql语句
        /// </summary>
        /// <param name="sugarClient">sugarClient</param>
        /// <param name="command">sql指令</param>
        /// <returns>影响的记录数</returns>
        public static int ExecuteSql(SqlSugarClient sugarClient, string command)
        {
            if (sugarClient == null) throw new NullReferenceException("null SqlSugarClient");
            //使用Ado获取控制台执行指令
            using IDbCommand cmd = sugarClient.Ado.Connection.CreateCommand();
            cmd.CommandText = command;
            //检查数据库链接
            sugarClient.Ado.CheckConnection();
            int ret = cmd.ExecuteNonQuery();
            if (!sugarClient.CurrentConnectionConfig.IsAutoCloseConnection) sugarClient.Close();
            return ret;
        }
        #endregion

        #region Client简单创建函数
        /// <summary>
        /// 创建一个SQLiteClient
        /// </summary>
        /// <param name="DBPath">数据库路径</param>
        /// <returns>默认开启的SqlSugarClient</returns>
        internal static SqlSugarClient CreateSqlSugarClient(string DBPath)
        {
            return new SqlSugarClient(new ConnectionConfig()
            {
                ConnectionString      = $"DATA SOURCE={DBPath}",
                DbType                = SqlSugar.DbType.Sqlite,
                IsAutoCloseConnection = true,
                InitKeyType           = InitKeyType.Attribute
            });
        }
        #endregion
    }
}
