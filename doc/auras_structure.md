# Auras — Project Structure

## Overview

Auras is a multi-project engineering effort. It includes the **Aura** AI agent and the **ASIS** toolchain that powers its file-management capabilities.

## Solution Layout

```
Auras/
├── Aura/                  # Python AI agent
│   ├── src/
│   │   ├── agent.py       # Agent loop: conversation, thinking, tool calls
│   │   ├── asis_client.py # ASIS CLI client wrapper
│   │   ├── cli.py         # Entry point (argparse)
│   │   └── config.py      # Env-based configuration
│   ├── .env.example       # API key & path template
│   └── pyproject.toml     # uv-managed Python project
│
├── AuraError/             # Shared C# error library
│   ├── Exceptions/        # Domain exception types (DuplicateFile, Validation, etc.)
│   └── Results/           # Result<T> monad
│
├── Tools/
│   └── ASIS/              # Archive System for Indexed Storage
│       ├── ASIS.CLI/      # CLI frontend (Program.cs, ConsoleWriter, etc.)
│       └── ASIS.Core/     # Core library (API, Models, Repositories, Services, Storage, Utils)
│
├── Test/
│   └── ASIS.Test/         # xUnit tests for ASIS.Core
│
├── doc/                   # Project documentation
├── Auras.sln              # .NET solution file
└── CLAUDE.md              # Agent workspace rules
```

## Key Directories

| Directory | Language | Purpose |
|-----------|----------|---------|
| `Aura/` | Python 3.11+ | AI agent powered by Claude. Manages archives through natural-language conversation. |
| `AuraError/` | C# .NET 10 | Shared error handling: typed exceptions, `Result<T>` monad. Referenced by both ASIS projects. |
| `Tools/ASIS/ASIS.CLI/` | C# .NET 10 | Command-line interface for ASIS: import, search, tag, delete files. |
| `Tools/ASIS/ASIS.Core/` | C# .NET 10 | Core archive engine: JSON-backed file records, hash/tag indexing, search. |
| `Test/ASIS.Test/` | C# (xUnit) | Unit tests for ASIS.Core repositories, services, and utilities. |

## Dependencies

- **ASIS.Core** → depends on `AuraError`
- **ASIS.CLI** → depends on `ASIS.Core` and `AuraError`
- **Aura** (Python) → calls `ASIS.CLI` as a subprocess
