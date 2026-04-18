using ASIS.Core.Repositories;

namespace ASIS.Test;

public class HashIndexRepositoryTests : IDisposable
{
    private readonly string _tmpDir;
    private readonly string _hashFile;
    private readonly HashIndexRepository _repo;

    public HashIndexRepositoryTests()
    {
        _tmpDir = Path.Combine(Path.GetTempPath(), "asis_test_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tmpDir);
        _hashFile = Path.Combine(_tmpDir, "hash_index.json");
        _repo = new HashIndexRepository(_hashFile);
    }

    public void Dispose()
    {
        Directory.Delete(_tmpDir, true);
    }

    [Fact]
    public void Exists_UnknownHash_ReturnsFalse()
    {
        Assert.False(_repo.Exists("unknownhash"));
    }

    [Fact]
    public void Add_ThenExists_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        _repo.Add("abc123", id);
        Assert.True(_repo.Exists("abc123"));
    }

    [Fact]
    public void Remove_ThenExists_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        _repo.Add("abc123", id);
        _repo.Remove("abc123");
        Assert.False(_repo.Exists("abc123"));
    }

    [Fact]
    public void Add_PersistsToFile()
    {
        var id = Guid.NewGuid();
        _repo.Add("persist_hash", id);
        var fresh = new HashIndexRepository(_hashFile);
        Assert.True(fresh.Exists("persist_hash"));
    }
}