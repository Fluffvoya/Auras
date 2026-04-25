namespace AuraError.Exceptions;

public class PhysicalFileNotFoundException : AuraException
{
    public string? FilePath { get; }

    public PhysicalFileNotFoundException(string filePath)
        : base("PHYSICAL_FILE_NOT_FOUND", $"Physical file not found at '{filePath}'.")
    {
        FilePath = filePath;
    }

    public PhysicalFileNotFoundException(string message, Exception inner)
        : base("PHYSICAL_FILE_NOT_FOUND", message, inner) { }
}
