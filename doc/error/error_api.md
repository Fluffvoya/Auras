# AuraError - API Reference

**Namespace**: `AuraError`
**Assembly**: `aura-error`

Error handling library with exception types and error codes, plus result types for functional error handling.

---

## Global Usings

```csharp
global using AuraError.Exceptions;
global using AuraError.Results;
```

---

## Result Types

### Result

```csharp
public class Result
{
    bool IsSuccess { get; }
    bool IsFailure { get; }
    string? Error { get; }
    string? ErrorCode { get; }

    static Result Success()
    static Result Failure(string error, string? errorCode = null)
}
```

---

### Result<T>

```csharp
public class Result<T>
{
    T? Value { get; }
    bool IsSuccess { get; }
    bool IsFailure { get; }
    string? Error { get; }
    string? ErrorCode { get; }

    static Result<T> Success(T value)
    static Result<T> Failure(string error, string? errorCode = null)
}
```

---

## Exception Types

All inherit from `AuraException` with `string Code { get; }`.

### AuraException

```csharp
public abstract class AuraException : Exception
{
    string Code { get; }
}
```

---

### DuplicateFileException

```csharp
public class DuplicateFileException : AuraException
```

**Code**: `"DUPLICATE_FILE"`
**Thrown**: File hash already exists in archive

---

### FileRecordNotFoundException

```csharp
public class FileRecordNotFoundException : AuraException
{
    Guid FileId { get; }
}
```

**Code**: `"FILE_NOT_FOUND"`
**Thrown**: File record not found

---

### PhysicalFileNotFoundException

```csharp
public class PhysicalFileNotFoundException : AuraException
{
    string? FilePath { get; }
}
```

**Code**: `"PHYSICAL_FILE_NOT_FOUND"`
**Thrown**: Physical file missing from disk

---

### FileNameConflictException

```csharp
public class FileNameConflictException : AuraException
{
    string? TargetPath { get; }
}
```

**Code**: `"FILE_NAME_CONFLICT"`
**Thrown**: Target filename already exists

---

### ValidationException

```csharp
public class ValidationException : AuraException
{
    string ParameterName { get; }
}
```

**Code**: `"VALIDATION_ERROR"`
**Thrown**: Input validation fails

---

### InvalidTagOperationException

```csharp
public class InvalidTagOperationException : AuraException
{
    string? Tag { get; }
    string? Operation { get; }
}
```

**Code**: `"INVALID_TAG_OPERATION"`
**Thrown**: Invalid tag operation (e.g., removing primary tag)