using com.cbgan.SuiseiBot.Code.Resource;
using System.Collections.Generic;

namespace com.cbgan.SuiseiBot.Resource
{
    internal static class ChatKeywords
    {
        /// <summary>
        /// 关键字初始化及存储
        /// </summary>
        public static Dictionary<string, KeywordType> KeyWords = new Dictionary<string, KeywordType>();
        public static void KeywordInit()
        {
            //1 娱乐功能
            KeyWords.Add(".r", KeywordType.SurpriseMFK);//随机数
            KeyWords.Add("给老子来个禁言套餐", KeywordType.SurpriseMFK);
            KeyWords.Add("请问可以告诉我你的年龄吗？", KeywordType.SurpriseMFK);
            KeyWords.Add("给爷来个优质睡眠套餐", KeywordType.SurpriseMFK);
            //2 奇奇怪怪的签到
            KeyWords.Add("彗酱今天也很可爱", KeywordType.Suisei);
            //3 公主连结小功能
            //暂无只需要关键词的功能

            KeyWords.Add("debug", KeywordType.Debug);
        }
    }
}
