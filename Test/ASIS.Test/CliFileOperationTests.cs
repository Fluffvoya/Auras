namespace ASIS.Test;

[Collection("CLI Tests")]
public class CliFileOperationTests
{
    [Fact]
    public void Process_Import_CopyMode_FileCopied()
    {
        using var helper = new CliTestHelper();
        var sourcePath = helper.CreateSourceFile("test.txt");
        var (_, output) = helper.Run($"import {sourcePath} doc");
        Assert.Contains("Imported", output);
        Assert.True(File.Exists(sourcePath), "Source file should still exist in copy mode");
    }

    [Fact]
    public void Process_Import_MoveMode_FileMoved()
    {
        using var helper = new CliTestHelper();
        var sourcePath = helper.CreateSourceFile("test.txt");
        var (_, output) = helper.Run($"import {sourcePath} doc --move");
        Assert.Contains("Imported", output);
        Assert.False(File.Exists(sourcePath), "Source file should be gone in move mode");
    }

    [Fact]
    public void Process_Import_WithDescription_DescriptionSet()
    {
        using var helper = new CliTestHelper();
        var sourcePath = helper.CreateSourceFile("test.txt");
        helper.Run($"import {sourcePath} doc --desc \"my file\"");
        var (_, searchOutput) = helper.Run("search name test");
        Assert.Contains("my file", searchOutput);
    }

    [Fact]
    public void Process_Import_WithAdditionalTags_TagsAdded()
    {
        using var helper = new CliTestHelper();
        var sourcePath = helper.CreateSourceFile("test.txt");
        helper.Run($"import {sourcePath} doc landscape summer");
        var (_, searchOutput) = helper.Run("search tag landscape");
        Assert.Contains("Found 1", searchOutput);
    }

    [Fact]
    public void Process_Import_MissingArgs_ShowsUsage()
    {
        using var helper = new CliTestHelper();
        var (_, output) = helper.Run("import");
        Assert.Contains("Usage", output);
    }

    [Fact]
    public void Process_Import_DuplicateHash_ShowsError()
    {
        using var helper = new CliTestHelper();
        var sourcePath = helper.CreateSourceFile("test.txt", "same content");
        helper.Run($"import {sourcePath} doc");
        var sourcePath2 = helper.CreateSourceFile("test2.txt", "same content");
        var (_, output) = helper.Run($"import {sourcePath2} doc");
        Assert.Contains("Import failed", output);
    }

    [Fact]
    public void Process_Rename_Success()
    {
        using var helper = new CliTestHelper();
        helper.ImportViaApi("test.txt", "doc");
        var (_, output) = helper.Run("rename test.txt renamed.txt");
        Assert.Contains("File renamed", output);
    }

    [Fact]
    public void Process_Rename_MissingArgs_ShowsUsage()
    {
        using var helper = new CliTestHelper();
        var (_, output) = helper.Run("rename");
        Assert.Contains("Usage", output);
    }

    [Fact]
    public void Process_Rename_NoMatch_ShowsError()
    {
        using var helper = new CliTestHelper();
        var (_, output) = helper.Run("rename nonexistent new.txt");
        Assert.Contains("No file found", output);
    }

    [Fact]
    public void Process_Retag_Success()
    {
        using var helper = new CliTestHelper();
        helper.ImportViaApi("test.txt", "doc");
        var (_, output) = helper.Run("retag test.txt img");
        Assert.Contains("Primary tag updated", output);
    }

    [Fact]
    public void Process_Retag_MissingArgs_ShowsUsage()
    {
        using var helper = new CliTestHelper();
        var (_, output) = helper.Run("retag");
        Assert.Contains("Usage", output);
    }

    [Fact]
    public void Process_TagAdd_Success()
    {
        using var helper = new CliTestHelper();
        helper.ImportViaApi("test.txt", "doc");
        var (_, output) = helper.Run("tag add test.txt landscape,summer");
        Assert.Contains("Tags added", output);
    }

    [Fact]
    public void Process_TagRemove_Success()
    {
        using var helper = new CliTestHelper();
        helper.ImportViaApi("test.txt", "doc", new List<string> { "doc", "landscape", "summer" });
        var (_, output) = helper.Run("tag remove test.txt landscape");
        Assert.Contains("Tags removed", output);
    }

    [Fact]
    public void Process_TagList_ShowsTags()
    {
        using var helper = new CliTestHelper();
        helper.ImportViaApi("test.txt", "doc");
        var (_, output) = helper.Run("tag list test.txt");
        Assert.Contains("test.txt", output);
    }

    [Fact]
    public void Process_TagUnknownSubcommand_ShowsError()
    {
        using var helper = new CliTestHelper();
        helper.ImportViaApi("test.txt", "doc");
        var (_, output) = helper.Run("tag badcmd test.txt");
        Assert.Contains("Unknown subcommand", output);
    }

    [Fact]
    public void Process_Info_ShowsFileDetails()
    {
        using var helper = new CliTestHelper();
        var record = helper.ImportViaApi("test.txt", "doc");
        var (_, output) = helper.Run("info test.txt");
        Assert.Contains(record.Id.ToString(), output);
        Assert.Contains("test.txt", output);
    }

    [Fact]
    public void Process_Info_NoMatch_ShowsError()
    {
        using var helper = new CliTestHelper();
        var (_, output) = helper.Run("info nonexistent");
        Assert.Contains("No file found", output);
    }

    [Fact]
    public void Process_Describe_Success()
    {
        using var helper = new CliTestHelper();
        helper.ImportViaApi("test.txt", "doc");
        var (_, output) = helper.Run("describe test.txt new description text");
        Assert.Contains("Description updated", output);
    }

    [Fact]
    public void Process_Delete_FileAndMetadataRemoved()
    {
        using var helper = new CliTestHelper();
        helper.ImportViaApi("test.txt", "doc");
        var (_, output) = helper.Run("delete test.txt");
        Assert.Contains("File deleted", output);
        var (_, searchOutput) = helper.Run("search name test");
        Assert.Contains("No matching files", searchOutput);
    }

    [Fact]
    public void Process_Delete_NoMatch_ShowsError()
    {
        using var helper = new CliTestHelper();
        var (_, output) = helper.Run("delete nonexistent");
        Assert.Contains("No file found", output);
    }

    [Fact]
    public void Process_Unlink_MetadataRemovedFileKept()
    {
        using var helper = new CliTestHelper();
        var record = helper.ImportViaApi("test.txt", "doc");
        var filePath = Path.Combine(helper.ArchiveDir, record.RelativePath);
        var (_, output) = helper.Run("unlink test.txt");
        Assert.Contains("Metadata removed", output);
        Assert.True(File.Exists(filePath), "Physical file should still exist after unlink");
    }

    [Fact]
    public void Process_Unlink_NoMatch_ShowsError()
    {
        using var helper = new CliTestHelper();
        var (_, output) = helper.Run("unlink nonexistent");
        Assert.Contains("No file found", output);
    }

    [Fact]
    public void Process_FileOps_NoArchiveOpen_ShowsError()
    {
        using var helper = new CliTestHelper(noArchive: true);
        var sourcePath = helper.CreateSourceFile("test.txt");
        var (_, output) = helper.Run($"import {sourcePath} doc");
        Assert.Contains("No archive open", output);
    }
}
