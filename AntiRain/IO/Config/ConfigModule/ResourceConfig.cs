using AntiRain.TypeEnum;

namespace AntiRain.IO.Config.ConfigModule
{
    /// <summary>
    /// 资源文件配置
    /// </summary>
    internal class ResourceConfig
    {
        /// <summary>
        /// PCR数据库区服选择
        /// CN,JP,TW
        /// 可以为单独区服
        /// </summary>
        public Server[] PCRDatabaseArea { get; set; }
    }
}
