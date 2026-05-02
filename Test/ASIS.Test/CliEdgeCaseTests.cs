namespace ASIS.Test;

[Collection("CLI Tests")]
public class CliEdgeCaseTests
{
    [Fact]
    public void Process_UnknownCommand_ShowsError()
    {
        using var helper = new CliTestHelper();
        var (_, output) = helper.Run("foobar");
        Assert.Contains("Unknown command", output);
    }

    [Fact]
    public void Process_EmptyInput_ReturnsTrue()
    {
        using var helper = new CliTestHelper();
        var (continueRunning, _) = helper.Run("");
        Assert.True(continueRunning);
    }

    [Fact]
    public void Process_Exit_ReturnsFalse()
    {
        using var helper = new CliTestHelper();
        var (continueRunning, _) = helper.Run("exit");
        Assert.False(continueRunning);
    }

    [Fact]
    public void Process_Help_ShowsAllCommands()
    {
        using var helper = new CliTestHelper();
        var (_, output) = helper.Run("help");
        Assert.Contains("create", output);
        Assert.Contains("open", output);
        Assert.Contains("import", output);
        Assert.Contains("search", output);
    }

    [Fact]
    public void Process_HelpSpecificCommand_ShowsUsage()
    {
        using var helper = new CliTestHelper();
        var (_, output) = helper.Run("help import");
        Assert.Contains("import", output);
    }

    [Fact]
    public void Process_HelpUnknownCommand_ShowsError()
    {
        using var helper = new CliTestHelper();
        var (_, output) = helper.Run("help foobar");
        Assert.Contains("Unknown command", output);
    }

    [Fact]
    public void Process_IdPrefix_ResolveFile_Works()
    {
        using var helper = new CliTestHelper();
        var record = helper.ImportViaApi("test.txt", "doc");
        var (_, output) = helper.Run($"info id:{record.Id}");
        Assert.Contains("test.txt", output);
    }

    [Fact]
    public void Process_IdPrefix_InvalidGuid_ShowsError()
    {
        using var helper = new CliTestHelper();
        var (_, output) = helper.Run("info id:notaguid");
        Assert.Contains("Invalid GUID", output);
    }

    [Fact]
    public void Process_CaseInsensitiveCommands_Work()
    {
        using var helper = new CliTestHelper();
        var (_, output) = helper.Run("HELP");
        Assert.Contains("create", output);
    }
}
