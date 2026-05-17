# CLAUDE.md — Auras Project

## Before You Start

1. **Read docs first.** Before any task, check `doc/` for relevant documentation. Read the corresponding doc files instead of full source code unless necessary.
2. **English only.** All documentation, comments, and commit messages must be written in English.
3. **Respect `.gitignore`.** Never read files or directories listed in `.gitignore`.
4. **Keep it concise.** Documentation should use concise and refined language. Avoid verbosity.

## Project Overview

**Auras** (Aura System) is a multi-project engineering effort consisting of the **Aura** AI agent and its supporting tools. Aura is an AI agent with persistent memory and the ability to think proactively. She listens to your joys and worries, helps with daily tasks, and accompanies you through work and play.

## Project Structure

```
Auras/
├── Aura/              # Python AI agent (Claude-powered)
├── Error/
│   └── AuraError.NET/ # Shared C# exception & result types
├── Tools/ASIS/        # ASIS archive system (C# .NET)
│   ├── ASIS.CLI/      # CLI frontend
│   └── ASIS.Core/     # Core library
├── Test/ASIS.Test/    # Unit tests for ASIS
├── Tools/Vocal/       # C++ tool (xmake)
└── doc/               # Project documentation
```

## Key Conventions

- **Python (Aura):** managed via `uv`, requires Python >=3.11
- **C++ (Tools/Vocal):** managed via `xmake`, requires C++23
- **C# (Tools/ASIS, Error/AuraError.NET, Test):** .NET 10.0
- **Documentation:** Markdown files under `doc/`, organized by component

## Branch Naming Convention

Branch names should follow the format: `<category>/<short-description>`

### Categories

- `feature` — New features or enhancements
- `bugfix` — Bug fixes
- `refactor` — Code refactoring (improving internal structure without changing external behavior)
- `docs` — Documentation updates
- `chore` — Miscellaneous tasks (dependency updates, CI configuration, cleanup)

### Rules

- Use English, lowercase, and hyphens to separate words
- Keep descriptions concise but meaningful
- Examples: `feature/add-new-feature`, `bugfix/fix-login-error`, `refactor/simplify-auth-logic`
