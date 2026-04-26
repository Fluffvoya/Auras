# CLAUDE.md

## Project Overview

Auras is a file archiving and metadata management system with two components:

- **ASIS** (Archive Storage & Information System): .NET 10.0 CLI for creating tagged file archives with SHA-256 deduplication, metadata indexing, and JSON-backed persistence.
- **Aura**: Python (>=3.11) AI agent wrapping the ASIS CLI behind a natural-language interface via OpenAI-compatible LLM function calling.

## Before Writing Code

**Always read the relevant docs first.** The `doc/` directory contains architecture overviews, API references, and usage guides. Consult these before searching or reading source code — they provide the canonical description of how each subsystem works:

- `doc/auras_structure.md` — overall system architecture (read this first for any cross-cutting change)
- `doc/asis/asis_structure.md` — ASIS.Core internals
- `doc/asis/asis_api.md` — ASISCoreAPI reference
- `doc/asis/asis_cli_usage.md` — CLI command behavior
- `doc/aura/aura_usage.md` — Aura AI agent behavior
- `doc/error/error_api.md` — error types and Result pattern

## Build / Run / Test Commands

```bash
dotnet build Auras.sln                          # Build all C# projects
dotnet run --project Tools/ASIS/ASIS.CLI         # Run ASIS CLI (REPL)
dotnet test Auras.sln                            # Run all xUnit tests
```

```bash
cd Aura && uv sync && uv run aura                # Install deps and run Aura AI agent
```

## Solution Structure

```
Auras.sln
├── AuraError/                    # Error library (AuraException base, Result<T> pattern)
├── Tools/ASIS/ASIS.Core/        # Core library (ASISCoreAPI facade, models, repos, services)
├── Tools/ASIS/ASIS.CLI/         # CLI REPL (~730-line Program.cs)
├── Test/ASIS.Test/              # xUnit tests
├── Aura/                         # Python AI agent (subprocess wrapper around ASIS.CLI)
└── doc/                          # Architecture docs, API references, usage guides
```

**Dependency chain**: `AuraError` → `ASIS.Core` → `ASIS.CLI`. The Python `Aura` package is independent — it spawns the compiled `asis.exe` as a subprocess.

## Architecture Patterns

**C# side:**
- **Facade**: `ASISCoreAPI` is the single public entry point, composing all services.
- **Repository pattern**: Each JSON metadata file has a dedicated repository class.
- **Result pattern**: `AuraError` provides `Result` / `Result<T>` for functional error handling (though services currently lean toward exceptions).
- **JSON persistence**: Manual `System.Text.Json` serialization in `JsonStorage<T>`, no ORM, no DI container.

**Archive on-disk layout:**
```
<archive-root>/
├── archive.json          # ArchiveConfig
├── metadata.json         # List<FileRecord>
├── tag_index.json        # Dict<string, HashSet<Guid>> (tag → file IDs)
├── hash_index.json       # Dict<string, Guid> (hash → file ID)
└── <primary-tag>/        # Actual files organized by primary tag directory
```

**Python side:**
- pydantic v2 config models (`LLMConfig`, `AppConfig`)
- Subprocess-based integration with ASIS CLI (stdin/stdout)
- OpenAI function-calling loop in `agent.py`

## C# Conventions

- **.NET 10.0** with nullable reference types enabled, implicit usings, file-scoped namespaces
- **Naming**: `PascalCase` public, `_camelCase` private fields
- **Tests**: xUnit `[Fact]`, naming convention `MethodName_Scenario_ExpectedBehavior`, cleanup via `IDisposable`
- **Git**: PR-based workflow into `main`, branches prefixed `feature/` / `fix/` / `doc/` / `test/`, imperative commit messages

## New Feature Workflow

When adding a new feature, follow these steps in order:

1. **Write the code** — implement the feature in the appropriate project (ASIS.Core for library logic, ASIS.CLI for command exposure, AuraError for error types). Follow the architecture patterns described above.
2. **Write unit tests** — add xUnit `[Fact]` tests in `Test/ASIS.Test/` covering the happy path, edge cases, and error conditions. Follow the `MethodName_Scenario_ExpectedBehavior` naming convention.
3. **Update documentation** — if the feature introduces new public API surface, CLI commands, or user-facing behavior, update the relevant doc files in `doc/`. At minimum, update the corresponding API or usage doc.

## Documentation

| File | Topic |
|------|-------|
| `doc/auras_structure.md` | Overall system architecture |
| `doc/asis/asis_structure.md` | ASIS.Core project structure |
| `doc/asis/asis_api.md` | ASISCoreAPI reference |
| `doc/asis/asis_cli_usage.md` | ASIS CLI user manual |
| `doc/aura/aura_usage.md` | Aura AI agent usage |
| `doc/error/error_api.md` | AuraError library API |
