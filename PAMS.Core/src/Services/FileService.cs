using PAMS.Core.Models;
using PAMS.Core.Repositories;
using PAMS.Core.Utils;

namespace PAMS.Core.Services;

public class FileService
{
    private readonly MetadataRepository _metadata;

    private readonly TagIndexRepository _tagIndex;

    private readonly HashIndexRepository _hashIndex;

    private readonly ConfigRepository _configRepository;

    public FileService(
        MetadataRepository metadata,
        TagIndexRepository tagIndex,
        HashIndexRepository hashIndex,
        ConfigRepository configRepository)
    {
        _metadata = metadata;
        _tagIndex = tagIndex;
        _hashIndex = hashIndex;
        _configRepository = configRepository;
    }

    public FileRecord ImportFile(
        string sourcePath,
        string primaryTag,
        List<string> tags,
        bool move = false)
    {
        var hash = HashHelper.ComputeSHA256(sourcePath);

        if (_hashIndex.Exists(hash))
            throw new Exception("Duplicate file detected");

        var fileName = Path.GetFileName(sourcePath);

        var tagDir = Path.Combine(_configRepository.GetArchiveRoot(), primaryTag);

        Directory.CreateDirectory(tagDir);

        var target = Path.Combine(tagDir, fileName);

        if (move)
            File.Move(sourcePath, target);
        else
            File.Copy(sourcePath, target, true);

        var record = new FileRecord
        {
            Id = Guid.NewGuid(),
            Name = fileName,
            RelativePath = Path.Combine(primaryTag, fileName),
            PrimaryTag = primaryTag,
            Tags = tags,
            Hash = hash,
            CreatedTime = DateTime.UtcNow
        };

        _metadata.Add(record);

        _hashIndex.Add(hash, record.Id);

        _tagIndex.Add(record.Id, tags);

        return record;
    }

    public void DeleteFile(Guid id)
    {
        var record = _metadata.Get(id);

        if (record == null)
            return;

        var path = Path.Combine(_configRepository.GetArchiveRoot(), record.RelativePath);

        if (File.Exists(path))
            File.Delete(path);

        _metadata.Remove(id);

        _hashIndex.Remove(record.Hash);

        _tagIndex.Remove(id);
    }
}