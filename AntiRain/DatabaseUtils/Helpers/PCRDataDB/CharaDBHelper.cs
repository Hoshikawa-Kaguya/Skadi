using System;
using System.Collections.Generic;
using System.Linq;
using AntiRain.DatabaseUtils.SqliteTool;
using Sora.Tool;
using SqlSugar;

namespace AntiRain.DatabaseUtils.Helpers.PCRDataDB
{
    /// <summary>
    /// 角色名数据库
    /// </summary>
    internal class CharaDBHelper
    {
        #region 属性
        /// <summary>
        /// 数据库路径
        /// </summary>
        private readonly string DBPath;
        #endregion

        #region 构造函数
        internal CharaDBHelper()
        {
            this.DBPath = SugarUtils.GetDataDBPath(SugarUtils.GlobalResDBName);
        }
        #endregion

        #region 公有方法
        /// <summary>
        /// 更新角色数据
        /// </summary>
        internal bool UpdateCharaData(List<PCRChara> charas)
        {
            if (charas == null || charas.Count == 0) return false;
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                //清空原有数据
                dbClient.Deleteable<PCRChara>().ExecuteCommand();
                //写入新的数据
                return dbClient.Insertable(charas).ExecuteCommand() > 0;
            }
            catch (Exception e)
            {
                ConsoleLog.Error("角色数据更新数据库错误",ConsoleLog.ErrorLogBuilder(e));
                return false;
            }
        }

        /// <summary>
        /// 通过关键词查找角色ID
        /// </summary>
        /// <param name="keyWord">关键词</param>
        internal int FindCharaId(string keyWord)
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                //查询角色名
                List<PCRChara> chara = dbClient.Queryable<PCRChara>().Where(name => name.Name.Contains(keyWord))
                                               .ToList();
                //检查是否检索到
                if (chara == null || chara.Count == 0) return -1;

                return chara.First().CharaId;
            }
            catch (Exception e)
            {
                ConsoleLog.Error("查找角色名时发生错误",ConsoleLog.ErrorLogBuilder(e));
                return -1;
            }
        } 
        #endregion
    }
}
