using ASIS.Core.Utils;

namespace ASIS.Test;

public class PathHelperTests : IDisposable
{
    private readonly string _tmpDir;

    public PathHelperTests()
    {
        _tmpDir = Path.Combine(Path.GetTempPath(), "asis_test_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tmpDir);
    }

    public void Dispose()
    {
        Directory.Delete(_tmpDir, true);
    }

    [Fact]
    public void GetRelativePath_ReturnsCorrectRelativePath()
    {
        var root = Path.Combine(_tmpDir, "archive");
        var full = Path.Combine(root, "photos", "pic.jpg");
        var result = PathHelper.GetRelativePath(root, full);
        Assert.Equal(Path.Combine("photos", "pic.jpg"), result);
    }

    [Fact]
    public void GetAbsolutePath_ReturnsCorrectCombinedPath()
    {
        var root = Path.Combine(_tmpDir, "archive");
        var result = PathHelper.GetAbsolutePath(root, Path.Combine("photos", "pic.jpg"));
        Assert.Equal(Path.Combine(root, "photos", "pic.jpg"), result);
    }
}