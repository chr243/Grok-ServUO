# Dude System (one Dude per type — ascension model)

Pokémon-style companion loop for Grok-ServUO, designed for **UOR (Ultima Online Renaissance)** rules:

- Classic `BaseCreature` Str/Dex/Int/Hits/VirtualArmor
- Classic pet control (`Controlled` / `ControlMaster` / `ControlSlots` / `OrderType.Follow`)
- Classic elemental Body IDs (13–16) and crystal-ball ItemID (`0xE2E`)
- No dependency on SA masteries, imbuing, or post-UOR pet features

Persistent state lives on **DudeBall**. The world creature is a temporary projection.

> **Design rule:** there is exactly **one Dude per type** (Fire, Water, Earth, Air, …).
> A Dude does not change species as it grows — it gains **levels** and **ascends** through stages.
> New types are added by one enum value + one profile row; no per-type branches elsewhere.

## Core model

### One Dude per type

Wild Dudes of a type are the same species at every strength. Growth is horizontal-agnostic:
same identity, higher level, higher stage.

| Type | Role | Level-1 style |
|------|------|---------------|
| **Fire** | baseline melee | balanced Str/Dex, standard hits |
| **Water** | caster | **Int ×2** gain per level |
| **Earth** | tank | **Hits ×1.5**, **+2 armor per level** |
| **Air** | fast | **Dex +3** gain per level |

Poison and other types are planned; the table is type-agnostic from day one.

### Ascension (stages)

Ascension replaces the old "evolve into a bigger species" idea. A Dude keeps its name and type.
Level-gated, costs no cores. Requires the Dude to be **unsummoned and stored in a DudeBall**.

| Stage | Max level | Ascend at | Follower slots | Gear slots | Skill cap | Summon radius |
|-------|-----------|-----------|----------------|------------|-----------|---------------|
| 1 | 10 | L10 | 1 | 2 | 100 | 1 |
| 2 | 20 | L20 | 2 | 3 | 110 | 2 |
| 3 | 30 | — | 3 | 4 | 120 | 3 |

- **No name change on ascension.** Display name is always `"{Type} Dude"` (e.g. `Fire Dude`).
- **Renaming is off.** A rename deed may exist later.

### Appearance

- **Shorts hue** = type colour (from the type profile)
- **Skin hue** = random per Dude
- **±4 IV rolls** (Str/Dex/Int) are the *only* individual variance. Rolled once at create/catch, never re-rolled.

### Abilities

- **All combat abilities come from equipped gear** (`DudeGear.AbilityId`), unlocked by gear level.
- Simple kits stay in `Abilities/<Element>/` (one file per ability) / `DudeAbilityRegistry`.

## Storage model (B1)

Only **identity + progress** are persisted on the ball:

`Type`, `Level`, `EvolutionStage`, the three IV rolls, combat skills, current HP.

Every combat stat (Str/Dex/Int/HitsMax/Damage/Armor/Mana) is **derived** by `DudeFormulas.Derive`
and recomputed on each event that can change it — **create, summon, level-up, ascension, skill change, load**.
Derived stats are **never serialized**, so a recalled Dude can never disagree with its saved self.

## Loops

### Combat / catch
Wild Dude → catch with Dude Ball (100%) → store → summon → fight → EXP/level → recall.

### Craft / Mixer / Dust
Filled Dude Ball → **Dude Mixer** (confirm) → **Dude Dust** + empty ball → **Dude Crafting Kit** recipe (`Iron Ingot` + `Dude Dust` → empty Dude Ball).

### Job Station / Gathering
Filled **Earth**-type Dude Ball → drop on **Dude Job Station** → Start Job → worker travels to nearest mineable tile → works → returns → deposits **Iron Ore** into station container → ball remains.

### Bosses (elemental farm)
Staff-spawned `DudeBoss` farm bosses — hostile, not catchable, no Dude Ball on death, no world spawners.
Melee 10–16, delayed AoE ability 14–20 (12s CD, range 6).

> **Essences** (`Ember/Tide/Stone/GaleCore`, base `DudeEssence`) are **reserved** for a future
> progression system. They are **not** part of ascension (ascension is free + level-gated) and
> currently do nothing when double-clicked. Kept as drops/collectables so the future system can consume them.

| Boss | Type | Ability | Unique core (0–2) |
|------|------|---------|-------------------|
| **Emberlord** | Fire | Ember Burst | Ember Essence |
| **Tidewarden** | Water | Tide Crash | Tide Essence |
| **Stonewarden** | Earth | Fault Line | Stone Essence |
| **Galewarden** | Air | Shear | Gale Essence |

### Trainer's Manual
**Trainer's Manual** item → double-click → target a wild/summoned `DudeCreature`, filled `DudeBall`, or `DudeBoss` → opens **DudeInfoGump** (name, type, level, EXP, stats, ability, status, owner). Ball data works without recalling. Bosses show a scout sheet (uncatchable).

## File map

