# Auras — Project Structure

## Overview

Auras is a multi-project engineering effort. It includes the **Aura** AI agent and the **ASIS** toolchain that powers its file-management capabilities.

## Solution Layout

```
Auras/
├── Aura/                  # Python AI agent
│   ├── src/               # Agent source code
│   └── pyproject.toml     # uv-managed Python project
│
├── Error/
│   └── AuraError.NET/     # Shared C# error library
│       ├── Exceptions/    # Domain exception types (DuplicateFile, Validation, etc.)
│       └── Results/       # Result<T> monad
│
├── Tools/
│   └── ASIS/              # Archive System for Indexed Storage
│       ├── ASIS.CLI/      # CLI frontend (Commands, Models, Repositories, Services)
│       └── ASIS.Core/     # Core library (API, Models, Repositories, Services, Storage, Utils)
│
├── Test/
│   └── ASIS.Test/         # xUnit tests for ASIS.Core and ASIS.CLI
│
├── doc/                   # Project documentation
├── .clang-format          # C++ code style
├── xmake.lua              # C++ build configuration
├── Auras.sln              # .NET solution file
├── nuget.config           # NuGet package source config
└── CLAUDE.md              # Agent workspace rules
```

## Key Directories

| Directory | Language | Purpose |
|-----------|----------|---------|
| `Aura/` | Python 3.11+ | AI agent powered by Claude. Manages archives through natural-language conversation. |
| `Error/AuraError.NET/` | C# .NET 10 | Shared error handling: typed exceptions, `Result<T>` monad. Referenced by both ASIS projects. |
| `Tools/ASIS/ASIS.CLI/` | C# .NET 10 | Command-line interface for ASIS: import, search, tag, delete files. |
| `Tools/ASIS/ASIS.Core/` | C# .NET 10 | Core archive engine: JSON-backed file records, hash/tag indexing, search. |
| `Test/ASIS.Test/` | C# (xUnit) | Unit tests for ASIS.Core and integration tests for ASIS.CLI. |

## Dependencies

- **ASIS.Core** → depends on `AuraError.NET`
- **ASIS.CLI** → depends on `ASIS.Core` and `AuraError.NET`
- **Aura** (Python) → calls `ASIS.CLI` as a subprocess
