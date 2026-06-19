using ASIS.Core;
using ASIS.Core.Models;
using ASIS.Core.Repositories;
using AuraError.Exceptions;

namespace ASIS.Test;

public class BatchOperationsTests : IDisposable
{
    private readonly string _tmpDir;
    private readonly string _archiveDir;
    private readonly ASISCoreAPI _api;

    public BatchOperationsTests()
    {
        _tmpDir = Path.Combine(Path.GetTempPath(), "asis_test_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tmpDir);
        _archiveDir = Path.Combine(_tmpDir, "archive");
        Directory.CreateDirectory(_archiveDir);

        var archiveJson = Path.Combine(_archiveDir, "archive.json");
        File.WriteAllText(archiveJson, "{\"Name\":\"TestArchive\",\"CreatedTime\":\"2024-01-01T00:00:00\"}");
        _api = new ASISCoreAPI(_archiveDir);
    }

    public void Dispose()
    {
        Directory.Delete(_tmpDir, true);
    }

    private string CreateSourceFile(string name, string content)
    {
        var path = Path.Combine(_tmpDir, "source", name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    private FileRecord ImportTestFile(string fileName, string primaryTag,
        List<string> tags, string content, string description = "")
    {
        var source = CreateSourceFile(fileName, content);
        return _api.ImportFile(source, primaryTag, tags, description, false);
    }

    private List<FileRecord> GetAllFiles()
    {
        return _api.SearchByName("");
    }

    // --- Batch ChangeFileName ---

    [Fact]
    public void BatchChangeFileName_AllSuccess()
    {
        // Files in different primary tags to avoid rename conflicts
        var f1 = ImportTestFile("a.txt", "doc", new List<string>(), "content_a");
        var f2 = ImportTestFile("b.txt", "img", new List<string>(), "content_b");
        var f3 = ImportTestFile("c.txt", "src", new List<string>(), "content_c");

        var result = _api.ChangeFileName(new[] { f1.Id, f2.Id, f3.Id }, "new.txt");

        Assert.Equal(3, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);

        var allFiles = GetAllFiles();
        Assert.All(allFiles, f => Assert.Equal("new.txt", f.Name));
    }

    [Fact]
    public void BatchChangeFileName_PartialFailure()
    {
        var f1 = ImportTestFile("a.txt", "doc", new List<string>(), "content_a");
        var f2 = ImportTestFile("b.txt", "img", new List<string>(), "content_b");
        var missingId = Guid.NewGuid();

        var result = _api.ChangeFileName(new[] { f1.Id, f2.Id, missingId }, "new.txt");

        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(1, result.FailureCount);
        var failure = result.Items.First(i => !i.IsSuccess);
        Assert.Equal(missingId, failure.FileId);
        Assert.Equal("FILE_NOT_FOUND", failure.ErrorCode);
    }

    [Fact]
    public void BatchChangeFileName_AllFailures()
    {
        var result = _api.ChangeFileName(new[] { Guid.NewGuid(), Guid.NewGuid() }, "new.txt");

        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(2, result.FailureCount);
    }

    [Fact]
    public void BatchChangeFileName_EmptyList()
    {
        var result = _api.ChangeFileName(Array.Empty<Guid>(), "new.txt");

        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
    }

    [Fact]
    public void BatchChangeFileName_ConflictInSameTag()
    {
        // Two files in the same tag: renaming both to the same name causes conflict on the second
        var f1 = ImportTestFile("a.txt", "doc", new List<string>(), "content_a");
        var f2 = ImportTestFile("b.txt", "doc", new List<string>(), "content_b");

        var result = _api.ChangeFileName(new[] { f1.Id, f2.Id }, "same.txt");

        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(1, result.FailureCount);
        Assert.Equal("FILE_NAME_CONFLICT", result.Items.First(i => !i.IsSuccess).ErrorCode);
    }

    // --- Batch ChangeDescription ---

    [Fact]
    public void BatchChangeDescription_AllSuccess()
    {
        var f1 = ImportTestFile("a.txt", "doc", new List<string>(), "content_a");
        var f2 = ImportTestFile("b.txt", "doc", new List<string>(), "content_b");

        var result = _api.ChangeDescription(new[] { f1.Id, f2.Id }, "updated description");

        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);

        var allFiles = GetAllFiles();
        Assert.All(allFiles, f => Assert.Equal("updated description", f.Description));
    }

    // --- Batch ChangePrimaryTag ---

    [Fact]
    public void BatchChangePrimaryTag_AllSuccess()
    {
        var f1 = ImportTestFile("a.txt", "doc", new List<string>(), "content_a");
        var f2 = ImportTestFile("b.txt", "doc", new List<string>(), "content_b");

        var result = _api.ChangePrimaryTag(new[] { f1.Id, f2.Id }, "newtag");

        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);

        var allFiles = GetAllFiles();
        Assert.All(allFiles, f => Assert.Equal("newtag", f.PrimaryTag));
    }

    [Fact]
    public void BatchChangePrimaryTag_MixedFailure()
    {
        var f1 = ImportTestFile("a.txt", "doc", new List<string>(), "content_a");
        var f2 = ImportTestFile("b.txt", "doc", new List<string>(), "content_b");

        // Delete physical file for f2 to cause PHYSICAL_FILE_NOT_FOUND
        File.Delete(Path.Combine(_archiveDir, "doc", "b.txt"));

        var result = _api.ChangePrimaryTag(new[] { f1.Id, f2.Id }, "newtag");

        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(1, result.FailureCount);
        Assert.Equal("PHYSICAL_FILE_NOT_FOUND", result.Items.First(i => !i.IsSuccess).ErrorCode);
    }

    // --- Batch AddTags ---

    [Fact]
    public void BatchAddTags_AllSuccess()
    {
        var f1 = ImportTestFile("a.txt", "doc", new List<string> { "existing" }, "content_a");
        var f2 = ImportTestFile("b.txt", "doc", new List<string>(), "content_b");

        var result = _api.AddTags(new[] { f1.Id, f2.Id }, new List<string> { "newtag" });

        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);

        var r1 = _api.SearchByName("a.txt").Single();
        var r2 = _api.SearchByName("b.txt").Single();
        Assert.Contains("newtag", r1.Tags);
        Assert.Contains("newtag", r2.Tags);
    }

