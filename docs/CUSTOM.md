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

## Dude system (v1 + Phase 2–3 + roster tiers + first boss + Trainer's Manual)

Pokémon-style companions under `Scripts/Custom/Dudes/`. Full overview: [`Scripts/Custom/Dudes/README.md`](../Scripts/Custom/Dudes/README.md).

### Phase 1 — Catch / summon
**Loop:** Wild Dude → catch with Dude Ball (100%) → store → summon (classic follower slots) → follow/fight + ability → EXP/level → recall → persists on the ball across restart.

**UOR notes:** Uses `AI_Melee`, `ControlSlots`/`ControlMaster`, elemental bodies 13–16, crystal ball `0xE2E`. No SA pet masteries or imbuing.

**Death policy:** Summoned Dude faints into the ball (state saved); re-summon revives. Prevents soft-lock.

### Phase 2 — Crafting / Mixer / Dust
- **DudeMixer** recycles a filled ball into **DudeDust** (confirm gump); empties the ball.
- **DudeDust** quantity from `DudeDustFormula` (level-based; rarity/type hooks reserved).
- **DefDudeCrafting** + **DudeCraftingKit**: placeholder recipe `5 Iron Ingot + 1 Dude Dust → empty Dude Ball` (100% success; retune freely).

### Phase 3 — Job Station + Earth Gathering
- **DudeJobStation** (container): one DudeBall; asks `DudeJobRegistry` what job the Dude can do.
- Only **Earth Gathering** (any Earth-type Dude: `pebblet` / `stonepaw` / `boulderback`): finds nearest mineable land tile, spawns **DudeJobWorker**, pathfinds (teleport fallback), works, returns, deposits Iron Ore. Roster also includes weak/medium/strong catchable species — see Dude README.
- Job state persists across restart; safety for stuck workers, full container, station/ball delete, etc.

### Elemental farm bosses
Staff-only spawn (`[SpawnEmberlord` / `[SpawnTidewarden` / `[SpawnStonewarden` / `[SpawnGalewarden`). Four `DudeBoss` farm bosses (Fire/Water/Earth/Air): delayed flashy AoE (Ember Burst / Tide Crash / Fault Line / Shear), stage-1-safe damage, not catchable; loot Dude Dust / Iron Ingots / typed essence cores 0–2. No world spawners, no phases, no core recipes yet.

### Trainer's Manual
`TrainersManual` + `DudeInfoGump`: double-click and target wild/summoned Dude, filled Dude Ball (no recall required), or Dude boss for a classic info sheet (stats, ability, status).

**Packet budget (intentional):**
- Ring of Fire flames are spaced ~1.5 tiles apart (67 effects per cast, was ~160); damage still covers every ring tile.
- Burn ticks show one light flame per target, one sound per pulse, and `*Burn*` at most every 10s.
- The floating "+N EXP" and Job Station status text go to the owner only.
- Spring plays heal effects only when it actually heals.
- Ground effects use location-only entities, not temporary `EffectItem`s.

**GM commands:** `[CreateDudeBall`, `[SpawnTestDude`, `[FillDudeBall`, `[CreateDudeMixer`, `[CreateDudeDust`, `[CreateDudeCraftKit`, `[CreateDudeJobStation`, `[StartDudeJob`, `[SpawnEmberlord`/`Tidewarden`/`Stonewarden`/`Galewarden`, `[CreateEmberCore`/`TideCore`/`StoneCore`/`GaleCore`, `[CreateTrainersManual`

**Still out of scope:** Multi-phase, auto world-spawn, elemental core recipes, processing jobs, multiple gathering professions, economy balancing.

## Workflow tips

1. Keep secrets (accounts, email passwords, private IPs you do not want public) out of git — use local-only overrides or untracked files if needed.
2. Put new systems and shard-specific logic under `Scripts/Custom/` rather than editing stock scripts in place.
3. If you must change an upstream script, note it in this file or in a short comment near the change.
4. After script changes, rebuild (`_windebug.bat` / `_makedebug` while developing) and restart the shard.
5. Stick to **C# 7.3**-compatible syntax in custom scripts (ServUO 57.4 / net48).

## Engine changes (performance / networking)

Upstream `Server/` files changed for speed and latency; each change has a comment at the site.

| File | Change |
|------|--------|
| `Server/Network/NetState.cs` | Send-completion race fixed (a full chunk could be left unsent, freezing the client until "Too much data pending" kicked it); disposed send queues cleared under lock; `FlushAll` no longer snapshots every key each loop |
| `Server/Network/SendQueue.cs` | Send chunks 512 B → 8 KB (same bytes in far fewer socket sends / TCP segments); gram release order fixed so a buffer can't be handed to two connections |
| `Server/Network/PacketHandlers.cs` | Relay auth keys never start with `0xEF` (the listener mistook them for a seed packet and dropped ~0.6% of logins as "encrypted") |
| `Server/Main.cs` | Deltas processed after timers/packets so their updates share the same flush; `FileLogger` keeps one handle open (was open/close per line, and per character for `Console.Write`) |
| `Server/Mobile.cs`, `Server/Item.cs` | `Delta` only wakes the core when first queued; `ProcessDelta` skips an allocation for hits/stam/mana-only updates; `PublicOverheadMessage` builds its packet only if someone receives it |
| `Server/Mobile.cs` (healthbars) | When a mobile comes into view (`SetLocation`, `SendEverything`, `SendIncomingPacket`), poison/yellow healthbar packets are sent only when the flag is on (was 2 packets per mobile, almost always "off"); one variant per client type (0x17 classic / 0x16 enhanced) instead of both; no packets pre-built on every step |
| `Server/Network/PacketWriter.cs` | Pool release is O(1) (a flag instead of `Stack.Contains` scanning the whole pool on every packet compile) |
| `Scripts/Misc/ConnectionLog.cs` | One persistent log handle instead of reopening the file per line |

## What not to invent here

Do not strip core ServUO trees. Game systems should come from ServUO or deliberate custom scripts — this scaffolding does not add fictional frameworks beyond documented Custom systems (e.g. Dudes).
