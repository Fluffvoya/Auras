namespace ASIS.CLI;

static class ConsoleWriter
{
    private const string Reset = "\u001b[0m";
    private const string Red = "\u001b[91m";
    private const string Green = "\u001b[92m";
    private const string Yellow = "\u001b[93m";
    private const string Cyan = "\u001b[96m";

    public static void Ok(string msg) => Write(Green, msg);
    public static void Err(string msg) => Write(Red, msg);
    public static void Warn(string msg) => Write(Yellow, msg);
    public static void Info(string msg) => Write(Cyan, msg);

    private static void Write(string color, string msg)
    {
        Console.WriteLine($"{color}{msg}{Reset}");
    }
}
