# ASIS.CLI

A Read-Eval-Print Loop (REPL) for archive management via `ASISCoreAPI`.

## Build & Run

```bash
dotnet build Tools/ASIS/ASIS.CLI
dotnet run --project Tools/ASIS/ASIS.CLI
```

```
> create my_archive
[my_archive] > import ./photo.jpg vacation
[my_archive] > exit
```

## File Identifier

All file operations accept a `<file>` argument:

| Format | Example | Behavior |
|--------|---------|----------|
| Substring | `vacation_photo` | Matches first file with name containing substring |
| Exact ID | `id:a1b2c3d4-...` | Matches by exact GUID |

When multiple files match a substring, use `id:<guid>` for exact matching.

## Archive

```
create <name> [path]   # Create archive directory (default path: ./)
open <path>            # Open existing archive
close                  # Close current archive
archive                # Show archive info
```

## File Operations

All file operations require an open archive.

```
import <path> <primary_tag> [tags] [--desc "text"] [--move]
    # Copy file into archive (--move to move instead)
    # Example: import ./photo.jpg vacation summer,beach --desc "Hawaii trip"

rename <file> <new_name>
    # Example: rename vacation_photo vacation_backup.jpg

retag <file> <new_primary_tag>
    # Example: retag vacation_photo travel

tag add <file> <tag1,tag2,...>
    # Example: tag add vacation_photo summer,2024

tag remove <file> <tag1,tag2,...>
    # Example: tag remove vacation_photo summer

tag list <file>
    # Example: tag list vacation_photo
    # Output:
    #   File: vacation_photo.jpg
    #   PrimaryTag: vacation
    #   Tags: [summer, 2024, beach]

info <file>
    # Shows: ID, Name, PrimaryTag, Tags, Description, Hash, Path, Created

describe <file> <description>
    # Use empty string "" to clear description

delete <file>        # Delete physical file and metadata
unlink <file>        # Remove metadata only (keep physical file)
```

## Search

```
search name <keyword>                   # substring match on name
search tag <tag1,tag2,...>              # file must have ALL tags
search time <yyyy-MM-dd> <yyyy-MM-dd>   # date range

id <guid> [--full]                      # lookup by GUID (--full for details)
diff                                     # show orphaned metadata / untracked files
```

Example search output:
```
──────────────────────────────────────────────────────
  Found 2 file(s):
──────────────────────────────────────────────────────
  [a1b2c3d4-...]
    Name:       vacation_photo.jpg
    PrimaryTag: vacation
    Tags:       [summer, beach]
    Desc:       Hawaii trip

  [e5f6a1b2-...]
    Name:       vacation_map.png
    PrimaryTag: travel
    Tags:       [map]
```

## Help & Exit

```
help              # list all commands
help <command>    # help for specific command
exit              # quit CLI
```

Errors are printed in red to stderr.