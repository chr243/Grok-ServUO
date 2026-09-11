# Dude System (v1 + Phase 2–3 + first boss + Trainer's Manual)

Pokémon-style companion loop for Grok-ServUO, designed for **UOR (Ultima Online Renaissance)** rules:

- Classic `BaseCreature` Str/Dex/Int/Hits/VirtualArmor
- Classic pet control (`Controlled` / `ControlMaster` / `ControlSlots` / `OrderType.Follow`)
- Classic elemental Body IDs (13–16) and crystal-ball ItemID (`0xE2E`)
- No dependency on SA masteries, imbuing, or post-UOR pet features

Persistent state lives on **DudeBall**. The world creature is a temporary projection.

## Loops

### Combat / catch (v1)
Wild Dude → catch with Dude Ball (100%) → store → summon → fight + ability → EXP/level → recall.

### Craft / Mixer / Dust (Phase 2)
Filled Dude Ball → **Dude Mixer** (confirm) → **Dude Dust** + empty ball → **Dude Crafting Kit** recipe (`Iron Ingot` + `Dude Dust` → empty Dude Ball).

### Job Station / Gathering (Phase 3)
Filled Earth Dude Ball → drop on **Dude Job Station** → Start Job → worker travels to nearest mineable tile → works → returns → deposits **Iron Ore** into station container → ball remains.

### Boss (Emberlord)
Staff-spawned **Emberlord** (`DudeBoss`) — hostile Fire boss, not catchable, no Dude Ball on death. Fight → Ember Burst AoE → corpse loot (Dude Dust, Iron Ingots, rare Ember Core). No world spawner.

### Trainer's Manual
**Trainer's Manual** item → double-click → target a wild/summoned `DudeCreature`, filled `DudeBall`, or `DudeBoss` → opens **DudeInfoGump** (name, type, level, EXP, stats, ability, status, owner). Ball data works without recalling. Bosses show a scout sheet (uncatchable).

## File map

| Path | Role |
|------|------|
| `DudeType.cs` / `DudeDefinition.cs` / `DudeRegistry.cs` / `DudeData.cs` | Species + persistent stats |
| `Abilities/*` | Combat abilities |
| `Items/DudeBall.cs` | Catch / store / summon / recall |
| `Items/DudeDust.cs` | Recycled dust (stackable) |
| `Items/DudeMixer.cs` | Dude → Dust converter (confirm gump) |
| `Items/DudeCraftingKit.cs` + `Craft/DefDudeCrafting.cs` | Empty ball crafting |
| `Items/DudeJobStation.cs` (+ Gump) | Generic job station + container |
| `Jobs/*` | Abstract jobs, registry, Earth gathering |
| `Mobiles/DudeCreature.cs` | Wild / summoned combat Dude |
| `Mobiles/DudeJobWorker.cs` | Temporary job worker |
| `Bosses/DudeBoss.cs` | Abstract uncatchable boss (`DudeBoss` → subclass) |
| `Bosses/Emberlord.cs` | First Fire boss + Ember Burst AoE |
| `Items/EmberCore.cs` | Rare Emberlord drop (future crafting) |
| `Items/TrainersManual.cs` | Inspect tool (target Dude / ball / boss) |
| `Items/DudeInfoGump.cs` | Read-only info sheet (+ `DudeInfoView`) |
| `Systems/*` | Capture, EXP, kill handler, dust formula |
| `Commands/DudeTestCommands.cs` | GM helpers |

## Architecture notes

- **Dust formula** (`DudeDustFormula`): level-based quantity; rarity/type multipliers reserved.
- **Jobs**: station asks `DudeJobRegistry.GetJobForDude(data)` — does not hard-code professions. Only **Earth Gathering** is registered.
- **Travel**: real `PathFollower` movement; stuck / timeout → teleport fallback; station never permanently blocked.
- **Persistence**: station serializes ball, job id, stage, times, destination, worker; on load recovers stage from elapsed time.
- **Safety**: rejects empty/wrong balls, multi-ball, no-job, no-resource; blocks ball lift mid-job; ejects ball/resources on station delete; deposits beside station if full.
- **Bosses**: `DudeBoss` is a `BaseCreature`, not a companion. `DudeCapture.GetCaptureBlockReason` / `DudeCreature.CanBeCaught` reject bosses. Ember Burst is boss-local AoE (companion Ember Burst stays single-target). Add a new boss by subclassing `DudeBoss` (stats/ability/unique loot).

## GM test commands

| Command | Purpose |
|---------|---------|
| `[CreateDudeBall` | Empty ball |
| `[SpawnTestDude stonepaw` | Wild Earth Dude |
| `[FillDudeBall stonepaw` | Skip catch |
| `[CreateDudeMixer` | Mixer |
| `[CreateDudeDust 5` | Dust stack |
| `[CreateDudeCraftKit` | Craft kit + ingots |
| `[CreateDudeJobStation` | Station at feet |
| `[StartDudeJob` | Target station to start |
| `[SpawnEmberlord` | Hostile Emberlord at your feet |
| `[CreateEmberCore` | Rare boss item |
| `[CreateTrainersManual` | Trainer's Manual (info gump) |

## In-game test steps

### Phase 2 loop
1. `[CreateDudeMixer`, `[CreateDudeBall`, `[FillDudeBall emberling`
2. Double-click Mixer → target filled ball → OK confirm → receive Dude Dust; ball empties.
3. `[CreateDudeCraftKit` (gives kit + iron). Ensure dust in pack.
4. Double-click kit → craft **Dude Ball** (Iron Ingot ×5 + Dude Dust ×1).

### Phase 3 loop
1. Place station near **mountains/caves** (mineable land tiles): `[CreateDudeJobStation`
2. `[CreateDudeBall` + `[FillDudeBall stonepaw` (Earth only for now).
3. Drag filled ball onto station → assigned.
4. Double-click station → **Start Job**.
5. Watch worker path to ore tile, work, return; open storage for Iron Ore.
6. After job: **Retrieve Dude Ball** from gump. Restart shard mid-job to verify recovery.

### Boss loop
1. `[SpawnEmberlord` (or `[add Emberlord`). It is hostile (`FightMode.Closest`).
2. Fight: melee + **Ember Burst** AoE fire (~12s cooldown, nearby players/pets).
3. `[CreateDudeBall` → double-click → target Emberlord: capture must fail ("cannot be caught in a Dude Ball").
4. Kill it: corpse has Dude Dust, Iron Ingots, 25% Ember Core. No Dude Ball drop.
5. `[CreateEmberCore` to inspect the item without fighting. Restart shard with the item in pack to verify serialize.

### Trainer's Manual loop
1. `[CreateTrainersManual` (or `[add TrainersManual`).
2. `[SpawnTestDude stonepaw` → double-click manual → target wild Dude → info gump (Wild).
3. `[CreateDudeBall` + `[FillDudeBall emberling` → target the filled ball (no need to summon) → Captured sheet with EXP/owner.
4. Summon from ball → target the pet → Summoned sheet with live HP.
5. `[SpawnEmberlord` → target boss → Boss sheet (uncatchable + Ember Burst). Empty balls / non-Dudes are rejected with messages.

## Out of scope (still)
Additional bosses, multi-phase fights, automatic world spawn, Ember Core recipes, resource-processing jobs, multiple gathering professions, economy balancing, evolution, rarity dust variants.
