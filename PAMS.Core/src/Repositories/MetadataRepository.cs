using System.Text.Json;
using PAMS.Core.Models;
using PAMS.Core.Storage;

namespace PAMS.Core.Repositories;

public class MetadataRepository
{
    private readonly string _file;

    private List<FileRecord> _records;

    public MetadataRepository(string file)
    {
        _file = file;
        
        if (File.Exists(file))
        {
            _records = JsonSerializer.Deserialize<List<FileRecord>>
                (File.ReadAllText(file)) ?? new();
        }
        else
        {
            _records = new();
        }
    }

    public List<FileRecord> GetAll()
    {
        return _records;
    }

    public FileRecord? Get(Guid id)
    {
        return _records.FirstOrDefault(f => f.Id == id);
    }

    public void Add(FileRecord record)
    {
        _records.Add(record);

        JsonStorage.Save(_file, _records);
    }

    public void Remove(Guid id)
    {
        _records.RemoveAll(f => f.Id == id);

        JsonStorage.Save(_file, _records);
    }
}