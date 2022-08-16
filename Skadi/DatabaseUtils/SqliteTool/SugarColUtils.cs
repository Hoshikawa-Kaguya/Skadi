using System.Reflection;
using SqlSugar;

namespace Skadi.DatabaseUtils.SqliteTool;

internal class SugarColUtils
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
        if (columnConfig?.ColumnDataType == null)
        {
            //整数类型
            if (property.PropertyType == typeof(sbyte)  ||
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
            if (property.PropertyType == typeof(double) ||
                property.PropertyType == typeof(float)  ||
                property.PropertyType == typeof(decimal))
                return "REAL";

            //字符串类型
            if (property.PropertyType == typeof(string))
                return "TEXT";

            //其他类型
            return "BLOB";
        }

        return columnConfig.ColumnDataType;
    }

    /// <summary>
    /// 获取当前字段的字段名
    /// </summary>
    /// <param name="property">当前处理的列属性</param>
    /// <returns>字段名</returns>
    public static string GetColName(PropertyInfo property)
    {
        SugarColumn columnConfig = property.GetCustomAttribute<SugarColumn>();
        if (columnConfig == null || string.IsNullOrEmpty(columnConfig.ColumnName))
            return property.Name;
        return columnConfig.ColumnName;
    }

    /// <summary>
    /// 判断是否为自增字段
    /// </summary>
    /// <param name="property">当前处理的列属性</param>
    /// <returns>SQL指令</returns>
    public static string ColIsIdentity(PropertyInfo property)
    {
        SugarColumn columnConfig = property.GetCustomAttribute<SugarColumn>();
        if (columnConfig == null)
            return "";
        else
            return columnConfig.IsIdentity ? "PRIMARY KEY AUTOINCREMENT" : "";
    }

    /// <summary>
    /// 判断是否为可空字段
    /// </summary>
    /// <param name="property">当前处理的列属性</param>
    /// <returns>SQL指令</returns>
    public static string ColIsNullable(PropertyInfo property)
    {
        SugarColumn columnConfig = property.GetCustomAttribute<SugarColumn>();
        if (columnConfig == null)
            return "NOT NULL";
        else
            return columnConfig.IsNullable ? "" : "NOT NULL";
    }

    /// <summary>
    /// 判断是否为主键
    /// </summary>
    /// <param name="property"></param>
    /// <returns>是否为主键</returns>
    public static bool ColIsPrimaryKey(PropertyInfo property)
    {
        SugarColumn columnConfig = property.GetCustomAttribute<SugarColumn>();
        if (columnConfig == null)
            return false;
        else
            return columnConfig.IsPrimaryKey;
    }

    #endregion
}