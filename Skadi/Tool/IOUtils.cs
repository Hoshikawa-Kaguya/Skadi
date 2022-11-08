using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YukariToolBox.LightLog;

namespace Skadi.Tool;

internal static class IoUtils
{
    #region IO工具

    /// <summary>
    /// 获取错误报告路径
    /// </summary>
    private static string GetCrashLogPath()
    {
        var pathBuilder = new StringBuilder();
#if DEBUG
        pathBuilder.Append(Environment.GetEnvironmentVariable("DebugDataPath"));
#else
            pathBuilder.Append(Environment.CurrentDirectory);
#endif
        pathBuilder.Append("/crashlog");
        //检查目录是否存在，不存在则新建一个
        Directory.CreateDirectory(pathBuilder.ToString());
        return pathBuilder.ToString();
    }

    /// <summary>
    /// 获取应用配置文件的绝对路径
    /// </summary>
    public static string GetUserConfigPath(long userId, string fileName)
    {
        if (userId < 10000)
            return null;
        var pathBuilder = new StringBuilder();
#if DEBUG
        pathBuilder.Append(Environment.GetEnvironmentVariable("DebugDataPath"));
#else
            pathBuilder.Append(Environment.CurrentDirectory);
#endif
        pathBuilder.Append("/config/");
        //二级文件夹
        pathBuilder.Append(userId);
        //检查目录是否存在，不存在则新建一个
        Directory.CreateDirectory(pathBuilder.ToString());
        pathBuilder.Append($"/{fileName}");
        return pathBuilder.ToString();
    }

    public static string GetGlobalConfigPath()
    {
        var pathBuilder = new StringBuilder();
#if DEBUG
        pathBuilder.Append(Environment.GetEnvironmentVariable("DebugDataPath"));
#else
            pathBuilder.Append(Environment.CurrentDirectory);
#endif
        pathBuilder.Append("/config");
        //检查目录是否存在，不存在则新建一个
        Directory.CreateDirectory(pathBuilder.ToString());
        pathBuilder.Append("/server_config.yaml");
        return pathBuilder.ToString();
    }

    /// <summary>
    /// 获取应用色图文件的绝对路径
    /// </summary>
    public static string GetHsoPath()
    {
        var pathBuilder = new StringBuilder();
#if DEBUG
        pathBuilder.Append(Environment.GetEnvironmentVariable("DebugDataPath"));
#else
            pathBuilder.Append(Environment.CurrentDirectory);
#endif
        pathBuilder.Append("/data/image/hso");
        //检查目录是否存在，不存在则新建一个
        Directory.CreateDirectory(pathBuilder.ToString());
        return pathBuilder.ToString();
    }

    /// <summary>
    /// 创建错误报告文件
    /// </summary>
    /// <param name="errorMessage">错误信息</param>
    public static void CrashLogGen(string errorMessage)
    {
        var pathBuilder = new StringBuilder();
        pathBuilder.Append(GetCrashLogPath());
        pathBuilder.Append("crash-");
        pathBuilder.Append(DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"));
        pathBuilder.Append(".log");

        using StreamWriter streamWriter = File.CreateText(pathBuilder.ToString());
        streamWriter.Write(errorMessage);
    }

    /// <summary>
    /// 获取色图文件夹的大小(Byte)
    /// </summary>
    public static long GetHsoSize()
    {
        return new DirectoryInfo(GetHsoPath())
               .GetFiles("*", SearchOption.AllDirectories)
               .Select(file => file.Length)
               .Sum();
    }

    /// <summary>
    /// 检查文件是否存在，如果不存在则创建新的空文件
    /// </summary>
    /// <param name="path">文件路径</param>
    public static bool CheckDbFileExists(string path)
    {
        try
        {
            if (File.Exists(path))
                return true;
            //数据库文件不存在，新建数据库
            Log.Warning("数据库初始化", "未找到数据库文件，创建新的数据库");
            Directory.CreateDirectory(Path.GetPathRoot(path) ?? string.Empty);
            File.Create(path).Close();
            return true;
        }
        catch (Exception e)
        {
            Log.Error(e, "IO", "File Check Error");
            return false;
        }
    }

    #endregion

    #region 文件读取工具

    /// <summary>
    /// 读取Json文件并返回为一个JObject
    /// </summary>
    /// <param name="jsonPath">json文件路径</param>
    /// <returns>保存整个文件信息的JObject</returns>
    public static JToken LoadJsonFile(string jsonPath)
    {
        try
        {
            StreamReader jsonFile = File.OpenText(jsonPath);
            JsonTextReader reader = new JsonTextReader(jsonFile);
            JToken jsonObject = JToken.ReadFrom(reader);
            return jsonObject;
        }
        catch (Exception e)
        {
            Log.Error("IO ERROR", $"读取文件{jsonPath}时出错，错误：\n{Log.ErrorLogBuilder(e)}");
            return null;
        }
    }

    #endregion

    #region 文件写入工具

    /// <summary>
    /// 将byte数组转换为文件并保存到指定地址
    /// </summary>
    /// <param name="buff">byte数组</param>
    /// <param name="savePath">保存地址</param>
    public static bool Bytes2File(byte[] buff, string savePath)
    {
        try
        {
            if (File.Exists(savePath))
                File.Delete(savePath);

            //将byte数组数据写入文件流
            using FileStream fileStream = new FileStream(savePath, FileMode.CreateNew);
            using BinaryWriter binaryWriter = new BinaryWriter(fileStream);
            binaryWriter.Write(buff, 0, buff.Length);
            binaryWriter.Close();
            fileStream.Close();
            return true;
        }
        catch (Exception e)
        {
            Log.Error("IO", $"保存文件时发生错误\n{Log.ErrorLogBuilder(e)}");
            return false;
        }
    }

    #endregion
}