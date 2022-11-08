using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Skadi.Command;
using Skadi.Entities;
using Skadi.Tool;
using Sora.Entities;
using Sora.Util;
using YukariToolBox.LightLog;

namespace Skadi.Services;

//TODO IOC
internal class QaConfigService
{
    private const string QA_CONFIG_FILE = "qa.json";

    private string QaConfigPath { get; }

    public QaConfigService(long loginId)
    {
        QaConfigPath = IoUtils.GetUserConfigPath(loginId, QA_CONFIG_FILE);
        if (!File.Exists(QaConfigPath))
        {
            Log.Warning("QA", "未找到QA配置文件，创建新的配置文件");
            using TextWriter writer = File.CreateText(QaConfigPath);
            writer.Write("[]");
            writer.Close();
        }
    }

    public void AddNewQA(QaData newQA)
    {
        var qaData = ReadFile();
        qaData.Add(newQA);
        UpdateFile(qaData);
    }

    public void DeleteQA(MessageBody qMsg)
    {
        var qaData = ReadFile();
        qaData.RemoveAll(qa => QA.MessageEqual(qMsg, qa.qMsg));
        UpdateFile(qaData);
    }

    public List<QaData> GetAllQA()
    {
        return ReadFile();
    }

    private List<QaData> ReadFile()
    {
        List<QaData> commands = new();

        JToken qaJson = JToken.Parse(File.ReadAllText(QaConfigPath));

        List<(string qMsg, string aMsg, long groupId)> temp =
            qaJson.ToObject<List<(string qMsg, string aMsg, long groupId)>>();

        if (temp == null)
            return null;
        foreach ((string qMsgStr, string aMsgStr, long groupId) in temp)
            commands.Add(new QaData
            {
                qMsg = CQCodeUtil.DeserializeMessage(qMsgStr),
                aMsg = CQCodeUtil.DeserializeMessage(aMsgStr),
                GroupId = groupId
            });

        return commands;
    }

    private void UpdateFile(List<QaData> data)
    {
        List<(string qMsg, string aMsg, long groupId)> temp = new();

        foreach (QaData qaData in data)
            temp.Add((qaData.qMsg.SerializeMessage(), qaData.aMsg.SerializeMessage(), qaData.GroupId));

        JToken json = JToken.FromObject(temp);
        File.WriteAllText(QaConfigPath, json.ToString(Formatting.None));
    }
}