# Documentation Structure

```
doc/
├── auras_structure.md      # Overall project layout and key directories
├── doc_structure.md         # This file — document index
│
├── aura/
│   └── aura_usage.md        # Aura agent setup, configuration, and usage
│
├── asis/
│   ├── asis_structure.md    # ASIS.Core project structure and components
│   ├── asis_api.md          # ASISCoreAPI public API reference
│   └── asis_cli_usage.md    # CLI commands and examples
│
└── error/
    └── error_api.md         # AuraError exception types and Result<T>
```

## Naming Convention

- Component docs live in their own subdirectory: `doc/<component>/`
- Files follow the pattern: `<component>_<topic>.md`

## Navigation

| Topic | File |
|-------|------|
| Project overview | `auras_structure.md` |
| Aura agent usage | `aura/aura_usage.md` |
| ASIS architecture | `asis/asis_structure.md` |
| ASIS public API | `asis/asis_api.md` |
| ASIS CLI reference | `asis/asis_cli_usage.md` |
| Error handling API | `error/error_api.md` |
