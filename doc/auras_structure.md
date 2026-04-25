# Auras - Program Structure Documentation

## Overview

**Auras** (Aura System) is a file archiving and metadata management system built with .NET 10.0. It provides a CLI application for organizing files into tagged archives, tracking file metadata, and searching through archived files. The system uses SHA-256 hashing to identify duplicate files and maintains metadata indices for fast searching.

## Solution Structure

```
D:\program\Auras\
├── Auras.sln                          # Solution file
├── AuraError/                         # Error handling library
│   ├── AuraError.csproj
│   ├── GlobalUsings.cs
│   ├── Exceptions/                    # Exception types
│   └── Results/                       # Result types
├── Tools/ASIS/
│   ├── ASIS.Core/                     # Core library
│   │   ├── ASIS.Core.csproj
│   └── src/
│       ├── ASISCoreAPI.cs             # Main public API
│       ├── Models/                    # Data models
│       ├── Repositories/              # Data access
│       ├── Services/                  # Business logic
│       ├── Storage/                   # JSON persistence
│       └── Utils/                     # Helpers
│   └── ASIS.CLI/                      # CLI application
│       ├── ASIS.CLI.csproj
│       ├── Program.cs
│       └── src/                       # CLI-specific code
└── Test/ASIS.Test/                    # Unit tests
```

## Projects

### AuraError
Error handling library providing exception types and result types for the entire solution. All exceptions inherit from `AuraException` with specific error codes.

### Tools/ASIS/ASIS.Core
Core library containing the main public API (`ASISCoreAPI`), repositories for JSON-based persistence, services for business logic, and models for data representation.

### Tools/ASIS/ASIS.CLI
Console application providing an interactive shell for archive management.

### Test/ASIS.Test
Unit tests for the ASIS.Core library.

## Key Features

- **File Import**: Import files with SHA-256 hash deduplication
- **Tagging System**: Primary tag and multiple secondary tags per file
- **Search**: Search by name, tags (intersection), or time range
- **Archive Diff**: Find orphaned metadata or untracked disk files
- **JSON Persistence**: Lightweight JSON-based storage for all metadata