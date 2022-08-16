using System;
using System.Reflection;
using SqlSugar;

namespace Skadi.DatabaseUtils.SqliteTool;

internal static class SugarTableUtils
{
    #region SugarTable辅助方法

    /// <summary>
    /// 获取表名
    /// </summary>
    /// <param name="tableType">表格类</param>
    /// <returns>表名</returns>
    public static string GetTableName(this Type tableType)
    {
        return (tableType.GetCustomAttribute<SugarTable>() ??
                new SugarTable(tableType.Name)).TableName;
    }

    #endregion
}