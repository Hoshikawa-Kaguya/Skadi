using System.Collections.Generic;

namespace Native.Core
{
    /// <summary>
    /// 此文件为自动生成文件请勿修改
    /// </summary>
    class PluginInfo
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
        public string name = "SuiSeiBot";

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
        public string description = "饼干神必机器人，噫hihihihi";

        /// <summary>
        /// 酷Q事件
        /// </summary>
        public List<Event> @event = new List<Event>
        {
            new Event()
            {
                id       = 1,
                type     = 21,
                name     = "私聊消息处理",
                function = "_eventPrivateMsg",
                priority = 30000
            },
            new Event()
            {
                id       = 2,
                type     = 2,
                name     = "群消息处理",
                function = "_eventGroupMsg",
                priority = 30000
            },
            new Event()
            {
                id       = 6,
                type     = 102,
                name     = "群成员减少事件处理",
                function = "_eventSystem_GroupMemberDecrease",
                priority = 30000
            },
            new Event()
            {
                id       = 8,
                type     = 104,
                name     = "群禁言事件处理",
                function = "_eventSystem_GroupBan",
                priority = 30000
            },
            new Event()
            {
                id       = 1001,
                type     = 1001,
                name     = "酷Q启动事件",
                function = "_eventStartup",
                priority = 30000
            },
            new Event()
            {
                id       = 1002,
                type     = 1002,
                name     = "酷Q关闭事件",
                function = "_eventExit",
                priority = 30000
            },
            new Event()
            {
                id       = 1003,
                type     = 1003,
                name     = "应用已被启用",
                function = "_eventEnable",
                priority = 30000
            },
            new Event()
            {
                id       = 1004,
                type     = 1004,
                name     = "应用将被停用",
                function = "_eventDisable",
                priority = 30000
            },
        };

        /// <summary>
        /// 悬浮窗
        /// </summary>
        public List<Status> status = new List<Status>()
        {
            new Status()
            {
                id       = 1,
                name     = "运行时间",
                title    = "UPTIME",
                function = "_statusUptime",
                period   = "1000"
            }
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

    public class Status
    {
        /// <summary>
        /// 
        /// </summary>
        public int id { get; set; }
        /// <summary>
        /// 运行时间
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string title { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string function { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string period { get; set; }
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
