using Spectre.Console;

namespace ASIS.CLI.Commands;

internal static class HelpCommand
{
    internal static bool Show(List<string> tokens)
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
}