    // --- Batch RemoveTags ---

    [Fact]
    public void BatchRemoveTags_AllSuccess()
    {
        var f1 = ImportTestFile("a.txt", "doc", new List<string> { "extra", "removable" }, "content_a");
        var f2 = ImportTestFile("b.txt", "doc", new List<string> { "extra", "removable" }, "content_b");

        var result = _api.RemoveTags(new[] { f1.Id, f2.Id }, new List<string> { "removable" });

        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);

        var r1 = _api.SearchByName("a.txt").Single();
        Assert.DoesNotContain("removable", r1.Tags);
    }

    [Fact]
    public void BatchRemoveTags_PrimaryTagFailure()
    {
        var f1 = ImportTestFile("a.txt", "doc", new List<string> { "doc" }, "content_a");
        var f2 = ImportTestFile("b.txt", "extra", new List<string>(), "content_b");
        _api.AddTags(f2.Id, new List<string> { "removable" });

        var result = _api.RemoveTags(new[] { f1.Id, f2.Id }, new List<string> { "doc", "removable" });

        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(1, result.FailureCount);
        Assert.Equal("INVALID_TAG_OPERATION", result.Items.First(i => !i.IsSuccess).ErrorCode);
    }

    // --- Batch DeleteFile ---

    [Fact]
    public void BatchDeleteFile_AllSuccess()
    {
        var f1 = ImportTestFile("a.txt", "doc", new List<string>(), "content_a");
        var f2 = ImportTestFile("b.txt", "doc", new List<string>(), "content_b");

        var result = _api.DeleteFile(new[] { f1.Id, f2.Id });

        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
        Assert.Empty(GetAllFiles());
    }

    // --- Batch DeleteMetadataOnly ---

    [Fact]
    public void BatchDeleteMetadataOnly_AllSuccess()
    {
        var f1 = ImportTestFile("a.txt", "doc", new List<string>(), "content_a");
        var f2 = ImportTestFile("b.txt", "doc", new List<string>(), "content_b");
        var file1 = Path.Combine(_archiveDir, "doc", "a.txt");

        var result = _api.DeleteMetadataOnly(new[] { f1.Id, f2.Id });

        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
        Assert.Empty(GetAllFiles());
        Assert.True(File.Exists(file1)); // physical file kept
    }

    // --- BatchResult consistency ---

    [Fact]
    public void BatchResult_CountsAreConsistent()
    {
        var f1 = ImportTestFile("a.txt", "doc", new List<string>(), "content_a");
        var missingId = Guid.NewGuid();

        var result = _api.ChangeFileName(new[] { f1.Id, missingId }, "new.txt");

        Assert.Equal(result.TotalCount, result.SuccessCount + result.FailureCount);
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public void BatchResult_EmptyListCounts()
    {
        var result = _api.DeleteFile(Array.Empty<Guid>());

        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
    }
}
