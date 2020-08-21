using System.Collections.Generic;
using System.Reflection;
using com.cbgan.SuiseiBot.Code.Tool.Log;

namespace com.cbgan.SuiseiBot.Code.IO.Config
{
    internal class ConfigClass
    {
        /// <summary>
        /// 日志等级
        /// </summary>
        public LogLevel LogLevel { set; get; }
        /// <summary>
        /// 全局指令开头字符或字符串
        /// </summary>
        public string GlobalCommandStartStr { set; get; }
        /// <summary>
        /// 各模块的控制开关
        /// </summary>
        public Module ModuleSwitch { set; get; }
        /// <summary>
        /// 自动动态刷新参数设置
        /// </summary>
        public TimeToDD DD_Config { set; get; }
    }

    internal class Module
    {
        /// <summary>
        /// 神必娱乐模块
        /// </summary>
        public bool HaveFun { set; get; }
        /// <summary>
        /// 会战管理器模块
        /// </summary>
        public bool PCR_GuildManager { set; get; }
        /// <summary>
        /// 慧酱每日签到模块
        /// </summary>
        public bool Suisei { set; get; }
        /// <summary>
        /// 调试模块
        /// </summary>
        public bool Debug { set; get; }
        /// <summary>
        /// 公会排名查询
        /// </summary>
        public bool PCR_GuildRank { set; get; }
        /// <summary>
        /// 
        /// </summary>
        public bool PCR_Dynamic { set; get; }
        public bool DDHelper { set; get; }

        #region 将已启用的模块名转为字符串
        public override string ToString()
        {
            List<string> ret = new List<string>();
            //遍历使能设置中的所有属性
            foreach (PropertyInfo property in typeof(Module).GetProperties())
            {
                if ((bool)property.GetValue(this, null))
                {
                    ret.Add(property.Name);
                }
            }
            return string.Join("\n",ret);
        }
        #endregion
    }

    internal class TimeToDD
    {
        public int FlashTime { set; get; }
        public long[] Users { set; get; }
    }
}
