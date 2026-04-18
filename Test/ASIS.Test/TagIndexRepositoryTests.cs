using ASIS.Core.Repositories;

namespace ASIS.Test;

public class TagIndexRepositoryTests : IDisposable
{
    private readonly string _tmpDir;
    private readonly string _tagFile;
    private readonly TagIndexRepository _repo;

    public TagIndexRepositoryTests()
    {
        _tmpDir = Path.Combine(Path.GetTempPath(), "asis_test_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tmpDir);
        _tagFile = Path.Combine(_tmpDir, "tag_index.json");
        _repo = new TagIndexRepository(_tagFile);
    }

    public void Dispose()
    {
        Directory.Delete(_tmpDir, true);
    }

    [Fact]
    public void Add_CreatesTagEntries()
    {
        var id = Guid.NewGuid();
        _repo.Add(id, new List<string> { "photo", "landscape" });
        Assert.Contains(id, _repo.GetFiles("photo"));
        Assert.Contains(id, _repo.GetFiles("landscape"));
    }

    [Fact]
    public void GetFiles_UnknownTag_ReturnsEmpty()
    {
        Assert.Empty(_repo.GetFiles("nonexistent"));
    }

    [Fact]
    public void Remove_RemovesFileIdFromAllTags()
    {
        var id = Guid.NewGuid();
        _repo.Add(id, new List<string> { "photo", "landscape" });
        _repo.Remove(id);
        Assert.Empty(_repo.GetFiles("photo"));
        Assert.Empty(_repo.GetFiles("landscape"));
    }

    [Fact]
    public void Add_PersistsToFile()
    {
        var id = Guid.NewGuid();
        _repo.Add(id, new List<string> { "photo" });
        var fresh = new TagIndexRepository(_tagFile);
        Assert.Contains(id, fresh.GetFiles("photo"));
    }
}