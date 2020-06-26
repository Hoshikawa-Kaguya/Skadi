using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace com.cbgan.SuiseiBot.Code.SqliteTool
{
    class SugarTableUtils : Attribute
    {
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
    }
}
