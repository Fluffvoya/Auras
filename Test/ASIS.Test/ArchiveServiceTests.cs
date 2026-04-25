using ASIS.Core.Models;
using ASIS.Core.Repositories;
using ASIS.Core.Services;

namespace ASIS.Test;

public class ArchiveServiceTests : IDisposable
{
    private readonly string _tmpDir;
    private readonly string _archiveDir;
    private readonly MetadataRepository _metadata;
    private readonly ArchiveConfigRepository _archiveConfig;
    private readonly ArchiveService _archiveService;

    public ArchiveServiceTests()
    {
        _tmpDir = Path.Combine(Path.GetTempPath(), "asis_test_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tmpDir);
        _archiveDir = Path.Combine(_tmpDir, "archive");
        Directory.CreateDirectory(_archiveDir);

        var archiveJson = Path.Combine(_archiveDir, "archive.json");
        var metaJson = Path.Combine(_archiveDir, "metadata.json");

        File.WriteAllText(archiveJson, "{\"Name\":\"TestArchive\",\"CreatedTime\":\"2024-01-01T00:00:00\"}");

        _archiveConfig = new ArchiveConfigRepository(archiveJson);
        _metadata = new MetadataRepository(metaJson);
        _archiveService = new ArchiveService(_metadata, _archiveConfig);
    }

    public void Dispose()
    {
        Directory.Delete(_tmpDir, true);
    }

    private FileRecord AddMetadataRecord(string relativePath, string name)
    {
        var record = new FileRecord
        {
            Id = Guid.NewGuid(),
            Hash = Guid.NewGuid().ToString(),
            Name = name,
            RelativePath = relativePath,
            PrimaryTag = "test",
            Tags = new List<string> { "test" },
            CreatedTime = DateTime.UtcNow
        };
        _metadata.Add(record);
        return record;
    }

    private void CreateDiskFile(string relativePath)
    {
        var fullPath = Path.Combine(_archiveDir, relativePath);
        var dir = Path.GetDirectoryName(fullPath);
        if (dir != null) Directory.CreateDirectory(dir);
        File.WriteAllText(fullPath, "test content");
    }

    [Fact]
    public void Diff_EmptyMetadataAndDisk_ReturnsEmptyResults()
    {
        var result = _archiveService.Diff();

        Assert.Empty(result.OrphanedMetadata);
        Assert.Empty(result.OrphanedDiskFiles);
    }

    [Fact]
    public void Diff_MetadataExists_DiskMissing_ReturnsOrphanedMetadata()
    {
        AddMetadataRecord("doc/test.txt", "test.txt");
        AddMetadataRecord("doc2/file.txt", "file.txt");

        var result = _archiveService.Diff();

        Assert.Equal(2, result.OrphanedMetadata.Count);
        Assert.Empty(result.OrphanedDiskFiles);
    }

    [Fact]
    public void Diff_DiskExists_MetadataMissing_ReturnsOrphanedDiskFiles()
    {
        CreateDiskFile("doc/test.txt");
        CreateDiskFile("doc2/file.txt");

        var result = _archiveService.Diff();

        Assert.Empty(result.OrphanedMetadata);
        Assert.Equal(2, result.OrphanedDiskFiles.Count);
    }

    [Fact]
    public void Diff_BothExist_NoOrphans()
    {
        var record = AddMetadataRecord("doc/test.txt", "test.txt");
        CreateDiskFile("doc/test.txt");

        var result = _archiveService.Diff();

        Assert.Empty(result.OrphanedMetadata);
        Assert.Empty(result.OrphanedDiskFiles);
    }

    [Fact]
    public void Diff_MixedScenario_ReturnsCorrectOrphans()
    {
        // Metadata only (orphaned)
        AddMetadataRecord("doc/orphan_meta.txt", "orphan_meta.txt");
        // Both exist
        var record = AddMetadataRecord("doc/both.txt", "both.txt");
        CreateDiskFile("doc/both.txt");
        // Disk only (orphaned)
        CreateDiskFile("doc/orphan_disk.txt");

        var result = _archiveService.Diff();

        Assert.Single(result.OrphanedMetadata);
        Assert.Single(result.OrphanedDiskFiles);
        Assert.Equal("orphan_meta.txt", result.OrphanedMetadata[0].Name);
        Assert.Contains("doc/orphan_disk.txt", result.OrphanedDiskFiles);
    }

    [Fact]
    public void Diff_NonExistentArchivePath_ReturnsEmptyResults()
    {
        var nonExistentDir = Path.Combine(_tmpDir, "nonexistent");
        Directory.CreateDirectory(nonExistentDir);
        var archiveJson = Path.Combine(nonExistentDir, "archive.json");
        var metaJson = Path.Combine(nonExistentDir, "metadata.json");

        File.WriteAllText(archiveJson, "{\"Name\":\"TestArchive\",\"CreatedTime\":\"2024-01-01T00:00:00\"}");

        var archive = new ArchiveService(
            new MetadataRepository(metaJson),
            new ArchiveConfigRepository(archiveJson));

        var result = archive.Diff();

        Assert.Empty(result.OrphanedMetadata);
        Assert.Empty(result.OrphanedDiskFiles);
    }
}