using Spectre.Console;

namespace ASIS.CLI;

static class ConsoleWriter
{
    public static void Ok(string msg) => AnsiConsole.MarkupLine($"[bold green]:check_mark: {Escape(msg)}[/]");
    public static void Err(string msg) => AnsiConsole.MarkupLine($"[bold red]:cross_mark: {Escape(msg)}[/]");
    public static void Warn(string msg) => AnsiConsole.MarkupLine($"[bold yellow]:warning: {Escape(msg)}[/]");
    public static void Info(string msg) => AnsiConsole.MarkupLine($"[cyan]{Escape(msg)}[/]");
    public static void Title(string msg) => AnsiConsole.MarkupLine($"[bold cyan]{Escape(msg)}[/]");
    public static void Label(string msg) => AnsiConsole.Markup($"[bold]{Escape(msg)}[/]");
    public static void Dimmed(string msg) => AnsiConsole.MarkupLine($"[dim]{Escape(msg)}[/]");

    public static void PrintDivider() => AnsiConsole.Write(new Rule().RuleStyle("dim"));

    public static void PrintWelcome()
    {
        var asciiArt = @"   █████╗  ███████╗ ██╗ ███████╗
  ██╔══██╗ ██╔════╝ ██║ ██╔════╝
  ███████║ ███████╗ ██║ ███████╗
  ██╔══██║ ╚════██║ ██║ ╚════██║
  ██║  ██║ ███████║ ██║ ███████║
  ╚═╝  ╚═╝ ╚══════╝ ╚═╝ ╚══════╝";

        AnsiConsole.MarkupLine($"[cyan]{Escape(asciiArt)}[/]");
        AnsiConsole.MarkupLine("[bold]  Archive Storage & Information System[/]");
        AnsiConsole.MarkupLine("[dim]  Type 'help' for commands, 'exit' to quit[/]");
        PrintDivider();
    }

    private static string Escape(string text) => text.Replace("[", "[[").Replace("]", "]]");
}
