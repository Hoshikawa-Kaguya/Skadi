using System.Collections.Generic;
using System.Reflection;

namespace SuiseiBot.IO.Config.ConfigModule
{
    /// <summary>
    /// 单用户配置文件定义
    /// </summary>
    internal class UserConfig
    {
        /// <summary>
        /// 各模块的控制开关
        /// </summary>
        public ModuleSwitch ModuleSwitch { set; get; }
        /// <summary>
        /// 自动动态刷新参数设置
        /// </summary>
        public BiliSubscription SubscriptionConfig { set; get; }
        /// <summary>
        /// 色图相关设置
        /// </summary>
        public Hso HsoConfig { set; get; }
    }

    /// <summary>
    /// 各模块开关
    /// </summary>
    internal class ModuleSwitch
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
        /// PCR国服动态订阅
        /// </summary>
        public bool PCR_Subscription { set; get; }
        /// <summary>
        /// B站UP主动态订阅
        /// </summary>
        public bool Bili_Subscription { set; get; }
        /// <summary>
        /// 来点色图
        /// </summary>
        public bool Hso { set; get; }
        /// <summary>
        /// 切噜翻译
        /// </summary>
        public bool Cheru { set; get; }

        #region 将已启用的模块名转为字符串
        public override string ToString()
        {
            List<string> ret = new List<string>();
            //遍历使能设置中的所有属性
            foreach (PropertyInfo property in typeof(ModuleSwitch).GetProperties())
            {
                if (property.GetValue(this, null) is bool isEnable && isEnable)
                {
                    ret.Add(property.Name);
                }
            }
            return string.Join("\n",ret);
        }
        #endregion
    }
}
