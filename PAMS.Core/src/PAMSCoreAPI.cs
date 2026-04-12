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

    public FileRecord ImportFile(string sourcePath,
        string primaryTag,
        List<string> tags,
        string description = "",
        bool move = false)
        => _fileService.ImportFile(sourcePath, primaryTag, tags, description, move);

    public void ChangeFileName(Guid id, string newFileName)
    {
        _fileService.ChangeFileName(id, newFileName);
    }

    public void ChangeFileName(string file, string newFileName)
    {
        var fileRecords = _searchService.SearchByName(file);
        foreach (var fileRecord in fileRecords)
        {
            _fileService.ChangeFileName(fileRecord.Id, newFileName);
        }
    }

    public void ChangeDescription(Guid id, string newDescription)
    {
        _fileService.ChangeDescription(id, newDescription);
    }

    public void ChangeDescription(string file, string newDescription)
    {
        var fileRecords = _searchService.SearchByName(file);
        foreach (var fileRecord in fileRecords)
        {
            _fileService.ChangeDescription(fileRecord.Id, newDescription);
        }
    }

    public void ChangePrimaryTag(Guid id, string newPrimaryTag)
    {
        _fileService.ChangePrimaryTag(id, newPrimaryTag);
    }

    public void ChangePrimaryTag(string file, string newPrimaryTag)
    {
        var fileRecords = _searchService.SearchByName(file);
        foreach (var fileRecord in fileRecords)
        {
            _fileService.ChangePrimaryTag(fileRecord.Id, newPrimaryTag);
        }
    }

    public void AddTags(Guid id, List<string> tags)
    {
        _fileService.AddTags(id, tags);
    }

    public void AddTags(string file, List<string> tags)
    {
        var fileRecords = _searchService.SearchByName(file);
        foreach (var fileRecord in fileRecords)
        {
            _fileService.AddTags(fileRecord.Id, tags);
        }
    }

    public void RemoveTags(Guid id, List<string> tags)
    {
        _fileService.RemoveTags(id, tags);
    }

    public void RemoveTags(string file, List<string> tags)
    {
        var fileRecords = _searchService.SearchByName(file);
        foreach (var fileRecord in fileRecords)
        {
            _fileService.RemoveTags(fileRecord.Id, tags);
        }
    }

    public void DeleteFile(Guid id)
    {
        _fileService.DeleteFile(id);
    }

    public void DeleteFile(string file)
    {
        var fileRecords = _searchService.SearchByName(file);
        foreach (var fileRecord in fileRecords)
        {
            _fileService.DeleteFile(fileRecord.Id);
        }
    }

    public List<FileRecord> SearchByName(string keyword) => _searchService.SearchByName(keyword);

    public List<FileRecord> SearchByTags(List<string> tags) => _searchService.SearchByTags(tags);
}