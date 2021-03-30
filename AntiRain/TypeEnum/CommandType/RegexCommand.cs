using System.ComponentModel;

namespace AntiRain.TypeEnum.CommandType
{
    /// <summary>
    /// 正则触发
    /// 正则表达式在Description中，仅在初始化时读取
    /// </summary>
    internal enum RegexCommand
    {
        /// <summary>
        /// 随机数
        /// </summary>
        [Description(@"^dice$")] Dice,

        /// <summary>
        /// 查找角色
        /// </summary>
        [Description(@"^谁是[\u4e00-\u9fa5]+$")] FindChara,//TODO 最后一个PCR功能（
        [Description(@"^debug")] Debug
    }
}