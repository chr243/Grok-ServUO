param(
    [Parameter(Mandatory = $true)][string[]]$Targets,   # "label=port" or "label=port=runtimeDir" (runtime dir adds server CPU)
    [string]$Exe,                                       # UoTest.exe; defaults to the Release build under Client\bin
    [int]$Clients = 10,
    [int]$Seconds = 30,
    [int]$Rounds = 2,
    [int]$SayMs = 400,
    [int]$SayLen = 0,
    [int]$Sync = 0
)

# Runs the stress client against one or more test servers, alternating between them each round so
# background load on the machine hits both builds evenly, and reports the CPU each server burned.
$ErrorActionPreference = "Stop"

if (-not $Exe) { $Exe = Join-Path $PSScriptRoot "Client\bin\Release\UoTest.exe" }
if (-not (Test-Path $Exe)) { throw "UoTest.exe not found at $Exe -- build Tests\Client\UoTest.csproj or pass -Exe." }

$parsed = foreach ($t in $Targets) {
    $parts = $t -split '=', 3
    if ($parts.Count -lt 2) { throw "Bad -Targets entry '$t'; expected label=port[=runtimeDir]." }
    [pscustomobject]@{ Name = $parts[0]; Port = [int]$parts[1]; Rt = if ($parts.Count -eq 3) { $parts[2] } else { $null } }
}

for ($round = 1; $round -le $Rounds; $round++) {
    foreach ($t in $parsed) {
        $proc = $null
        if ($t.Rt -and (Test-Path "$($t.Rt)\pid.txt")) {
            $proc = Get-Process -Id ([int](Get-Content "$($t.Rt)\pid.txt")) -ErrorAction SilentlyContinue
        }
        $cpu0 = if ($proc) { $proc.TotalProcessorTime.TotalMilliseconds } else { 0 }

        "===== $($t.Name) round $round (port $($t.Port))"
        & $Exe -mode stress -port $t.Port -clients $Clients -seconds $Seconds -sayMs $SayMs -sayLen $SayLen -sync $Sync `
            -prefix ("s{0}{1}{2}" -f $Sync, $round, $t.Name.Substring(0, 1).ToLower())

        if ($proc) {
            $proc.Refresh()
            "  serverCPU={0:n0}ms over the run (login + {1}s load)  workingSet={2:n0}MB" -f `
                ($proc.TotalProcessorTime.TotalMilliseconds - $cpu0), $Seconds, ($proc.WorkingSet64 / 1MB)
        }
    }
}
