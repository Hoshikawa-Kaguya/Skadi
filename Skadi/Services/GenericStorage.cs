using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ProtoBuf;
using Skadi.Entities;
using Skadi.Entities.ConfigModule;
using Skadi.Interface;
using Skadi.Resource;
using Sora.Entities;
using YukariToolBox.LightLog;
using Path = System.IO.Path;
using YamlSerialization = YamlDotNet.Serialization;
using PbSerializer = ProtoBuf.Serializer;

namespace Skadi.Services;

public class GenericStorage : IGenericStorage
{
#region 只读量

#if DEBUG
    private static readonly string ROOT_DIR = Environment.GetEnvironmentVariable("DebugDataPath");
#else
    private static readonly string ROOT_DIR = Environment.CurrentDirectory;
#endif
    private static string FILE_CRASH => $"crash-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.log";

    private const string QA_FILE = "qa.bin";

#endregion

#region Config Buffer

    private ConcurrentDictionary<long, UserConfig> UserConfigs { get; }

    private GlobalConfig GlobalConfigInstance { get; set; }

    private YamlSerialization.IDeserializer YamlDeserializer { get; }

#endregion

    public GenericStorage()
    {
        Log.Info("GenericStorage", "Service init");
        UserConfigs          = new ConcurrentDictionary<long, UserConfig>();
        GlobalConfigInstance = null;
        YamlDeserializer = new YamlSerialization.DeserializerBuilder()
                           .IgnoreUnmatchedProperties()
                           .Build();
        InitQaFile().AsTask().Wait();
    }

#region Config

