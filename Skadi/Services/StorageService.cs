using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Skadi.Entities.ConfigModule;
using Skadi.Interface;
using Skadi.Resource;
using YamlDotNet.Serialization;
using YukariToolBox.LightLog;
using Path = System.IO.Path;

namespace Skadi.Services;

public class StorageService : IStorageService
{
#region 只读量

#if DEBUG
    private static readonly string ROOT_DIR = Environment.GetEnvironmentVariable("DebugDataPath");
#else
    private static readonly string ROOT_DIR = Environment.CurrentDirectory;
#endif

    private const string SUB_CONFIG = "config";
    private const string SUB_CRASH  = "crashlog";
    private const string SUB_DATA   = "data";
    private const string SUB_HSO    = "hso";

    private const  string FILE_GLOBAL_CONFIG = "server_config.yaml";
    private const  string FILE_USER_CONFIG   = "config.yaml";
    private const  string FILE_QA            = "qa.json";
    private const  string FILE_DB            = "data.db";
    private static string FILE_CRASH => $"crash-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.log";

#endregion

#region Config Buffer

    private ConcurrentDictionary<long, UserConfig> UserConfigs { get; }

    private GlobalConfig GlobalConfigInstance { get; set; }

    private IDeserializer Deserializer { get; }

#endregion

    public StorageService()
    {
        Log.Info("StorageService", "Service init");
        UserConfigs          = new ConcurrentDictionary<long, UserConfig>();
        GlobalConfigInstance = null;
        Deserializer = new DeserializerBuilder()
                       .IgnoreUnmatchedProperties()
                       .Build();
    }

#region Config

    public GlobalConfig GetGlobalConfig()
    {
        if (GlobalConfigInstance != null) return GlobalConfigInstance;

        Log.Debug("StorageService", "读取全局配置文件");
        string path = GetGlobalConfigFilePath();
        if (!File.Exists(path)) CreateAndWriteStrFile(path, ConfigResourse.InitGlobalConfig);

        GlobalConfig config = ReadYamlFile<GlobalConfig>(path);
        if (config == null) return config;

        //参数合法性检查
        if ((int)config.LogLevel is < 0 or > 3)
            config.LogLevel = LogLevel.Info;
        if (config.HeartBeatTimeOut == 0)
            config.HeartBeatTimeOut = 10;
        if (config.OnebotApiTimeOut == 0)
            config.OnebotApiTimeOut = 2000;
        if (config.Port is 0 or > 65535)
            config.Port = 9200;
        GlobalConfigInstance = config;
        return config;
    }

    public UserConfig GetUserConfig(long userId)
    {
        if (UserConfigs.ContainsKey(userId)) return UserConfigs[userId];

        Log.Debug("StorageService", $"读取用户[{userId}]配置文件");
        string path = GetUserConfigFilePath(userId);
        if (!File.Exists(path)) CreateAndWriteStrFile(path, ConfigResourse.InitUserConfig);

        UserConfig config = ReadYamlFile<UserConfig>(path);
        if (config == null) return config;

        //参数合法性检查
        if (config.HsoConfig.SizeLimit >= 1)
            config.HsoConfig.SizeLimit = 1024;
        UserConfigs.TryAdd(userId, config);
        return config;
    }

    public void RemoveUserConfig(long userId)
    {
        if (UserConfigs.ContainsKey(userId)) UserConfigs.Remove(userId, out _);
    }

#endregion

#region Path

    private static string GetSubDirPath(string subName)
    {
        string path = $"{ROOT_DIR.Trim('/').Trim('\\')}/{subName}";
        CheckDirExistsOrCreate(path);
        return path;
    }

    private static string GetGlobalConfigFilePath()
    {
        return $"{GetSubDirPath(SUB_CONFIG)}/{FILE_GLOBAL_CONFIG}";
    }

    private static string GetUserConfigFilePath(long userId)
    {
        string path = $"{GetSubDirPath(SUB_CONFIG)}/{userId}/{FILE_USER_CONFIG}";
        CheckDirExistsOrCreate(Path.GetDirectoryName(path));
        return path;
    }

    //TODO ZoneTree替代QA持久化
    public static string GetQAFilePath(long userId)
    {
        string path = $"{GetSubDirPath(SUB_CONFIG)}/{userId}/{FILE_QA}";
        CheckDirExistsOrCreate(Path.GetDirectoryName(path));
        return path;
    }

    //TODO 使用服务直接接管
    public static string GetHsoPath()
    {
        return $"{GetSubDirPath(SUB_HSO)}";
    }

    public static string GetUserDbPath(long userId)
    {
        string path = $"{GetSubDirPath(SUB_DATA)}/{userId}/{FILE_DB}";
        CheckDirExistsOrCreate(Path.GetDirectoryName(path));
        if (!File.Exists(path))
        {
            //数据库文件不存在，新建数据库
            Log.Warning("StorageService", "未找到数据库文件，创建新的数据库");
            File.Create(path).Close();
        }
        return path;
    }

    public static void CrashLogGen(string errorMessage)
    {
        string             crashFile    = $"{GetSubDirPath(SUB_CRASH)}/{FILE_CRASH}";
        using StreamWriter streamWriter = File.CreateText(crashFile);
        streamWriter.Write(errorMessage);
    }

#endregion

#region Util

    private void CreateAndWriteStrFile(string path, byte[] data)
    {
        Log.Debug("StorageService", $"Try write file:{path}");
        try
        {
            string           text   = Encoding.UTF8.GetString(data);
            using TextWriter writer = File.CreateText(path);
            writer.Write(text);
            writer.Close();
        }
        catch (Exception e)
        {
            Log.Fatal(e, "StorageService", "无法读取全局配置文件");
            Thread.Sleep(5000);
            Environment.Exit(-1);
        }
    }

    private T ReadYamlFile<T>(string path)
    {
        Log.Debug("StorageService", $"Try read yaml file:{path}");
        try
        {
            using TextReader reader = File.OpenText(path);
            return Deserializer.Deserialize<T>(reader);
        }
        catch (Exception e)
        {
            Log.Error("StorageService", Log.ErrorLogBuilder(e));
            return default;
        }
    }

    private static void CheckDirExistsOrCreate(string dir)
    {
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

#endregion
}