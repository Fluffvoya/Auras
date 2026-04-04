using PAMS.Core.Repositories;

namespace PAMS.Core.Services;

public class ConfigService
{
    private readonly ConfigRepository _configRepository;

    public ConfigService(ConfigRepository configRepository)
    {
        _configRepository = configRepository;
    }

    public void ChangeArchiveRoot(string archiveRoot)
    {
        
    }
}