using System.ComponentModel;

namespace AntiRain.Resource.TypeEnum.CommandType
{
    /// <summary>
    /// 正则触发
    /// 正则表达式在Description中，仅在初始化时读取
    /// </summary>
    internal enum RegexCommand
    {
        /// <summary>
        /// 慧酱签到
        /// </summary>
        [Description(@"^彗酱今天也很可爱$")]
        SuiseiHello,
        /// <summary>
        /// 切噜编码
        /// </summary>
        [Description(@"^切噜一下")]
        CheruEncode,
        /// <summary>
        /// 切噜翻译
        /// </summary>
        [Description(@"^切噜(?:~|～)")]
        CheruDecode
    }
}
