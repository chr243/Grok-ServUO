# Local test tooling

Runtime tests for this shard without a real UO client: a headless client that speaks the protocol,
and test-only GM commands that drive the Dude system in-server and report PASS/FAIL.

Nothing here ships with the shard. The harness `.cs` files live outside `Scripts/`, so a normal
`dotnet build` never sees them; only `Tests\build.ps1 -Harness` compiles them in. **Do not copy them
into `Scripts/` on a live shard** — they register admin commands that spawn, delete and rewrite things.

Needs Windows, PowerShell and the .NET SDK. Client `.mul`/`.uop` files are needed only if
`Config/DataPath.cfg` cannot find them (pass `-DataPath`).

| Path | Role |
|------|------|
| `Client/UoTest.cs` + `HuffmanTable.cs` + `UoTest.csproj` | Headless UO client (login, walk, speech, stress, spawn watch) |
| `Harness/DudeRefactorTest.cs` | `[DrtRun` full Dude suite, `[DrtWorld` + `[DrtDump` for save/load comparisons |
| `Harness/DudeMixerGearTest.cs` | `[TestMixGear`: Mixer recycle / gear return |
| `build.ps1` | Compiles the shard (plus the harness) into a throwaway folder |
| `setup-runtime.ps1` | Creates an isolated loopback runtime (own saves, accounts, port) |
| `srv.ps1` | Starts / stops a runtime in the background |
| `stress.ps1` | Runs the stress client against one or more runtimes and reports server CPU |

## Quickstart

```powershell
cd <shard checkout>
dotnet build Tests\Client\UoTest.csproj -c Release          # -> Tests\Client\bin\Release\UoTest.exe

$t = "$env:TEMP\uotest"
.\Tests\build.ps1 -OutDir "$t\bin" -Harness                 # compile this checkout + the harness
.\Tests\setup-runtime.ps1 -BinDir "$t\bin" -OutDir "$t\rt" -Port 2601
.\Tests\srv.ps1 start "$t\rt" -First                        # -First creates the admin / adminpw owner account

.\Tests\Client\bin\Release\UoTest.exe -mode walk -port 2601 -account walker -password pw
.\Tests\srv.ps1 stop "$t\rt"
```

The runtime is bound to `127.0.0.1` with autosave off and its own `Saves/`, so it cannot touch a real
shard. Accounts are created on first login; the client makes a character if the account has none.

## Client modes

`UoTest.exe -mode <mode> -port <port> [options]`, host defaults to `127.0.0.1`.

| Mode | What it does | Main options |
|------|--------------|--------------|
| `walk` | Logs in, walks back and forth, reports packets, bytes and move-ack round trips | `-steps 60 -interval 450` |
| `stress` | Many clients moving and talking at once | `-clients 10 -seconds 30 -moveMs 450 -sayMs 400 -sayLen 0 -sync 0 -prefix s` |
| `relay` | Repeats login -> relay -> game login, counting failed handoffs | `-count 500 -prefix relay` |
| `say` | Runs a script of commands as `admin` and prints what comes back | `-lines "a\|b" -delay 2000 -waitMs 120000` |
| `spawnwatch` | Places Dude spawners, shortens their delays, kills everything and times the respawns | `-stale 320 -watch 45` |

In `say`, `~2000` sleeps 2s instead of speaking and `?text` waits (up to `-waitMs`) for a server
message containing `text`, so a script can follow a harness run that takes a while:

```powershell
UoTest.exe -mode say -port 2601 -lines "[DrtRun|?DRT DONE|~1000|[TestMixGear|?MIXTEST DONE"
```

Every mode prints the packet ids it saw, which is often the quickest way to spot a change in what
the server sends.

## Harness commands

Available once the server is built with `-Harness`, to an Administrator account.

| Command | What it does |
|---------|--------------|
| `[DrtRun` | ~97 checks: summon / recall / park / re-summon for all 12 species, the 8 gear actives with their cooldowns and effects, the stage-3 passives (direct and during a live AI fight), linked form and casting, and guarding Dudes defending their owner. Ends with `DRT DONE: n passed, m failed` |
| `[TestMixGear` | 12 checks on recycling a ball through the Mixer and returning the Dude's gear |
| `[DrtWorld` | Builds a world holding Dudes in every state: 12 filled balls, two summoned, three parked, one fainted, gear worn and loose, two wild |
| `[DrtDump <label>` | Writes `drt-<label>.txt` in the runtime folder: every ball, Dude and gear piece with its persisted state, sorted, leaving out what moves on its own (locations, current hits of anything in the world) |

`[DrtRun` fights in the open: it moves the owner outside guarded ground first, because town guards
kill anything that attacks a player in town before the Dudes can react.

## Comparing two branches

Build each branch into its own runtime, run the same script against both, and diff the results with
numbers masked (stats, damage rolls and AI timing vary run to run):

```powershell
git worktree add C:\t\a origin/main
git worktree add C:\t\b my-branch
.\Tests\build.ps1 -Repo C:\t\a -OutDir C:\t\bin-a -Harness
.\Tests\build.ps1 -Repo C:\t\b -OutDir C:\t\bin-b -Harness
.\Tests\setup-runtime.ps1 -BinDir C:\t\bin-a -OutDir C:\t\rt-a -Port 2601
.\Tests\setup-runtime.ps1 -BinDir C:\t\bin-b -OutDir C:\t\rt-b -Port 2602
.\Tests\srv.ps1 start C:\t\rt-a -First; .\Tests\srv.ps1 start C:\t\rt-b -First

$s = "[DrtRun|?DRT DONE|~1500|[TestMixGear|?MIXTEST DONE"
.\Tests\Client\bin\Release\UoTest.exe -mode say -port 2601 -lines $s -waitMs 150000 > a.log
.\Tests\Client\bin\Release\UoTest.exe -mode say -port 2602 -lines $s -waitMs 150000 > b.log

$mask = { ($input | Select-String '^< (DRT|MIXTEST)') -replace '0x[0-9A-F]+', '0xS' -replace '-?\d+(\.\d+)?', 'N' }
Compare-Object (Get-Content a.log | & $mask) (Get-Content b.log | & $mask)

.\Tests\stress.ps1 -Targets "a=2601=C:\t\rt-a", "b=2602=C:\t\rt-b" -Clients 10 -Seconds 30
```

Keep the two runtimes on separate ports and start both before running, so background load on the
machine lands on both. Server CPU figures are noisy; packet counts and check results are not.

## Save compatibility across branches

```powershell
# on branch A: build a world, save it, record it
UoTest.exe -mode say -port 2601 -lines "[DrtWorld|?DRT WORLD READY|[save|~5000|[DrtDump a-pre|?DRT DUMP"
.\Tests\srv.ps1 stop C:\t\rt-a

# hand A's world to branch B and compare what each one loaded
Copy-Item -Recurse C:\t\rt-a\Saves C:\t\rt-b\Saves
.\Tests\srv.ps1 start C:\t\rt-a; .\Tests\srv.ps1 start C:\t\rt-b
UoTest.exe -mode say -port 2601 -lines "~2000|[DrtDump a-post|?DRT DUMP"
UoTest.exe -mode say -port 2602 -lines "~2000|[DrtDump b-from-a|?DRT DUMP"
Compare-Object (Get-Content C:\t\rt-a\drt-a-post.txt) (Get-Content C:\t\rt-b\drt-b-from-a.txt)
```

Then save on B and load that world back on A for the other direction. The world-load line in
`stdout.log` (`...done (N items, M mobiles ...)`) should hold steady across restarts; a climbing
count means something is duplicating itself on load.
