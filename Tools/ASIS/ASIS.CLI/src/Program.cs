using System.Text;
using ASIS.Core;
using ASIS.Core.Models;
using ASIS.Core.Storage;
using Spectre.Console;

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
            var prompt = archive == null
                ? "[orange3]>[/] "
                : $"[orange1][[{archive}]][/] [orange3]>[/] ";
            AnsiConsole.Markup(prompt);
            string? input = Console.ReadLine();
            if (input == null) { AnsiConsole.WriteLine(); continue; }
            if (!Process(input)) break;
        }
    }

    static bool Process(string input)
    {
        var tokens = SplitInput(input);
        if (tokens.Count == 0) return true;

        var cmd = tokens[0].ToLowerInvariant();
        return cmd switch
        {
            "create" => CreateArchive(tokens),
            "open" => OpenArchive(tokens),
            "close" => CloseArchive(),
            "archive" => ShowArchiveInfo(),
            "import" => ImportFile(tokens),
            "rename" => RenameFile(tokens),
            "retag" => RetagFile(tokens),
            "tag" => TagCommand(tokens),
            "info" => InfoFile(tokens),
            "describe" => DescribeFile(tokens),
            "delete" => DeleteFile(tokens),
            "unlink" => UnlinkFile(tokens),
            "search" => Search(tokens),
            "batch" => BatchCommand(tokens),
            "id" => IdLookup(tokens),
            "diff" => ShowDiff(),
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
        var diff = _api!.Diff();

        var table = new Table()
            .Title("[bold darkorange]Archive Info[/]")
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("[bold]Property[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Value[/]").LeftAligned());

        table.AddRow("Name", Markup.Escape(_api.ArchiveName ?? ""));
        table.AddRow("Files", _api.SearchByName("").Count.ToString());
        table.AddRow("Orphaned", diff.OrphanedMetadata.Count.ToString());
        table.AddRow("Untracked", diff.OrphanedDiskFiles.Count.ToString());

        AnsiConsole.Write(table);
        return true;
    }

    static bool ShowDiff()
    {
        if (!RequireArchive()) return true;
        var diff = _api!.Diff();

        if (!diff.OrphanedMetadata.Any() && !diff.OrphanedDiskFiles.Any())
        {
            ConsoleWriter.Ok("Archive is clean - no orphans or untracked files.");
            return true;
        }

        if (diff.OrphanedMetadata.Any())
        {
            ConsoleWriter.Warn($"{diff.OrphanedMetadata.Count()} orphaned metadata record(s):");
            foreach (var m in diff.OrphanedMetadata)
                AnsiConsole.MarkupLine($"  [dim]-[/] {Markup.Escape(m.Name)} [dim]({m.Id})[/]");
        }
        if (diff.OrphanedDiskFiles.Any())
        {
            ConsoleWriter.Warn($"{diff.OrphanedDiskFiles.Count()} untracked file(s):");
            foreach (var f in diff.OrphanedDiskFiles)
                AnsiConsole.MarkupLine($"  [dim]-[/] {Markup.Escape(f)}");
        }
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

    static List<FileRecord> ResolveFiles(string identifier)
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
            return new List<FileRecord> { file };
        }

        return _api!.SearchByName(identifier).ToList();
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
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("[bold]Field[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Value[/]").LeftAligned());

        table.AddRow("File", Markup.Escape(file.Name));
        table.AddRow("PrimaryTag", Markup.Escape(file.PrimaryTag));
        table.AddRow("Tags", $"[[{string.Join(", ", file.Tags ?? new List<string>())}]]");

        AnsiConsole.Write(table);
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
        var table = new Table()
            .Title("[bold darkorange]File Info[/]")
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("[bold]Field[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Value[/]").LeftAligned());

        table.AddRow("ID", file.Id.ToString());
        table.AddRow("Name", Markup.Escape(file.Name));
        table.AddRow("PrimaryTag", Markup.Escape(file.PrimaryTag));
        table.AddRow("Tags", $"[[{string.Join(", ", file.Tags ?? new List<string>())}]]");
        table.AddRow("Description", Markup.Escape(file.Description ?? "(none)"));
        table.AddRow("Hash", file.Hash);
        table.AddRow("Path", Markup.Escape(file.RelativePath));
        table.AddRow("Created", file.CreatedTime.ToString("yyyy-MM-dd HH:mm:ss"));

        AnsiConsole.Write(table);
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

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("[bold darkorange]ID[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Name[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]PrimaryTag[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Tags[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Description[/]").LeftAligned());

        foreach (var r in results)
        {
            table.AddRow(
                r.Id.ToString(),
                Markup.Escape(r.Name),
                Markup.Escape(r.PrimaryTag),
                $"[[{string.Join(", ", r.Tags ?? new List<string>())}]]",
                Markup.Escape(r.Description ?? "")
            );
        }

        AnsiConsole.Write(table);
    }

    // ==================== Batch Operations ====================

    static bool BatchCommand(List<string> tokens)
    {
        if (!RequireArchive()) return true;
        if (tokens.Count < 3)
        {
            ConsoleWriter.Info("Usage: batch <operation> <file> [args...]");
            ConsoleWriter.Dimmed("Operations: rename, retag, describe, delete, unlink, tag add, tag remove");
            ConsoleWriter.Dimmed("Example: batch rename report updated_report");
            ConsoleWriter.Dimmed("Example: batch tag add vacation landscape,summer");
            return true;
        }

        string op = tokens[1].ToLowerInvariant();
        string fileIdentifier = tokens[2];

        List<FileRecord> files;
        try
        {
            files = ResolveFiles(fileIdentifier);
        }
        catch (Exception ex)
        {
            ConsoleWriter.Err($"Batch failed: {ex.Message}");
            return true;
        }

        if (files.Count == 0)
        {
            ConsoleWriter.Warn("No files match the search criteria.");
            return true;
        }

        if (IsDestructiveOp(op, tokens))
        {
            if (!AnsiConsole.Confirm($"[yellow]This will affect {files.Count} file(s). Proceed?[/]"))
            {
                ConsoleWriter.Info("Cancelled.");
                return true;
            }
        }

        ConsoleWriter.Info($"Processing {files.Count} file(s)...");
        var ids = files.Select(f => f.Id).ToList();

        BatchResult? result = op switch
        {
            "rename" => BatchRename(ids, tokens),
            "retag" => BatchRetag(ids, tokens),
            "describe" => BatchDescribe(ids, tokens),
            "delete" => _api!.DeleteFile(ids),
            "unlink" => _api!.DeleteMetadataOnly(ids),
            "tag" => BatchTagSubcommand(ids, tokens),
            _ => null
        };

        if (result == null)
        {
            ConsoleWriter.Err($"Unknown batch operation: {op}. Valid: rename, retag, describe, delete, unlink, tag add, tag remove");
            return true;
        }

        PrintBatchResult(op, result);
        return true;
    }

    static bool IsDestructiveOp(string op, List<string> tokens)
    {
        if (op is "delete" or "unlink" or "retag")
            return true;
        return false;
    }

    static BatchResult? BatchTagSubcommand(List<Guid> ids, List<string> tokens)
    {
        if (tokens.Count < 4)
        {
            ConsoleWriter.Info("Usage: batch tag add|remove <file> <tag1,tag2,...>");
            return null;
        }

        return tokens[2].ToLowerInvariant() switch
        {
            "add" => BatchTagAdd(ids, tokens),
            "remove" => BatchTagRemove(ids, tokens),
            _ => null
        };
    }

    static BatchResult BatchRename(List<Guid> ids, List<string> tokens)
    {
        if (tokens.Count < 4)
            throw new Exception("Usage: batch rename <file> <new_name>");
        return _api!.ChangeFileName(ids, tokens[3]);
    }

    static BatchResult BatchRetag(List<Guid> ids, List<string> tokens)
    {
        if (tokens.Count < 4)
            throw new Exception("Usage: batch retag <file> <new_primary_tag>");
        return _api!.ChangePrimaryTag(ids, tokens[3]);
    }

    static BatchResult BatchDescribe(List<Guid> ids, List<string> tokens)
    {
        if (tokens.Count < 4)
            throw new Exception("Usage: batch describe <file> <description>");
        string description = string.Join(" ", tokens.Skip(3));
        return _api!.ChangeDescription(ids, description);
    }

    static BatchResult BatchTagAdd(List<Guid> ids, List<string> tokens)
    {
        if (tokens.Count < 5)
            throw new Exception("Usage: batch tag add <file> <tag1,tag2,...>");
        var tags = ParseTags(tokens[4]);
        return _api!.AddTags(ids, tags);
    }

    static BatchResult BatchTagRemove(List<Guid> ids, List<string> tokens)
    {
        if (tokens.Count < 5)
            throw new Exception("Usage: batch tag remove <file> <tag1,tag2,...>");
        var tags = ParseTags(tokens[4]);
        return _api!.RemoveTags(ids, tags);
    }

    static void PrintBatchResult(string operation, BatchResult result)
    {
        var table = new Table()
            .Title($"[bold darkorange]Batch '{operation}' Results[/]")
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("[bold]Metric[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Value[/]").LeftAligned());

        table.AddRow("Total", result.TotalCount.ToString());
        table.AddRow("Succeeded", $"[green]{result.SuccessCount}[/]");
        table.AddRow("Failed", result.FailureCount > 0 ? $"[red]{result.FailureCount}[/]" : "0");

        AnsiConsole.Write(table);

        if (result.FailureCount > 0)
        {
            var failureTable = new Table()
                .Title("[yellow]Failures[/]")
                .Border(TableBorder.Rounded)
                .AddColumn(new TableColumn("[bold]File ID[/]").LeftAligned())
                .AddColumn(new TableColumn("[bold]Error[/]").LeftAligned());

            foreach (var item in result.Items.Where(i => !i.IsSuccess))
            {
                string errorCode = item.ErrorCode != null ? $" [[{item.ErrorCode}]]" : "";
                string errorMessage = item.Error != null ? $" [[{item.ErrorCode}]]" : "";
                failureTable.AddRow(item.FileId.ToString(), $"{Markup.Escape(errorMessage)}{errorCode}");
            }

            AnsiConsole.Write(failureTable);
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
                var table = new Table()
                    .Border(TableBorder.Rounded)
                    .AddColumn(new TableColumn("[bold]Field[/]").LeftAligned())
                    .AddColumn(new TableColumn("[bold]Value[/]").LeftAligned());

                table.AddRow("Name", Markup.Escape(file.Name));
                table.AddRow("PrimaryTag", Markup.Escape(file.PrimaryTag));
                table.AddRow("Tags", $"[[{string.Join(", ", file.Tags ?? new List<string>())}]]");
                table.AddRow("Description", Markup.Escape(file.Description ?? "(none)"));

                AnsiConsole.Write(table);
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
        var grid = new Grid()
            .AddColumn(new GridColumn().NoWrap())
            .AddColumn(new GridColumn());

        grid.AddRow("[bold darkorange]ASIS.CLI[/]", "[dim]Archive Management Shell[/]");

        AnsiConsole.Write(new Rule("[bold]Archive Management[/]").RuleStyle("dim"));
        AnsiConsole.MarkupLine("  [bold]create[/] <name> [[path]]          Create a new archive");
        AnsiConsole.MarkupLine("  [bold]open[/] <path>                  Open an existing archive");
        AnsiConsole.MarkupLine("  [bold]close[/]                        Close the current archive");
        AnsiConsole.MarkupLine("  [bold]archive[/]                      Show current archive info");
        AnsiConsole.MarkupLine("  [bold]diff[/]                         Show orphaned/untracked files");
        AnsiConsole.WriteLine();

        AnsiConsole.Write(new Rule("[bold]File Operations[/]").RuleStyle("dim"));
        AnsiConsole.MarkupLine("  [bold]import[/] <path> <tag> [[tags...]] [[--desc \"...\"]] [[--move]]");
        AnsiConsole.MarkupLine("                              Import a file (copy by default)");
        AnsiConsole.MarkupLine("  [bold]rename[/] <file> <new>         Rename a file");
        AnsiConsole.MarkupLine("  [bold]retag[/] <file> <tag>          Change primary tag");
        AnsiConsole.MarkupLine("  [bold]tag[/] add <file> <t1,t2>      Add tags");
        AnsiConsole.MarkupLine("  [bold]tag[/] remove <file> <t>       Remove tags");
        AnsiConsole.MarkupLine("  [bold]tag[/] list <file>             List all tags");
        AnsiConsole.MarkupLine("  [bold]info[/] <file>                 Show file information");
        AnsiConsole.MarkupLine("  [bold]describe[/] <file> <desc>      Set file description");
        AnsiConsole.MarkupLine("  [bold]delete[/] <file>               Delete file and metadata");
        AnsiConsole.MarkupLine("  [bold]unlink[/] <file>               Remove metadata only");
        AnsiConsole.WriteLine();

        AnsiConsole.Write(new Rule("[bold]Search[/]").RuleStyle("dim"));
        AnsiConsole.MarkupLine("  [bold]search[/] name <keyword>       Search by name substring");
        AnsiConsole.MarkupLine("  [bold]search[/] tag <t1,t2>          Search by tag intersection");
        AnsiConsole.MarkupLine("  [bold]search[/] time <s> <e>         Search by date range");
        AnsiConsole.WriteLine();

        AnsiConsole.Write(new Rule("[bold]Batch Operations[/]").RuleStyle("dim"));
        AnsiConsole.MarkupLine("  [bold]batch[/] rename <file> <n>     Batch rename files");
        AnsiConsole.MarkupLine("  [bold]batch[/] retag <file> <tag>    Batch change primary tag");
        AnsiConsole.MarkupLine("  [bold]batch[/] describe <file> <d>   Batch set description");
        AnsiConsole.MarkupLine("  [bold]batch[/] delete <file>         Batch delete files");
        AnsiConsole.MarkupLine("  [bold]batch[/] unlink <file>         Batch unlink metadata");
        AnsiConsole.MarkupLine("  [bold]batch[/] tag add <file> <t>    Batch add tags");
        AnsiConsole.MarkupLine("  [bold]batch[/] tag remove <file> <t> Batch remove tags");
        AnsiConsole.WriteLine();

        AnsiConsole.Write(new Rule("[bold]ID Lookup[/]").RuleStyle("dim"));
        AnsiConsole.MarkupLine("  [bold]id[/] <guid> [[--full]]          Look up file by ID");
        AnsiConsole.WriteLine();

        AnsiConsole.Write(new Rule("[bold]System[/]").RuleStyle("dim"));
        AnsiConsole.MarkupLine("  [bold]help[/] [[command]]              Show help");
        AnsiConsole.MarkupLine("  [bold]exit[/]                        Exit CLI");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[dim]<file> can be a name substring or 'id:<guid>' for exact match.[/]");
    }

    static void PrintCommandHelp(string cmd)
    {
        var helpTexts = new Dictionary<string, (string usage, string description)>
        {
            ["create"] = ("create <name> [[path]]", "Create a new archive at the specified path."),
            ["open"] = ("open <path>", "Open an existing archive for editing."),
            ["close"] = ("close", "Close the current archive."),
            ["archive"] = ("archive", "Show information about the current archive."),
            ["import"] = ("import <source_path> <primary_tag> [[tags...]] [[--desc \"...\"]] [[--move]]", "Import a file into the archive. Use --move to move instead of copy."),
            ["rename"] = ("rename <file> <new_name>", "Rename a file in the archive."),
            ["retag"] = ("retag <file> <new_primary_tag>", "Change the primary tag of a file."),
            ["tag"] = ("tag add|remove|list <file> [[tags]]", "Manage tags on a file."),
            ["info"] = ("info <file>", "Show detailed information about a file."),
            ["describe"] = ("describe <file> <description>", "Set or update the description of a file."),
            ["delete"] = ("delete <file>", "Delete the physical file and its metadata."),
            ["unlink"] = ("unlink <file>", "Remove metadata only (keep the physical file)."),
            ["search"] = ("search name|tag|time <args>", "Search for files. Types:\n  name <keyword> - substring match on name\n  tag <t1,t2>    - must have ALL tags\n  time <s> <e>   - date range (yyyy-MM-dd)"),
            ["id"] = ("id <guid> [[--full]]", "Look up a file by its ID. Use --full for complete details."),
            ["help"] = ("help [[command]]", "Show help for all commands or a specific command."),
            ["exit"] = ("exit", "Exit the CLI."),
            ["batch"] = ("batch <operation> <file> [[args...]]", "Run an operation on all files matching <file>.\nOperations: rename, retag, describe, delete, unlink, tag add, tag remove\nExamples:\n  batch rename report new_report\n  batch tag add vacation landscape,summer\n  batch delete temp")
        };

        if (helpTexts.TryGetValue(cmd.ToLowerInvariant(), out var text))
        {
            var panel = new Panel(text.description)
                .Header($"[bold]{text.usage}[/]")
                .Border(BoxBorder.Rounded)
                .Padding(1, 0);
            AnsiConsole.Write(panel);
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
