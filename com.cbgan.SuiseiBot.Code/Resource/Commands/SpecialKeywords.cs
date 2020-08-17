using System.Collections.Generic;
using com.cbgan.SuiseiBot.Code.Resource.Enum;

namespace com.cbgan.SuiseiBot.Code.Resource.Commands
{
    /// <summary>
    /// 存放需要参数的关键词
    /// </summary>
    internal class SpecialKeywords
    {
        public static Dictionary<string, SpecialKeywordsType> Keywords = new Dictionary<string, SpecialKeywordsType>();

        /// <summary>
        /// 初始化特殊关键词
        /// </summary>
        public static void SpecialKeywordsInit()
        {
            Keywords.Add("查询排名",SpecialKeywordsType.PCRTools);
        }

        public static SpecialKeywordsType TryGetKeywordType(string message)
        {
            string keyword = message.Split(' ')[0];
            Keywords.TryGetValue(keyword, out SpecialKeywordsType keywordType);
            return keywordType;
        }
    }
}
