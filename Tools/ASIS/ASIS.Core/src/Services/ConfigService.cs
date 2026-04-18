using ASIS.Core.Repositories;

namespace ASIS.Core.Services;

public class ConfigService
{
    private readonly ConfigRepository _configRepository;

    public ConfigService(ConfigRepository configRepository)
    {
        _configRepository = configRepository;
    }
}