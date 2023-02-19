using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using Skadi.Services;
using SqlSugar;
using DbType = SqlSugar.DbType;

namespace Skadi.Database.SqliteTool;

/// <summary>
/// SQLite数据库ORM工具类
/// 用于完成对数据库的基本操作
/// </summary>
internal static class SugarUtils
{
#region 数据库常量

    //资源数据库名
    public const string GLOBAL_RES_DB_NAME = "res";

#endregion

#region 表辅助函数

    /// <summary>
    /// 删除表
    /// </summary>
    /// <param name="tableType">自定义表格类</param>
    /// <param name="sugarClient">SqlSugarClient</param>
    /// <param name="tableName">表名，为空值则为默认值</param>
    /// <returns>影响的记录数</returns>
    public static int DeleteTable(this Type tableType, SqlSugarClient sugarClient, string tableName = null)
    {
        if (sugarClient == null)
            throw new NullReferenceException("null SqlSugarClient");
        using IDbCommand cmd = sugarClient.Ado.Connection.CreateCommand();
        //检查表名
        if (string.IsNullOrEmpty(tableName))
            tableName = tableType.GetTableName();
        cmd.CommandText = $"DROP TABLE {tableName}";
        //检查数据库链接
        sugarClient.Ado.CheckConnection();
        int ret = cmd.ExecuteNonQuery();
        if (sugarClient.CurrentConnectionConfig.IsAutoCloseConnection)
            sugarClient.Close();
        return ret;
    }

    /// <summary>
    /// 创建新表，返回影响的记录数
    /// 本方法只用于创建包含联合主键的表
    /// </summary>
    /// <param name="tableType">自定义表格类</param>
    /// <param name="sugarClient">SqlSugarClient</param>
    /// <param name="tableName">表名，为空值则为默认值</param>
    /// <returns>影响的记录数</returns>
    public static int CreateTable(this Type tableType, SqlSugarClient sugarClient, string tableName = null)
    {
        if (sugarClient == null)
            throw new NullReferenceException("null SqlSugarClient");
        using IDbCommand cmd = sugarClient.Ado.Connection.CreateCommand();
        //检查表名
        if (string.IsNullOrEmpty(tableName))
            tableName = tableType.GetTableName();
        //写入创建新表指令
        cmd.CommandText = $"CREATE TABLE {tableName} (";
        PropertyInfo[] properties   = tableType.GetProperties();
        int            i            = 0;
        List<string>   primaryKeys  = new();
        bool           haveIdentity = false;
        foreach (var colInfo in properties)
        {
            i++;
            //写入字段信息
            cmd.CommandText +=
                $"{SugarColUtils.GetColName(colInfo)} "
                + $"{SugarColUtils.GetColType(colInfo)} "
                + $"{SugarColUtils.ColIsNullable(colInfo)} "
                + $"{SugarColUtils.ColIsIdentity(colInfo)}";
            if (i != properties.Length)
                cmd.CommandText += ",";
            if (SugarColUtils.ColIsPrimaryKey(colInfo) && string.IsNullOrEmpty(SugarColUtils.ColIsIdentity(colInfo))
               )
                primaryKeys.Add(SugarColUtils.GetColName(colInfo));
            if (!string.IsNullOrEmpty(SugarColUtils.ColIsIdentity(colInfo)))
                haveIdentity = true;
        }

        if (primaryKeys.Count != 0 && !haveIdentity) //当有多主键时
            cmd.CommandText +=
                $",PRIMARY KEY({string.Join(",", primaryKeys)})";

        cmd.CommandText += ")";
        //检查数据库链接
        sugarClient.Ado.CheckConnection();
        int ret = cmd.ExecuteNonQuery();
        if (!sugarClient.CurrentConnectionConfig.IsAutoCloseConnection)
            sugarClient.Close();
        return ret;
    }

    /// <summary>
    /// 查找是否存在同名表
    /// </summary>
    /// <param name="tableType">自定义表格类</param>
    /// <param name="sugarClient">SqlSugarClient</param>
    /// <param name="tableName">表名可为空</param>
    /// <returns>返回True/False代表是否存在</returns>
    public static bool TableExists(this Type tableType, SqlSugarClient sugarClient, string tableName = null)
    {
        if (sugarClient == null)
            throw new NullReferenceException("null SqlSugarClient");
        //检查表名
        if (string.IsNullOrEmpty(tableName))
            tableName = tableType.GetTableName();
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
        if (sugarClient == null)
            throw new NullReferenceException("null SqlSugarClient");
        //使用Ado获取控制台执行指令
        using IDbCommand cmd = sugarClient.Ado.Connection.CreateCommand();
        cmd.CommandText = command;
        //检查数据库链接
        sugarClient.Ado.CheckConnection();
        int ret = cmd.ExecuteNonQuery();
        if (!sugarClient.CurrentConnectionConfig.IsAutoCloseConnection)
            sugarClient.Close();
        return ret;
    }

#endregion

#region Client简单创建函数

    /// <summary>
    /// 创建一个SQLiteClient
    /// </summary>
    /// <param name="loginUid">登录账户ID</param>
    /// <returns>默认开启的SqlSugarClient</returns>
    internal static SqlSugarClient CreateUserDbClient(long loginUid)
    {
        string dbPath = GenericStorage.GetUserDbPath(loginUid);
        return new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString      = $"DATA SOURCE={dbPath}",
            DbType                = DbType.Sqlite,
            IsAutoCloseConnection = true,
            InitKeyType           = InitKeyType.Attribute
        });
    }

#endregion
}