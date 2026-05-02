using System.Text;
using ASIS.CLI.Commands;
using ASIS.Core;
using ASIS.Core.Models;
using Spectre.Console;

namespace ASIS.CLI;

internal static class CommandRouter
{
    private static ASISCoreAPI? _api;

    internal static ASISCoreAPI? Api
    {
        get => _api;
        set => _api = value;
    }

    internal static bool Process(string input)
    {
        var tokens = SplitInput(input);
        if (tokens.Count == 0) return true;

        var cmd = tokens[0].ToLowerInvariant();
        return cmd switch
        {
            "create" => ArchiveCommands.Create(tokens),
            "open" => ArchiveCommands.Open(tokens),
            "close" => ArchiveCommands.Close(),
            "archive" => ArchiveCommands.ShowInfo(),
            "import" => FileCommands.Import(tokens),
            "rename" => FileCommands.Rename(tokens),
            "retag" => FileCommands.Retag(tokens),
            "tag" => FileCommands.Tag(tokens),
            "info" => FileCommands.Info(tokens),
            "describe" => FileCommands.Describe(tokens),
            "delete" => FileCommands.Delete(tokens),
            "unlink" => FileCommands.Unlink(tokens),
            "search" => SearchCommands.Search(tokens),
            "batch" => BatchCommands.Execute(tokens),
            "id" => IdCommand.Lookup(tokens),
            "diff" => ArchiveCommands.ShowDiff(),
            "help" => HelpCommand.Show(tokens),
            "exit" => false,
            _ => UnknownCommand(cmd)
        };
    }

    internal static List<string> SplitInput(string input)
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

    internal static bool RequireArchive()
    {
        if (_api == null)
        {
            ConsoleWriter.Err("No archive open. Use 'open <path>' first.");
            return false;
        }
        return true;
    }

    internal static FileRecord ResolveFile(string identifier)
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

    internal static FileRecord? TryResolveFile(string identifier)
    {
        try { return ResolveFile(identifier); }
        catch { return null; }
    }

    internal static List<FileRecord> ResolveFiles(string identifier)
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

    internal static List<string> ParseTags(string? tagsStr)
    {
        if (string.IsNullOrWhiteSpace(tagsStr)) return new List<string>();
        return tagsStr.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList();
    }

    internal static bool UnknownCommand(string cmd)
    {
        ConsoleWriter.Err($"Unknown command: {cmd}. Type 'help' for available commands.");
        return true;
    }

    internal static bool UnknownSubcommand(string cmd, string subcmd, params string[] valid)
    {
        string validList = string.Join(", ", valid);
        ConsoleWriter.Err($"Unknown subcommand '{subcmd}' for '{cmd}'. Valid: {validList}.");
        return true;
    }
}
