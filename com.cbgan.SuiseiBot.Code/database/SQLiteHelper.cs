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
    public class SQLiteHelper
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
                this.SQLConnection = new SQLiteConnection("DATA SOURCE=" + this.DBPath);
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
                if (this.SQLConnection != null && this.SQLConnection.State != System.Data.ConnectionState.Closed)
                {
                    if (this.IsRunTrans && this.AutoCommit)
                    {
                        this.Commit();
                    }
                    this.SQLConnection.Close();
                    this.SQLConnection = null;
                    return true;
                }
                else throw new Exception("Close dbfile failed(Connection is closed or null");
            }
            catch (Exception) { throw; }
        }

        /// <summary>
        /// 创建新表，返回影响的记录数
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="paramName">字段名</param>
        /// <param name="paramType">字段类型</param>
        /// <param name="indexPrimaryKey">是否将第一个字段设为主键</param>
        /// <returns>影响的记录数</returns>
        public int CreateTable(string tableName, string[] paramsName, string[] paramsType, bool indexPrimaryKey)
        {
            if (string.IsNullOrEmpty(tableName)) throw new Exception("Create new table failed(table name is null)");//有空表名
            if (paramsName.Length != paramsType.Length) throw new Exception("Get illegal params");//有不合法的数据
            try
            {
                if (this.SQLConnection != null && this.SQLConnection.State != System.Data.ConnectionState.Closed)
                {
                    SQLiteCommand cmd = new SQLiteCommand(this.SQLConnection);
                    //查找是否有同名表
                    cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='" + tableName + "'";
                    //有同名表
                    if (Convert.ToBoolean(cmd.ExecuteScalar())) return 0;
                    //写入创建新表指令
                    cmd.CommandText = "CREATE TABLE " + tableName + "(" + paramsName[0] + " " + paramsType[0];
                    if (indexPrimaryKey) cmd.CommandText += " PRIMARY KEY";
                    cmd.CommandText += ",";
                    for (int i = 1; i < paramsName.Length; i++)
                    {
                        cmd.CommandText += paramsName[i] + " " + paramsType[i];
                        if (i != paramsName.Length - 1) cmd.CommandText += ",";
                    }
                    cmd.CommandText += ")";
                    return cmd.ExecuteNonQuery();
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
                    #region 判断是否存在主键，并判断是否插入重复主键
                    string primaryKeyName = null;
                    primaryKeyName = GetPrimaryKeyName(tableName);
                    if (!string.IsNullOrEmpty(primaryKeyName))//当存在主键时查找是否有重复主键
                    {
                        int primaryKeyIndex = paramsName.ToList().IndexOf(primaryKeyName);
                        cmd.CommandText = "SELECT COUNT(*) FROM " + tableName + " WHERE " + primaryKeyName + " = '" + paramsData[primaryKeyIndex] + "'";
                        if (Convert.ToInt32(cmd.ExecuteScalar()) > 0) return 0;
                    }
                    #endregion
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
        public SQLiteDataReader FindRow(string tableName, string primaryKeyValue)
        {
            if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(primaryKeyValue)) throw new Exception("Get null params");
            try
            {
                if (this.SQLConnection != null && this.SQLConnection.State != System.Data.ConnectionState.Closed)
                {
                    SQLiteCommand cmd = new SQLiteCommand(this.SQLConnection);
                    cmd.CommandText = "SELECT * FROM " + tableName + " WHERE " + GetPrimaryKeyName(tableName) + "='" + primaryKeyValue + "'";
                    return cmd.ExecuteReader();
                }
                else throw new Exception("Database not connected");
            }
            catch (Exception) { throw; }
        }

        /// <summary>
        /// 使用主键值删除行，返回影响的记录数
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="primaryKeyValue">主键值</param>
        /// <returns>影响的记录数</returns>
        public int DeleteRow(string tableName, string primaryKeyValue)
        {
            if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(primaryKeyValue)) throw new Exception("Get null params");
            try
            {
                if (this.SQLConnection != null && this.SQLConnection.State != System.Data.ConnectionState.Closed)
                {
                    SQLiteCommand cmd = new SQLiteCommand(this.SQLConnection);
                    cmd.CommandText = "DELETE FROM " + tableName + " WHERE " + GetPrimaryKeyName(tableName) + "='" + primaryKeyValue + "'";
                    return cmd.ExecuteNonQuery();
                }
                else throw new Exception("Database not connected");
            }
            catch (Exception) { throw; }
        }

        /// <summary>
        /// 通过主键值查找行并更新数据，返回影响的记录数
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="updateName"></param>
        /// <param name="updateValue"></param>
        /// <param name="primaryKeyValue"></param>
        /// <returns>影响的记录数</returns>
        public int UpdateData(string tableName, string updateName, string updateValue, string primaryKeyValue)
        {
            if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(updateName) ||
                string.IsNullOrEmpty(updateValue) || string.IsNullOrEmpty(primaryKeyValue)) throw new Exception("Get null params");
            try
            {
                if (this.SQLConnection != null && this.SQLConnection.State != System.Data.ConnectionState.Closed)
                {
                    SQLiteCommand cmd = new SQLiteCommand(this.SQLConnection);
                    cmd.CommandText = "UPDATE " + tableName + " SET " + updateName + "='" + updateValue + "' WHERE " + GetPrimaryKeyName(tableName) + "='" + primaryKeyValue + "'";
                    Console.WriteLine(cmd.CommandText);
                    return cmd.ExecuteNonQuery();
                }
                else throw new Exception("Database not connected");
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

        /// <summary>
        /// 查找表主键名
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <returns></returns>
        private string GetPrimaryKeyName(string tableName)
        {
            SQLiteCommand cmd = new SQLiteCommand(this.SQLConnection);
            cmd.CommandText = "PRAGMA TABLE_INFO(" + tableName + ")";
            try
            {
                using (SQLiteDataReader dataReader = cmd.ExecuteReader())//查找主键
                {
                    while (dataReader.Read())
                    {
                        if (Convert.ToBoolean(dataReader["pk"]))//查找到主键名
                            Console.WriteLine(dataReader["Name"].ToString());
                        return dataReader["Name"].ToString();
                    }
                }
            }
            catch (Exception) { throw; }
            return null;
        }
    }
}
