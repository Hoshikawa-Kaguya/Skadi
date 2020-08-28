using System.Collections.Generic;
using com.cbgan.SuiseiBot.Code.Resource.TypeEnum.CmdType;

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
            KeyWords.Add(".r", WholeMatchCmdType.SurpriseMFK_Random);//随机数
            KeyWords.Add("给老子来个禁言套餐", WholeMatchCmdType.SurpriseMFK_Ban);
            KeyWords.Add("请问可以告诉我你的年龄吗？", WholeMatchCmdType.SurpriseMFK_24YearsOld);
            KeyWords.Add("给爷来个优质睡眠套餐", WholeMatchCmdType.SurpriseMFK_RedTea);
            //2 奇奇怪怪的签到
            KeyWords.Add("彗酱今天也很可爱", WholeMatchCmdType.Suisei_SignIn);
            //3 公主连结小功能
            //暂无(不是)
            //4 来点色图！
            KeyWords.Add("来点色图",WholeMatchCmdType.Hso);
            KeyWords.Add("来点涩图",WholeMatchCmdType.Hso);

            KeyWords.Add("debug", WholeMatchCmdType.Debug);
        }
    }
}
