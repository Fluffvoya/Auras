# PAMS Architecture

## Overview

PAMS (Personal Archive Management System) is a .NET-based file archiving library with CLI tooling. It provides file import, organization by tags, duplicate detection, and search capabilities.

## Project Structure

```
PAMS/
├── PAMS.Core/          # Core library
│   ├── Models/         # Data models
│   ├── Repositories/   # Data persistence
│   ├── Services/       # Business logic
│   ├── Storage/        # Storage abstractions
│   └── Utils/          # Utility classes
│
└── PAMS.CLI/           # Command-line interface
```

## Layers

### Models (`PAMS.Core.Models`)

| Class | Description |
|-------|-------------|
| `FileRecord` | Represents an archived file with metadata (ID, hash, name, tags, path, description) |
| `Tag` | Simple tag representation |
| `Config` | User configuration (username) |
| `ArchiveConfig` | Archive metadata (name, creation time) |

### Repositories (`PAMS.Core.Repositories`)

JSON-based persistence layer:

| Class | File | Purpose |
|-------|------|---------|
| `MetadataRepository` | `metadata.json` | File record storage |
| `TagIndexRepository` | `tag_index.json` | Tag-to-files index |
| `HashIndexRepository` | `hash_index.json` | Hash-to-fileID index (deduplication) |
| `ConfigRepository` | `config.json` | User settings |
| `ArchiveConfigRepository` | `archive.json` | Archive metadata |

### Services (`PAMS.Core.Services`)

| Class | Responsibility |
|-------|----------------|
| `FileService` | File import, rename, tag changes, deletion |
| `SearchService` | Search by name or tags |
| `ArchiveConfigService` | Archive configuration access |
| `ConfigService` | User configuration management |

### Storage (`PAMS.Core.Storage`)

| Class | Purpose |
|-------|---------|
| `JsonStorage` | Generic JSON serialization/deserialization |

### Utils (`PAMS.Core.Utils`)

| Class | Purpose |
|-------|---------|
| `HashHelper` | SHA-256 file hashing for deduplication |
| `PathHelper` | Path manipulation utilities |

## Data Flow

### File Import

```
Source File → Hash Check → Physical Copy/Move → Metadata Save → Index Updates
```

### File Search

```
Query → SearchService → Metadata/TagIndex → Filter → Results
```

## Key Design Decisions

- **JSON Storage**: All data stored as JSON for human readability
- **Hash-based Deduplication**: SHA-256 prevents duplicate files
- **Tag Index**: Separate index for efficient tag-based search
- **Primary Tag**: Each file has one primary tag determining its physical location
