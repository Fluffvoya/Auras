using ASIS.Core.Models;
using ASIS.Core.Repositories;
using ASIS.Core.Services;

namespace ASIS.Test;

public class SearchServiceTests : IDisposable
{
    private readonly string _tmpDir;
    private readonly MetadataRepository _metadata;
    private readonly TagIndexRepository _tagIndex;
    private readonly SearchService _search;

    public SearchServiceTests()
    {
        _tmpDir = Path.Combine(Path.GetTempPath(), "asis_test_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tmpDir);
        _metadata = new MetadataRepository(Path.Combine(_tmpDir, "metadata.json"));
        _tagIndex = new TagIndexRepository(Path.Combine(_tmpDir, "tag_index.json"));
        _search = new SearchService(_metadata, _tagIndex);
    }

    public void Dispose()
    {
        Directory.Delete(_tmpDir, true);
    }

    private FileRecord CreateRecord(string name, string primaryTag, List<string> tags, DateTime createdTime)
    {
        var record = new FileRecord
        {
            Id = Guid.NewGuid(),
            Name = name,
            PrimaryTag = primaryTag,
            Tags = tags,
            CreatedTime = createdTime
        };
        _metadata.Add(record);
        _tagIndex.Add(record.Id, tags);
        return record;
    }

    // --- SearchByName ---
    [Fact]
    public void SearchByName_CaseInsensitiveMatch()
    {
        CreateRecord("Photo.jpg", "photo", new List<string> { "photo" }, DateTime.UtcNow);
        var results = _search.SearchByName("photo");
        Assert.Single(results);
        Assert.Equal("Photo.jpg", results[0].Name);
    }

    [Fact]
    public void SearchByName_PartialMatch()
    {
        CreateRecord("my_photo_2024.jpg", "photo", new List<string> { "photo" }, DateTime.UtcNow);
        var results = _search.SearchByName("photo");
        Assert.Single(results);
    }

    [Fact]
    public void SearchByName_NoMatch_ReturnsEmpty()
    {
        CreateRecord("document.pdf", "doc", new List<string> { "doc" }, DateTime.UtcNow);
        var results = _search.SearchByName("photo");
        Assert.Empty(results);
    }

    [Fact]
    public void SearchByName_EmptyKeyword_MatchesAll()
    {
        CreateRecord("a.jpg", "a", new List<string> { "a" }, DateTime.UtcNow);
        CreateRecord("b.jpg", "b", new List<string> { "b" }, DateTime.UtcNow);
        var results = _search.SearchByName("");
        Assert.Equal(2, results.Count);
    }

    // --- SearchByTags ---
    [Fact]
    public void SearchByTags_SingleTag()
    {
        CreateRecord("pic.jpg", "photo", new List<string> { "photo", "landscape" }, DateTime.UtcNow);
        var results = _search.SearchByTags(new List<string> { "photo" });
        Assert.Single(results);
    }

    [Fact]
    public void SearchByTags_MultipleTags_Intersection()
    {
        var r1 = CreateRecord("pic1.jpg", "photo", new List<string> { "photo", "landscape" }, DateTime.UtcNow);
        var r2 = CreateRecord("pic2.jpg", "photo", new List<string> { "photo", "portrait" }, DateTime.UtcNow);
        var r3 = CreateRecord("pic3.jpg", "photo", new List<string> { "landscape", "portrait" }, DateTime.UtcNow);

        var results = _search.SearchByTags(new List<string> { "photo", "landscape" });
        Assert.Single(results);
        Assert.Equal(r1.Id, results[0].Id);
    }

    [Fact]
    public void SearchByTags_EmptyList_ReturnsEmpty()
    {
        CreateRecord("pic.jpg", "photo", new List<string> { "photo" }, DateTime.UtcNow);
        var results = _search.SearchByTags(new List<string>());
        Assert.Empty(results);
    }

    [Fact]
    public void SearchByTags_UnknownTag_ReturnsEmpty()
    {
        CreateRecord("pic.jpg", "photo", new List<string> { "photo" }, DateTime.UtcNow);
        var results = _search.SearchByTags(new List<string> { "nonexistent" });
        Assert.Empty(results);
    }

    // --- SearchByTime ---
    [Fact]
    public void SearchByTime_WithinRange()
    {
        var time = new DateTime(2024, 6, 15);
        CreateRecord("a.jpg", "a", new List<string> { "a" }, time);
        var results = _search.SearchByTime(
            new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));
        Assert.Single(results);
    }

    [Fact]
    public void SearchByTime_OutsideRange_ReturnsEmpty()
    {
        CreateRecord("a.jpg", "a", new List<string> { "a" }, new DateTime(2024, 1, 1));
        var results = _search.SearchByTime(
            new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));
        Assert.Empty(results);
    }

    [Fact]
    public void SearchByTime_StartAfterEnd_ReturnsEmpty()
    {
        CreateRecord("a.jpg", "a", new List<string> { "a" }, new DateTime(2024, 6, 15));
        var results = _search.SearchByTime(
            new DateTime(2024, 6, 30), new DateTime(2024, 6, 1));
        Assert.Empty(results);
    }

    [Fact]
    public void SearchByTime_ExactBoundary()
    {
        var start = new DateTime(2024, 6, 1);
        CreateRecord("a.jpg", "a", new List<string> { "a" }, start);
        var results = _search.SearchByTime(start, start);
        Assert.Single(results);
    }
}