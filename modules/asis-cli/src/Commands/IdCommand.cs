using Spectre.Console;

namespace ASIS.CLI.Commands;

internal static class IdCommand
{
    internal static bool Lookup(List<string> tokens)
    {
        if (!CommandRouter.RequireArchive()) return true;
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

            var allFiles = CommandRouter.Api!.SearchByName("");
            var file = allFiles.FirstOrDefault(r => r.Id == guid);

            if (file == null)
                throw new Exception($"No file found with ID: {guid}");

            if (isFull)
            {
                FileCommands.PrintFileInfo(file);
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
}
