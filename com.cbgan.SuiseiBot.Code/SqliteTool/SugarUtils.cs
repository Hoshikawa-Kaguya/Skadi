using SqlSugar;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;

namespace com.cbgan.SuiseiBot.Code.database
{
    /// <summary>
    /// SQLite数据库ORM工具类
    /// 用于完成对数据库的基本操作
    /// </summary>
    internal class SugarUtils
    {
        /// <summary>
        /// 创建新的数据库文件
        /// </summary>
        /// <returns>true 创建成功 false 创建失败</returns>
        public static bool CreateNewSQLiteDBFile(SqlSugarClient sugarClient)
        {
            if (sugarClient == null) throw new NullReferenceException("null SqlSugarClient");
            //找到数据路径字符串
            string[] connectionArgs = sugarClient.CurrentConnectionConfig.ConnectionString.Split('=');
            if (!connectionArgs[0].Equals("DATA SOURCE")||sugarClient.CurrentConnectionConfig.DbType!=SqlSugar.DbType.Sqlite) 
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
        /// 创建新表，返回影响的记录数
        /// [注意:含有联合主键字段的表字段只支持NOT NULL属性]
        /// </summary>
        /// <typeparam name="TableClass">自定义表格类</typeparam>
        /// <param name="sugarClient">SqlSugarClient</param>
        /// <returns>影响的记录数</returns>
        public static int CreateTable<TableClass>(SqlSugarClient sugarClient)
        {
            if (sugarClient == null) throw new NullReferenceException("null SqlSugarClient");
            try
            {
                if (sugarClient.Ado.Connection.State != System.Data.ConnectionState.Open)
                    throw new InvalidOperationException("Database not ready");
                using (IDbCommand cmd = sugarClient.Ado.Connection.CreateCommand())
                {
                    //写入创建新表指令
                    cmd.CommandText = $"CREATE TABLE {SugarTableUtils.GetTableName<TableClass>()} (";
                    PropertyInfo[] properties = typeof(TableClass).GetProperties();
                    int i = 0;
                    foreach (PropertyInfo colInfo in properties)
                    {
                        i++;
                        //写入字段信息
                        cmd.CommandText +=
                            $"{SugarColUtils.GetColName(colInfo)} " +
                            $"{SugarColUtils.GetColType(colInfo)} " +
                            $"{SugarColUtils.ColIsNullable(colInfo)}";
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
        /// <returns>返回True/False代表是否存在</returns>
        public static bool TableExists<TableClass>(SqlSugarClient sugarClient)
        {
            if (sugarClient == null) throw new NullReferenceException("null SqlSugarClient");
            if (sugarClient.Ado.Connection.State != System.Data.ConnectionState.Open)
                throw new InvalidOperationException("Database not ready");
            try
            {
                using (IDbCommand cmd = sugarClient.Ado.Connection.CreateCommand())
                {
                    //查找是否有同名表
                    cmd.CommandText = $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{SugarTableUtils.GetTableName<TableClass>()}'";
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

        /// <summary>
        /// 查找是否有相同键值
        /// </summary>
        /// <typeparam name="TableClass">自定义表格类</typeparam>
        /// <param name="expression"></param>
        /// <returns>相同值的个数</returns>
        public static long GetCount<TableClass>(SqlSugarClient sugarClient, Expression<Func<TableClass, bool>> expression)
        {
            if (sugarClient == null) throw new NullReferenceException("null SqlSugarClient");
            if (sugarClient.Ado.Connection.State != System.Data.ConnectionState.Open)
                throw new InvalidOperationException("Database not ready");
            try
            {
                BinaryExpression memberExpr = expression.Body as BinaryExpression;//将表达式转化为合适的子类
                List<string> propertyNames = GetNames(memberExpr);//获取表达式种需要比较的变量名
                Dictionary<string, string> namePairs = SugarColUtils.GetAllColName<TableClass>();//实际表名和类属性名的对应字典
                if (memberExpr != null)
                {
                    //将表达式文本中的符号替换为sql语句的符号
                    string epressionString = memberExpr.ToString();
                    epressionString = epressionString.Replace("AndAlso", "AND");
                    epressionString = epressionString.Replace("OrElse", "OR");
                    epressionString = epressionString.Replace("\"", "'");
                    foreach (string propertyName in propertyNames)//将属性名替换为表的字段名
                    {
                        namePairs.TryGetValue(
                            propertyName.Split(new char[] { '.' })[1],
                            out string currentColName);
                        epressionString = epressionString.Replace(
                            propertyName,
                            currentColName);
                    }
                    //生成sql语句
                    string sqlText = $"SELECT COUNT(*) FROM {SugarTableUtils.GetTableName<TableClass>()} WHERE {epressionString}";
                    using (IDbCommand cmd = sugarClient.Ado.Connection.CreateCommand())
                    {
                        cmd.CommandText = sqlText;
                        return (long)cmd.ExecuteScalar();
                    }
                }
                else throw new ArgumentOutOfRangeException("Unsupported Expression type");
            }
            catch (Exception) { throw; }
        }

        /// <summary>
        /// 获取条件表达式中的所有属性名
        /// </summary>
        /// <param name="binaryExpression"></param>
        /// <returns>属性名列表</returns>
        private static List<string> GetNames(BinaryExpression binaryExpression)
        {
            if (result.Count != 0)//列表不为空时清空列表
                result = new List<string>();
            ForeachExpressions(binaryExpression);
            return result;
        }
        //用于存放返回结果的列表
        static List<string> result = new List<string>();

        /// <summary>
        /// 递归遍历条件树获取属性名称
        /// </summary>
        /// <param name="binaryExpression">条件表达式</param>
        private static void ForeachExpressions(BinaryExpression binaryExpression)
        {
            if (binaryExpression.NodeType == ExpressionType.AndAlso || binaryExpression.NodeType == ExpressionType.OrElse)
            {
                //中序遍历，不进行节点的解析
                ForeachExpressions(binaryExpression.Left as BinaryExpression);
                ForeachExpressions(binaryExpression.Right as BinaryExpression);
            }
            else
            {
                //刷选出的非逻辑运算表达式
                result.Add(binaryExpression.Left.ToString());
            }
        }
    }
}
