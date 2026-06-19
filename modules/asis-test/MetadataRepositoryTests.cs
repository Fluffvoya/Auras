using ASIS.Core.Models;
using ASIS.Core.Repositories;

namespace ASIS.Test;

public class MetadataRepositoryTests : IDisposable
{
    private readonly string _tmpDir;
    private readonly string _metaFile;
    private readonly MetadataRepository _repo;

    public MetadataRepositoryTests()
    {
        _tmpDir = Path.Combine(Path.GetTempPath(), "asis_test_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tmpDir);
        _metaFile = Path.Combine(_tmpDir, "metadata.json");
        _repo = new MetadataRepository(_metaFile);
    }

    public void Dispose()
    {
        Directory.Delete(_tmpDir, true);
    }

    [Fact]
    public void GetAll_EmptyRepo_ReturnsEmptyList()
    {
        var all = _repo.GetAll();
        Assert.Empty(all);
    }

    [Fact]
    public void Add_AndGet_ReturnsRecord()
    {
        var record = new FileRecord { Id = Guid.NewGuid(), Name = "test.jpg" };
        _repo.Add(record);
        var retrieved = _repo.Get(record.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("test.jpg", retrieved.Name);
    }

    [Fact]
    public void Get_UnknownId_ReturnsNull()
    {
        Assert.Null(_repo.Get(Guid.NewGuid()));
    }

    [Fact]
    public void Remove_DeletesRecord()
    {
        var record = new FileRecord { Id = Guid.NewGuid(), Name = "rem.jpg" };
        _repo.Add(record);
        _repo.Remove(record.Id);
        Assert.Null(_repo.Get(record.Id));
    }

    [Fact]
    public void GetAll_ReturnsAllRecords()
    {
        _repo.Add(new FileRecord { Id = Guid.NewGuid(), Name = "a.jpg" });
        _repo.Add(new FileRecord { Id = Guid.NewGuid(), Name = "b.jpg" });
        Assert.Equal(2, _repo.GetAll().Count);
    }

    [Fact]
    public void Add_PersistsToFile()
    {
        var record = new FileRecord { Id = Guid.NewGuid(), Name = "persist.jpg" };
        _repo.Add(record);

        // Reload from disk
        var fresh = new MetadataRepository(_metaFile);
        Assert.NotNull(fresh.Get(record.Id));
    }
}