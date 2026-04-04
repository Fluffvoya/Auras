using System.Text.Json;

namespace PAMS.Core.Repositories;

public class HashIndexRepository
{
    private readonly string _file;

    private Dictionary<string, Guid> _index;

    public HashIndexRepository(string file)
    {
        _file = file;

        if (File.Exists(file))
        {
            _index = JsonSerializer.Deserialize<
                    Dictionary<string, Guid>>
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

    public bool Exists(string hash)
    {
        return _index.ContainsKey(hash);
    }

    public void Add(string hash, Guid id)
    {
        _index[hash] = id;

        Save();
    }

    public void Remove(string hash)
    {
        _index.Remove(hash);

        Save();
    }
}