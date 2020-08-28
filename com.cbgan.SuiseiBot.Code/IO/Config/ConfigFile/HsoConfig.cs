using com.cbgan.SuiseiBot.Code.Resource.TypeEnum;

namespace com.cbgan.SuiseiBot.Code.IO.Config.ConfigFile
{
    internal class HsoConfig
    {
        /// <summary>
        /// 色图源类型
        /// </summary>
        public SetuSourceType Source { set; get; }
        /// <summary>
        /// LoliconToken
        /// </summary>
        public string LoliconToken { set; get; }
        /// <summary>
        /// YukariToken
        /// </summary>
        public string YukariToken { set; get; }
    }
}
