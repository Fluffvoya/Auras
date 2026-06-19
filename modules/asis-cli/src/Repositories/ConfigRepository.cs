using ASIS.CLI.Models;
using ASIS.Core.Storage;

namespace ASIS.CLI.Repositories;

public class ConfigRepository
{
    private readonly string _file;

    public Config Config { get; private set; }

    public ConfigRepository(string file)
    {
        _file = file;
        string? folder = Path.GetDirectoryName(_file);
        if (!Path.Exists(folder))
        {
            if (folder != null)
            {
                Directory.CreateDirectory(folder);
            }
        }

        if (!File.Exists(_file))
        {
            Config = new Config();
            Save();
        }
        else
        {
            Config = JsonStorage.Load<Config>(_file);
        }
    }

    public void Save()
    {
        JsonStorage.Save(_file, Config);
    }
}