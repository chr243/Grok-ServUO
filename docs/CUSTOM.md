# Customization guide (Grok-ServUO)

This shard starts from ServUO Publish **57.4.1**. Prefer additive changes so upstream merges stay manageable.

**Ruleset target:** Ultima Online **Renaissance (UOR)**. Prefer classic pet/follower control, Str/Dex/Int/Hits combat stats, and pre-AOS-friendly Body/ItemIDs in custom content. Stock `Config/Expansion.cfg` may still say `EJ` until you retarget — Dude scripts do not require that change to compile or run.

## Recommended locations

| Area | Path | Notes |
|------|------|--------|
| Server name / bind / port | `Config/Server.cfg` | `Name`, `@Listen`, `@Address`, `@Port` |
| Client data path | `Config/DataPath.cfg` | Set `CustomPath` for mul/UOP files (required on non-Windows) |
| Client allowances | `Config/Client.cfg` | Regular / UOTD / God / EC |
| Expansion / era | `Config/Expansion.cfg` | For UOR play, set `CurrentExpansion=UOR` when ready |
| Other tunables | `Config/*.cfg` | Housing, loot, saves, etc. |
| Custom C# scripts | `Scripts/Custom/` | Add new scripts here; see `Scripts/Custom/README.md` |
| Core scripts (upstream) | `Scripts/` | Prefer not editing unless necessary; document any forks |
| Server engine | `Server/` | Rarely touch; keep changes minimal and documented |
| Spawns / data | `Spawns/`, `Data/`, `RevampedSpawns/` | Content data; customize carefully |

## Dude system (v1)

Pokémon-style companions under `Scripts/Custom/Dudes/`. Full overview and file map: [`Scripts/Custom/Dudes/README.md`](../Scripts/Custom/Dudes/README.md).

**Loop:** Wild Dude → catch with Dude Ball (100%) → store → summon (classic follower slots) → follow/fight + ability → EXP/level → recall → persists on the ball across restart.

**UOR notes:** Uses `AI_Melee`, `ControlSlots`/`ControlMaster`, elemental bodies 13–16, crystal ball `0xE2E`. No SA pet masteries or imbuing.

**GM test commands:** `[CreateDudeBall`, `[SpawnTestDude`, `[SpawnAllTestDudes`, `[FillDudeBall`

**Death policy:** Summoned Dude faints into the ball (state saved); re-summon revives. Prevents soft-lock.

**Out of scope (v1):** Evolution, jobs/stations, DudeMixer, DudeDust, bosses, complex capture formulas.

## Workflow tips

1. Keep secrets (accounts, email passwords, private IPs you do not want public) out of git — use local-only overrides or untracked files if needed.
2. Put new systems and shard-specific logic under `Scripts/Custom/` rather than editing stock scripts in place.
3. If you must change an upstream script, note it in this file or in a short comment near the change.
4. After script changes, rebuild (`_windebug.bat` / `_makedebug` while developing) and restart the shard.
5. Stick to **C# 7.3**-compatible syntax in custom scripts (ServUO 57.4 / net48).

## What not to invent here

Do not strip core ServUO trees. Game systems should come from ServUO or deliberate custom scripts — this scaffolding does not add fictional frameworks beyond documented Custom systems (e.g. Dudes).
