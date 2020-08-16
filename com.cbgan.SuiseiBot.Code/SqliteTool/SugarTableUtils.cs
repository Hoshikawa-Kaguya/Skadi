using System;
using System.Reflection;
using SqlSugar;

namespace com.cbgan.SuiseiBot.Code.SqliteTool
{
    internal class SugarTableUtils : Attribute
    {
        #region SugarTable辅助方法
        /// <summary>
        /// 获取表名
        /// </summary>
        /// <typeparam name="TableClass">表格实体</typeparam>
        /// <returns>表名</returns>
        public static string GetTableName<TableClass>()
        {
            return (typeof(TableClass).GetCustomAttribute<SugarTable>() ??
                    new SugarTable(typeof(TableClass).Name))
                .TableName;
        }
        #endregion
    }
}
