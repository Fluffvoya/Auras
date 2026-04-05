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
            case "import": return Import(commands);
            case "rename": return Rename(commands);
            case "retag": return ChangePrimaryTag(commands);
            case "addtag": return AddTags(commands);
            case "rmtag": return RemoveTags(commands);
            case "delete": return DeleteFile(commands);
            case "search": return Search(commands);
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

    // 检查是否已打开归档
    static bool RequireArchive()
    {
        if (_api == null)
        {
            Console.WriteLine("Error: Please open an archive first. (use 'open <path>')");
            return false;
        }

        return true;
    }

    // 解析逗号分隔的标签字符串
    static List<string> ParseTags(string? tagsStr)
    {
        if (string.IsNullOrWhiteSpace(tagsStr)) return new List<string>();
        return tagsStr.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    static bool Import(List<string> commands)
    {
        if (!RequireArchive()) return true;
        if (commands.Count < 3)
        {
            Console.WriteLine("Usage: import <source_path> <primary_tag> [tag1,tag2] [--move]");
            return true;
        }

        string sourcePath = commands[1];
        string primaryTag = commands[2];
        bool move = commands.Contains("--move");

        string? tagsStr = commands.Skip(3).FirstOrDefault(t => t != "--move");
        List<string> tags = ParseTags(tagsStr);

        try
        {
            var record = _api.ImportFile(sourcePath, primaryTag, tags, move);
            Console.WriteLine($"Successfully imported: {record.Name} (ID: {record.Id})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Import failed: {ex.Message}");
        }

        return true;
    }

    static bool Rename(List<string> commands)
    {
        if (!RequireArchive()) return true;
        if (commands.Count < 3)
        {
            Console.WriteLine("Usage: rename <file_keyword> <new_name>");
            return true;
        }

        _api.ChangeFileName(commands[1], commands[2]);
        Console.WriteLine("File name changed successfully.");
        return true;
    }

    static bool ChangePrimaryTag(List<string> commands)
    {
        if (!RequireArchive()) return true;
        // 用法: retag <file_keyword> <new_primary_tag>
        if (commands.Count < 3)
        {
            Console.WriteLine("Usage: retag <file_keyword> <new_primary_tag>");
            return true;
        }

        _api.ChangePrimaryTag(commands[1], commands[2]);
        Console.WriteLine("Primary tag changed successfully.");
        return true;
    }

    static bool AddTags(List<string> commands)
    {
        if (!RequireArchive()) return true;
        if (commands.Count < 3)
        {
            Console.WriteLine("Usage: addtag <file_keyword> <tag1,tag2,tag3>");
            return true;
        }

        var tags = ParseTags(commands[2]);
        _api.AddTags(commands[1], tags);
        Console.WriteLine($"Tags [{string.Join(", ", tags)}] added successfully.");
        return true;
    }

    static bool RemoveTags(List<string> commands)
    {
        if (!RequireArchive()) return true;
        if (commands.Count < 3)
        {
            Console.WriteLine("Usage: rmtag <file_keyword> <tag1,tag2>");
            return true;
        }

        var tags = ParseTags(commands[2]);
        _api.RemoveTags(commands[1], tags);
        Console.WriteLine($"Tags [{string.Join(", ", tags)}] removed successfully.");
        return true;
    }

    static bool DeleteFile(List<string> commands)
    {
        if (!RequireArchive()) return true;
        if (commands.Count < 2)
        {
            Console.WriteLine("Usage: delete <file_keyword>");
            return true;
        }

        _api.DeleteFile(commands[1]);
        Console.WriteLine("File deleted successfully.");
        return true;
    }

    static bool Search(List<string> commands)
    {
        if (!RequireArchive()) return true;
        if (commands.Count < 3)
        {
            Console.WriteLine("Usage: search --name <keyword> OR search --tag <tag1,tag2>");
            return true;
        }

        List<FileRecord> results = new List<FileRecord>();
        try
        {
            if (commands[1] == "--name")
            {
                results = _api.SearchByName(commands[2]);
            }
            else if (commands[1] == "--tag")
            {
                var tags = ParseTags(commands[2]);
                results = _api.SearchByTags(tags);
            }
            else
            {
                Console.WriteLine("Error: Search type must be '--name' or '--tag'.");
                return true;
            }

            if (results.Count == 0)
            {
                Console.WriteLine("No matching files found.");
            }
            else
            {
                Console.WriteLine($"Found {results.Count} file(s):");
                foreach (var r in results)
                {
                    Console.WriteLine(
                        $" - ID: {r.Id} | Name: {r.Name} | PrimaryTag: {r.PrimaryTag} | Tags: [{string.Join(", ", r.Tags ?? new List<string>())}]");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Search failed: {ex.Message}");
        }

        return true;
    }
}