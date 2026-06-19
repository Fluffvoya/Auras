using ASIS.Core.Utils;

namespace ASIS.Test;

public class HashHelperTests : IDisposable
{
    private readonly string _tmpDir;

    public HashHelperTests()
    {
        _tmpDir = Path.Combine(Path.GetTempPath(), "asis_test_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tmpDir);
    }

    public void Dispose()
    {
        Directory.Delete(_tmpDir, true);
    }

    [Fact]
    public void ComputeSHA256_ReturnsHexString()
    {
        var file = Path.Combine(_tmpDir, "hash.txt");
        File.WriteAllText(file, "hello");
        var hash = HashHelper.ComputeSHA256(file);
        // SHA256 of "hello" is 2CF24DBA5FB0A30E26E83B2AC5B9E29E1B161E5C1FA7425E73043362938B9824
        Assert.Equal("2CF24DBA5FB0A30E26E83B2AC5B9E29E1B161E5C1FA7425E73043362938B9824", hash);
    }

    [Fact]
    public void ComputeSHA256_ConsistentAcrossCalls()
    {
        var file = Path.Combine(_tmpDir, "consistent.txt");
        File.WriteAllText(file, "same content");
        var hash1 = HashHelper.ComputeSHA256(file);
        var hash2 = HashHelper.ComputeSHA256(file);
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeSHA256_DifferentContent_DifferentHash()
    {
        var file1 = Path.Combine(_tmpDir, "a.txt");
        var file2 = Path.Combine(_tmpDir, "b.txt");
        File.WriteAllText(file1, "content A");
        File.WriteAllText(file2, "content B");
        Assert.NotEqual(HashHelper.ComputeSHA256(file1), HashHelper.ComputeSHA256(file2));
    }
}