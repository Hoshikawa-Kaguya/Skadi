using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using Skadi.Entities;
using Skadi.Entities.ConfigModule;
using Sora.Entities;

namespace Skadi.Interface;

public interface IGenericStorage
{
    GlobalConfig GetGlobalConfig();

    UserConfig GetUserConfig(long userId);

    void RemoveUserConfig(long userId);

    ValueTask<bool> SaveOrUpdateFile(MemoryStream data, string file);

    ValueTask<MemoryStream> ReadFile(string file);

    ValueTask<ConcurrentDictionary<QaKey, MessageBody>> ReadQaData();

    ValueTask<bool> SaveQaData(ConcurrentDictionary<QaKey, MessageBody> saveData);
}