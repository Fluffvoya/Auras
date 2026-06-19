using ASIS.CLI.Repositories;

namespace ASIS.CLI.Services;

public class ConfigService
{
    private readonly ConfigRepository _configRepository;

    public ConfigService(ConfigRepository configRepository)
    {
        _configRepository = configRepository;
    }
}