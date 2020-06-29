using SqlSugar;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace com.cbgan.SuiseiBot.Code.SqliteTool
{
    internal class SugarColUtils : Attribute
    {
        #region SugarColumn辅助方法
        /// <summary>
        /// 判断字段类型
        /// </summary>
        /// <param name="property">当前处理的列属性</param>
        /// <returns>SQL指令</returns>
        public static string GetColType(PropertyInfo property)
        {
            SugarColumn columnConfig = property.GetCustomAttribute<SugarColumn>();
            if (columnConfig == null || columnConfig.ColumnDataType == null)
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
                if (    property.PropertyType == typeof(string))
                    return "TEXT";

                //其他类型
                return "BLOB";
            }
            else return columnConfig.ColumnDataType;
        }

        /// <summary>
        /// 判断是否为自增字段
        /// </summary>
        /// <param name="property">当前处理的列属性</param>
        /// <returns>SQL指令</returns>
        public static string ColIsIdentity(PropertyInfo property)
        {
            SugarColumn columnConfig = property.GetCustomAttribute<SugarColumn>();
            if (columnConfig == null) return "";
            else return columnConfig.IsIdentity ? "PRIMARY KEY AUTOINCREMENT" : "";
        }

        /// <summary>
        /// 判断是否为可空字段
        /// </summary>
        /// <param name="property">当前处理的列属性</param>
        /// <returns>SQL指令</returns>
        public static string ColIsNullable(PropertyInfo property)
        {
            SugarColumn columnConfig = property.GetCustomAttribute<SugarColumn>();
            if (columnConfig == null) return "NOT NULL";
            else return columnConfig.IsNullable ? "" : "NOT NULL";
        }

        /// <summary>
        /// 获取当前字段的字段名
        /// </summary>
        /// <param name="property">当前处理的列属性</param>
        /// <returns>字段名</returns>
        public static string GetColName(PropertyInfo property)
        {
            SugarColumn columnConfig = property.GetCustomAttribute<SugarColumn>();
            if (columnConfig == null || string.IsNullOrEmpty(columnConfig.ColumnName)) return property.Name;
            return columnConfig.ColumnName;
        }

        /// <summary>
        /// 获取所有字段的字段名
        /// 并返回键值对
        /// </summary>
        /// <param name="property">当前处理的列属性</param>
        /// <returns>键值对[类属性名，字段名]</returns>
        public static Dictionary<string,string> GetAllColName<TableClass>()
        {
            Dictionary<string, string> namePairs = new Dictionary<string, string>();
            foreach (PropertyInfo property in typeof(TableClass).GetProperties())  
            {
                namePairs.Add(
                    property.Name,          //属性名
                    GetColName(property)    //字段名
                    );
            }
            return namePairs;
        }

        /// <summary>
        /// 判断是否为主键
        /// </summary>
        /// <param name="property"></param>
        /// <returns>是否为主键</returns>
        public static bool ColIsPrimaryKey(PropertyInfo property)
        {
            SugarColumn columnConfig = property.GetCustomAttribute<SugarColumn>();
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
                if (ColIsPrimaryKey(colInfo)) ColNames.Add(GetColName(colInfo));
            }
            return ColNames.Count == 0 ? null : ColNames;
        }
        #endregion
    }
}
