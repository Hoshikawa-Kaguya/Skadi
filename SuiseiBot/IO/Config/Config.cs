using System;
using System.IO;
using System.Text;
using System.Threading;
using SharpYaml.Serialization;
using SuiseiBot.Code.IO.Config.ConfigClass;
using SuiseiBot.Code.IO.Config.Res;
using SuiseiBot.Code.Tool.LogUtils;

namespace SuiseiBot.Code.IO.Config
{
    internal class Config
    {
        #region 属性
        private string Path { set; get; }
        public MainConfig LoadedConfig { private set; get; }
        #endregion

        #region 构造函数
        /// <summary>
        /// ConfigIO构造函数，默认构造时加载本地配置文件
        /// </summary>
        /// <param name="loginQQ"></param>
        /// <param name="initConfig"></param>
        public Config(long loginQQ, bool initConfig = true)
        {
            this.Path = IOUtils.GetConfigPath(loginQQ.ToString());
            //执行一次加载
            if (initConfig) ConfigFileInit();
        }
        #endregion

        #region 公有方法
        /// <summary>
        /// 加载配置文件
        /// </summary>
        public bool LoadConfig()
        {
            try
            {
                Serializer       serializer = new Serializer();
                using TextReader reader     = File.OpenText(Path);
                LoadedConfig = serializer.Deserialize<MainConfig>(reader);
                return true;
            }
            catch (Exception e)
            {
                ConsoleLog.Error("ConfigIO ERROR", ConsoleLog.ErrorLogBuilder(e));
                return false;
            }
        }
        #endregion

        #region 私有方法
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
                    ConsoleLog.Debug("ConfigIO", "读取配置文件");
                    return;
                }
                //没读取到文件时创建新的文件
                ConsoleLog.Error("ConfigIO", "未找到配置文件");
                ConsoleLog.Warning("ConfigIO", "创建新的配置文件");
                string           initConfigText = Encoding.UTF8.GetString(InitRes.initconfig);
                using (TextWriter writer = File.CreateText(Path))
                {
                    writer.Write(initConfigText);
                    writer.Close();
                }
                //读取生成的配置文件
                if (!LoadConfig()) throw new IOException("无法读取生成的配置文件");
            }
            catch (Exception e)
            {
                ConsoleLog.Fatal("ConfigIO ERROR", ConsoleLog.ErrorLogBuilder(e));
                Thread.Sleep(5000);
                Environment.Exit(-1);
            }
        }
        #endregion
    }
}
