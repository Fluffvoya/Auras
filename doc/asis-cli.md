# asis-cli

Interactive shell for managing ASIS archives. Provides commands for archive creation, file import/export, tagging, search, and archive maintenance.

## Getting Started

### Build & Run

    cd Tools/ASIS/ASIS.CLI
    dotnet build
    dotnet run

### Create an Archive

    create myarchive ./archives

### Open an Archive

    open ./archives/myarchive

## Commands

### Archive Management

| Command | Description |
|---------|-------------|
| `create <name> [path]` | Create new archive. Defaults to current directory. |
| `open <path>` | Open existing archive. Requires `archive.json`. |
| `close` | Close current archive. |
| `archive` | Show archive info (name, file count, orphans). |
| `diff` | Show orphaned metadata and untracked files. |

### File Operations

| Command | Description |
|---------|-------------|
| `import <path> <tag> [tags...] [--desc "..."] [--move]` | Import file. Use `--move` to move instead of copy. |
| `rename <file> <new_name>` | Rename a file. |
| `retag <file> <new_primary_tag>` | Change primary tag. |
| `tag add <file> <t1,t2,...>` | Add tags. |
| `tag remove <file> <t1,t2,...>` | Remove tags. |
| `tag list <file>` | List all tags. |
| `info <file>` | Show full file details. |
| `describe <file> <description>` | Set file description. |
| `delete <file>` | Delete file and metadata. |
| `unlink <file>` | Remove metadata only (keep physical file). |

### Search

| Command | Description |
|---------|-------------|
| `search name <keyword>` | Substring match on filename. |
| `search tag <t1,t2,...>` | Match ALL specified tags. |
| `search time <start> <end>` | Date range (format: `yyyy-MM-dd`). |

### ID Lookup

| Command | Description |
|---------|-------------|
| `id <guid> [--full]` | Look up file by ID. Use `--full` for complete details. |

### Batch Operations

Run an operation on all files matching a search pattern.

| Command | Description |
|---------|-------------|
| `batch rename <file> <new_name>` | Rename all matching files. |
| `batch retag <file> <new_tag>` | Change primary tag on all matching files. |
| `batch describe <file> <desc>` | Set description on all matching files. |
| `batch delete <file>` | Delete all matching files and metadata. |
| `batch unlink <file>` | Remove metadata for all matching files (keep files). |
| `batch tag add <file> <t1,t2,...>` | Add tags to all matching files. |
| `batch tag remove <file> <t1,t2,...>` | Remove tags from all matching files. |

**Confirmation**: Destructive operations (`delete`, `unlink`, `retag`) prompt for confirmation before executing.

**Partial failures**: Each file is processed independently. A summary is shown at the end with success/failure counts.

### System

| Command | Description |
|---------|-------------|
| `help [command]` | Show help for all or specific command. |
| `exit` | Exit CLI. |

## File Resolution

Most commands accept `<file>` as either:

- **Name substring** — Matches first file containing the string
- **`id:<guid>`** — Exact match by file ID

When multiple files match a substring, use `id:<guid>` for disambiguation.

**Batch commands** use the same `<file>` format but return **all** matching files.
If zero files match, the operation is cancelled with a warning.

## Examples

### Basic Workflow

    create projects ./archives
    open ./archives/projects
    import ./docs/readme.md documentation
    import ./src/main.cs code,csharp --desc "Main application entry"
    search tag documentation
    tag add "readme" overview,getting-started
    rename "readme" README.md
    describe "readme" "Project overview and setup instructions"
    info "README.md"
    archive
    diff

### Import Options

    import photo.jpg images              # Copy (default)
    import photo.jpg images --move       # Move
    import report.pdf documents --desc "Q4 2025 financial report"
    import data.csv data,csv,analysis    # Multiple tags

### Search

    search name invoice
    search tag documents,pending
    search time 2025-01-01 2025-12-31

### Batch Operations

    batch retag vacation holiday
    batch delete temp
    batch tag add report quarterly,finance
    batch describe invoice "Q4 2025 invoice"

### Cleanup

    diff                    # Check for problems
    unlink id:<guid>        # Remove orphaned metadata
    delete id:<guid>        # Delete everything