    public GlobalConfig GetGlobalConfig()
    {
        if (GlobalConfigInstance != null) return GlobalConfigInstance;

        Log.Debug("GenericStorage", "读取全局配置文件");
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

        Log.Debug("GenericStorage", $"读取用户[{userId}]配置文件");
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

#region QA

    public async ValueTask<Dictionary<QaKey, MessageBody>> ReadQaData()
    {
        string path = $"{GetDataDirPath()}/{QA_FILE}";
        await InitQaFile();
        Log.Debug("GenericStorage", "read qa file");
        using MemoryStream data = await ReadFile(path);
        if (data is null) return null;
        if (data.Length == 0) return new Dictionary<QaKey, MessageBody>();
        try
        {
            QaDataFile qaData = PbSerializer.Deserialize<QaDataFile>(data);
            return qaData.Data;
        }
        catch (Exception e)
        {
            Log.Error(e, "GenericStorage", "读取QA数据时发生错误");
            return null;
        }
    }

    public async ValueTask<bool> SaveQaData(Dictionary<QaKey, MessageBody> saveData)
    {
        QaDataFile file = new()
        {
            Data = saveData
        };
        string             path = $"{GetDataDirPath()}/{QA_FILE}";
        using MemoryStream data = file.SerializeData();
        Log.Debug("GenericStorage", "save qa file");
        return await SaveOrUpdateFile(data, path);
    }

    private async ValueTask InitQaFile()
    {
        string path = $"{GetDataDirPath()}/{QA_FILE}";
        if (!File.Exists(path))
        {
            Log.Warning("GenericStorage", "init qa file");
            QaDataFile file = new()
            {
                Data = new Dictionary<QaKey, MessageBody>()
            };
            using MemoryStream data = file.SerializeData();
            await SaveOrUpdateFile(data, path);
        }
    }

    [ProtoContract]
    private class QaDataFile
    {
        [ProtoMember(1)]
        public Dictionary<QaKey, MessageBody> Data;

        public MemoryStream SerializeData()
        {
            MemoryStream ms = new();
            PbSerializer.Serialize(ms, this);
            ms.Position = 0;
            return ms;
        }
    }

#endregion

#region GenericFile

    public async ValueTask<bool> SaveOrUpdateFile(MemoryStream data, string file)
    {
        try
        {
            await using FileStream fileStream = File.Create(file);
            data.Position = 0;
            await data.CopyToAsync(fileStream);
            fileStream.Close();
        }
        catch (Exception e)
        {
            Log.Error(e, "GenericStorage", $"File[{file}]write error");
            return false;
        }

        return true;
    }

    public async ValueTask<MemoryStream> ReadFile(string file)
    {
        if (!File.Exists(file)) return null;
        try
        {
            byte[] buf = await File.ReadAllBytesAsync(file);
            MemoryStream data = new(buf);
            data.Position = 0;
            return data;
        }
        catch (Exception e)
        {
            Log.Error(e, "GenericStorage", $"File[{file}]read error");
            return null;
        }
    }

    public ValueTask<bool> DeleteFile(string file)
    {
        if (!File.Exists(file)) return new ValueTask<bool>(false);
        File.Delete(file);
        return new ValueTask<bool>(true);
    }

#endregion

#region Path

    private static string GetGlobalConfigFilePath()
    {
        string path = $"{ROOT_DIR}/config/server_config.yaml";
        CheckDir(path);
        return path;
    }

    private static string GetUserConfigFilePath(long userId)
    {
        string path = $"{ROOT_DIR}/config/{userId}/config.yaml";
        CheckDir(path);
        return path;
    }

    //TODO 使用服务直接接管
    public static string GetHsoPath()
    {
        return $"{ROOT_DIR}/hso";
    }

    public static string GetUserDbPath(long userId)
    {
        string path = $"{ROOT_DIR}/data/{userId}/data.db";
        CheckDir(path);
        if (!File.Exists(path))
        {
            //数据库文件不存在，新建数据库
            Log.Warning("GenericStorage", "未找到数据库文件，创建新的数据库");
            File.Create(path).Close();
        }

        return path;
    }

    public static string GetUserDataDirPath(long userId, string dataType)
    {
        string path = $"{ROOT_DIR}/data/{userId}/{dataType}";
        CheckDir(path);
        return path;
    }

    public static string GetDataDirPath()
    {
        string path = $"{ROOT_DIR}/data";
        CheckDir(path);
        return path;
    }

    public static void CrashLogGen(string errorMessage)
    {
        string             crashFile    = $"{ROOT_DIR}/crash/{FILE_CRASH}";
        using StreamWriter streamWriter = File.CreateText(crashFile);
        streamWriter.Write(errorMessage);
    }

#endregion

#region Util

    private void CreateAndWriteStrFile(string path, byte[] data)
    {
        Log.Debug("GenericStorage", $"Try write file:{path}");
        try
        {
            string           text   = Encoding.UTF8.GetString(data);
            using TextWriter writer = File.CreateText(path);
            writer.Write(text);
            writer.Close();
        }
        catch (Exception e)
        {
            Log.Fatal(e, "GenericStorage", "无法读取全局配置文件");
            Thread.Sleep(5000);
            Environment.Exit(-1);
        }
    }

    private static void CheckDir(string path, bool isDir = false)
    {
        Log.Verbose("GenericStorage", $"Check work dir:{path}");
        Stack<string> paths = new();
        if (isDir) paths.Push(path);

        string dir = Path.GetDirectoryName(path);
        while (dir != ROOT_DIR)
        {
            paths.Push(dir);
            dir = Path.GetDirectoryName(dir);
        }

        while (paths.Count != 0)
        {
            string temp = paths.Pop();
            Log.Verbose("GenericStorage", $"dir_c:{temp}");
            if (!Directory.Exists(temp)) Directory.CreateDirectory(dir);
        }
    }

    private T ReadYamlFile<T>(string path)
    {
        Log.Debug("GenericStorage", $"Try read yaml file:{path}");
        try
        {
            using TextReader reader = File.OpenText(path);
            return YamlDeserializer.Deserialize<T>(reader);
        }
        catch (Exception e)
        {
            Log.Error("GenericStorage", Log.ErrorLogBuilder(e));
            return default;
        }
    }

#endregion
}