using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.cbgan.SuiseiBot.Code.database
{
    /// <summary>
    /// SQLite数据库帮助类
    /// 用于完成对数据库的基本操作
    /// </summary>
    internal class SQLiteHelper
    {
        #region 属性
        /// <summary>
        /// 数据库路径
        /// </summary>
        public string DBPath { private set; get; }
        public SQLiteConnection SQLConnection { private set; get; }//连接对象
        public SQLiteTransaction SQLiteTrans { private set; get; }//事务对象
        public bool IsRunTrans { private set; get; }//事务运行标识
        public bool AutoCommit { private set; get; }//事务自动提交标识
        #endregion

        #region 构造函数
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dbPath">数据库路径</param>
        public SQLiteHelper(string dbPath)
        {
            this.DBPath = dbPath;
            this.IsRunTrans = false;
            this.AutoCommit = false;
            this.SQLConnection = new SQLiteConnection("DATA SOURCE=" + this.DBPath);
        }
        #endregion

        /// <summary>
        /// 创建新的数据库文件
        /// </summary>
        /// <returns>true 创建成功 false 创建失败</returns>
        public bool CreateNewDBFile()
        {
            if (string.IsNullOrEmpty(DBPath)) throw new Exception("Create new dbfile failed(DBPath is null");
            try
            {
                if (File.Exists(DBPath)) return false;
                SQLiteConnection.CreateFile(DBPath);
                return true;
            }
            catch (Exception e)
            {
                throw new Exception("Create new dbfile failed(" + DBPath + ")\n" + e.Message);
            }
        }

        /// <summary>
        /// 打开数据库连接
        /// </summary>
        /// <returns>true 连接成功 false 连接失败</returns>
        public bool OpenDB()
        {
            try
            {
                if (string.IsNullOrEmpty(DBPath)) throw new Exception("Open dbfile failed(DBPath is null");
                this.SQLConnection.Open();
                return true;
            }
            catch (Exception) { throw; }
        }

        /// <summary>
        /// 关闭数据库连接
        /// </summary>
        /// <returns>true 关闭成功 false 关闭失败</returns>
        public bool CloseDB()
        {
            try
            {
                this.SQLConnection.Close();
                return true;
            }
            catch (Exception) { throw; }
        }

        /// <summary>
        /// 创建新表，返回影响的记录数
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="paramsName">字段名</param>
        /// <param name="paramsType">字段类型</param>
        /// <param name="primaryKeyName">主键名</param>
        /// <returns>影响的记录数</returns>
        public int CreateTable(string tableName, string[] paramsName, string[] paramsType, string[] primaryKeyName)
        {
            if (string.IsNullOrEmpty(tableName)) throw new Exception("Create new table failed(table name is null)");//有空表名
            if (paramsName.Length != paramsType.Length) throw new Exception("Get illegal params");//有不合法的数据
            try
            {
                if (this.SQLConnection != null && this.SQLConnection.State != System.Data.ConnectionState.Closed)
                {
                    SQLiteCommand cmd = new SQLiteCommand(this.SQLConnection);
                    //写入创建新表指令
                    cmd.CommandText = "CREATE TABLE " + tableName + "(";
                    for (int i = 0; i < paramsName.Length; i++)
                    {
                        cmd.CommandText += paramsName[i] + " " + paramsType[i];
                        if (i != paramsName.Length - 1) cmd.CommandText += ",";
                    }
                    if (primaryKeyName.Length != 0)//当有主键时
                    {
                        cmd.CommandText +=
                            ",PRIMARY KEY(" +
                            string.Join(",", primaryKeyName) +
                            ")";
                    }
                    cmd.CommandText += ")";
                    return cmd.ExecuteNonQuery();
                }
                else throw new Exception("Database not connected");
            }
            catch (Exception) { throw; }
        }

        /// <summary>
        /// 查找是否存在同名表
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns>返回True/False代表是否存在</returns>
        public bool TableExists(string tableName)
        {
            if (string.IsNullOrEmpty(tableName)) throw new Exception("Create new table failed(table name is null)");//有空表名
            try
            {
                if (this.SQLConnection != null && this.SQLConnection.State != System.Data.ConnectionState.Closed)
                {
                    SQLiteCommand cmd = new SQLiteCommand(this.SQLConnection);
                    //查找是否有同名表
                    cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='" + tableName + "'";
                    return Convert.ToBoolean(cmd.ExecuteScalar());
                }
                else throw new Exception("Database not connected");
            }
            catch (Exception) { throw; }
        }

        /// <summary>
        /// 删除表，返回影响的记录数
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <returns>影响的记录数</returns>
        public int DeleteTable(string tableName)
        {
            if (string.IsNullOrEmpty(tableName)) throw new Exception("Create new table failed(table name is null)");//有空表名
            try
            {
                if (this.SQLConnection != null && this.SQLConnection.State != System.Data.ConnectionState.Closed)
                {
                    SQLiteCommand cmd = new SQLiteCommand(this.SQLConnection);
                    cmd.CommandText = "DROP TABLE IF EXISTS " + tableName;
                    return cmd.ExecuteNonQuery();
                }
                else throw new Exception("Database not connected");
            }
            catch (Exception) { throw; }
        }

        /// <summary>
        /// 在表中插入新行数据，返回影响的记录数
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="paramsName">字段名</param>
        /// <param name="paramsData">值</param>
        /// <returns>影响的记录数</returns>
        public int InsertRow(string tableName, string[] paramsName, string[] paramsData)
        {
            if (string.IsNullOrEmpty(tableName)) throw new Exception("Create new table failed(table name is null)");//有空表名
            if (paramsName.Length != paramsData.Length) throw new Exception("Get illegal params");//有不合法的数据
            try
            {
                if (this.SQLConnection != null && this.SQLConnection.State != System.Data.ConnectionState.Closed)
                {
                    SQLiteCommand cmd = new SQLiteCommand(this.SQLConnection);
                    //插入数据
                    cmd.CommandText = "INSERT INTO " + tableName + " (\"" +
                        string.Join("\",\"", paramsName) +//写入字段名
                        "\") VALUES (\"" +
                        string.Join("\",\"", paramsData) + "\");";//写入数据
                    return cmd.ExecuteNonQuery();
                }
                else throw new Exception("Database not connected");
            }
            catch (Exception) { throw; }
        }

        /// <summary>
        /// 使用主键值查找行数据，返回行数据
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="primaryKeyValue">主键值</param>
        /// <returns>返回SQLiteDataReader为查找行的数据</returns>
        public SQLiteDataReader FindRow(string tableName, string[] keyNames, string[] keyValues)
        {
            if (string.IsNullOrEmpty(tableName) || keyNames.Length != keyValues.Length) throw new Exception("Get illegal params");
            try
            {
                if (this.SQLConnection != null && this.SQLConnection.State != System.Data.ConnectionState.Closed)
                {
                    SQLiteCommand cmd = new SQLiteCommand(this.SQLConnection);
                    cmd.CommandText = "SELECT * FROM " + tableName + " WHERE ";
                    for (int i = 0; i < keyNames.Length; i++)
                    {
                        cmd.CommandText += keyNames[i] + "='" + keyValues[i] + "'";
                        if (keyNames.Length > 1 && i != keyNames.Length - 1) cmd.CommandText += " AND ";
                    }
                    return cmd.ExecuteReader();
                }
                else throw new Exception("Database not connected");
            }
            catch (Exception) { throw; }
        }

        /// <summary>
        /// 使用键值删除行，返回影响的记录数
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="keyNames">字段名</param>
        /// <param name="keyValues">字段名</param>
        /// <returns>影响的记录数</returns>
        public int DeleteRow(string tableName, string[] keyNames, string[] keyValues)
        {
            if (string.IsNullOrEmpty(tableName) || keyNames.Length != keyValues.Length) throw new Exception("Get illegal params");
            try
            {
                if (this.SQLConnection != null && this.SQLConnection.State != System.Data.ConnectionState.Closed)
                {
                    SQLiteCommand cmd = new SQLiteCommand(this.SQLConnection);
                    cmd.CommandText = "DELETE FROM " + tableName + " WHERE ";
                    for (int i = 0; i < keyNames.Length; i++)
                    {
                        cmd.CommandText += keyNames[i] + "='" + keyValues[i] + "'";
                        if (keyNames.Length > 1 && i != keyNames.Length - 1) cmd.CommandText += " AND ";
                    }
                    return cmd.ExecuteNonQuery();
                }
                else throw new Exception("Database not connected");
            }
            catch (Exception) { throw; }
        }

        /// <summary>
        /// 通过键值查找行并更新数据，返回影响的记录数
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="updateName">要更新的字段名</param>
        /// <param name="updateValue">要更新的值</param>
        /// <param name="keyNames">要查找的字段名数组</param>
        /// <param name="keyValues">要查找的字段名值</param>
        /// <returns>影响的记录数</returns>
        public int UpdateData(string tableName, string updateName, string updateValue, string[] keyNames, string[] keyValues)
        {
            if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(updateName) || string.IsNullOrEmpty(updateValue) ||
                keyValues.Length != keyNames.Length) throw new Exception("Get null params");
            try
            {
                if (this.SQLConnection != null && this.SQLConnection.State != System.Data.ConnectionState.Closed)
                {
                    SQLiteCommand cmd = new SQLiteCommand(this.SQLConnection);
                    cmd.CommandText = "UPDATE " + tableName + " SET " + updateName + "='" + updateValue + "' WHERE ";
                    for (int i = 0; i < keyNames.Length; i++)
                    {
                        cmd.CommandText += keyNames[i] + "='" + keyValues[i] + "'";
                        if (keyNames.Length > 1 && i != keyNames.Length - 1) cmd.CommandText += " AND ";
                    }
                    return cmd.ExecuteNonQuery();
                }
                else throw new Exception("Database not connected");
            }
            catch (Exception) { throw; }
        }

        /// <summary>
        /// 查找是否有相同键值
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="keyNames">要查找的字段名数组</param>
        /// <param name="keyValues">要查找的字段名值</param>
        /// <returns>返回包含主键名的List</returns>
        public int GetCount(string tableName, string[] keyNames, string[] keyValues)
        {
            try
            {
                SQLiteCommand cmd = new SQLiteCommand(this.SQLConnection);
                cmd.CommandText = "SELECT COUNT(*) FROM " + tableName + " WHERE ";
                for (int i = 0; i < keyNames.Length; i++)
                {
                    cmd.CommandText += keyNames[i] + "='" + keyValues[i] + "'";
                    if (keyNames.Length > 1 && i != keyNames.Length - 1) cmd.CommandText += " AND ";
                }
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch (Exception) { throw; }
        }

        /// <summary>
        /// 执行SQL语句，返回影响的记录数
        /// </summary>
        /// <param name="SQLString">SQL语句</param>
        /// <returns>影响的记录数</returns>
        public int ExecuteSql(string SQLString)
        {
            if (string.IsNullOrEmpty(SQLString)) throw new Exception("Get null command");
            if (this.SQLConnection != null && this.SQLConnection.State != System.Data.ConnectionState.Closed)
            {
                SQLiteCommand cmd = new SQLiteCommand(this.SQLConnection);
                cmd.CommandText = SQLString;
                return cmd.ExecuteNonQuery();
            }
            else throw new Exception("Database not connected");
        }

        /// <summary>
        /// 开始数据库事务
        /// </summary>
        public void BeginTransaction()
        {
            this.SQLConnection.BeginTransaction();
            this.IsRunTrans = true;
        }

        /// <summary>
        /// 开始数据库事务
        /// </summary>
        /// <param name="isoLevel">事务锁级别</param>
        public void BeginTransaction(IsolationLevel isoLevel)
        {
            this.SQLConnection.BeginTransaction(isoLevel);
            this.IsRunTrans = true;
        }

        /// <summary>
        /// 提交当前挂起的事务
        /// </summary>
        public void Commit()
        {
            if (this.IsRunTrans)
            {
                this.SQLiteTrans.Commit();
                this.IsRunTrans = false;
            }
        }
    }
}
