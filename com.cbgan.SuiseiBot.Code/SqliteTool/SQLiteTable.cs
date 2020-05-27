using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace com.cbgan.SuiseiBot.Code.SqliteTool
{
    internal class SQLiteTable : Attribute
    {
        public string TableName { get; set; }
        public string TableDescription { get; set; }

        /// <summary>
        /// 获取表格类的表名
        /// </summary>
        /// <typeparam name="TableClass">自定义表格类</typeparam>
        /// <returns>表格名</returns>
        public static string GetTableName<TableClass>()
        {
            SQLiteTable infos = typeof(TableClass).GetCustomAttribute<SQLiteTable>();
            if (infos == null || string.IsNullOrEmpty(infos.TableName))
            {
                infos = new SQLiteTable();
                infos.TableName = typeof(TableClass).Name;
            }
            return infos.TableName;
        }
    }
}
