namespace AuraError.Exceptions;

public class DuplicateFileException : AuraException
{
    public DuplicateFileException()
        : base("DUPLICATE_FILE", "A file with the same hash already exists in the archive.") { }

    public DuplicateFileException(string hash)
        : base("DUPLICATE_FILE", $"A file with hash '{hash[..8]}...' already exists in the archive.") { }

    public DuplicateFileException(string message, Exception inner)
        : base("DUPLICATE_FILE", message, inner) { }
}
