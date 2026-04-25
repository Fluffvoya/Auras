# ASIS.CLI - Usage Guide

**Assembly**: `asis`
**Entry Point**: `Tools/ASIS/ASIS.CLI/Program.cs`
**Framework**: .NET 10.0

ASIS.CLI is an interactive command-line shell for archive management. It wraps `ASISCoreAPI` with a Read-Eval-Print Loop (REPL).

---

## Getting Started

### Build

```bash
cd Tools/ASIS/ASIS.CLI
dotnet build
```

### Run

```bash
dotnet run
```

The CLI starts a prompt loop:

```
> _
[archive_name] > _
```

Commands are entered interactively. Use `exit` to quit.

---

## Archive Management

### create

Create a new archive directory with `archive.json`.

```
create <archive_name> [path]
```

- `archive_name` — name of the archive
- `path` — optional root path (defaults to `./`)

```
create my_archive
create my_archive /path/to/parent
```

### open

Open an existing archive for subsequent commands.

```
open <path>
```

```
open ./my_archive
open /absolute/path/to/archive
```

### close

Close the currently open archive.

```
close
```

---

## File Operations

All file operations require an open archive.

### import

Import a file into the archive (copied by default).

```
import <source_path> <primary_tag> [tag1,tag2] [--desc "description"] [--move]
```

- `source_path` — path to the source file
- `primary_tag` — required primary tag
- `tag1,tag2` — optional additional tags (comma-separated)
- `--desc` — optional description text
- `--move` — move the file instead of copying

```
import ./photo.jpg vacation
import ./doc.pdf work --desc "Q1 report"
import ./data.zip archive --move
```

### add

Shortcut for `import --move` (always moves the file).

```
add <source_path> <primary_tag> [tags]
```

```
add ./file.zip archived
```

### rename

Rename a file in the archive.

```
rename <file_keyword> <new_name>
```

- `file_keyword` — substring match against existing file name

```
rename photo vacation_backup.jpg
```

### retag

Change the primary tag of a file.

```
retag <file_keyword> <new_primary_tag>
```

```
retag photo_vacation new_category
```

### addtag

Add tags to a file.

```
addtag <file_keyword> <tag1,tag2>
```

```
addtag photo_vacation summer,2024
```

### rmtag

Remove tags from a file.

```
rmtag <file_keyword> <tag1,tag2>
```

```
rmtag photo_vacation summer
```

### delete

Delete both the physical file and its metadata.

```
delete <file_keyword>
```

```
delete old_file
```

### unlink

Remove metadata only (unlink the file from the archive without deleting the physical file).

```
unlink <file_keyword>
```

```
unlink duplicated_file
```

---

## Search

### search

Search for files by name, tags, or time range.

```
search --name <keyword>
search --tag <tag1,tag2>
search --time <start> <end>
```

- `--name` — substring match on file name
- `--tag` — intersection match (file must have ALL tags)
- `--time` — date range (format: `yyyy-MM-dd`)

```
search --name photo
search --tag vacation,summer
search --time 2025-01-01 2025-12-31
```

Output format:

```
 - ID: <guid> | Name: <name> | PrimaryTag: <tag> | Tags: [<tags>] | Description: <desc>
```

---

## Maintenance

### diff

Check for orphaned metadata and untracked files in the archive.

```
diff
```

- **Orphaned metadata** — records in `metadata.json` without a corresponding physical file
- **Untracked files** — physical files not recorded in `metadata.json`

```
diff
```

---

## Error Handling

Errors are printed to stderr:

| Message | Meaning |
|---------|---------|
| `Unknown command: <cmd>` | Unrecognized command |
| `No archive open. Use 'open <path>' first.` | No archive loaded |
| `Import failed: <reason>` | File import error |
| `Search failed: <reason>` | Search operation error |
| `Diff failed: <reason>` | Diff operation error |

---

## Command Summary

| Command | Description |
|---------|-------------|
| `create <name> [path]` | Create new archive |
| `open <path>` | Open existing archive |
| `close` | Close current archive |
| `import <path> <tag> [tags] [--move]` | Import file (copy) |
| `add <path> <tag> [tags]` | Import file (move) |
| `rename <keyword> <name>` | Rename file |
| `retag <keyword> <tag>` | Change primary tag |
| `addtag <keyword> <tags>` | Add tags |
| `rmtag <keyword> <tags>` | Remove tags |
| `delete <keyword>` | Delete file and metadata |
| `unlink <keyword>` | Remove metadata only |
| `search --name <kw>` | Search by name |
| `search --tag <tags>` | Search by tags |
| `search --time <start> <end>` | Search by date range |
| `diff` | Check archive consistency |
| `exit` | Exit CLI |
