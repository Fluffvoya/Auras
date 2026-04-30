namespace AuraError.Exceptions;

public class InvalidTagOperationException : AuraException
{
    public string? Tag { get; }
    public string? Operation { get; }

    public InvalidTagOperationException(string operation, string tag)
        : base("INVALID_TAG_OPERATION", $"Cannot {operation} tag '{tag}'.")
    {
        Tag = tag;
        Operation = operation;
    }

    public InvalidTagOperationException(string message, Exception inner)
        : base("INVALID_TAG_OPERATION", message, inner) { }
}
