using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuiseiBot.Resource.TypeEnum
{
    internal enum SetuSourceType : int
    {
        /// <summary>
        /// 混合源模式
        /// </summary>
        Mix = 1,
        /// <summary>
        /// 从Lolicon获取图片信息
        /// </summary>
        Lolicon = 2,
        /// <summary>
        /// 从Yukari获取图片信息
        /// </summary>
        Yukari = 3,
        /// <summary>
        /// 从本地读取图片
        /// </summary>
        Local = 4
    }
}
