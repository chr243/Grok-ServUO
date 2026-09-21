param(
    [Parameter(Mandatory = $true)][string]$BinDir,          # folder holding ServUO.exe / Scripts.dll / Ultima.dll
    [Parameter(Mandatory = $true)][string]$OutDir,          # runtime folder to (re)create -- deleted if it exists
    [Parameter(Mandatory = $true)][int]$Port,
    [string]$Repo = (Split-Path $PSScriptRoot -Parent),     # shard checkout to take Config/ and Data/ from
    [string]$DataPath                                       # client mul/UOP folder, if it is not auto-detected
)

# Builds an isolated, loopback-only ServUO runtime so a test shard never touches the real one:
# its own world saves, accounts and logs, bound to 127.0.0.1 on $Port, with autosave off.
$ErrorActionPreference = "Stop"

if (Test-Path $OutDir) { Remove-Item -Recurse -Force $OutDir }
New-Item -ItemType Directory -Force $OutDir | Out-Null

foreach ($f in @("ServUO.exe", "Scripts.dll", "Ultima.dll")) { Copy-Item (Join-Path $BinDir $f) $OutDir }
foreach ($f in @("ServUO.exe.config", "zlibwapi32.dll", "zlibwapi64.dll")) { Copy-Item (Join-Path $Repo $f) $OutDir }
Copy-Item -Recurse (Join-Path $Repo "Config") (Join-Path $OutDir "Config")
Copy-Item -Recurse (Join-Path $Repo "Data") (Join-Path $OutDir "Data")
New-Item -ItemType Directory -Force (Join-Path $OutDir "Scripts") | Out-Null   # empty: skips the startup script build

$cfg = Join-Path $OutDir "Config"
if ($DataPath) { Add-Content -Encoding ascii (Join-Path $cfg "DataPath.cfg") "`r`nCustomPath=$DataPath" }

$server = Get-Content (Join-Path $cfg "Server.cfg")
$server = $server -replace '^@?Listen=.*', 'Listen=127.0.0.1' -replace '^@?Address=.*', 'Address=127.0.0.1' -replace '^@?Port=.*', "Port=$Port"
Set-Content -Encoding ascii (Join-Path $cfg "Server.cfg") $server

# The stress client logs in many accounts from 127.0.0.1.
$acct = Get-Content (Join-Path $cfg "Accounts.cfg")
$acct = $acct -replace '^AccountsPerIp=.*', 'AccountsPerIp=100'
Set-Content -Encoding ascii (Join-Path $cfg "Accounts.cfg") $acct

# Saves happen only when a test asks for them, so runs stay reproducible.
$save = Get-Content (Join-Path $cfg "AutoSave.cfg")
$save = $save -replace '^Enabled=.*', 'Enabled=False'
Set-Content -Encoding ascii (Join-Path $cfg "AutoSave.cfg") $save

"Runtime ready: $OutDir (port $Port)"
Get-Content (Join-Path $cfg "Server.cfg") | Where-Object { $_ -match '^(Listen|Address|Port)=' }
