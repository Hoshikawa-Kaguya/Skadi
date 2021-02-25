using System;
using System.Collections.Generic;
using System.Linq;
using AntiRain.DatabaseUtils.SqliteTool;
using SqlSugar;
using YukariToolBox.FormatLog;

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
                Log.Error("角色数据更新数据库错误", Log.ErrorLogBuilder(e));
                return false;
            }
        }

        /// <summary>
        /// 通过关键词查找角色信息
        /// </summary>
        /// <param name="keyWord">关键词</param>
        internal PCRChara FindChara(string keyWord)
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                //查询角色名
                List<PCRChara> chara = dbClient.Queryable<PCRChara>().Where(name => name.Name.Contains(keyWord))
                                               .ToList();
                //检查是否检索到
                if (chara == null || chara.Count == 0) return null;

                return chara.First();
            }
            catch (Exception e)
            {
                Log.Error("数据库错误", Log.ErrorLogBuilder(e));
                return null;
            }
        }

        /// <summary>
        /// 由角色ID查找角色信息
        /// </summary>
        /// <param name="charaId">角色ID</param>
        internal PCRChara FindChara(int charaId)
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                return dbClient.Queryable<PCRChara>().InSingle(charaId);
            }
            catch (Exception e)
            {
                Log.Error("数据库错误", Log.ErrorLogBuilder(e));
                return null;
            }
        }

        #endregion
    }
}