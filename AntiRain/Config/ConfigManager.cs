using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using AntiRain.Config.ConfigModule;
using AntiRain.IO;
using SharpYaml.Serialization;
using YukariToolBox.LightLog;

namespace AntiRain.Config
{
    internal static class ConfigManager
    {
        #region 配置存储区

        private static readonly Dictionary<long, UserConfig> _userConfigs  = new();
        private static          GlobalConfig                 _globalConfig = new();

        private const string CONFIG_FILE = "config.yaml";

        #endregion

        #region 公有方法

        /// <summary>
        /// 初始化用户配置文件并返回当前配置文件内容
        /// 初始化成功后写入到私有属性中，第二次读取则不需要重新读取
        /// </summary>
        public static bool UserConfigFileInit(long uid)
        {
            try
            {
                var userConfigPath = IoUtils.GetUserConfigPath(uid, CONFIG_FILE);
                //当读取到文件时直接返回
                if (File.Exists(userConfigPath) && LoadUserConfig(out var ret, userConfigPath))
                {
                    Log.Debug("ConfigIO", "读取配置文件");
                    //已经存在则更新
                    if (_userConfigs.ContainsKey(uid))
                    {
                        _userConfigs[uid] = ret;
                        return true;
                    }

                    return _userConfigs.TryAdd(uid, ret);
                }

                //没读取到文件时创建新的文件
                Log.Error("ConfigIO", "未找到配置文件");
                Log.Warning("ConfigIO", "创建新的配置文件");
                var initConfigText = Encoding.UTF8.GetString(InitRes.InitUserConfig);
                using (TextWriter writer = File.CreateText(userConfigPath))
                {
                    writer.Write(initConfigText);
                    writer.Close();
                }

                //读取生成的配置文件
                if (!LoadUserConfig(out var retNew, userConfigPath)) throw new IOException("无法读取生成的配置文件");
                return _userConfigs.TryAdd(uid, retNew);
            }
            catch (Exception e)
            {
                Log.Fatal(e, "ConfigIO", "初始化配置文件时发生错误");
                Thread.Sleep(5000);
                Environment.Exit(-1);
            }

            return false;
        }

        /// <summary>
        /// 初始化全局配置文件并返回当前配置文件内容
        /// 初始化成功后写入到私有属性中，第二次读取则不需要重新读取
        /// </summary>
        public static bool GlobalConfigFileInit()
        {
            try
            {
                var globalConfigPath = IoUtils.GetGlobalConfigPath();
                //当读取到文件时直接返回
                if (File.Exists(globalConfigPath) && LoadGlobalConfig(out var ret, globalConfigPath))
                {
                    Log.Debug("ConfigIO", "读取配置文件");
                    _globalConfig = ret;
                    return true;
                }

                //没读取到文件时创建新的文件
                Log.Error("ConfigIO", "未找到配置文件");
                Log.Warning("ConfigIO", "创建新的配置文件");
                var initConfigText = Encoding.UTF8.GetString(InitRes.InitGlobalConfig);
                using (TextWriter writer = File.CreateText(globalConfigPath))
                {
                    writer.Write(initConfigText);
                    writer.Close();
                }

                //读取生成的配置文件
                if (!LoadGlobalConfig(out var retNew, globalConfigPath)) throw new IOException("无法读取生成的配置文件");
                _globalConfig = retNew;
                return true;
            }
            catch (Exception e)
            {
                Log.Fatal(e, "ConfigIO", "无法读取全局配置文件，程序将停止");
                Thread.Sleep(5000);
                Environment.Exit(-1);
            }

            _globalConfig = null;
            return false;
        }

        /// <summary>
        /// 尝试获取用户配置
        /// </summary>
        /// <param name="uid">uid</param>
        /// <param name="userConfig">配置</param>
        public static bool TryGetUserConfig(long uid, out UserConfig userConfig)
            => _userConfigs.TryGetValue(uid, out userConfig);

        /// <summary>
        /// 尝试获取全局配置
        /// </summary>
        /// <param name="globalConfig">配置</param>
        public static bool TryGetGlobalConfig(out GlobalConfig globalConfig)
        {
            globalConfig = _globalConfig;
            return _globalConfig != null;
        }

        /// <summary>
        /// 移除已过期的用户配置
        /// </summary>
        public static bool TryRemoveUserConfig(long uid)
            => _userConfigs.Remove(uid);

        #endregion

        #region 私有读取方法

        /// <summary>
        /// 加载用户配置文件
        /// 读取成功后写入到私有属性中，第二次读取则不需要重新读取
        /// </summary>
        /// <param name="userConfig">读取到的配置文件数据</param>
        /// <param name="path">路径</param>
        private static bool LoadUserConfig(out UserConfig userConfig, string path)
        {
            Log.Debug("ConfigIO", "读取用户配置");
            try
            {
                //反序列化配置文件
                var              serializer = new Serializer();
                using TextReader reader     = File.OpenText(path);
                userConfig = serializer.Deserialize<UserConfig>(reader);
                if (userConfig is null) return false;
                //参数合法性检查
                if (userConfig.HsoConfig.SizeLimit >= 1) return true;
                Log.Error("读取用户配置", "参数值超出合法范围，重新生成配置文件");
                userConfig = null;
                return false;
            }
            catch (Exception e)
            {
                Log.Error("读取用户配置文件时发生错误", Log.ErrorLogBuilder(e));
                userConfig = null;
                return false;
            }
        }

        /// <summary>
        /// 加载服务器全局配置文件
        /// 读取成功后写入到私有属性中，第二次读取则不需要重新读取
        /// </summary>
        /// <param name="globalConfig">读取到的配置文件数据</param>
        /// <param name="path">路径</param>
        private static bool LoadGlobalConfig(out GlobalConfig globalConfig, string path)
        {
            Log.Debug("ConfigIO", "读取全局配置");
            try
            {
                //反序列化配置文件
                var              serializer = new Serializer();
                using TextReader reader     = File.OpenText(path);
                globalConfig = serializer.Deserialize<GlobalConfig>(reader);
                if(globalConfig is null) return false;
                //参数合法性检查
                if ((int) globalConfig.LogLevel is < 0 or > 3 ||
                    globalConfig.HeartBeatTimeOut == 0        ||
                    globalConfig.OnebotApiTimeOut == 0        ||
                    globalConfig.Port is 0 or > 65535)
                {
                    Log.Error("读取全局配置", "参数值超出合法范围，重新生成配置文件");
                    globalConfig = null;
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                Log.Error("读取全局配置文件时发生错误", Log.ErrorLogBuilder(e));
                globalConfig = null;
                return false;
            }
        }

        #endregion
    }
}