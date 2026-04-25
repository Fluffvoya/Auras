using System.Text;
using ASIS.Core;
using ASIS.Core.Models;
using ASIS.Core.Storage;

namespace ASIS.CLI;

class Program
{
    private static ASISCoreAPI? _api;

    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        ConsoleWriter.PrintWelcome();
        while (true)
        {
            var archive = _api?.ArchiveName;
            Console.Write(archive == null ? $"{Cyan}> {Reset}" : $"{Magenta}[{archive}] {Cyan}> {Reset}");
            string? input = Console.ReadLine();
            if (input == null) { Console.WriteLine(); continue; }
            if (!Process(input)) break;
        }
    }

    private const string Magenta = "\u001b[95m";
    private const string Reset = "\u001b[0m";
    private const string Cyan = "\u001b[96m";
    private const string Bold = "\u001b[1m";

    static bool Process(string input)
    {
        var tokens = SplitInput(input);
        if (tokens.Count == 0) return true;

        var cmd = tokens[0].ToLowerInvariant();
        return cmd switch
        {
            // Archive management
            "create" => CreateArchive(tokens),
            "open" => OpenArchive(tokens),
            "close" => CloseArchive(),
            "archive" => ShowArchiveInfo(),

            // File operations
            "import" => ImportFile(tokens),
            "rename" => RenameFile(tokens),
            "retag" => RetagFile(tokens),
            "tag" => TagCommand(tokens),
            "info" => InfoFile(tokens),
            "describe" => DescribeFile(tokens),
            "delete" => DeleteFile(tokens),
            "unlink" => UnlinkFile(tokens),

            // Search
            "search" => Search(tokens),

            // ID lookup
            "id" => IdLookup(tokens),

            // Archive utilities
            "diff" => ShowDiff(),

            // System
            "help" => Help(tokens),
            "exit" => false,

            _ => UnknownCommand(cmd)
        };
    }

    static List<string> SplitInput(string input)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ' ' && !inQuotes)
            {
                if (current.Length > 0)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }
        if (current.Length > 0) result.Add(current.ToString());
        return result;
    }

    static bool RequireArchive()
    {
        if (_api == null)
        {
            ConsoleWriter.Err("No archive open. Use 'open <path>' first.");
            return false;
        }
        return true;
    }

    // ==================== Archive Management ====================

    static bool CreateArchive(List<string> tokens)
    {
        if (tokens.Count < 2)
        {
            ConsoleWriter.Info("Usage: create <name> [path]");
            return true;
        }
        string name = tokens[1];
        string path = tokens.Count > 2 ? tokens[2] : "./";
        string archiveRoot = Path.Combine(path, name);

        if (Directory.Exists(archiveRoot))
        {
            ConsoleWriter.Warn($"Archive '{name}' already exists at {path}");
            return true;
        }

        Directory.CreateDirectory(archiveRoot);
        var config = new ArchiveConfig { Name = name, CreatedTime = DateTime.UtcNow };
        JsonStorage.Save(Path.Combine(archiveRoot, "archive.json"), config);
        ConsoleWriter.Ok($"Archive '{name}' created at {archiveRoot}");
        return true;
    }

    static bool OpenArchive(List<string> tokens)
    {
        if (tokens.Count < 2)
        {
            ConsoleWriter.Info("Usage: open <path>");
            return true;
        }
        string archivePath = tokens[1];

        if (!Directory.Exists(archivePath))
        {
            ConsoleWriter.Err($"Directory not found: {archivePath}");
            return true;
        }
        if (!File.Exists(Path.Combine(archivePath, "archive.json")))
        {
            ConsoleWriter.Err($"No archive at {archivePath} (missing archive.json)");
            return true;
        }

        _api = new ASISCoreAPI(archivePath);
        ConsoleWriter.Ok($"Opened archive: {_api.ArchiveName}");
        return true;
    }

    static bool CloseArchive()
    {
        _api = null;
        ConsoleWriter.Info("Archive closed.");
        return true;
    }

    static bool ShowArchiveInfo()
    {
        if (!RequireArchive()) return true;
        ConsoleWriter.PrintDivider();
        ConsoleWriter.Title("Archive Info");
        ConsoleWriter.Label("  Name:     ");
        Console.WriteLine(_api!.ArchiveName);
        var diff = _api.Diff();
        ConsoleWriter.Label("  Files:    ");
        Console.WriteLine(_api.SearchByName("").Count.ToString());
        ConsoleWriter.Label("  Orphaned: ");
        Console.WriteLine(diff.OrphanedMetadata.Count.ToString());
        ConsoleWriter.Label("  Untracked:");
        Console.WriteLine(diff.OrphanedDiskFiles.Count.ToString());
        ConsoleWriter.PrintDivider();
        return true;
    }

    static bool ShowDiff()
    {
        if (!RequireArchive()) return true;
        var diff = _api!.Diff();
        ConsoleWriter.PrintDivider();
        ConsoleWriter.Title("Archive Diff");
        if (!diff.OrphanedMetadata.Any() && !diff.OrphanedDiskFiles.Any())
        {
            ConsoleWriter.Ok("  Archive is clean - no orphans or untracked files.");
        }
        else
        {
            if (diff.OrphanedMetadata.Any())
            {
                ConsoleWriter.Warn($"  {diff.OrphanedMetadata.Count()} orphaned metadata record(s):");
                foreach (var m in diff.OrphanedMetadata)
                    Console.WriteLine($"    - {m.Name} ({m.Id})");
            }
            if (diff.OrphanedDiskFiles.Any())
            {
                ConsoleWriter.Warn($"  {diff.OrphanedDiskFiles.Count()} untracked file(s):");
                foreach (var f in diff.OrphanedDiskFiles)
                    Console.WriteLine($"    - {f}");
            }
        }
        ConsoleWriter.PrintDivider();
        return true;
    }

    // ==================== File Identifier Resolution ====================

    static FileRecord ResolveFile(string identifier)
    {
        if (identifier.StartsWith("id:", StringComparison.OrdinalIgnoreCase))
        {
            string idStr = identifier[3..];
            if (!Guid.TryParse(idStr, out Guid guid))
                throw new Exception($"Invalid GUID format: {idStr}");

            var allFiles = _api!.SearchByName("");
            var file = allFiles.FirstOrDefault(r => r.Id == guid);
            if (file == null)
                throw new Exception($"No file found with ID: {guid}");
            return file;
        }

        var results = _api!.SearchByName(identifier).ToList();
        if (results.Count == 0)
            throw new Exception($"No file found matching: {identifier}");
        if (results.Count > 1)
            throw new Exception($"Multiple files match '{identifier}': {results.Count} found. Use 'id:<guid>' for exact match.");

        return results[0];
    }

    static FileRecord? TryResolveFile(string identifier)
    {
        try { return ResolveFile(identifier); }
        catch { return null; }
    }

    // ==================== File Operations ====================

    static bool ImportFile(List<string> tokens)
    {
        if (!RequireArchive()) return true;
        if (tokens.Count < 3)
        {
            ConsoleWriter.Info("Usage: import <source_path> <primary_tag> [tags...] [--desc \"description\"] [--move]");
            return true;
        }

        string sourcePath = tokens[1];
        string primaryTag = tokens[2];
        bool move = false;
        string description = "";
        var additionalTags = new List<string>();

        for (int i = 3; i < tokens.Count; i++)
        {
            string token = tokens[i];
            if (token == "--move") { move = true; continue; }
            if (token == "--desc" && i + 1 < tokens.Count) { description = tokens[++i]; continue; }
            additionalTags.Add(token);
        }

        try
        {
            var record = _api!.ImportFile(sourcePath, primaryTag, additionalTags, description, move);
            ConsoleWriter.Ok($"Imported: {record.Name} ({record.Id})");
        }
        catch (Exception ex)
        {
            ConsoleWriter.Err($"Import failed: {ex.Message}");
        }
        return true;
    }

    static bool RenameFile(List<string> tokens)
    {
        if (!RequireArchive()) return true;
        if (tokens.Count < 3)
        {
            ConsoleWriter.Info("Usage: rename <file> <new_name>");
            return true;
        }

        try
        {
            var file = ResolveFile(tokens[1]);
            _api!.ChangeFileName(file!.Id, tokens[2]);
            ConsoleWriter.Ok("File renamed.");
        }
        catch (Exception ex)
        {
            ConsoleWriter.Err($"Rename failed: {ex.Message}");
        }
        return true;
    }

    static bool RetagFile(List<string> tokens)
    {
        if (!RequireArchive()) return true;
        if (tokens.Count < 3)
        {
            ConsoleWriter.Info("Usage: retag <file> <new_primary_tag>");
            return true;
        }

        try
        {
            var file = ResolveFile(tokens[1]);
            _api!.ChangePrimaryTag(file!.Id, tokens[2]);
            ConsoleWriter.Ok("Primary tag updated.");
        }
        catch (Exception ex)
        {
            ConsoleWriter.Err($"Retag failed: {ex.Message}");
        }
        return true;
    }

    static bool TagCommand(List<string> tokens)
    {
        if (!RequireArchive()) return true;
        if (tokens.Count < 3)
        {
            ConsoleWriter.Info("Usage: tag add|remove|list <file> [tags]");
            return true;
        }

        string subcmd = tokens[1].ToLowerInvariant();
        string fileIdentifier = tokens[2];

        try
        {
            var file = ResolveFile(fileIdentifier);

            return subcmd switch
            {
                "add" => TagAdd(file, tokens),
                "remove" => TagRemove(file, tokens),
                "list" => TagList(file),
                _ => UnknownSubcommand("tag", subcmd, "add", "remove", "list")
            };
        }
        catch (Exception ex)
        {
            ConsoleWriter.Err($"Tag command failed: {ex.Message}");
        }
        return true;
    }

    static List<string> ParseTags(string? tagsStr)
    {
        if (string.IsNullOrWhiteSpace(tagsStr)) return new List<string>();
        return tagsStr.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList();
    }

    static bool TagAdd(FileRecord file, List<string> tokens)
    {
        if (tokens.Count < 4)
        {
            ConsoleWriter.Info("Usage: tag add <file> <tag1,tag2,...>");
            return true;
        }

        var tags = ParseTags(tokens[3]);
        _api!.AddTags(file.Id, tags);
        ConsoleWriter.Ok($"Tags added: {string.Join(", ", tags)}");
        return true;
    }

    static bool TagRemove(FileRecord file, List<string> tokens)
    {
        if (tokens.Count < 4)
        {
            ConsoleWriter.Info("Usage: tag remove <file> <tag1,tag2,...>");
            return true;
        }

        var tags = ParseTags(tokens[3]);
        _api!.RemoveTags(file.Id, tags);
        ConsoleWriter.Ok($"Tags removed: {string.Join(", ", tags)}");
        return true;
    }

    static bool TagList(FileRecord file)
    {
        Console.WriteLine($"File: {file.Name}");
        Console.WriteLine($"PrimaryTag: {file.PrimaryTag}");
        Console.WriteLine($"Tags: [{string.Join(", ", file.Tags ?? new List<string>())}]");
        return true;
    }

    static bool InfoFile(List<string> tokens)
    {
        if (!RequireArchive()) return true;
        if (tokens.Count < 2)
        {
            ConsoleWriter.Info("Usage: info <file>");
            return true;
        }

        try
        {
            var file = ResolveFile(tokens[1]);
            PrintFileInfo(file!);
        }
        catch (Exception ex)
        {
            ConsoleWriter.Err($"Info failed: {ex.Message}");
        }
        return true;
    }

    static void PrintFileInfo(FileRecord file)
    {
        Console.WriteLine($"  ID:          {file.Id}");
        Console.WriteLine($"  Name:        {file.Name}");
        Console.WriteLine($"  PrimaryTag:  {file.PrimaryTag}");
        Console.WriteLine($"  Tags:        [{string.Join(", ", file.Tags ?? new List<string>())}]");
        Console.WriteLine($"  Description: {file.Description ?? "(none)"}");
        Console.WriteLine($"  Hash:        {file.Hash}");
        Console.WriteLine($"  Path:        {file.RelativePath}");
        Console.WriteLine($"  Created:     {file.CreatedTime:yyyy-MM-dd HH:mm:ss}");
    }

    static bool DescribeFile(List<string> tokens)
    {
        if (!RequireArchive()) return true;
        if (tokens.Count < 3)
        {
            ConsoleWriter.Info("Usage: describe <file> <description>");
            return true;
        }

        try
        {
            var file = ResolveFile(tokens[1]);
            string description = string.Join(" ", tokens.Skip(2));
            _api!.ChangeDescription(file!.Id, description);
            ConsoleWriter.Ok("Description updated.");
        }
        catch (Exception ex)
        {
            ConsoleWriter.Err($"Describe failed: {ex.Message}");
        }
        return true;
    }

    static bool DeleteFile(List<string> tokens)
    {
        if (!RequireArchive()) return true;
        if (tokens.Count < 2)
        {
            ConsoleWriter.Info("Usage: delete <file>");
            return true;
        }

        try
        {
            var file = ResolveFile(tokens[1]);
            _api!.DeleteFile(file!.Id);
            ConsoleWriter.Ok("File deleted.");
        }
        catch (Exception ex)
        {
            ConsoleWriter.Err($"Delete failed: {ex.Message}");
        }
        return true;
    }

    static bool UnlinkFile(List<string> tokens)
    {
        if (!RequireArchive()) return true;
        if (tokens.Count < 2)
        {
            ConsoleWriter.Info("Usage: unlink <file>");
            return true;
        }

        try
        {
            var file = ResolveFile(tokens[1]);
            _api!.DeleteMetadataOnly(file!.Id);
            ConsoleWriter.Ok("Metadata removed (file kept).");
        }
        catch (Exception ex)
        {
            ConsoleWriter.Err($"Unlink failed: {ex.Message}");
        }
        return true;
    }

    // ==================== Search ====================

    static bool Search(List<string> tokens)
    {
        if (!RequireArchive()) return true;
        if (tokens.Count < 2)
        {
            ConsoleWriter.Info("Usage: search name|tag|time <args>");
            return true;
        }

        string searchType = tokens[1].ToLowerInvariant();

        try
        {
            List<FileRecord> results = searchType switch
            {
                "name" => SearchByName(tokens),
                "tag" => SearchByTags(tokens),
                "time" => SearchByTime(tokens),
                _ => throw new Exception($"Unknown search type: {searchType}. Use name, tag, or time.")
            };

            PrintSearchResults(results);
        }
        catch (Exception ex)
        {
            ConsoleWriter.Err($"Search failed: {ex.Message}");
        }
        return true;
    }

    static List<FileRecord> SearchByName(List<string> tokens)
    {
        if (tokens.Count < 3) throw new Exception("Usage: search name <keyword>");
        return _api!.SearchByName(tokens[2]).ToList();
    }

    static List<FileRecord> SearchByTags(List<string> tokens)
    {
        if (tokens.Count < 3) throw new Exception("Usage: search tag <tag1,tag2,...>");
        var tags = ParseTags(tokens[2]);
        return _api!.SearchByTags(tags).ToList();
    }

    static List<FileRecord> SearchByTime(List<string> tokens)
    {
        if (tokens.Count < 4) throw new Exception("Usage: search time <start> <end> (format: yyyy-MM-dd)");
        if (!DateTime.TryParse(tokens[2], out DateTime start)) throw new Exception($"Invalid start date: {tokens[2]}");
        if (!DateTime.TryParse(tokens[3], out DateTime end)) throw new Exception($"Invalid end date: {tokens[3]}");
        end = end.Date.AddDays(1).AddTicks(-1);
        return _api!.SearchByTime(start, end).ToList();
    }

    static void PrintSearchResults(List<FileRecord> results)
    {
        if (results.Count == 0)
        {
            ConsoleWriter.Warn("No matching files found.");
            return;
        }

        ConsoleWriter.Ok($"Found {results.Count} file(s):");
        ConsoleWriter.PrintDivider();
        foreach (var r in results)
        {
            Console.WriteLine($"  {Cyan}[{r.Id}]{Reset}");
            Console.WriteLine($"    {Bold}Name:{Reset}       {r.Name}");
            Console.WriteLine($"    {Bold}PrimaryTag:{Reset} {r.PrimaryTag}");
            Console.WriteLine($"    {Bold}Tags:{Reset}       [{string.Join(", ", r.Tags ?? new List<string>())}]");
            if (!string.IsNullOrEmpty(r.Description))
                Console.WriteLine($"    {Bold}Desc:{Reset}       {r.Description}");
            Console.WriteLine();
        }
    }

    // ==================== ID Lookup ====================

    static bool IdLookup(List<string> tokens)
    {
        if (!RequireArchive()) return true;
        if (tokens.Count < 2)
        {
            ConsoleWriter.Info("Usage: id <guid> [--full]");
            return true;
        }

        string idStr = tokens[1];
        bool isFull = tokens.Count > 2 && tokens[2] == "--full";

        try
        {
            if (!Guid.TryParse(idStr, out Guid guid))
                throw new Exception($"Invalid GUID format: {idStr}");

            var allFiles = _api!.SearchByName("");
            var file = allFiles.FirstOrDefault(r => r.Id == guid);

            if (file == null)
                throw new Exception($"No file found with ID: {guid}");

            if (isFull)
            {
                PrintFileInfo(file);
            }
            else
            {
                Console.WriteLine($"Name:       {file.Name}");
                Console.WriteLine($"PrimaryTag: {file.PrimaryTag}");
                Console.WriteLine($"Tags:       [{string.Join(", ", file.Tags ?? new List<string>())}]");
                Console.WriteLine($"Description:{file.Description ?? "(none)"}");
            }
        }
        catch (Exception ex)
        {
            ConsoleWriter.Err($"ID lookup failed: {ex.Message}");
        }
        return true;
    }

    // ==================== Help ====================

    static bool Help(List<string> tokens)
    {
        if (tokens.Count < 2)
        {
            PrintAllHelp();
        }
        else
        {
            PrintCommandHelp(tokens[1]);
        }
        return true;
    }

    static void PrintAllHelp()
    {
        ConsoleWriter.PrintDivider();
        ConsoleWriter.Title("  ASIS.CLI - Archive Management Shell");
        ConsoleWriter.PrintDivider();

        ConsoleWriter.Label("  Archive Management:");
        Console.WriteLine("    create <name> [path]   Create a new archive");
        Console.WriteLine("    open <path>             Open an existing archive");
        Console.WriteLine("    close                  Close the current archive");
        Console.WriteLine("    archive                 Show current archive info");
        Console.WriteLine("    diff                    Show orphaned/untracked files");
        Console.WriteLine();

        ConsoleWriter.Label("  File Operations:");
        Console.WriteLine("    import <path> <tag> [tags...] [--desc \"...\"] [--move]");
        Console.WriteLine("                              Import a file (copy by default)");
        Console.WriteLine("    rename <file> <new>     Rename a file");
        Console.WriteLine("    retag <file> <tag>      Change primary tag");
        Console.WriteLine("    tag add <file> <t1,t2>  Add tags");
        Console.WriteLine("    tag remove <file> <t>   Remove tags");
        Console.WriteLine("    tag list <file>         List all tags");
        Console.WriteLine("    info <file>             Show file information");
        Console.WriteLine("    describe <file> <desc>  Set file description");
        Console.WriteLine("    delete <file>           Delete file and metadata");
        Console.WriteLine("    unlink <file>           Remove metadata only");
        Console.WriteLine();

        ConsoleWriter.Label("  Search:");
        Console.WriteLine("    search name <keyword>   Search by name substring");
        Console.WriteLine("    search tag <t1,t2>      Search by tag intersection");
        Console.WriteLine("    search time <s> <e>     Search by date range");
        Console.WriteLine();

        ConsoleWriter.Label("  ID Lookup:");
        Console.WriteLine("    id <guid> [--full]      Look up file by ID");
        Console.WriteLine();

        ConsoleWriter.Label("  System:");
        Console.WriteLine("    help [command]          Show help");
        Console.WriteLine("    exit                    Exit CLI");
        Console.WriteLine();

        ConsoleWriter.Dimmed("  <file> can be a name substring or 'id:<guid>' for exact match.");
        ConsoleWriter.PrintDivider();
    }

    static void PrintCommandHelp(string cmd)
    {
        var helpTexts = new Dictionary<string, string>
        {
            ["create"] = "create <name> [path]\n  Create a new archive at the specified path.",
            ["open"] = "open <path>\n  Open an existing archive for editing.",
            ["close"] = "close\n  Close the current archive.",
            ["archive"] = "archive\n  Show information about the current archive.",
            ["import"] = "import <source_path> <primary_tag> [tags...] [--desc \"...\"] [--move]\n  Import a file into the archive. Use --move to move instead of copy.",
            ["rename"] = "rename <file> <new_name>\n  Rename a file in the archive.",
            ["retag"] = "retag <file> <new_primary_tag>\n  Change the primary tag of a file.",
            ["tag"] = "tag add|remove|list <file> [tags]\n  Manage tags on a file.",
            ["info"] = "info <file>\n  Show detailed information about a file.",
            ["describe"] = "describe <file> <description>\n  Set or update the description of a file.",
            ["delete"] = "delete <file>\n  Delete the physical file and its metadata.",
            ["unlink"] = "unlink <file>\n  Remove metadata only (keep the physical file).",
            ["search"] = "search name|tag|time <args>\n  Search for files. Types:\n    name <keyword>  - substring match on name\n    tag <t1,t2>     - must have ALL tags\n    time <s> <e>    - date range (yyyy-MM-dd)",
            ["id"] = "id <guid> [--full]\n  Look up a file by its ID. Use --full for complete details.",
            ["help"] = "help [command]\n  Show help for all commands or a specific command.",
            ["exit"] = "exit\n  Exit the CLI."
        };

        if (helpTexts.TryGetValue(cmd.ToLowerInvariant(), out string? text))
        {
            Console.WriteLine(text);
        }
        else
        {
            ConsoleWriter.Err($"Unknown command: {cmd}");
        }
    }

    // ==================== Error Handling ====================

    static bool UnknownCommand(string cmd)
    {
        ConsoleWriter.Err($"Unknown command: {cmd}. Type 'help' for available commands.");
        return true;
    }

    static bool UnknownSubcommand(string cmd, string subcmd, params string[] valid)
    {
        string validList = string.Join(", ", valid);
        ConsoleWriter.Err($"Unknown subcommand '{subcmd}' for '{cmd}'. Valid: {validList}.");
        return true;
    }
}
