using ASIS.Core.Storage;

namespace ASIS.Test;

public class JsonStorageTests : IDisposable
{
    private readonly string _tmpDir;

    public JsonStorageTests()
    {
        _tmpDir = Path.Combine(Path.GetTempPath(), "asis_test_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tmpDir);
    }

    public void Dispose()
    {
        Directory.Delete(_tmpDir, true);
    }

    [Fact]
    public void Load_MissingFile_ReturnsNewInstance()
    {
        var path = Path.Combine(_tmpDir, "nonexistent.json");
        var result = JsonStorage.Load<List<string>>(path);
        Assert.Empty(result);
    }

    [Fact]
    public void Save_ThenLoad_RoundTrip()
    {
        var path = Path.Combine(_tmpDir, "roundtrip.json");
        var data = new List<string> { "a", "b", "c" };
        JsonStorage.Save(path, data);
        var loaded = JsonStorage.Load<List<string>>(path);
        Assert.Equal(data, loaded);
    }

    [Fact]
    public void Load_ExistingFile_Deserializes()
    {
        var path = Path.Combine(_tmpDir, "existing.json");
        JsonStorage.Save(path, new Dictionary<string, int> { ["x"] = 1 });
        var result = JsonStorage.Load<Dictionary<string, int>>(path);
        Assert.True(result.ContainsKey("x"));
        Assert.Equal(1, result["x"]);
    }
}