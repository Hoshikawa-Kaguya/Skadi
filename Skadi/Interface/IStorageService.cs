using Skadi.Entities.ConfigModule;

namespace Skadi.Interface;

public interface IStorageService
{
    GlobalConfig GetGlobalConfig();

    UserConfig GetUserConfig(long userId);

    void RemoveUserConfig(long userId);
}