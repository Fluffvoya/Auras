using ASIS.Core.Repositories;

namespace ASIS.Test;

public class ArchiveConfigRepositoryTests : IDisposable
{
    private readonly string _tmpDir;

    public ArchiveConfigRepositoryTests()
    {
        _tmpDir = Path.Combine(Path.GetTempPath(), "asis_test_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tmpDir);
    }

    public void Dispose()
    {
        Directory.Delete(_tmpDir, true);
    }

    [Fact]
    public void ArchiveName_NewFile_DefaultsEmpty()
    {
        var path = Path.Combine(_tmpDir, "archive.json");
        var repo = new ArchiveConfigRepository(path);
        Assert.Equal(string.Empty, repo.ArchiveName);
    }

    [Fact]
    public void ArchiveName_ReadsFromFile()
    {
        var path = Path.Combine(_tmpDir, "archive.json");
        File.WriteAllText(path, "{\"Name\":\"MyArchive\",\"CreatedTime\":\"2024-01-01T00:00:00\"}");
        var repo = new ArchiveConfigRepository(path);
        Assert.Equal("MyArchive", repo.ArchiveName);
    }

    [Fact]
    public void ArchivePath_ReturnsDirectoryOfConfigFile()
    {
        var path = Path.Combine(_tmpDir, "archive.json");
        File.WriteAllText(path, "{\"Name\":\"Test\",\"CreatedTime\":\"2024-01-01T00:00:00\"}");
        var repo = new ArchiveConfigRepository(path);
        Assert.Equal(_tmpDir, repo.ArchivePath);
    }
}