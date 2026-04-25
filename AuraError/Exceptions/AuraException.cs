namespace AuraError.Exceptions;

public abstract class AuraException : Exception
{
    public string Code { get; }

    protected AuraException(string code, string message) : base(message)
    {
        Code = code;
    }

    protected AuraException(string code, string message, Exception inner) : base(message, inner)
    {
        Code = code;
    }
}