| Path | Role |
|------|------|
| `DudeType.cs` / `DudeDefinition.cs` / `DudeRegistry.cs` | Type enum + registry (one def per type) |
| `DudeTypeProfile.cs` / `DudeTypeProfiles.cs` | **Single source of truth per type** (stats, hues, gains, VFX) |
| `DudeStage.cs` | Global ascension table (level cap, slots, skill cap, radius) |
| `DudeData.cs` | Persistent state + `RecomputeStats()` (derived stats) |
| `DudeFormulas.cs` | Derived-stat math (`Derive`) |
| `DudeVfx.cs` | Summon/despawn particle families |
| `Systems/DudeTypeConfig.cs` | `Data/DudeTypes.cfg` overlay for live balance tuning |
| `Abilities/DudeAbility.cs` / `DudeAbilityRegistry.cs` | Ability base class + registry |
| `Abilities/DudeAbilityVfx.cs` / `DudeAbilityConfig.cs` | Shared ability VFX / fight-list helpers; live tuning (`[DudeAbilities`) |
| `Abilities/Fire` `Water` `Earth` `Air/*Ability.cs` | One file per ability. Active: `Execute` (summoned Dude) / `ExecuteLinked` (linked player). Stage-3 passives: `Pulse` / `PulseLinked`; Slipstream: `ReduceCooldown` |
| `Items/DudeBall.cs` | Catch / store / Mixer clear / context menu |
| `Spells/Summoning/DudeBall.Summon.cs` | Summon / recall / park (the ball's summon "spell") |
| `Spells/Summoning/DudeSummonEffects.cs` | Summon / despawn FX |
| `Spells/Linked/DudeLinkSystem.Casting.cs` | Linked-player `[abi1` / `[abi2` casting + passive pulse timer |
| `Items/DudeGear.cs` + `DudeGearSet.cs` | Wearable gear granting abilities |
| `Items/DudeJobStation.cs` (+ Gump) | Generic job station + container |
| `Jobs/*` | Abstract jobs, registry, Earth gathering |
| `Mobiles/Summons/DudeCreature.cs` | Wild / summoned combat Dude: definition, stats, loot, persistence |
| `Mobiles/Summons/DudeCreature.Summon.cs` | Despawn FX sequence, sync to ball, faint / park |
| `Mobiles/Summons/DudeCreature.Inventory.cs` | Type shorts / sash, DudeGear slots, equip rules, hat / shield / costume effects |
| `Mobiles/Summons/DudeCreature.Abilities.cs` | Picks abilities from equipped gear, cooldowns, passive dispatch |
| `Mobiles/Summons/FireDude.cs` … `AirDude.cs` | Named types (one file each, `[add`-able) |
| `Mobiles/AI/DudeCreature.Behavior.cs` | Pack AI: guard stance, target filtering, formation spread, shield taunt |
| `Mobiles/DudeJobWorker.cs` | Temporary job worker |
| `Bosses/*` | Abstract uncatchable boss + four elemental farm bosses |
| `Items/TrainersManual.cs` / `DudeInfoGump.cs` | Inspect tool + info sheet |
| `Systems/*` | Capture, EXP, kill handler, dust formula, link state |
| `Commands/*` | GM helpers |

## Type profiles

Each type is one row in `DudeTypeProfiles.RegisterDefaults()` (overridable via `Data/DudeTypes.cfg`).
A profile carries: display name, shorts/ball hue, summon sound + VFX, spawn weight, level-1
Str/Dex/Int/Hits/damage/armor, per-level gains, hits-gain multiplier, armor gain.

## Architecture notes

- **Layout** mirrors ServUO's player-summoned creatures: the creature under `Mobiles/Summons/` (as `Scripts/Mobiles/Summons`), its AI layer under `Mobiles/AI/`, summoning and player casting under `Spells/`, and one ability per file grouped by element (like `Scripts/Spells/<circle>/`). `DudeCreature`, `DudeBall` and `DudeLinkSystem` are `partial` so each concern sits in its own folder; type names and namespaces are unchanged, so saves load as before.
- **Dust formula** (`DudeDustFormula`): level-based quantity; rarity/type multipliers reserved.
- **Jobs**: station asks `DudeJobRegistry.GetJobForDude(data)` — does not hard-code professions. Only **Earth Gathering** is registered.
- **Travel**: real `PathFollower` movement; stuck / timeout → teleport fallback; station never permanently blocked.
- **Persistence**: station serializes ball, job id, stage, times, destination, worker; on load recovers stage from elapsed time.
- **Safety**: rejects empty/wrong balls, multi-ball, no-job, no-resource; blocks ball lift mid-job; ejects ball/resources on station delete; deposits beside station if full.
- **Bosses**: `DudeBoss` is a `BaseCreature`, not a companion. Capture is rejected for bosses. Boss abilities are local.

## GM test commands

| Command | Purpose |
|---------|---------|
| `[CreateDudeBall` | Empty ball |
| `[SpawnTestDude <type>` | Wild Dude of a type (e.g. `earth`) |
| `[FillDudeBall <type>` | Skip catch |
| `[CreateDudeMixer` | Mixer |
| `[CreateDudeDust 5` | Dust stack |
| `[CreateDudeCraftKit` | Craft kit + ingots |
| `[CreateDudeJobStation` | Station at feet |
| `[StartDudeJob` | Target station to start |
| `[SpawnEmberlord` / `Tidewarden` / `Stonewarden` / `Galewarden` | Hostile elemental boss |
| `[CreateTrainersManual` | Trainer's Manual (info gump) |
| `[DudeScale` | Scaling / stage helpers |

## Out of scope (still)

Multi-phase fights, automatic world spawn, elemental core recipes, resource-processing jobs,
multiple gathering professions, economy balancing, rarity dust variants, rename deeds.