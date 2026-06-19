using System.Text;
using Spectre.Console;

namespace ASIS.CLI;

class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        ConsoleWriter.PrintWelcome();
        while (true)
        {
            var archive = CommandRouter.Api?.ArchiveName;
            var prompt = archive == null
                ? "[orange3]>[/] "
                : $"[orange1][[{archive}]][/] [orange3]>[/] ";
            AnsiConsole.Markup(prompt);
            string? input = Console.ReadLine();
            if (input == null) { AnsiConsole.WriteLine(); continue; }
            if (!CommandRouter.Process(input)) break;
        }
    }
}
