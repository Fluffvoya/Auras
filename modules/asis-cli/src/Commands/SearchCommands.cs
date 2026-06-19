using ASIS.Core.Models;
using Spectre.Console;

namespace ASIS.CLI.Commands;

internal static class SearchCommands
{
    internal static bool Search(List<string> tokens)
    {
        if (!CommandRouter.RequireArchive()) return true;
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
        return CommandRouter.Api!.SearchByName(tokens[2]).ToList();
    }

    static List<FileRecord> SearchByTags(List<string> tokens)
    {
        if (tokens.Count < 3) throw new Exception("Usage: search tag <tag1,tag2,...>");
        var tags = CommandRouter.ParseTags(tokens[2]);
        return CommandRouter.Api!.SearchByTags(tags).ToList();
    }

    static List<FileRecord> SearchByTime(List<string> tokens)
    {
        if (tokens.Count < 4) throw new Exception("Usage: search time <start> <end> (format: yyyy-MM-dd)");
        if (!DateTime.TryParse(tokens[2], out DateTime start)) throw new Exception($"Invalid start date: {tokens[2]}");
        if (!DateTime.TryParse(tokens[3], out DateTime end)) throw new Exception($"Invalid end date: {tokens[3]}");
        end = end.Date.AddDays(1).AddTicks(-1);
        return CommandRouter.Api!.SearchByTime(start, end).ToList();
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
}
