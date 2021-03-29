using AntiRain.DatabaseUtils;
using AntiRain.DatabaseUtils.Helpers.PCRDataDB;

namespace AntiRain.Resource.PCRResource
{
    /// <summary>
    /// 角色别名处理
    /// </summary>
    internal class CharaParser
    {
        #region 属性

        private CharaDBHelper CharaDBHelper { get; }

        #endregion

        #region 构造函数

        internal CharaParser()
        {
            this.CharaDBHelper = new CharaDBHelper();
        }

        #endregion

        #region 公有方法

        /// <summary>
        /// 查找角色
        /// </summary>
        /// <param name="keyWord">关键词</param>
        internal PCRChara FindChara(string keyWord)
            => CharaDBHelper.FindChara(keyWord);

        /// <summary>
        /// 查找角色
        /// </summary>
        /// <param name="charaId">id</param>
        internal PCRChara FindChara(int charaId)
            => CharaDBHelper.FindChara(charaId);

        #endregion
    }
}