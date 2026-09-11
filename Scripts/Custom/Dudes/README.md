# Dude System (v1)

Pokémon-style companion loop for Grok-ServUO, designed for **UOR (Ultima Online Renaissance)** rules:

- Classic `BaseCreature` Str/Dex/Int/Hits/VirtualArmor
- Classic pet control (`Controlled` / `ControlMaster` / `ControlSlots` / `OrderType.Follow`)
- Classic elemental Body IDs (13–16) and crystal-ball ItemID (`0xE2E`)
- No dependency on SA masteries, imbuing, or post-UOR pet features

Persistent state lives on **DudeBall**. The world creature is a temporary projection.

## File map

| File | Role |
|------|------|
| `DudeType.cs` | Fire / Water / Earth / Air enum |
| `DudeDefinition.cs` | Species template |
| `DudeRegistry.cs` | Register / lookup species (add new Dudes here) |
| `DudeData.cs` | Serializable persistent stats |
| `Abilities/*` | Ability base + four type abilities + registry |
| `Items/DudeBall.cs` | Catch / store / summon / recall + Serialize |
| `Mobiles/DudeCreature.cs` | Wild + summoned creature, combat AI, faint-on-death |
| `Systems/DudeCapture.cs` | Modular 100% capture chance |
| `Systems/DudeExperience.cs` | EXP + level-up |
| `Systems/DudeKillHandler.cs` | EventSink kill → EXP |
| `Commands/DudeTestCommands.cs` | GM test commands |

## Test commands (GM)

- `[CreateDudeBall` — empty ball in backpack
- `[SpawnTestDude emberling` — wild Dude (ids: emberling, tideling, stonepaw, gustling)
- `[SpawnAllTestDudes` — one of each
- `[FillDudeBall emberling` — target a ball to skip catch

## Death policy

Summoned Dude death writes last state to the ball as **Fainted**, deletes the world creature, and clears the summon link. Re-summoning revives to full HP. This avoids soft-locks.
