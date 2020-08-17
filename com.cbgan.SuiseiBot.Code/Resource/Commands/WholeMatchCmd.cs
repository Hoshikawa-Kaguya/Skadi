using System.Collections.Generic;
using com.cbgan.SuiseiBot.Code.Resource.Enum;

namespace com.cbgan.SuiseiBot.Code.Resource.Commands
{
    internal static class WholeMatchCmd
    {
        /// <summary>
        /// 关键字初始化及存储
        /// </summary>
        public static Dictionary<string, WholeMatchCmdType> KeyWords = new Dictionary<string, WholeMatchCmdType>();
        public static void KeywordInit()
        {
            //1 娱乐功能
            KeyWords.Add(".r", WholeMatchCmdType.SurpriseMFK);//随机数
            KeyWords.Add("给老子来个禁言套餐", WholeMatchCmdType.SurpriseMFK);
            KeyWords.Add("请问可以告诉我你的年龄吗？", WholeMatchCmdType.SurpriseMFK);
            KeyWords.Add("给爷来个优质睡眠套餐", WholeMatchCmdType.SurpriseMFK);
            //2 奇奇怪怪的签到
            KeyWords.Add("彗酱今天也很可爱", WholeMatchCmdType.Suisei);
            //3 公主连结小功能
            //暂无只需要关键词的功能

            KeyWords.Add("debug", WholeMatchCmdType.Debug);
        }
    }
}
