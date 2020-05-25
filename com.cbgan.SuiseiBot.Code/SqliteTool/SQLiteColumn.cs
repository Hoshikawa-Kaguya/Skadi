using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace com.cbgan.SuiseiBot.Code.SqliteTool
{
    internal class SQLiteColumn : Attribute
    {
        /// <summary>
        /// 是否为自增字段
        /// </summary>
        public bool IsIdentity { get; set; }
        /// <summary>
        /// 是否为主键
        /// 此属性无法使用函数返回SQL指令
        /// </summary>
        public bool IsPrimaryKey { get; set; }
        /// <summary>
        /// 字段类型
        /// </summary>
        public string ColumnType { get; set; }
        /// <summary>
        /// 是否可空
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// 判断字段类型
        /// </summary>
        /// <param name="property">当前处理的列属性</param>
        /// <returns>SQL指令</returns>
        public static string GetColType(PropertyInfo property)
        {
            SQLiteColumn columnConfig = (SQLiteColumn)property.GetCustomAttribute(typeof(SQLiteColumn), true);
            if (columnConfig == null || columnConfig.ColumnType == null)
            {
                //整数类型
                if (    property.PropertyType == typeof(sbyte)  ||
                        property.PropertyType == typeof(byte)   ||
                        property.PropertyType == typeof(short)  ||
                        property.PropertyType == typeof(ushort) ||
                        property.PropertyType == typeof(int)    ||
                        property.PropertyType == typeof(uint)   ||
                        property.PropertyType == typeof(long)   ||
                        property.PropertyType == typeof(ulong)  ||
                        property.PropertyType == typeof(char)   ||
                        property.PropertyType == typeof(bool))
                    return "INTEGER";

                //浮点数类型
                if (    property.PropertyType == typeof(double) ||
                        property.PropertyType == typeof(float)  ||
                        property.PropertyType == typeof(decimal))
                    return "REAL";

                //字符串类型
                if (property.PropertyType == typeof(string))
                    return "TEXT";

                //其他类型
                return "BLOB";
            }
            else return columnConfig.ColumnType;
        }

        /// <summary>
        /// 判断是否为自增字段
        /// </summary>
        /// <param name="property">当前处理的列属性</param>
        /// <returns>SQL指令</returns>
        public static string ColIsIdentity(PropertyInfo property)
        {
            SQLiteColumn columnConfig = (SQLiteColumn)property.GetCustomAttribute(typeof(SQLiteColumn), true);
            if (columnConfig == null) return "";
            else return columnConfig.IsIdentity ? "AUTOINCREMENT" : "";
        }

        /// <summary>
        /// 判断是否为可空字段
        /// </summary>
        /// <param name="property">当前处理的列属性</param>
        /// <returns>SQL指令</returns>
        public static string ColIsNullable(PropertyInfo property)
        {
            SQLiteColumn columnConfig = (SQLiteColumn)property.GetCustomAttribute(typeof(SQLiteColumn), true);
            if (columnConfig == null) return "NOT NULL";
            else return columnConfig.IsNullable ? "" : "NOT NULL";
        }

        /// <summary>
        /// 判断是否为主键
        /// </summary>
        /// <param name="property"></param>
        /// <returns>是否为主键</returns>
        public static bool ColIsPrimaryKey(PropertyInfo property)
        {
            SQLiteColumn columnConfig = (SQLiteColumn)property.GetCustomAttribute(typeof(SQLiteColumn), true);
            if (columnConfig == null) return false;
            else return columnConfig.IsPrimaryKey;
        }

        /// <summary>
        /// 获取所有的主键名
        /// </summary>
        /// <typeparam name="TableClass">自定义表格类</typeparam>
        /// <returns>包含主键字段名的List</returns>
        public static List<string> GetColPrimaryKeys<TableClass>()
        {
            List<string> ColNames = new List<string>();
            foreach (PropertyInfo colInfo in typeof(TableClass).GetProperties())
            {
                if (ColIsPrimaryKey(colInfo)) ColNames.Add(colInfo.Name);
            }
            return ColNames.Count == 0 ? null : ColNames;
        }

    }
}
