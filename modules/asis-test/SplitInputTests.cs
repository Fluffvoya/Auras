namespace ASIS.Test;

[Collection("CLI Tests")]
public class SplitInputTests
{
    [Fact]
    public void SplitInput_EmptyString_ReturnsEmptyList()
    {
        using var helper = new CliTestHelper();
        var result = helper.SplitInput("");
        Assert.Empty(result);
    }

    [Fact]
    public void SplitInput_SingleWord_ReturnsSingleToken()
    {
        using var helper = new CliTestHelper();
        var result = helper.SplitInput("help");
        Assert.Single(result);
        Assert.Equal("help", result[0]);
    }

    [Fact]
    public void SplitInput_MultipleWords_SplitsOnSpaces()
    {
        using var helper = new CliTestHelper();
        var result = helper.SplitInput("import file.txt doc");
        Assert.Equal(3, result.Count);
        Assert.Equal("import", result[0]);
        Assert.Equal("file.txt", result[1]);
        Assert.Equal("doc", result[2]);
    }

    [Fact]
    public void SplitInput_QuotedString_TreatsAsSingleToken()
    {
        using var helper = new CliTestHelper();
        var result = helper.SplitInput("describe file \"my description\"");
        Assert.Equal(3, result.Count);
        Assert.Equal("describe", result[0]);
        Assert.Equal("file", result[1]);
        Assert.Equal("my description", result[2]);
    }

    [Fact]
    public void SplitInput_MultipleQuotes_ParsesCorrectly()
    {
        using var helper = new CliTestHelper();
        var result = helper.SplitInput("rename \"my file.txt\" \"new name.txt\"");
        Assert.Equal(3, result.Count);
        Assert.Equal("rename", result[0]);
        Assert.Equal("my file.txt", result[1]);
        Assert.Equal("new name.txt", result[2]);
    }

    [Fact]
    public void SplitInput_LeadingTrailingSpaces_Trimmed()
    {
        using var helper = new CliTestHelper();
        var result = helper.SplitInput("  help  ");
        Assert.Single(result);
        Assert.Equal("help", result[0]);
    }

    [Fact]
    public void SplitInput_MultipleSpaces_Skipped()
    {
        using var helper = new CliTestHelper();
        var result = helper.SplitInput("import  file.txt  doc");
        Assert.Equal(3, result.Count);
        Assert.Equal("import", result[0]);
        Assert.Equal("file.txt", result[1]);
        Assert.Equal("doc", result[2]);
    }

    [Fact]
    public void SplitInput_UnclosedQuote_IncludesRemainder()
    {
        using var helper = new CliTestHelper();
        var result = helper.SplitInput("describe file \"unclosed");
        Assert.Equal(3, result.Count);
        Assert.Equal("describe", result[0]);
        Assert.Equal("file", result[1]);
        Assert.Equal("unclosed", result[2]);
    }

    [Fact]
    public void SplitInput_FlagWithArgs_ParsedCorrectly()
    {
        using var helper = new CliTestHelper();
        var result = helper.SplitInput("import file.txt doc --desc \"a file\" --move");
        Assert.Equal(6, result.Count);
        Assert.Equal("import", result[0]);
        Assert.Equal("file.txt", result[1]);
        Assert.Equal("doc", result[2]);
        Assert.Equal("--desc", result[3]);
        Assert.Equal("a file", result[4]);
        Assert.Equal("--move", result[5]);
    }
}
