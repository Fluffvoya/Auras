using ASIS.Core.Models;
using ASIS.Core.Storage;
using Spectre.Console;

namespace ASIS.CLI.Commands;

internal static class ArchiveCommands
{
    internal static bool Create(List<string> tokens)
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

    internal static bool Open(List<string> tokens)
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

        CommandRouter.Api = new ASIS.Core.ASISCoreAPI(archivePath);
        ConsoleWriter.Ok($"Opened archive: {CommandRouter.Api.ArchiveName}");
        return true;
    }

    internal static bool Close()
    {
        CommandRouter.Api = null;
        ConsoleWriter.Info("Archive closed.");
        return true;
    }

    internal static bool ShowInfo()
    {
        if (!CommandRouter.RequireArchive()) return true;
        var diff = CommandRouter.Api!.Diff();

        var table = new Table()
            .Title("[bold darkorange]Archive Info[/]")
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("[bold]Property[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Value[/]").LeftAligned());

        table.AddRow("Name", Markup.Escape(CommandRouter.Api.ArchiveName ?? ""));
        table.AddRow("Files", CommandRouter.Api.SearchByName("").Count.ToString());
        table.AddRow("Orphaned", diff.OrphanedMetadata.Count.ToString());
        table.AddRow("Untracked", diff.OrphanedDiskFiles.Count.ToString());

        AnsiConsole.Write(table);
        return true;
    }

    internal static bool ShowDiff()
    {
        if (!CommandRouter.RequireArchive()) return true;
        var diff = CommandRouter.Api!.Diff();

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
}
