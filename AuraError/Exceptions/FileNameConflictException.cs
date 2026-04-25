namespace AuraError.Exceptions;

public class FileNameConflictException : AuraException
{
    public string? TargetPath { get; }

    public FileNameConflictException(string targetPath)
        : base("FILE_NAME_CONFLICT", $"A file with the same name already exists at '{targetPath}'.")
    {
        TargetPath = targetPath;
    }

    public FileNameConflictException(string message, Exception inner)
        : base("FILE_NAME_CONFLICT", message, inner) { }
}
