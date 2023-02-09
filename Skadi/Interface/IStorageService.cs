using System.IO;
using System.Threading.Tasks;
using Skadi.Entities.ConfigModule;

namespace Skadi.Interface;

public interface IStorageService
{
    GlobalConfig GetGlobalConfig();

    UserConfig GetUserConfig(long userId);

    void RemoveUserConfig(long userId);

    ValueTask<bool> SaveOrUpdateDataFile(MemoryStream data, long userId, string fileType, string fileName);
    
    ValueTask<MemoryStream> ReadUserDataFile(long userId, string fileType, string fileName);
}