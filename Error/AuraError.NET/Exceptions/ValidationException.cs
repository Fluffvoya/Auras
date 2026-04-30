namespace AuraError.Exceptions;

public class ValidationException : AuraException
{
    public string ParameterName { get; }

    public ValidationException(string message)
        : base("VALIDATION_ERROR", message)
    {
        ParameterName = string.Empty;
    }

    public ValidationException(string message, string parameterName)
        : base("VALIDATION_ERROR", message)
    {
        ParameterName = parameterName;
    }

    public ValidationException(string message, string parameterName, Exception inner)
        : base("VALIDATION_ERROR", message, inner)
    {
        ParameterName = parameterName;
    }
}
