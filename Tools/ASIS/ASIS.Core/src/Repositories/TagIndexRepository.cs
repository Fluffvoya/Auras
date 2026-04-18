using System.Text.Json;

namespace ASIS.Core.Repositories;

public class TagIndexRepository
{
    private readonly string _file;

    private Dictionary<string, HashSet<Guid>> _index;

    public TagIndexRepository(string file)
    {
        _file = file;

        if (File.Exists(file))
        {
            _index = JsonSerializer.Deserialize<Dictionary<string, HashSet<Guid>>>
                (File.ReadAllText(file)) ?? new();
        }
        else
        {
            _index = new();
        }
    }

    private void Save()
    {
        File.WriteAllText(
            _file,
            JsonSerializer.Serialize(
                _index,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                }));
    }

    public void Add(Guid fileId, List<string> tags)
    {
        foreach (var tag in tags)
        {
            if (!_index.ContainsKey(tag))
                _index[tag] = new HashSet<Guid>();

            _index[tag].Add(fileId);
        }

        Save();
    }

    public void Remove(Guid fileId)
    {
        foreach (var tag in _index.Keys)
        {
            _index[tag].Remove(fileId);
        }

        Save();
    }

    public HashSet<Guid> GetFiles(string tag)
    {
        if (_index.ContainsKey(tag))
            return _index[tag];

        return new HashSet<Guid>();
    }
}