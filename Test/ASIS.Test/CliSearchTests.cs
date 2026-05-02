namespace ASIS.Test;

[Collection("CLI Tests")]
public class CliSearchTests
{
    [Fact]
    public void Process_SearchName_MatchesSubstring()
    {
        using var helper = new CliTestHelper();
        helper.ImportViaApi("photo.jpg", "img");
        var (_, output) = helper.Run("search name photo");
        Assert.Contains("photo.jpg", output);
        Assert.Contains("Found 1", output);
    }

    [Fact]
    public void Process_SearchName_NoMatch_ShowsWarning()
    {
        using var helper = new CliTestHelper();
        helper.ImportViaApi("document.pdf", "doc");
        var (_, output) = helper.Run("search name nonexistent");
        Assert.Contains("No matching files", output);
    }

    [Fact]
    public void Process_SearchName_MissingArgs_ShowsUsage()
    {
        using var helper = new CliTestHelper();
        var (_, output) = helper.Run("search name");
        Assert.Contains("Usage", output);
    }

    [Fact]
    public void Process_SearchTag_SingleTag_FindsMatch()
    {
        using var helper = new CliTestHelper();
        helper.ImportViaApi("photo.jpg", "img", new List<string> { "img", "landscape" });
        var (_, output) = helper.Run("search tag landscape");
        Assert.Contains("Found 1", output);
    }

    [Fact]
    public void Process_SearchTag_MultipleTags_Intersection()
    {
        using var helper = new CliTestHelper();
        helper.ImportViaApi("photo1.jpg", "img", new List<string> { "img", "landscape" }, "", "content1");
        helper.ImportViaApi("photo2.jpg", "img", new List<string> { "img", "portrait" }, "", "content2");
        var (_, output) = helper.Run("search tag img,landscape");
        Assert.Contains("Found 1", output);
        Assert.Contains("photo1.jpg", output);
    }

    [Fact]
    public void Process_SearchTag_UnknownTag_ReturnsEmpty()
    {
        using var helper = new CliTestHelper();
        helper.ImportViaApi("photo.jpg", "img");
        var (_, output) = helper.Run("search tag nonexistent");
        Assert.Contains("No matching files", output);
    }

    [Fact]
    public void Process_SearchTime_WithinRange_FindsMatch()
    {
        using var helper = new CliTestHelper();
        helper.ImportViaApi("photo.jpg", "img");
        var (_, output) = helper.Run("search time 2024-01-01 2099-12-31");
        Assert.Contains("Found", output);
    }

    [Fact]
    public void Process_SearchTime_OutsideRange_ReturnsEmpty()
    {
        using var helper = new CliTestHelper();
        helper.ImportViaApi("photo.jpg", "img");
        var (_, output) = helper.Run("search time 2020-01-01 2020-12-31");
        Assert.Contains("No matching files", output);
    }

    [Fact]
    public void Process_SearchTime_InvalidDate_ShowsError()
    {
        using var helper = new CliTestHelper();
        var (_, output) = helper.Run("search time bad-date 2024-01-01");
        Assert.Contains("Invalid start date", output);
    }

    [Fact]
    public void Process_SearchUnknownType_ShowsError()
    {
        using var helper = new CliTestHelper();
        var (_, output) = helper.Run("search badtype foo");
        Assert.Contains("Unknown search type", output);
    }
}
