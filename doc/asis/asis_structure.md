# ASIS.Core - Project Structure

## Overview

`ASIS.Core` is the core library of the Auras system. It provides the main public API (`ASISCoreAPI`) for archive management, along with repositories for JSON-based persistence, services for business logic, and models for data representation.

## Project Info

- **Assembly Name**: `asis-core`
- **Target Framework**: .NET 10.0

## Directory Structure

```
Tools/ASIS/ASIS.Core/
├── ASIS.Core.csproj
└── src/
    ├── ASISCoreAPI.cs         # Main public API (Facade)
    ├── Models/                # Data models
    ├── Repositories/          # Data access layer
    ├── Services/             # Business logic
    ├── Storage/               # JSON persistence
    └── Utils/                # Helper utilities
```

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
| `FileService` | Core file operations: import, rename, retag, add/remove tags, delete. Throws `AuraError` exceptions on validation failures |
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
| `HashHelper` | `ComputeSHA256(string file)` - Computes SHA-256 hash of a file |
| `PathHelper` | `GetRelativePath()` and `GetAbsolutePath()` - Path manipulation utilities |

## Dependencies

- `AuraError` - Error handling library
- `System.Text.Json` - JSON serialization