namespace AuraError.Results;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }
    public string? ErrorCode { get; }

    protected Result(bool isSuccess, string? error, string? errorCode)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorCode = errorCode;
    }

    public static Result Success() => new(true, null, null);

    public static Result Failure(string error, string? errorCode = null) =>
        new(false, error, errorCode);

    public static Result<T> Success<T>(T value) => new(value, true, null, null);

    public static Result<T> Failure<T>(string error, string? errorCode = null) =>
        new(default, false, error, errorCode);
}

public class Result<T>
{
    public T? Value { get; }
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }
    public string? ErrorCode { get; }

    internal Result(T? value, bool isSuccess, string? error, string? errorCode)
    {
        Value = value;
        IsSuccess = isSuccess;
        Error = error;
        ErrorCode = errorCode;
    }

    public static implicit operator Result<T>(T value) => Result.Success(value);

    public static implicit operator Result<T>(Result result)
    {
        if (result.IsSuccess)
            throw new InvalidOperationException("Cannot convert a success result without a value.");
        return Result.Failure<T>(result.Error!, result.ErrorCode);
    }
}
