namespace ASIS.CLI;

static class ConsoleWriter
{
    private const string Reset = "\u001b[0m";
    private const string Bold = "\u001b[1m";
    private const string Dim = "\u001b[2m";
    private const string Red = "\u001b[91m";
    private const string Green = "\u001b[92m";
    private const string Yellow = "\u001b[93m";
    private const string Cyan = "\u001b[96m";
    private const string Magenta = "\u001b[95m";

    public static void Ok(string msg) => Write(Green, msg);
    public static void Err(string msg) => Write(Red, msg);
    public static void Warn(string msg) => Write(Yellow, msg);
    public static void Info(string msg) => Write(Cyan, msg);
    public static void Title(string msg) => Write(Bold + Cyan, msg);
    public static void Label(string msg) => Write(Bold, msg);
    public static void Dimmed(string msg) => Write(Dim, msg);

    private static void Write(string color, string msg)
    {
        Console.WriteLine($"{color}{msg}{Reset}");
    }

    public static void PrintDivider()
    {
        Console.WriteLine($"{Dim}{new string('─', 50)}{Reset}");
    }

    public static void PrintWelcome()
    {

        Console.WriteLine($@"{Cyan}
   █████╗  ███████╗ ██╗ ███████╗
  ██╔══██╗ ██╔════╝ ██║ ██╔════╝
  ███████║ ███████╗ ██║ ███████╗
  ██╔══██║ ╚════██║ ██║ ╚════██║
  ██║  ██║ ███████║ ██║ ███████║
  ╚═╝  ╚═╝ ╚══════╝ ╚═╝ ╚══════╝{Reset}");
        Console.WriteLine($"{Bold}  Archive Storage & Information System{Reset}");
        Console.WriteLine($"{Dim}  Type 'help' for commands, 'exit' to quit{Reset}");
        PrintDivider();
    }
}
