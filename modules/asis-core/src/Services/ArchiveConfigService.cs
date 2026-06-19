using ASIS.Core.Models;
using ASIS.Core.Repositories;

namespace ASIS.Core.Services;

public class ArchiveConfigService
{
    private readonly ArchiveConfigRepository _archiveConfigRepository;

    public ArchiveConfigService(ArchiveConfigRepository archiveConfigRepository)
    {
        _archiveConfigRepository = archiveConfigRepository;
    }

    public string ArchiveName => _archiveConfigRepository.ArchiveName;
}