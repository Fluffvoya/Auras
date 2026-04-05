using PAMS.Core.Models;
using PAMS.Core.Repositories;

namespace PAMS.Core.Services;

public class ArchiveConfigService
{
    private readonly ArchiveConfigRepository _archiveConfigRepository;

    public ArchiveConfigService(ArchiveConfigRepository archiveConfigRepository)
    {
        _archiveConfigRepository = archiveConfigRepository;
    }

    public string ArchiveName => _archiveConfigRepository.ArchiveName;
}