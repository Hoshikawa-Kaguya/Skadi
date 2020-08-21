using System;
using System.IO;
using System.Threading;
using com.cbgan.SuiseiBot.Code.Tool.Log;
using SharpYaml.Serialization;

namespace com.cbgan.SuiseiBot.Code.IO.Config
{
    internal class ConfigIO
    {
        #region 属性
        private string Path { set; get; }
        public ConfigClass LoadedConfig { private set; get; }
        #endregion

        #region 构造函数
        public ConfigIO(long LoginQQ)
        {
            this.Path = IOUtils.GetConfigPath(LoginQQ.ToString());
            //执行一次初始化
            ConfigFileInit();
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 初始化配置文件
        /// </summary>
        /// <returns>ConfigClass</returns>
        private ConfigClass getInitConfigClass()
        {
            return new ConfigClass
            {
                GlobalCommandStartStr = string.Empty,
                LogLevel              = LogLevel.Info,
                ModuleSwitch = new Module
                {
                    DDHelper         = false,
                    Debug            = false,
                    HaveFun          = true,
                    PCR_GuildManager = false,
                    PCR_GuildRank    = false,
                    PCR_Dynamic      = false,
                    Suisei           = false
                },
                DD_Config = new TimeToDD
                {
                    FlashTime = 3600,
                    Users     = new long[]{}
                }
            };
        }
        /// <summary>
        /// 加载配置文件
        /// </summary>
        private bool LoadConfig()
        {
            try
            {
                Serializer serializer = new Serializer();
                using TextReader reader = File.OpenText(Path);
                LoadedConfig = serializer.Deserialize<ConfigClass>(reader);
                return true;
            }
            catch (Exception e)
            {
                ConsoleLog.Debug("无法读取配置文件", ConsoleLog.ErrorLogBuilder(e));
                return false;
            }
        }
        /// <summary>
        /// 初始化配置文件并返回当前配置文件内容
        /// </summary>
        private void ConfigFileInit()
        {
            try
            {
                //当读取到文件时直接返回
                if (File.Exists(Path) && LoadConfig())
                {
                    ConsoleLog.Warning("初始化", "读取配置文件");
                    return;
                }
                //没读取到文件时创建新的文件
                ConsoleLog.Warning("初始化", "未找到配置文件");
                Serializer       serializer = new Serializer(new SerializerSettings { });
                ConfigClass      config     = getInitConfigClass();
                string           configText = serializer.Serialize(config);
                using TextWriter writer     = File.CreateText(Path);
                writer.Write(configText);
                LoadedConfig = config;
            }
            catch (Exception e)
            {
                ConsoleLog.Fatal("初始化错误", ConsoleLog.ErrorLogBuilder(e));
                Thread.Sleep(5000);
                Environment.Exit(-1);
            }
        }
        #endregion
    }
}
