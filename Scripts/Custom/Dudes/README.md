# Dude System (v1 + Phase 2–3)

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
| `Systems/*` | Capture, EXP, kill handler, dust formula |
| `Commands/DudeTestCommands.cs` | GM helpers |

## Architecture notes

- **Dust formula** (`DudeDustFormula`): level-based quantity; rarity/type multipliers reserved.
- **Jobs**: station asks `DudeJobRegistry.GetJobForDude(data)` — does not hard-code professions. Only **Earth Gathering** is registered.
- **Travel**: real `PathFollower` movement; stuck / timeout → teleport fallback; station never permanently blocked.
- **Persistence**: station serializes ball, job id, stage, times, destination, worker; on load recovers stage from elapsed time.
- **Safety**: rejects empty/wrong balls, multi-ball, no-job, no-resource; blocks ball lift mid-job; ejects ball/resources on station delete; deposits beside station if full.

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

## Out of scope (still)
Bosses, resource-processing jobs, multiple gathering professions, economy balancing, evolution, rarity dust variants.
