using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.cbgan.SuiseiBot.Resource
{
    public class ChatKeywords
    {
        /// <summary>
        /// 关键字初始化及存储
        /// </summary>
        public static Dictionary<string, int> key_word = new Dictionary<string, int>();
        public static void Keyword_init()
        {
            //1 娱乐功能
            key_word.Add(".r", 1);//随机数
            key_word.Add("给老子来个禁言套餐", 1);
            key_word.Add("请问可以告诉我你的年龄吗？", 1);
            key_word.Add("给爷来个优质睡眠套餐", 1);
            //2 奇奇怪怪的签到
            key_word.Add("彗酱今天也很可爱", 2);

            key_word.Add("debug", 1);
        }
    }
}
