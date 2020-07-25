using Native.Sdk.Cqp;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Reflection;

namespace com.cbgan.SuiseiBot.Code.SqliteTool
{
    /// <summary>
    /// SQLite数据库ORM工具类
    /// 用于完成对数据库的基本操作
    /// </summary>
    internal static class SugarUtils
    {
        #region IO辅助函数
        /// <summary>
        /// 创建新的数据库文件
        /// </summary>
        /// <returns>true 创建成功 false 创建失败</returns>
        public static bool CreateNewSQLiteDBFile(SqlSugarClient sugarClient)
        {
            if (sugarClient == null) throw new NullReferenceException("null SqlSugarClient");
            //找到数据路径字符串
            string[] connectionArgs = sugarClient.CurrentConnectionConfig.ConnectionString.Split('=');
            if (!connectionArgs[0].Equals("DATA SOURCE") || sugarClient.CurrentConnectionConfig.DbType != SqlSugar.DbType.Sqlite)
                throw new ArgumentOutOfRangeException("Unsupported dbtype");
            try
            {
                if (File.Exists(connectionArgs[1])) return false;
                SQLiteConnection.CreateFile(connectionArgs[1]);
                return true;
            }
            catch (Exception e)
            {
                throw new IOException("Create new dbfile failed(" + connectionArgs[1] + ")\n" + e.Message);
            }
        }

        /// <summary>
        /// 获取当前数据库的绝对路径
        /// </summary>
        public static Func<CQApi, string> GetDBPath = (cqApi) => $"{Directory.GetCurrentDirectory()}\\data\\{cqApi.GetLoginQQ()}\\suisei.db";
        #endregion

        #region 表辅助函数

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
            try
            {
                if (sugarClient.Ado.Connection.State != System.Data.ConnectionState.Open)
                    throw new InvalidOperationException("Database not ready");
                using (IDbCommand cmd = sugarClient.Ado.Connection.CreateCommand())
                {
                    //检查表名
                    if (string.IsNullOrEmpty(tableName)) tableName = SugarTableUtils.GetTableName<TableClass>();
                    //写入创建新表指令
                    cmd.CommandText = $"CREATE TABLE {tableName} (";
                    PropertyInfo[] properties = typeof(TableClass).GetProperties();
                    int i = 0;
                    foreach (PropertyInfo colInfo in properties)
                    {
                        i++;
                        //写入字段信息
                        cmd.CommandText +=
                            $"{SugarColUtils.GetColName(colInfo)} " +
                            $"{SugarColUtils.GetColType(colInfo)} " +
                            $"{SugarColUtils.ColIsNullable(colInfo)} " +
                            $"{SugarColUtils.ColIsIdentity(colInfo)}";
                        if (i != properties.Length) cmd.CommandText += ",";
                    }
                    List<string> primaryKeys = SugarColUtils.GetColPrimaryKeys<TableClass>();
                    if (primaryKeys != null)//当有主键时
                    {
                        cmd.CommandText +=
                            $",PRIMARY KEY({string.Join(",", primaryKeys)})";
                    }
                    cmd.CommandText += ")";
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (Exception) { throw; }
        }

        /// <summary>
        /// 查找是否存在同名表
        /// </summary>
        /// <typeparam name="TableClass">自定义表格类</typeparam>
        /// <param name="sugarClient">SqlSugarClient</param>
        /// <param name="tableName">表名</param>
        /// <returns>返回True/False代表是否存在</returns>
        public static bool TableExists<TableClass>(SqlSugarClient sugarClient, string tableName = null)
        {
            if (sugarClient == null) throw new NullReferenceException("null SqlSugarClient");
            if (sugarClient.Ado.Connection.State != System.Data.ConnectionState.Open)
                throw new InvalidOperationException("Database not ready");
            try
            {
                using (IDbCommand cmd = sugarClient.Ado.Connection.CreateCommand())
                {
                    //检查是否有可变表名
                    if (string.IsNullOrEmpty(tableName)) tableName = SugarTableUtils.GetTableName<TableClass>();
                    //查找是否有同名表
                    cmd.CommandText = $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{tableName}'";
                    return Convert.ToBoolean(cmd.ExecuteScalar());
                }
            }
            catch (Exception) { throw; }
        }

        /// <summary>
        /// 删除表，返回影响的记录数
        /// </summary>
        /// <typeparam name="TableClass">自定义表格类</typeparam>
        /// <param name="sugarClient">sugarClient</param>
        /// <returns>影响的记录数</returns>
        public static int DeleteTable<TableClass>(SqlSugarClient sugarClient)
        {
            if (sugarClient == null) throw new NullReferenceException("null SqlSugarClient");
            if (sugarClient.Ado.Connection.State != System.Data.ConnectionState.Open)
                throw new InvalidOperationException("Database not ready");
            try
            {
                using (IDbCommand cmd = sugarClient.Ado.Connection.CreateCommand())
                {
                    cmd.CommandText = $"DROP TABLE IF EXISTS {SugarTableUtils.GetTableName<TableClass>()}";
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (Exception) { throw; }
        }

        /// <summary>
        /// 删除表，返回影响的记录数
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="sugarClient">sugarClient</param>
        /// <returns>影响的记录数</returns>
        public static int DeleteTable(SqlSugarClient sugarClient, string tableName)
        {
            if (sugarClient == null) throw new NullReferenceException("null SqlSugarClient");
            if (sugarClient.Ado.Connection.State != System.Data.ConnectionState.Open)
                throw new InvalidOperationException("Database not ready");
            try
            {
                using (IDbCommand cmd = sugarClient.Ado.Connection.CreateCommand())
                {
                    cmd.CommandText = $"DROP TABLE IF EXISTS {tableName}";
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (Exception) { throw; }
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
            if (sugarClient.Ado.Connection.State != System.Data.ConnectionState.Open)
                throw new InvalidOperationException("Database not ready");
            //使用Ado获取控制台执行指令
            using (IDbCommand cmd = sugarClient.Ado.Connection.CreateCommand())
            {
                cmd.CommandText = command;
                return cmd.ExecuteNonQuery();
            }
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
            SqlSugarClient dbClient = new SqlSugarClient(new ConnectionConfig()
            {
                ConnectionString      = $"DATA SOURCE={DBPath}",
                DbType                = SqlSugar.DbType.Sqlite,
                IsAutoCloseConnection = true,
                InitKeyType           = InitKeyType.Attribute
            });
            dbClient.Open();
            return dbClient;
        }
        #endregion
    }
}
