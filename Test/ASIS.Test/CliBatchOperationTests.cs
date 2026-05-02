namespace ASIS.Test;

[Collection("CLI Tests")]
public class CliBatchOperationTests
{
    [Fact]
    public void Process_BatchRename_Success()
    {
        using var helper = new CliTestHelper();
        helper.ImportViaApi("test.txt", "doc");
        var (_, output) = helper.Run("batch rename test.txt renamed.txt");
        Assert.Contains("Succeeded", output);
    }

    [Fact]
    public void Process_BatchRename_MissingArgs_ShowsUsage()
    {
        using var helper = new CliTestHelper();
        var (_, output) = helper.Run("batch rename");
        Assert.Contains("Usage", output);
    }

    [Fact]
    public void Process_BatchDescribe_Success()
    {
        using var helper = new CliTestHelper();
        helper.ImportViaApi("test.txt", "doc");
        var (_, output) = helper.Run("batch describe test.txt new description");
        Assert.Contains("Succeeded", output);
    }

    [Fact]
    public void Process_BatchUnknownOp_ShowsError()
    {
        using var helper = new CliTestHelper();
        helper.ImportViaApi("test.txt", "doc");
        var (_, output) = helper.Run("batch badop test.txt");
        Assert.Contains("Unknown batch operation", output);
    }

    [Fact]
    public void Process_BatchNoMatch_ShowsWarning()
    {
        using var helper = new CliTestHelper();
        var (_, output) = helper.Run("batch rename nonexistent new.txt");
        Assert.Contains("No files match", output);
    }
}
