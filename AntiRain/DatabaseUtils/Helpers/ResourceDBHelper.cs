using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AntiRain.DatabaseUtils.SqliteTool;

namespace AntiRain.DatabaseUtils.Helpers
{
    internal class ResourceDBHelper
    {
        #region 属性
        /// <summary>
        /// 数据库路径
        /// </summary>
        private readonly string DBPath;
        #endregion

        #region 构造函数

        internal ResourceDBHelper()
        {
            this.DBPath = SugarUtils.GetDataDBPath(SugarUtils.GlobalResDBName);
        }
        #endregion
    }
}
