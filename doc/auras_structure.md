# Auras — System Architecture

## Overview

**Auras** is a file archiving and metadata management system with two components:

- **ASIS** (Archive Storage & Information System): .NET 10.0 CLI for creating tagged file archives with SHA-256 deduplication, metadata indexing, and JSON-backed persistence.
- **Aura**: Python (>=3.11) AI agent wrapping the ASIS CLI behind a natural-language interface via OpenAI-compatible LLM function calling.

```
User ──→ Aura (Python AI agent) ──→ ASIS.CLI (subprocess) ──→ ASIS.Core ──→ Archive files
          │                         │
          └─ OpenAI-compatible ─────┘
            function-calling loop
```

## Solution Structure

```
Auras
├── Auras.sln
├── AuraError/                              # Error handling library
│   ├── AuraError.csproj
│   ├── GlobalUsings.cs
│   ├── Exceptions/
│   │   ├── AuraException.cs                # Abstract base (Code + Message)
│   │   ├── DuplicateFileException.cs
│   │   ├── FileNameConflictException.cs
│   │   ├── FileRecordNotFoundException.cs
│   │   ├── InvalidTagOperationException.cs
│   │   ├── PhysicalFileNotFoundException.cs
│   │   └── ValidationException.cs
│   └── Results/
│       └── Result.cs                       # Result / Result<T> pattern
├── Tools/ASIS/
│   ├── ASIS.Core/                          # Core library
│   │   ├── ASIS.Core.csproj
│   │   └── src/
│   │       ├── ASISCoreAPI.cs              # Public facade composing all services
│   │       ├── Models/                     # FileRecord, ArchiveConfig, Tag
│   │       ├── Repositories/               # One repo per JSON file (metadata, config, tags, hashes)
│   │       ├── Services/                   # FileService, SearchService, ArchiveService, ArchiveConfigService
│   │       ├── Storage/
│   │       │   └── JsonStorage.cs          # Generic JSON persistence helper
│   │       └── Utils/                      # HashHelper (SHA-256), PathHelper
│   └── ASIS.CLI/                           # CLI application
│       ├── ASIS.CLI.csproj
│       └── src/
│           ├── Program.cs                  # REPL loop + all command handlers
│           ├── ConsoleWriter.cs            # ANSI color output helpers
│           ├── Models/Config.cs            # CLI config model (UserName)
│           ├── Repositories/ConfigRepository.cs
│           └── Services/ConfigService.cs
├── Test/ASIS.Test/                         # xUnit tests (10 test files)
├── Aura/                                   # Python AI agent
│   ├── pyproject.toml                      # hatchling build, deps: openai, pydantic
│   ├── .env.example
│   ├── .python-version
│   └── src/
│       ├── __init__.py                     # __version__ = "0.1.0"
│       ├── __main__.py                     # python -m aura entry point
│       ├── cli.py                          # CLI: config management + interactive chat loop
│       ├── config.py                       # pydantic v2 models (LLMConfig, AppConfig)
│       ├── agent.py                        # AuraAgent: OpenAI function-calling loop
│       └── asis_client.py                  # ASISClient: subprocess wrapper over stdin/stdout
└── doc/
    ├── auras_structure.md                  # This file
    ├── asis/
    ├── aura/
    └── error/
```

## Dependency Chain

```
AuraError ──→ ASIS.Core ──→ ASIS.CLI
                              ↑
              Aura (Python) ──┘  (spawns ASIS.CLI as subprocess)
```

C# projects have a linear dependency: `AuraError` → `ASIS.Core` → `ASIS.CLI`. The Python `Aura` package is independent — it spawns the compiled `asis.exe` over stdin/stdout.

---

## Projects

### AuraError (.NET)

Error handling library referenced by all C# projects. Provides:

- **`AuraException`**: Abstract base exception with `Code` (string error code) and `Message`.
- **6 concrete exception types**: `DuplicateFileException`, `FileNameConflictException`, `FileRecordNotFoundException`, `InvalidTagOperationException`, `PhysicalFileNotFoundException`, `ValidationException`.
- **`Result` / `Result<T>`**: Functional success/failure union with `IsSuccess`, `IsFailure`, `Error`, `ErrorCode`. Supports implicit conversion from value types.

### ASIS.Core (.NET)

