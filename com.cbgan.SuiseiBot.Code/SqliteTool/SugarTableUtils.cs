using SqlSugar;
using System;
using System.Reflection;

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
            SugarTable tableInfo = typeof(TableClass).GetCustomAttribute<SugarTable>();
            if (tableInfo == null) tableInfo = new SugarTable(typeof(TableClass).Name);
            return tableInfo.TableName;
        }
        #endregion
    }
}
