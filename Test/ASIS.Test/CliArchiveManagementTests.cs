namespace ASIS.Test;

[Collection("CLI Tests")]
public class CliArchiveManagementTests
{
    [Fact]
    public void Process_CreateArchive_CreatesDirectoryAndConfig()
    {
        using var helper = new CliTestHelper(noArchive: true);
        var archiveName = "NewArchive";
        var (_, output) = helper.Run($"create {archiveName} {helper.TmpDir}");
        Assert.Contains("created", output.ToLower());
        Assert.True(File.Exists(Path.Combine(helper.TmpDir, archiveName, "archive.json")));
    }

    [Fact]
    public void Process_CreateArchive_AlreadyExists_ShowsWarning()
    {
        using var helper = new CliTestHelper(noArchive: true);
        var archiveName = "ExistingArchive";
        Directory.CreateDirectory(Path.Combine(helper.TmpDir, archiveName));
        var (_, output) = helper.Run($"create {archiveName} {helper.TmpDir}");
        Assert.Contains("already exists", output.ToLower());
    }

    [Fact]
    public void Process_CreateArchive_MissingArgs_ShowsUsage()
    {
        using var helper = new CliTestHelper(noArchive: true);
        var (_, output) = helper.Run("create");
        Assert.Contains("Usage", output);
    }

    [Fact]
    public void Process_OpenArchive_Success()
    {
        using var helper = new CliTestHelper(noArchive: true);
        // Create an archive first
        var archiveName = "OpenTest";
        var archiveDir = Path.Combine(helper.TmpDir, archiveName);
        Directory.CreateDirectory(archiveDir);
        File.WriteAllText(Path.Combine(archiveDir, "archive.json"),
            $"{{\"Name\":\"{archiveName}\",\"CreatedTime\":\"2024-01-01T00:00:00\"}}");

        var (_, output) = helper.Run($"open {archiveDir}");
        Assert.Contains("Opened archive", output);
    }

    [Fact]
    public void Process_OpenArchive_DirectoryNotFound_ShowsError()
    {
        using var helper = new CliTestHelper(noArchive: true);
        var (_, output) = helper.Run("open /nonexistent/path");
        Assert.Contains("not found", output.ToLower());
    }

    [Fact]
    public void Process_OpenArchive_MissingArchiveJson_ShowsError()
    {
        using var helper = new CliTestHelper(noArchive: true);
        var emptyDir = Path.Combine(helper.TmpDir, "empty");
        Directory.CreateDirectory(emptyDir);
        var (_, output) = helper.Run($"open {emptyDir}");
        Assert.Contains("missing archive.json", output);
    }

    [Fact]
    public void Process_CloseArchive_ThenArchiveShowsNoOpen()
    {
        using var helper = new CliTestHelper();
        helper.Run("close");
        var (_, output) = helper.Run("archive");
        Assert.Contains("No archive open", output);
    }

    [Fact]
    public void Process_ArchiveInfo_ShowsDetails()
    {
        using var helper = new CliTestHelper();
        helper.ImportViaApi("test.txt", "doc");
        var (_, output) = helper.Run("archive");
        Assert.Contains("TestArchive", output);
    }

    [Fact]
    public void Process_ArchiveInfo_NoArchive_ShowsError()
    {
        using var helper = new CliTestHelper(noArchive: true);
        var (_, output) = helper.Run("archive");
        Assert.Contains("No archive open", output);
    }

    [Fact]
    public void Process_Diff_CleanArchive_ShowsClean()
    {
        using var helper = new CliTestHelper();
        helper.ImportViaApi("test.txt", "doc");
        var (_, output) = helper.Run("diff");
        Assert.Contains("clean", output.ToLower());
    }

    [Fact]
    public void Process_Diff_OrphanedMetadata_ShowsCount()
    {
        using var helper = new CliTestHelper();
        var record = helper.ImportViaApi("test.txt", "doc");
        // Delete the physical file but leave metadata
        var filePath = Path.Combine(helper.ArchiveDir, record.RelativePath);
        if (File.Exists(filePath)) File.Delete(filePath);
        var (_, output) = helper.Run("diff");
        Assert.Contains("orphaned", output.ToLower());
    }

    [Fact]
    public void Process_Diff_NoArchive_ShowsError()
    {
        using var helper = new CliTestHelper(noArchive: true);
        var (_, output) = helper.Run("diff");
        Assert.Contains("No archive open", output);
    }
}
