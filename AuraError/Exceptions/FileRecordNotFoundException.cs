namespace AuraError.Exceptions;

public class FileRecordNotFoundException : AuraException
{
    public Guid FileId { get; }

    public FileRecordNotFoundException(Guid fileId)
        : base("FILE_NOT_FOUND", $"File record with ID '{fileId}' was not found.")
    {
        FileId = fileId;
    }

    public FileRecordNotFoundException(string message, Exception inner)
        : base("FILE_NOT_FOUND", message, inner) { }
}