Core library containing all archive management logic. No ORM, no DI container — manual instantiation throughout.

| Component | Role |
|-----------|------|
| `ASISCoreAPI` | Single public facade. Composes 4 services with manual DI in the constructor. |
| `Models` | `FileRecord` (Id, Hash, Name, RelativePath, Description, PrimaryTag, Tags, CreatedTime), `ArchiveConfig` (Name, CreatedTime), `Tag` |
| `Repositories` | One per JSON file: `MetadataRepository`, `ArchiveConfigRepository`, `HashIndexRepository`, `TagIndexRepository`. Each loads on construction, persists on mutation. |
| `Services` | `FileService` (import, rename, retag, tag, describe, delete, unlink), `SearchService` (by name, tag intersection, time range), `ArchiveService` (diff), `ArchiveConfigService` (name accessor) |
| `JsonStorage` | Static helper: `Load<T>` / `Save<T>` with `System.Text.Json` |
| `Utils` | `HashHelper.ComputeSHA256(path)`, `PathHelper` (relative/absolute path helpers) |

**ASISCoreAPI public surface:** `ImportFile`, `ChangeFileName`, `ChangeDescription`, `ChangePrimaryTag`, `AddTags`, `RemoveTags`, `DeleteFile`, `DeleteMetadataOnly`, `SearchByName`, `SearchByTags`, `SearchByTime`, `Diff`, `ArchiveName`. Each file-mutating method has an overload accepting a Guid or a name string.

### ASIS.CLI (.NET)

Interactive REPL console application (~730 lines in `Program.cs`). Parses commands, calls `ASISCoreAPI`, formats output via `ConsoleWriter`.

**Commands:** `create`, `open`, `close`, `archive`, `diff`, `import`, `rename`, `retag`, `tag` (add/remove/list), `info`, `describe`, `delete`, `unlink`, `search` (name/tag/time), `id`, `help`, `exit`.

**CLI internal structure:** Own `Models/Config`, `Repositories/ConfigRepository`, `Services/ConfigService` for user config (currently limited to `UserName`). `ConsoleWriter` provides ANSI-colored output methods (`Ok`, `Err`, `Warn`, `Info`, `Title`, `Label`, `Dimmed`).

### ASIS.Test (.NET)

xUnit tests for ASIS.Core. Convention: `MethodName_Scenario_ExpectedBehavior`, cleanup via `IDisposable`. Covers services, repositories, storage, and utilities.

### Aura (Python)

AI agent wrapping ASIS.CLI behind a natural-language interface.

| Module | Class | Role |
|--------|-------|------|
| `config.py` | `AppConfig`, `LLMConfig` | pydantic v2 models. Config persisted to `~/.aura/config.json`. |
| `asis_client.py` | `ASISClient` | Spawns `asis.exe` as subprocess, reads stdout on a daemon thread, provides `send(command)` → output with stabilization detection and ANSI stripping. |
| `agent.py` | `AuraAgent` | OpenAI function-calling loop. Defines one tool: `run_asis_command`. System prompt describes capabilities and includes full ASIS docs. |
| `cli.py` | `main()` | Entry point: `aura config [show|set]` for config management, or interactive chat loop with `AuraAgent`. |

**Flow:** User input → LLM API call with tool definition → LLM may request `run_asis_command` → agent forwards command to ASISClient subprocess → output returned to LLM → final text response displayed.

**Configuration (`~/.aura/config.json`):**

| Key | Description | Default |
|-----|-------------|---------|
| `llm.api_key` | API key | (required) |
| `llm.base_url` | API base URL | `https://api.openai.com/v1` |
| `llm.model` | Model name | `gpt-4o` |
| `asis_path` | Path to `asis.exe` | `../Tools/ASIS/ASIS.CLI/bin/Release/net10.0/asis.exe` |
| `doc_path` | Path to ASIS docs | `../doc/asis/asis_cli_usage.md` |

## Archive On-Disk Layout

```
<archive-root>/
├── archive.json          # ArchiveConfig { Name, CreatedTime }
├── metadata.json         # List<FileRecord>
├── tag_index.json        # Dict<string, HashSet<Guid>>  (tag → file IDs)
├── hash_index.json       # Dict<string, Guid>            (hash → file ID)
└── <primary-tag>/        # Files organized by primary tag directory
    └── <filename>
```
