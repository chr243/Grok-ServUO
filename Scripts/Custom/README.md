# Scripts/Custom

Place shard-specific C# scripts in this folder (or subfolders).

ServUO compiles scripts under `Scripts/`. Keep custom code here so it stays separate from upstream Publish 57.4.1 sources.

## Active systems

| System | Path | Notes |
|--------|------|--------|
| **Dudes** | `Dudes/` | Catch/summon + Mixer/Dust + Job Station + first boss (Emberlord). See `Dudes/README.md`. |

## Guidelines

- Custom commands, items, mobiles, or gumps belong here
- Prefer wrapping stock systems over editing them in place
- C# 7.3 only (no nullable refs, switch expressions, `using` declarations, records, etc.)
- Rebuild the server after changes
