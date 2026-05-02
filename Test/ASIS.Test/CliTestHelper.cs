using System.Reflection;
using ASIS.CLI;
using ASIS.Core;
using ASIS.Core.Models;
using Spectre.Console;

namespace ASIS.Test;

internal class CliTestHelper : IDisposable
{
    private readonly string _tmpDir;
    private readonly string _archiveDir;
    private readonly TextReader _originalIn;
    private readonly StringWriter _outputCapture;
    private readonly IAnsiConsole _originalConsole;
    private bool _disposed;

    public string ArchiveDir => _archiveDir;
    public string TmpDir => _tmpDir;

    public CliTestHelper(string archiveName = "TestArchive") : this(false, archiveName) { }

    public CliTestHelper(bool noArchive, string archiveName = "TestArchive")
    {
        _tmpDir = Path.Combine(Path.GetTempPath(), "asis_cli_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tmpDir);
        _archiveDir = Path.Combine(_tmpDir, archiveName);

        _originalIn = Console.In;
        _originalConsole = AnsiConsole.Console;

        _outputCapture = new StringWriter();
        Console.SetOut(_outputCapture);
        AnsiConsole.Console = CreateConsole();

        if (!noArchive)
        {
            Directory.CreateDirectory(_archiveDir);
            File.WriteAllText(
                Path.Combine(_archiveDir, "archive.json"),
                $"{{\"Name\":\"{archiveName}\",\"CreatedTime\":\"2024-01-01T00:00:00\"}}");
            SetApiField(new ASISCoreAPI(_archiveDir));
        }
    }

    public (bool continueRunning, string output) Run(string input)
    {
        _outputCapture.GetStringBuilder().Clear();
        Console.SetOut(_outputCapture);
        AnsiConsole.Console = CreateConsole();
        bool result = Program.Process(input);
        string output = _outputCapture.ToString();
        return (result, output);
    }

    public List<string> SplitInput(string input) => Program.SplitInput(input);

    public string CreateSourceFile(string name, string content = "test content")
    {
        var path = Path.Combine(_tmpDir, "source", name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    public FileRecord ImportViaApi(string fileName, string primaryTag,
        List<string>? tags = null, string description = "", string? content = null)
    {
        var source = CreateSourceFile(fileName, content ?? $"content_{Guid.NewGuid()}");
        var api = GetApiField();
        return api!.ImportFile(source, primaryTag, tags ?? new List<string> { primaryTag }, description, false);
    }

    private IAnsiConsole CreateConsole()
    {
        return AnsiConsole.Create(new AnsiConsoleSettings
        {
            Out = new AnsiConsoleOutput(_outputCapture),
        });
    }

    private static void SetApiField(ASISCoreAPI? api)
    {
        var field = typeof(Program).GetField("_api",
            BindingFlags.Static | BindingFlags.NonPublic);
        field!.SetValue(null, api);
    }

    private static ASISCoreAPI? GetApiField()
    {
        var field = typeof(Program).GetField("_api",
            BindingFlags.Static | BindingFlags.NonPublic);
        return (ASISCoreAPI?)field!.GetValue(null);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        AnsiConsole.Console = _originalConsole;
        Console.SetIn(_originalIn);
        SetApiField(null);

        try { Directory.Delete(_tmpDir, true); }
        catch { /* best effort */ }
    }
}
