using PAMS.Core.Models;
using PAMS.Core.Repositories;
using PAMS.Core.Utils;

namespace PAMS.Core.Services;

public class FileService
{
    private readonly MetadataRepository _metadata;

    private readonly TagIndexRepository _tagIndex;

    private readonly HashIndexRepository _hashIndex;

    private readonly ArchiveConfigRepository _archiveConfig;

    private readonly ConfigRepository _configRepository;

    public FileService(
        MetadataRepository metadata,
        TagIndexRepository tagIndex,
        HashIndexRepository hashIndex,
        ArchiveConfigRepository archiveConfig,
        ConfigRepository configRepository)
    {
        _metadata = metadata;
        _tagIndex = tagIndex;
        _hashIndex = hashIndex;
        _archiveConfig = archiveConfig;
        _configRepository = configRepository;
    }

    public FileRecord ImportFile(
        string sourcePath,
        string primaryTag,
        List<string> tags,
        string description = "",
        bool move = false)
    {
        var hash = HashHelper.ComputeSHA256(sourcePath);

        if (_hashIndex.Exists(hash))
            throw new Exception("Duplicate file detected");

        var fileName = Path.GetFileName(sourcePath);

        var rootDir = _archiveConfig.ArchivePath ?? "";

        var tagDir = Path.Combine(rootDir, primaryTag);

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
            CreatedTime = DateTime.UtcNow,
            Description = description
        };

        _metadata.Add(record);

        _hashIndex.Add(hash, record.Id);

        _tagIndex.Add(record.Id, tags);

        return record;
    }

    public void ChangeFileName(Guid id, string newFileName)
    {
        if (string.IsNullOrWhiteSpace(newFileName))
        {
            throw new ArgumentException("File name cannot be empty.", nameof(newFileName));
        }

        if (newFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException(
                $"File name contains invalid characters. Allowed characters exclude: {string.Join(", ", Path.GetInvalidFileNameChars())}",
                nameof(newFileName));
        }

        var record = _metadata.Get(id);
        if (record == null) throw new Exception("File record not found");

        var rootDir = _archiveConfig.ArchivePath ?? "";
        var oldPath = Path.Combine(rootDir, record.RelativePath);
        var newPath = Path.Combine(rootDir, record.PrimaryTag, newFileName);

        if (newPath == oldPath)
        {
            throw new IOException("A file with the same name already exists in the target directory.");
        }

        if (File.Exists(newPath))
        {
            throw new IOException("A file with the same name already exists in the target directory.");
        }

        var newHash = HashHelper.ComputeSHA256(newPath);

        if (_hashIndex.Exists(newHash))
        {
            throw new Exception("Duplicate file detected based on the new file content hash");
        }

        // 当一切都判断无误后再移动文件并修改元数据
        if (!File.Exists(oldPath))
        {
            throw new FileNotFoundException("File not found", oldPath);
        }

        File.Move(oldPath, newPath);

        record.Name = newFileName;
        record.RelativePath = Path.Combine(record.PrimaryTag, newFileName);

        _metadata.Remove(id);
        _metadata.Add(record);
    }
    
    public void ChangeDescription(Guid id, string newDescription)
    {
        var record = _metadata.Get(id);
        if (record == null) throw new Exception("File record not found");

        record.Description = newDescription;

        _metadata.Remove(id);
        _metadata.Add(record);
    }

    public void ChangePrimaryTag(Guid id, string newPrimaryTag)
    {
        var record = _metadata.Get(id);
        if (record == null) throw new Exception("File record not found");

        var rootDir = _archiveConfig.ArchivePath ?? "";
        var oldPath = Path.Combine(rootDir, record.RelativePath);
        var newDir = Path.Combine(rootDir, newPrimaryTag);
        var newPath = Path.Combine(newDir, record.Name);

        if (newPath == oldPath)
        {
            throw new IOException("A file with the same name already exists in the target directory.");
        }

        // 创建新目录并移动物理文件
        if (Directory.Exists(newDir))
        {
            Directory.CreateDirectory(newDir);
        }

        if (!File.Exists(oldPath))
        {
            throw new FileNotFoundException("File not found", oldPath);
        }

        File.Move(oldPath, newPath);
        // 更新内存中的记录属性
        record.PrimaryTag = newPrimaryTag;
        record.RelativePath = Path.Combine(newPrimaryTag, record.Name);

        // 逻辑约束：主标签理应存在于 Tags 列表中
        if (!record.Tags.Contains(newPrimaryTag))
        {
            record.Tags.Add(newPrimaryTag);
        }

        // 重建标签索引
        _tagIndex.Remove(id);
        _tagIndex.Add(id, record.Tags);

        // 更新元数据
        _metadata.Remove(id);
        _metadata.Add(record);
    }

    public void AddTags(Guid id, List<string> tags)
    {
        var record = _metadata.Get(id);
        if (record == null) throw new Exception("File record not found");

        // 将新标签合并到记录中（去重）
        foreach (var tag in tags)
        {
            if (!record.Tags.Contains(tag))
            {
                record.Tags.Add(tag);
            }
        }

        _tagIndex.Add(id, tags);

        // 更新元数据
        _metadata.Remove(id);
        _metadata.Add(record);
    }

    public void RemoveTags(Guid id, List<string> tags)
    {
        var record = _metadata.Get(id);
        if (record == null) throw new Exception("File record not found");

        if (tags.Contains(record.PrimaryTag))
        {
            throw new InvalidOperationException("Cannot remove the primary tag.");
        }

        record.Tags.RemoveAll(t => tags.Contains(t));

        _tagIndex.Remove(id);
        _tagIndex.Add(id, record.Tags);

        _metadata.Remove(id);
        _metadata.Add(record);
    }

    public void DeleteFile(Guid id)
    {
        var record = _metadata.Get(id);

        if (record == null)
            return;

        var rootDir = _archiveConfig.ArchivePath ?? "";

        var file = Path.Combine(rootDir, record.RelativePath);

        if (File.Exists(file))
            File.Delete(file);

        _metadata.Remove(id);

        _hashIndex.Remove(record.Hash);

        _tagIndex.Remove(id);
    }

    public void DeleteMetadataOnly(Guid id)
    {
        var record = _metadata.Get(id);

        if (record == null)
            return;

        _metadata.Remove(id);

        _hashIndex.Remove(record.Hash);

        _tagIndex.Remove(id);
    }
}