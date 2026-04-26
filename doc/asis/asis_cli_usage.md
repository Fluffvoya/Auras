# ASIS.CLI - User Manual

**Namespace**: `ASIS.CLI`
**Project**: `Tools/ASIS/ASIS.CLI`

Interactive shell for managing ASIS archives. Provides commands for archive creation, file import/export, tagging, search, and archive maintenance.

---

## Getting Started

### Build & Run

```bash
cd Tools/ASIS/ASIS.CLI
dotnet build
dotnet run
```

### Create an Archive

```bash
create myarchive ./archives
```

### Open an Archive

```bash
open ./archives/myarchive
```

---

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

### System

| Command | Description |
|---------|-------------|
| `help [command]` | Show help for all or specific command. |
| `exit` | Exit CLI. |

---

## File Resolution

Most commands accept `<file>` as either:

- **Name substring** - Matches first file containing the string
- **`id:<guid>`** - Exact match by file ID

When multiple files match a substring, use `id:<guid>` for disambiguation.

---

## Examples

### Basic Workflow

```bash
# Create and open archive
create projects ./archives
open ./archives/projects

# Import files
import ./docs/readme.md documentation
import ./src/main.cs code,csharp --desc "Main application entry"

# Search
search tag documentation
search name readme

# Update tags
tag add "readme" overview,getting-started
tag list "readme"

# Rename and describe
rename "readme" README.md
describe "readme" "Project overview and setup instructions"

# View file info
info "README.md"
id <guid> --full

# Archive maintenance
archive
diff
```

### Import Options

```bash
# Copy file (default)
import photo.jpg images

# Move file instead
import photo.jpg images --move

# With description
import report.pdf documents --desc "Q4 2025 financial report"

# Multiple tags
import data.csv data,csv,analysis
```

### Search Examples

```bash
# Find files by name
search name invoice

# Find files with specific tags
search tag documents,pending

# Find files by date
search time 2025-01-01 2025-12-31
```

### Cleanup

```bash
# Check for problems
diff

# Remove orphaned metadata (file kept on disk)
unlink id:<guid>

# Delete everything
delete id:<guid>
```
