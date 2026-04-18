using ASIS.Core.Models;
using ASIS.Core.Repositories;

namespace ASIS.Core.Services;

public class SearchService
{
    private readonly MetadataRepository _metadata;

    private readonly TagIndexRepository _tagIndex;

    public SearchService(
        MetadataRepository metadata,
        TagIndexRepository tagIndex)
    {
        _metadata = metadata;
        _tagIndex = tagIndex;
    }

    public List<FileRecord> SearchByName(string keyword)
    {
        keyword = keyword.ToLower();

        return _metadata
            .GetAll()
            .Where(f => f.Name.ToLower().Contains(keyword))
            .ToList();
    }

    public List<FileRecord> SearchByTags(List<string> tags)
    {
        if (tags.Count == 0)
            return new();

        HashSet<Guid>? result = null;

        foreach (var tag in tags)
        {
            var files = _tagIndex.GetFiles(tag);

            if (result == null)
                result = new HashSet<Guid>(files);
            else
                result.IntersectWith(files);
        }

        return result!
            .Select(id => _metadata.Get(id)!)
            .Where(f => f != null)
            .ToList();
    }
}
