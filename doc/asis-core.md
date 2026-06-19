# asis-core

Core archive library providing the main public API (`ASISCoreAPI`) for archive management, along with repositories for JSON-based persistence, services for business logic, and models for data representation.

## Directory Structure

    Tools/ASIS/ASIS.Core/
    ├── ASIS.Core.csproj
    └── src/
        ├── ASISCoreAPI.cs         # Main public API (Facade)
        ├── Models/                # Data models
        ├── Repositories/          # Data access layer
        ├── Services/              # Business logic
        ├── Storage/               # JSON persistence
        └── Utils/                 # Helper utilities

## Components

### Models (`src/Models/`)

| Class | Description |
|-------|-------------|
| `FileRecord` | Represents an archived file with Id, Hash, Name, RelativePath, Description, PrimaryTag, Tags, CreatedTime |
| `Tag` | Simple tag model with Name property |
| `ArchiveConfig` | Archive metadata with Name and CreatedTime |

### Repositories (`src/Repositories/`)

| Class | Description |
|-------|-------------|
| `MetadataRepository` | Manages `List<FileRecord>` persisted to `metadata.json` |
| `TagIndexRepository` | Maps tags to file IDs: `Dictionary<string, HashSet<Guid>>` persisted to `tag_index.json` |
| `HashIndexRepository` | Maps hashes to file IDs: `Dictionary<string, Guid>` persisted to `hash_index.json` |
| `ArchiveConfigRepository` | Manages `ArchiveConfig` persisted to `archive.json` |

### Services (`src/Services/`)

| Class | Description |
|-------|-------------|
| `FileService` | Core file operations: import, rename, retag, add/remove tags, delete |
| `SearchService` | Search operations: by name (substring), by tags (intersection), by time range |
| `ArchiveService` | Archive-wide operations: `Diff()` finds orphaned metadata and untracked disk files |
| `ArchiveConfigService` | Provides read-only access to archive name |

### Storage (`src/Storage/`)

| Class | Description |
|-------|-------------|
| `JsonStorage` | Generic `Load<T>()` and `Save<T>()` for JSON file persistence |

### Utils (`src/Utils/`)

| Class | Description |
|-------|-------------|
| `HashHelper` | `ComputeSHA256(string file)` — Computes SHA-256 hash of a file |
| `PathHelper` | `GetRelativePath()` and `GetAbsolutePath()` — Path manipulation utilities |

## ASISCoreAPI Reference

**Namespace**: `ASIS.Core`
**File**: `Tools/ASIS/ASIS.Core/src/ASISCoreAPI.cs`

### Constructor

    public ASISCoreAPI(string archiveRoot)

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `ArchiveName` | `string` | Gets archive name from config |

### Methods

#### ImportFile

    public FileRecord ImportFile(
        string sourcePath,
        string primaryTag,
        IEnumerable<string>? tags = null,
        string? description = null,
        bool move = false
    )

Import a file into the archive. Returns the created `FileRecord`.

**Throws**: `ValidationException`, `PhysicalFileNotFoundException`, `DuplicateFileException`, `FileNameConflictException`

#### ChangeFileName

    public void ChangeFileName(Guid id, string newFileName)
    public void ChangeFileName(string file, string newFileName)

**Throws**: `ValidationException`, `FileRecordNotFoundException`, `FileNameConflictException`

#### ChangeDescription

    public void ChangeDescription(Guid id, string newDescription)
    public void ChangeDescription(string file, string newDescription)

**Throws**: `ValidationException`, `FileRecordNotFoundException`

#### ChangePrimaryTag

    public void ChangePrimaryTag(Guid id, string newPrimaryTag)
    public void ChangePrimaryTag(string file, string newPrimaryTag)

**Throws**: `ValidationException`, `FileRecordNotFoundException`

#### AddTags

    public void AddTags(Guid id, IEnumerable<string> tags)
    public void AddTags(string file, IEnumerable<string> tags)

**Throws**: `ValidationException`, `FileRecordNotFoundException`

#### RemoveTags

    public void RemoveTags(Guid id, IEnumerable<string> tags)
    public void RemoveTags(string file, IEnumerable<string> tags)

**Throws**: `ValidationException`, `FileRecordNotFoundException`, `InvalidTagOperationException`

#### DeleteFile

    public void DeleteFile(Guid id)
    public void DeleteFile(string file)

**Throws**: `ValidationException`, `FileRecordNotFoundException`

#### DeleteMetadataOnly

    public void DeleteMetadataOnly(Guid id)
    public void DeleteMetadataOnly(string file)

**Throws**: `ValidationException`, `FileRecordNotFoundException`

### Batch Operations

All batch methods accept a collection of file IDs and process each item independently.
Per-item failures are collected in the returned `BatchResult` — one failure does not
stop the remaining items.

    public BatchResult ChangeFileName(IEnumerable<Guid> ids, string newFileName)
    public BatchResult ChangeDescription(IEnumerable<Guid> ids, string newDescription)
    public BatchResult ChangePrimaryTag(IEnumerable<Guid> ids, string newPrimaryTag)
    public BatchResult AddTags(IEnumerable<Guid> ids, List<string> tags)
    public BatchResult RemoveTags(IEnumerable<Guid> ids, List<string> tags)
    public BatchResult DeleteFile(IEnumerable<Guid> ids)
    public BatchResult DeleteMetadataOnly(IEnumerable<Guid> ids)

**Result model**:

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
        public string? ErrorCode { get; set; }
    }

### Search

    public IEnumerable<FileRecord> SearchByName(string keyword)
    public IEnumerable<FileRecord> SearchByTags(IEnumerable<string> tags)
    public IEnumerable<FileRecord> SearchByTime(DateTime start, DateTime end)

### Diff

    public (IEnumerable<FileRecord> orphanedMetadata, IEnumerable<string> untrackedFiles) Diff()
