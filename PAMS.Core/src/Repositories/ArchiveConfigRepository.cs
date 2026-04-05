using System.Text.Json;
using PAMS.Core.Models;

namespace PAMS.Core.Repositories;

public class ArchiveConfigRepository
{
    private readonly string _file;

    private ArchiveConfig _archiveConfig;

    public ArchiveConfigRepository(string file)
    {
        _file = file;

        if (File.Exists(file))
        {
            _archiveConfig = JsonSerializer.Deserialize<ArchiveConfig>
                (File.ReadAllText(file)) ?? new();
        }
        else
        {
            _archiveConfig = new();
        }
    }

    public string ArchiveName => _archiveConfig.Name;
    
    public string? ArchivePath => Path.GetDirectoryName(_file);
}