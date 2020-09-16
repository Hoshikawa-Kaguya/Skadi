using System.Collections.Generic;

namespace SuiseiBot.Code
{
    /// <summary>
    /// 此文件为自动生成文件请勿修改
    /// </summary>
    public class PluginInfo
    {
        /// <summary>
        /// Unknown
        /// </summary>
        public int ret = 1;

        /// <summary>
        /// API版本号
        /// </summary>
        public int apiver = 10;

        /// <summary>
        /// 插件名
        /// </summary>
        public string name = "SuiseiBot";

        /// <summary>
        /// 插件版本名
        /// </summary>
        public string version = "0.2.1.0";

        /// <summary>
        /// 版本号
        /// </summary>
        public int version_id = 2;

        /// <summary>
        /// 作者名
        /// </summary>
        public string author = "SUISEI_DEV_GROUP";

        /// <summary>
        /// 插件描述
        /// </summary>
        public string description = "すいちゃんは——今日もかわいい！";

        /// <summary>
        /// 酷Q事件
        /// </summary>
        public List<Event> @event = new List<Event>
        {
            new Event()
            {
                id       = 1,
                type     = 21,
                name     = "PrivateMessageInterface",
                function = "_eventPrivateMsg",
                priority = 30000
            },
            new Event()
            {
                id       = 2,
                type     = 2,
                name     = "GroupMessageInterface",
                function = "_eventGroupMsg",
                priority = 30000
            },
            new Event()
            {
                id       = 1003,
                type     = 1003,
                name     = "AppEnableInterface",
                function = "_eventEnable",
                priority = 30000
            },
        };

        public int[] auth = {
            30,
            101,
            103,
            106,
            121,
            124,
            126,
            128,
            130,
            131,
            132,
            160,
            161,
            180
        };
    }

    public class Event
    {
        /// <summary>
        /// 
        /// </summary>
        public int id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int type { get; set; }
        /// <summary>
        /// 私聊消息处理
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string function { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int priority { get; set; }
    }
}
