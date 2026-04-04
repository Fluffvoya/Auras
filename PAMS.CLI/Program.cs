using System.Text;
using PAMS.Core;
using PAMS.Core.Models;

class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        if (args.Length == 0)
        {
            PrintHelp();
            return;
        }


        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PAMS"
        );
        string file = Path.Combine(folder, "config.json");
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        var api = new PAMSCoreAPI(file);

        try
        {
            Dispatch(api, args);
        }
        catch (Exception ex)
        {
            WriteError(ex.Message);
        }
    }

    static void Dispatch(PAMSCoreAPI api, string[] args)
    {
        var command = args[0];

        switch (command)
        {
            case "config":
                HandleConfig(api, args.Skip(1).ToArray());
                break;

            case "import":
                HandleImport(api, args.Skip(1).ToArray());
                break;

            case "delete":
                HandleDelete(api, args.Skip(1).ToArray());
                break;

            case "search":
                HandleSearch(api, args.Skip(1).ToArray());
                break;

            case "help":
                PrintHelp();
                break;

            default:
                WriteError("Unknown command");
                PrintHelp();
                break;
        }
    }

    static void HandleConfig(PAMSCoreAPI api, string[] args)
    {
        if (args.Length < 2)
        {
            PrintHelp();
            return;
        }

        if (args[0] == "set-root")
        {
            api.ChangeArchiveRoot(args[1]);
            WriteSuccess("Archive root updated.");
        }
    }

    static void HandleImport(PAMSCoreAPI api, string[] args)
    {
        if (args.Length == 0)
            throw new Exception("File path required.");

        string file = args[0];

        string primary = "";
        List<string> tags = new();
        bool move = false;

        for (int i = 1; i < args.Length; i++)
        {
            var arg = args[i];

            switch (arg)
            {
                case "-p":
                case "--primary":
                    primary = args[++i];
                    break;

                case "-t":
                case "--tags":
                    while (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                        tags.Add(args[++i]);
                    break;

                case "--move":
                    move = true;
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(primary))
            throw new Exception("Primary tag required (-p).");

        var record = api.ImportFile(file, primary, tags, move);

        WriteSuccess("File imported.");
        PrintRecord(record);
    }

    static void HandleDelete(PAMSCoreAPI api, string[] args)
    {
        if (args.Length == 0)
            throw new Exception("File name required.");

        var name = args[0];

        api.DeleteFile(name);

        WriteWarning($"Deleted files matching: {name}");
    }

    static void HandleSearch(PAMSCoreAPI api, string[] args)
    {
        if (args.Length < 2)
            throw new Exception("Usage: search name <keyword> | search tag <tags>");

        var type = args[0];

        if (type == "name")
        {
            var keyword = args[1];
            var results = api.SearchByName(keyword);
            PrintResults(results);
        }
        else if (type == "tag")
        {
            var tags = args.Skip(1).ToList();
            var results = api.SearchByTags(tags);
            PrintResults(results);
        }
        else
        {
            throw new Exception("Unknown search type.");
        }
    }

    static void PrintResults(List<FileRecord> records)
    {
        if (records.Count == 0)
        {
            WriteWarning("No results found.");
            return;
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\nFound {records.Count} result(s):\n");
        Console.ResetColor();

        foreach (var r in records)
        {
            PrintRecord(r);
            Console.WriteLine();
        }
    }

    static void PrintRecord(FileRecord r)
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(r.Name);
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  ID: {r.Id}");
        Console.WriteLine($"  Path: {r.RelativePath}");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"  Primary: {r.PrimaryTag}");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"  Tags: {string.Join(", ", r.Tags)}");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.DarkGreen;
        Console.WriteLine($"  Created: {r.CreatedTime}");
        Console.ResetColor();
    }

    static void PrintHelp()
    {
        Console.WriteLine("PAMS CLI");
        Console.WriteLine();

        Console.WriteLine("Commands:");
        Console.WriteLine("  config set-root <path>");
        Console.WriteLine("  import <file> -p <primary> -t <tags...> [--move]");
        Console.WriteLine("  delete <name>");
        Console.WriteLine("  search name <keyword>");
        Console.WriteLine("  search tag <tag1> <tag2> ...");
    }

    static void WriteSuccess(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✔ " + msg);
        Console.ResetColor();
    }

    static void WriteWarning(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("⚠ " + msg);
        Console.ResetColor();
    }

    static void WriteError(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("✖ " + msg);
        Console.ResetColor();
    }
}