namespace ASIS.Test;

[Collection("CLI Tests")]
public class CliIdLookupTests
{
    [Fact]
    public void Process_IdLookup_Found_ShowsDetails()
    {
        using var helper = new CliTestHelper();
        var record = helper.ImportViaApi("test.txt", "doc");
        var (_, output) = helper.Run($"id {record.Id}");
        Assert.Contains("test.txt", output);
    }

    [Fact]
    public void Process_IdLookup_FullFlag_ShowsAllFields()
    {
        using var helper = new CliTestHelper();
        var record = helper.ImportViaApi("test.txt", "doc");
        var (_, output) = helper.Run($"id {record.Id} --full");
        Assert.Contains("test.txt", output);
        // --full shows hash and path fields in a table
        var stripped = System.Text.RegularExpressions.Regex.Replace(output, @"\x1b\[[\d;]*m", "");
        Assert.Contains("Hash", stripped);
        Assert.Contains("Path", stripped);
    }

    [Fact]
    public void Process_IdLookup_NotFound_ShowsError()
    {
        using var helper = new CliTestHelper();
        var (_, output) = helper.Run($"id {Guid.NewGuid()}");
        Assert.Contains("No file found", output);
    }

    [Fact]
    public void Process_IdLookup_InvalidGuid_ShowsError()
    {
        using var helper = new CliTestHelper();
        var (_, output) = helper.Run("id not-a-guid");
        Assert.Contains("Invalid GUID", output);
    }

    [Fact]
    public void Process_IdLookup_MissingArgs_ShowsUsage()
    {
        using var helper = new CliTestHelper();
        var (_, output) = helper.Run("id");
        Assert.Contains("Usage", output);
    }
}
