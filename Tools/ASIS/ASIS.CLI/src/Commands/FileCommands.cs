using ASIS.Core.Models;
using Spectre.Console;

namespace ASIS.CLI.Commands;

internal static class FileCommands
{
    internal static bool Import(List<string> tokens)
    {
        if (!CommandRouter.RequireArchive()) return true;
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
            var record = CommandRouter.Api!.ImportFile(sourcePath, primaryTag, additionalTags, description, move);
            ConsoleWriter.Ok($"Imported: {record.Name} ({record.Id})");
        }
        catch (Exception ex)
        {
            ConsoleWriter.Err($"Import failed: {ex.Message}");
        }
        return true;
    }

    internal static bool Rename(List<string> tokens)
    {
        if (!CommandRouter.RequireArchive()) return true;
        if (tokens.Count < 3)
        {
            ConsoleWriter.Info("Usage: rename <file> <new_name>");
            return true;
        }

        try
        {
            var file = CommandRouter.ResolveFile(tokens[1]);
            CommandRouter.Api!.ChangeFileName(file!.Id, tokens[2]);
            ConsoleWriter.Ok("File renamed.");
        }
        catch (Exception ex)
        {
            ConsoleWriter.Err($"Rename failed: {ex.Message}");
        }
        return true;
    }

    internal static bool Retag(List<string> tokens)
    {
        if (!CommandRouter.RequireArchive()) return true;
        if (tokens.Count < 3)
        {
            ConsoleWriter.Info("Usage: retag <file> <new_primary_tag>");
            return true;
        }

        try
        {
            var file = CommandRouter.ResolveFile(tokens[1]);
            CommandRouter.Api!.ChangePrimaryTag(file!.Id, tokens[2]);
            ConsoleWriter.Ok("Primary tag updated.");
        }
        catch (Exception ex)
        {
            ConsoleWriter.Err($"Retag failed: {ex.Message}");
        }
        return true;
    }

    internal static bool Tag(List<string> tokens)
    {
        if (!CommandRouter.RequireArchive()) return true;
        if (tokens.Count < 3)
        {
            ConsoleWriter.Info("Usage: tag add|remove|list <file> [tags]");
            return true;
        }

        string subcmd = tokens[1].ToLowerInvariant();
        string fileIdentifier = tokens[2];

        try
        {
            var file = CommandRouter.ResolveFile(fileIdentifier);

            return subcmd switch
            {
                "add" => TagAdd(file, tokens),
                "remove" => TagRemove(file, tokens),
                "list" => TagList(file),
                _ => CommandRouter.UnknownSubcommand("tag", subcmd, "add", "remove", "list")
            };
        }
        catch (Exception ex)
        {
            ConsoleWriter.Err($"Tag command failed: {ex.Message}");
        }
        return true;
    }

    static bool TagAdd(FileRecord file, List<string> tokens)
    {
        if (tokens.Count < 4)
        {
            ConsoleWriter.Info("Usage: tag add <file> <tag1,tag2,...>");
            return true;
        }

        var tags = CommandRouter.ParseTags(tokens[3]);
        CommandRouter.Api!.AddTags(file.Id, tags);
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

        var tags = CommandRouter.ParseTags(tokens[3]);
        CommandRouter.Api!.RemoveTags(file.Id, tags);
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

    internal static bool Info(List<string> tokens)
    {
        if (!CommandRouter.RequireArchive()) return true;
        if (tokens.Count < 2)
        {
            ConsoleWriter.Info("Usage: info <file>");
            return true;
        }

        try
        {
            var file = CommandRouter.ResolveFile(tokens[1]);
            PrintFileInfo(file!);
        }
        catch (Exception ex)
        {
            ConsoleWriter.Err($"Info failed: {ex.Message}");
        }
        return true;
    }

    internal static void PrintFileInfo(FileRecord file)
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

    internal static bool Describe(List<string> tokens)
    {
        if (!CommandRouter.RequireArchive()) return true;
        if (tokens.Count < 3)
        {
            ConsoleWriter.Info("Usage: describe <file> <description>");
            return true;
        }

        try
        {
            var file = CommandRouter.ResolveFile(tokens[1]);
            string description = string.Join(" ", tokens.Skip(2));
            CommandRouter.Api!.ChangeDescription(file!.Id, description);
            ConsoleWriter.Ok("Description updated.");
        }
        catch (Exception ex)
        {
            ConsoleWriter.Err($"Describe failed: {ex.Message}");
        }
        return true;
    }

    internal static bool Delete(List<string> tokens)
    {
        if (!CommandRouter.RequireArchive()) return true;
        if (tokens.Count < 2)
        {
            ConsoleWriter.Info("Usage: delete <file>");
            return true;
        }

        try
        {
            var file = CommandRouter.ResolveFile(tokens[1]);
            CommandRouter.Api!.DeleteFile(file!.Id);
            ConsoleWriter.Ok("File deleted.");
        }
        catch (Exception ex)
        {
            ConsoleWriter.Err($"Delete failed: {ex.Message}");
        }
        return true;
    }

    internal static bool Unlink(List<string> tokens)
    {
        if (!CommandRouter.RequireArchive()) return true;
        if (tokens.Count < 2)
        {
            ConsoleWriter.Info("Usage: unlink <file>");
            return true;
        }

        try
        {
            var file = CommandRouter.ResolveFile(tokens[1]);
            CommandRouter.Api!.DeleteMetadataOnly(file!.Id);
            ConsoleWriter.Ok("Metadata removed (file kept).");
        }
        catch (Exception ex)
        {
            ConsoleWriter.Err($"Unlink failed: {ex.Message}");
        }
        return true;
    }
}
