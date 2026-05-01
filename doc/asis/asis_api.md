# ASISCoreAPI - API Reference

**Namespace**: `ASIS.Core`
**File**: `Tools/ASIS/ASIS.Core/src/ASISCoreAPI.cs`

Main public API for archive management. Takes `archiveRoot` path and internally creates all repositories and services.

---

## Constructor

```csharp
public ASISCoreAPI(string archiveRoot)
```

---

## Properties

| Property      | Type     | Description                   |
| ------------- | -------- | ----------------------------- |
| `ArchiveName` | `string` | Gets archive name from config |

---

## Methods

### ImportFile

```csharp
public FileRecord ImportFile(
    string sourcePath,
    string primaryTag,
    IEnumerable<string>? tags = null,
    string? description = null,
    bool move = false
)
```

Import a file into the archive. Returns the created `FileRecord`.

**Parameters**:

- `sourcePath` - Path to source file
- `primaryTag` - Primary tag (required)
- `tags` - Additional tags
- `description` - Description text
- `move` - Move (true) or copy (false)

**Throws**: `ValidationException`, `PhysicalFileNotFoundException`, `DuplicateFileException`, `FileNameConflictException`

---

### ChangeFileName

```csharp
public void ChangeFileName(Guid id, string newFileName)
public void ChangeFileName(string file, string newFileName)
```

Rename a file or `newFileName`.

**Throws**: `ValidationException`, `FileRecordNotFoundException`, `FileNameConflictException`

---

### ChangeDescription

```csharp
public void ChangeDescription(Guid id, string newDescription)
public void ChangeDescription(string file, string newDescription)
```

Update file description.

**Throws**: `ValidationException`, `FileRecordNotFoundException`

---

### ChangePrimaryTag

```csharp
public void ChangePrimaryTag(Guid id, string newPrimaryTag)
public void ChangePrimaryTag(string file, string newPrimaryTag)
```

Change primary tag (retag).

**Throws**: `ValidationException`, `FileRecordNotFoundException`

---

### AddTags

```csharp
public void AddTags(Guid id, IEnumerable<string> tags)
public void AddTags(string file, IEnumerable<string> tags)
```

Add tags to a file.

**Throws**: `ValidationException`, `FileRecordNotFoundException`

---

### RemoveTags

```csharp
public void RemoveTags(Guid id, IEnumerable<string> tags)
public void RemoveTags(string file, IEnumerable<string> tags)
```

Remove tags from a file.

**Throws**: `ValidationException`, `FileRecordNotFoundException`, `InvalidTagOperationException`

---

### DeleteFile

```csharp
public void DeleteFile(Guid id)
public void DeleteFile(string file)
```

Delete physical file and metadata.

**Throws**: `ValidationException`, `FileRecordNotFoundException`

---

### DeleteMetadataOnly

```csharp
public void DeleteMetadataOnly(Guid id)
public void DeleteMetadataOnly(string file)
```

Remove metadata only (unlink file from archive).

**Throws**: `ValidationException`, `FileRecordNotFoundException`

---

### Batch Operations

All batch methods accept a collection of file IDs and process each item independently.
Per-item failures are collected in the returned `BatchResult` — one failure does not
stop the remaining items. Batch operations do not modify `FileService` or repositories.

```csharp
public BatchResult ChangeFileName(IEnumerable<Guid> ids, string newFileName)
public BatchResult ChangeDescription(IEnumerable<Guid> ids, string newDescription)
public BatchResult ChangePrimaryTag(IEnumerable<Guid> ids, string newPrimaryTag)
public BatchResult AddTags(IEnumerable<Guid> ids, List<string> tags)
public BatchResult RemoveTags(IEnumerable<Guid> ids, List<string> tags)
public BatchResult DeleteFile(IEnumerable<Guid> ids)
public BatchResult DeleteMetadataOnly(IEnumerable<Guid> ids)
```

**Result model**:

```csharp
public class BatchResult
{
    public int TotalCount { get; }
    public int SuccessCount { get; }
    public int FailureCount { get; }
    public List<BatchItemResult> Items { get; }
}

public class BatchItemResult
{
    public Guid FileId { get; set; }
    public bool IsSuccess { get; set; }
    public string? Error { get; set; }
    public string? ErrorCode { get; set; } // AuraException.Code value
}
```

**Error handling**: Each file is wrapped in try/catch. `ErrorCode` maps to the
`AuraException.Code` when the exception is an AuraException subtype.

---

### SearchByName

```csharp
public IEnumerable<FileRecord> SearchByName(string keyword)
```

Search files by filename substring match.

**Throws**: `ValidationException`

---

### SearchByTags

```csharp
public IEnumerable<FileRecord> SearchByTags(IEnumerable<string> tags)
```

Search files by tag intersection (must have ALL tags).

**Throws**: `ValidationException`

---

### SearchByTime

```csharp
public IEnumerable<FileRecord> SearchByTime(DateTime start, DateTime end)
```

Search files within time range.

---

### Diff

```csharp
public (IEnumerable<FileRecord> orphanedMetadata, IEnumerable<string> untrackedFiles) Diff()
```

Find orphaned metadata and untracked disk files.
