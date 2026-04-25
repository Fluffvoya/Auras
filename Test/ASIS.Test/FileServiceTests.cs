using ASIS.Core.Models;
using ASIS.Core.Repositories;
using ASIS.Core.Services;

namespace ASIS.Test;

public class FileServiceTests : IDisposable
{
    private readonly string _tmpDir;
    private readonly string _archiveDir;
    private readonly MetadataRepository _metadata;
    private readonly TagIndexRepository _tagIndex;
    private readonly HashIndexRepository _hashIndex;
    private readonly ArchiveConfigRepository _archiveConfig;
    private readonly FileService _fileService;

    public FileServiceTests()
    {
        _tmpDir = Path.Combine(Path.GetTempPath(), "asis_test_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tmpDir);
        _archiveDir = Path.Combine(_tmpDir, "archive");
        Directory.CreateDirectory(_archiveDir);

        var archiveJson = Path.Combine(_archiveDir, "archive.json");
        var metaJson = Path.Combine(_archiveDir, "metadata.json");
        var tagJson = Path.Combine(_archiveDir, "tag_index.json");
        var hashJson = Path.Combine(_archiveDir, "hash_index.json");

        // Write archive config with a name so ArchivePath resolves
        File.WriteAllText(archiveJson, "{\"Name\":\"TestArchive\",\"CreatedTime\":\"2024-01-01T00:00:00\"}");

        _archiveConfig = new ArchiveConfigRepository(archiveJson);
        _metadata = new MetadataRepository(metaJson);
        _tagIndex = new TagIndexRepository(tagJson);
        _hashIndex = new HashIndexRepository(hashJson);
        _fileService = new FileService(_metadata, _tagIndex, _hashIndex, _archiveConfig);
    }

    public void Dispose()
    {
        Directory.Delete(_tmpDir, true);
    }

    private string CreateSourceFile(string name, string content = "test content")
    {
        var path = Path.Combine(_tmpDir, "source", name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    private FileRecord ImportTestFile(string fileName, string primaryTag, List<string> tags, string content = "test content")
    {
        var source = CreateSourceFile(fileName, content);
        return _fileService.ImportFile(source, primaryTag, tags);
    }

    // --- ImportFile ---
    [Fact]
    public void ImportFile_Copy_Success()
    {
        var source = CreateSourceFile("test.txt", "hello");
        var record = _fileService.ImportFile(source, "doc", new List<string> { "doc" }, "a doc", false);

        Assert.NotNull(record);
        Assert.Equal("test.txt", record.Name);
        Assert.Equal("doc", record.PrimaryTag);
        Assert.Contains("doc", record.Tags);
        Assert.Equal("a doc", record.Description);
        Assert.True(File.Exists(Path.Combine(_archiveDir, "doc", "test.txt")));
        Assert.True(File.Exists(source)); // copy, source still exists
    }

    [Fact]
    public void ImportFile_Move_Success()
    {
        var source = CreateSourceFile("move.txt", "hello");
        var record = _fileService.ImportFile(source, "doc", new List<string> { "doc" }, "", true);

        Assert.True(File.Exists(Path.Combine(_archiveDir, "doc", "move.txt")));
        Assert.False(File.Exists(source)); // move, source gone
    }

    [Fact]
    public void ImportFile_DuplicateHash_Throws()
    {
        var content = "same content here";
        var source1 = CreateSourceFile("file1.txt", content);
        _fileService.ImportFile(source1, "doc", new List<string> { "doc" });

        var source2 = CreateSourceFile("file2.txt", content);
        Assert.Throws<Exception>(() => _fileService.ImportFile(source2, "doc", new List<string> { "doc" }));
    }

    [Fact]
    public void ImportFile_MetadataPersisted()
    {
        var record = ImportTestFile("persist.txt", "tag1", new List<string> { "tag1", "tag2" });

        var retrieved = _metadata.Get(record.Id);
        Assert.NotNull(retrieved);
        Assert.Equal(record.Name, retrieved.Name);
        Assert.Equal(record.Hash, retrieved.Hash);
    }

    // --- ChangeFileName ---
    [Fact]
    public void ChangeFileName_ValidRename()
    {
        var record = ImportTestFile("old.txt", "doc", new List<string> { "doc" }, "unique1");
        _fileService.ChangeFileName(record.Id, "new.txt");

        Assert.False(File.Exists(Path.Combine(_archiveDir, "doc", "old.txt")));
        Assert.True(File.Exists(Path.Combine(_archiveDir, "doc", "new.txt")));

        var updated = _metadata.Get(record.Id);
        Assert.NotNull(updated);
        Assert.Equal("new.txt", updated.Name);
    }

    [Fact]
    public void ChangeFileName_EmptyName_ThrowsArgumentException()
    {
        var record = ImportTestFile("test.txt", "doc", new List<string> { "doc" }, "unique2");
        Assert.Throws<ArgumentException>(() => _fileService.ChangeFileName(record.Id, ""));
    }

    [Fact]
    public void ChangeFileName_InvalidChars_ThrowsArgumentException()
    {
        var record = ImportTestFile("test.txt", "doc", new List<string> { "doc" }, "unique3");
        Assert.Throws<ArgumentException>(() => _fileService.ChangeFileName(record.Id, "bad|name.txt"));
    }

    [Fact]
    public void ChangeFileName_NonexistentId_Throws()
    {
        Assert.Throws<Exception>(() => _fileService.ChangeFileName(Guid.NewGuid(), "new.txt"));
    }

    // --- ChangeDescription ---
    [Fact]
    public void ChangeDescription_Valid()
    {
        var record = ImportTestFile("desc.txt", "doc", new List<string> { "doc" }, "unique4");
        _fileService.ChangeDescription(record.Id, "new description");

        var updated = _metadata.Get(record.Id);
        Assert.NotNull(updated);
        Assert.Equal("new description", updated.Description);
    }

    [Fact]
    public void ChangeDescription_NonexistentId_Throws()
    {
        Assert.Throws<Exception>(() => _fileService.ChangeDescription(Guid.NewGuid(), "desc"));
    }

    // --- ChangePrimaryTag ---
    [Fact]
    public void ChangePrimaryTag_Valid()
    {
        var record = ImportTestFile("tagtest.txt", "oldTag", new List<string> { "oldTag" }, "unique5");
        _fileService.ChangePrimaryTag(record.Id, "newTag");

        Assert.True(File.Exists(Path.Combine(_archiveDir, "newTag", "tagtest.txt")));

        var updated = _metadata.Get(record.Id);
        Assert.NotNull(updated);
        Assert.Equal("newTag", updated.PrimaryTag);
        Assert.Contains("newTag", updated.Tags);
    }

    [Fact]
    public void ChangePrimaryTag_NonexistentId_Throws()
    {
        Assert.Throws<Exception>(() => _fileService.ChangePrimaryTag(Guid.NewGuid(), "newTag"));
    }

    // --- AddTags ---
    [Fact]
    public void AddTags_NewTagsAdded()
    {
        var record = ImportTestFile("addtag.txt", "photo", new List<string> { "photo" }, "unique6");
        _fileService.AddTags(record.Id, new List<string> { "landscape", "summer" });

        var updated = _metadata.Get(record.Id);
        Assert.NotNull(updated);
        Assert.Contains("landscape", updated.Tags);
        Assert.Contains("summer", updated.Tags);
    }

    [Fact]
    public void AddTags_SkipsDuplicates()
    {
        var record = ImportTestFile("dup.txt", "photo", new List<string> { "photo", "landscape" }, "unique7");
        _fileService.AddTags(record.Id, new List<string> { "landscape" });

        var updated = _metadata.Get(record.Id);
        Assert.NotNull(updated);
        Assert.Single(updated.Tags, t => t == "landscape");
    }

    [Fact]
    public void AddTags_NonexistentId_Throws()
    {
        Assert.Throws<Exception>(() => _fileService.AddTags(Guid.NewGuid(), new List<string> { "tag" }));
    }

    // --- RemoveTags ---
    [Fact]
    public void RemoveTags_RemovesSpecifiedTags()
    {
        var record = ImportTestFile("rmtag.txt", "photo", new List<string> { "photo", "landscape", "summer" }, "unique8");
        _fileService.RemoveTags(record.Id, new List<string> { "landscape" });

        var updated = _metadata.Get(record.Id);
        Assert.NotNull(updated);
        Assert.DoesNotContain("landscape", updated.Tags);
    }

    [Fact]
    public void RemoveTags_PrimaryTag_ThrowsInvalidOperationException()
    {
        var record = ImportTestFile("ptag.txt", "photo", new List<string> { "photo" }, "unique9");
        Assert.Throws<InvalidOperationException>(() => _fileService.RemoveTags(record.Id, new List<string> { "photo" }));
    }

    [Fact]
    public void RemoveTags_NonexistentId_Throws()
    {
        Assert.Throws<Exception>(() => _fileService.RemoveTags(Guid.NewGuid(), new List<string> { "tag" }));
    }

    // --- DeleteFile ---
    [Fact]
    public void DeleteFile_RemovesPhysicalFileAndMetadata()
    {
        var record = ImportTestFile("del.txt", "doc", new List<string> { "doc" }, "unique10");
        var filePath = Path.Combine(_archiveDir, "doc", "del.txt");
        Assert.True(File.Exists(filePath));

        _fileService.DeleteFile(record.Id);

        Assert.False(File.Exists(filePath));
        Assert.Null(_metadata.Get(record.Id));
        Assert.False(_hashIndex.Exists(record.Hash));
    }

    [Fact]
    public void DeleteFile_NonexistentId_NoException()
    {
        _fileService.DeleteFile(Guid.NewGuid()); // should not throw
    }

    // --- DeleteMetadataOnly ---
    [Fact]
    public void DeleteMetadataOnly_KeepsPhysicalFile()
    {
        var record = ImportTestFile("metaonly.txt", "doc", new List<string> { "doc" }, "unique11");
        var filePath = Path.Combine(_archiveDir, "doc", "metaonly.txt");

        _fileService.DeleteMetadataOnly(record.Id);

        Assert.True(File.Exists(filePath)); // physical file still exists
        Assert.Null(_metadata.Get(record.Id));
    }

    [Fact]
    public void DeleteMetadataOnly_NonexistentId_NoException()
    {
        _fileService.DeleteMetadataOnly(Guid.NewGuid()); // should not throw
    }
}