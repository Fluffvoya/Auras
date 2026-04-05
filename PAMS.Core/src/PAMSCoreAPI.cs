using PAMS.Core.Models;
using PAMS.Core.Repositories;
using PAMS.Core.Services;

namespace PAMS.Core;

public class PAMSCoreAPI
{
    private readonly FileService _fileService;

    private readonly SearchService _searchService;

    private readonly ArchiveConfigService _archiveConfigService;

    private readonly ConfigService _configService;

    public PAMSCoreAPI(string configFile, string archiveRoot)
    {
        var configRepo = new ConfigRepository(configFile);

        var archiveConfigRepo
            = new ArchiveConfigRepository(Path.Combine(archiveRoot, "archive.json"));

        var metadataRepo =
            new MetadataRepository(Path.Combine(archiveRoot, "metadata.json"));

        var tagIndexRepo =
            new TagIndexRepository(Path.Combine(archiveRoot, "tag_index.json"));

        var hashIndexRepo =
            new HashIndexRepository(Path.Combine(archiveRoot, "hash_index.json"));

        _fileService = new FileService(metadataRepo, tagIndexRepo, hashIndexRepo, archiveConfigRepo, configRepo);
        _searchService = new SearchService(metadataRepo, tagIndexRepo);
        _archiveConfigService = new ArchiveConfigService(archiveConfigRepo);
        _configService = new ConfigService(configRepo);
    }

    public string ArchiveName => _archiveConfigService.ArchiveName;

    public void ChangeArchiveRoot(string archiveRoot)
    {
    }

    public FileRecord ImportFile(string sourcePath,
        string primaryTag,
        List<string> tags,
        bool move = false)
        => _fileService.ImportFile(sourcePath, primaryTag, tags, move);


    public void DeleteFile(string file)
    {
        var fileRecords = _searchService.SearchByName(file);
        foreach (var fileRecord in fileRecords)
        {
            _fileService.DeleteFile(fileRecord.Id);
        }
    }

    public void DeleteFile(Guid id)
    {
        _fileService.DeleteFile(id);
    }

    public List<FileRecord> SearchByName(string keyword) => _searchService.SearchByName(keyword);

    public List<FileRecord> SearchByTags(List<string> tags) => _searchService.SearchByTags(tags);
}