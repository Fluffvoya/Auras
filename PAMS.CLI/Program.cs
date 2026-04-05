using System.Text;
using PAMS.Core;
using PAMS.Core.Models;
using PAMS.Core.Storage;

namespace PAMS.CLI;

class Program
{
    private static PAMSCoreAPI? _api;

    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        while (true)
        {
            var archive = _api?.ArchiveName;
            if (archive == null)
            {
                Console.Write("> ");
            }
            else
            {
                Console.Write($"[{archive}] > ");
            }

            string? input = Console.ReadLine();
            if (input == null)
            {
                Console.WriteLine();
                continue;
            }

            if (!Process(input)) break;
        }
    }

    static bool Process(string input)
    {
        List<string> commands = input.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (commands.Count <= 0) return true;
        switch (commands[0])
        {
            case "open": return commands.Count <= 1 || Open(commands[1]);
            case "create":
            {
                if (commands.Count <= 1)
                {
                    return true;
                }

                if (commands.Count == 2)
                {
                    return Create(commands[1], "./");
                }

                return Create(commands[1], commands[2]);
            }
            case "close": return Close();
            case "exit": return false;
            default:
            {
                Console.WriteLine($"Can not identify command:{commands[0]}");
                break;
            }
        }

        return true;
    }

    static bool Create(string archive, string path)
    {
        string archiveRoot = Path.Combine(path, archive);
        if (Path.Exists(archiveRoot))
        {
            Console.WriteLine($"{archiveRoot} already exists");
            return true;
        }

        Directory.CreateDirectory(archiveRoot);
        ArchiveConfig archiveConfig = new ArchiveConfig()
        {
            Name = archive,
            CreatedTime = DateTime.UtcNow,
        };
        JsonStorage.Save(Path.Combine(archiveRoot, "archive.json"), archiveConfig);
        return true;
    }

    static bool Open(string path)
    {
        if (Path.Exists(path))
        {
            string fileDir = Path.Combine(path, "archive.json");
            if (!File.Exists(fileDir)) return true;
            _api = new PAMSCoreAPI(
                Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData)
                    , "PAMS", "config.json")
                , path);
        }

        return true;
    }

    static bool Close()
    {
        _api = null;
        return true;
    }
}