# Customization guide (Grok-ServUO)

This shard starts from ServUO Publish **57.4.1**. Prefer additive changes so upstream merges stay manageable.

## Recommended locations

| Area | Path | Notes |
|------|------|--------|
| Server name / bind / port | `Config/Server.cfg` | `Name`, `@Listen`, `@Address`, `@Port` |
| Client data path | `Config/DataPath.cfg` | Set `CustomPath` for mul/UOP files (required on non-Windows) |
| Client allowances | `Config/Client.cfg` | Regular / UOTD / God / EC |
| Expansion / era | `Config/Expansion.cfg` | Match your intended client era |
| Other tunables | `Config/*.cfg` | Housing, loot, saves, etc. |
| Custom C# scripts | `Scripts/Custom/` | Add new scripts here; see `Scripts/Custom/README.md` |
| Core scripts (upstream) | `Scripts/` | Prefer not editing unless necessary; document any forks |
| Server engine | `Server/` | Rarely touch; keep changes minimal and documented |
| Spawns / data | `Spawns/`, `Data/`, `RevampedSpawns/` | Content data; customize carefully |

## Workflow tips

1. Keep secrets (accounts, email passwords, private IPs you do not want public) out of git — use local-only overrides or untracked files if needed.
2. Put new systems and shard-specific logic under `Scripts/Custom/` rather than editing stock scripts in place.
3. If you must change an upstream script, note it in this file or in a short comment near the change.
4. After script changes, rebuild (`_windebug.bat` / `_makedebug` while developing) and restart the shard.

## What not to invent here

Do not strip core ServUO trees. Game systems should come from ServUO or deliberate custom scripts — this scaffolding does not add fictional frameworks or invented mechanics.
