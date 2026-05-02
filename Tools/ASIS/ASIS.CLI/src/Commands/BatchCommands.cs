using ASIS.Core.Models;
using Spectre.Console;

namespace ASIS.CLI.Commands;

internal static class BatchCommands
{
    internal static bool Execute(List<string> tokens)
    {
        if (!CommandRouter.RequireArchive()) return true;
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
            files = CommandRouter.ResolveFiles(fileIdentifier);
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
            "delete" => CommandRouter.Api!.DeleteFile(ids),
            "unlink" => CommandRouter.Api!.DeleteMetadataOnly(ids),
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
        return CommandRouter.Api!.ChangeFileName(ids, tokens[3]);
    }

    static BatchResult BatchRetag(List<Guid> ids, List<string> tokens)
    {
        if (tokens.Count < 4)
            throw new Exception("Usage: batch retag <file> <new_primary_tag>");
        return CommandRouter.Api!.ChangePrimaryTag(ids, tokens[3]);
    }

    static BatchResult BatchDescribe(List<Guid> ids, List<string> tokens)
    {
        if (tokens.Count < 4)
            throw new Exception("Usage: batch describe <file> <description>");
        string description = string.Join(" ", tokens.Skip(3));
        return CommandRouter.Api!.ChangeDescription(ids, description);
    }

    static BatchResult BatchTagAdd(List<Guid> ids, List<string> tokens)
    {
        if (tokens.Count < 5)
            throw new Exception("Usage: batch tag add <file> <tag1,tag2,...>");
        var tags = CommandRouter.ParseTags(tokens[4]);
        return CommandRouter.Api!.AddTags(ids, tags);
    }

    static BatchResult BatchTagRemove(List<Guid> ids, List<string> tokens)
    {
        if (tokens.Count < 5)
            throw new Exception("Usage: batch tag remove <file> <tag1,tag2,...>");
        var tags = CommandRouter.ParseTags(tokens[4]);
        return CommandRouter.Api!.RemoveTags(ids, tags);
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
}
