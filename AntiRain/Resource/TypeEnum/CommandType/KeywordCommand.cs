using System.ComponentModel;

namespace AntiRain.Resource.TypeEnum.CommandType
{
    /// <summary>
    /// 关键字触发
    /// 关键字在Description中以空格分隔多个关键字，仅在初始化时读取
    /// </summary>
    internal enum KeywordCommand
    {
        /// <summary>
        /// 昏睡红茶
        /// </summary>
        [Description("优质睡眠 昏睡红茶 昏睡套餐 健康睡眠")]
        RedTea,
        /// <summary>
        /// 24岁，是学生
        /// </summary>
        [Description("24岁，是学生")]
        Student,
        /// <summary>
        /// 来点色图！
        /// </summary>
        [Description("来点色图 来点涩图 我要看色图")]
        Hso,
        /// <summary>
        /// 随机禁言
        /// </summary>
        [Description("随机禁言")]
        RandomBan
    }
}
