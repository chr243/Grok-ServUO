param(
    [Parameter(Mandatory = $true)][string]$OutDir,          # folder to write ServUO.exe / Scripts.dll / Ultima.dll into
    [string]$Repo = (Split-Path $PSScriptRoot -Parent),     # shard checkout to compile
    [switch]$Harness,                                       # also compile Tests/Harness/*.cs into Scripts.dll
    [string[]]$ExtraScripts = @(),                          # extra .cs files for Scripts.dll
    [switch]$SkipScripts,
    [string]$Csc                                            # csc.exe, or Roslyn csc.dll to run through `dotnet`
)

# Compiles the shard into a throwaway folder without touching the checkout, mirroring the SDK-style
# projects (net48, C# 7.3, x64, Release defines). `dotnet build` writes into the checkout root and
# cannot add the test harness, which is why this exists.
$ErrorActionPreference = "Stop"

function Resolve-Csc {
    if ($Csc) { return $Csc }

    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vswhere) {
        $root = & $vswhere -latest -products * -property installationPath -nologo 2>$null | Select-Object -First 1
        if ($root) {
            $exe = Join-Path $root "MSBuild\Current\Bin\Roslyn\csc.exe"
            if (Test-Path $exe) { return $exe }
        }
    }

    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($dotnet) {
        $sdks = Join-Path (Split-Path $dotnet.Source -Parent) "sdk"
        if (Test-Path $sdks) {
            $dll = Get-ChildItem $sdks -Directory |
                Sort-Object { try { [version]($_.Name -replace '-.*$') } catch { [version]'0.0' } } -Descending |
                ForEach-Object { Join-Path $_.FullName "Roslyn\bincore\csc.dll" } |
                Where-Object { Test-Path $_ } | Select-Object -First 1
            if ($dll) { return $dll }
        }
    }

    throw "No C# compiler found. Install the .NET SDK (or VS Build Tools), or pass -Csc."
}

$cscPath = Resolve-Csc
$fx = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319"
New-Item -ItemType Directory -Force $OutDir | Out-Null

$fxRefs = @("mscorlib", "System", "System.Core", "System.Data", "System.Drawing", "System.IO.Compression.FileSystem",
    "System.Numerics", "System.Runtime.Serialization", "System.Xml", "System.Xml.Linq", "Microsoft.CSharp") |
    ForEach-Object { "/reference:`"$fx\$_.dll`"" }

$common = @("/nologo", "/nostdlib+", "/langversion:7.3", "/platform:x64", "/unsafe+", "/optimize+",
    "/define:TRACE;NEWTIMERS;ServUO", "/debug-", "/nowarn:1701,1702", "/warn:4", "/deterministic+", "/filealign:512")

function Invoke-Csc([string]$name, [string[]]$options, [string[]]$sources) {
    $rsp = Join-Path $OutDir "$name.rsp"
    ($common + $fxRefs + $options + ($sources | ForEach-Object { "`"$_`"" })) | Set-Content -Encoding utf8 $rsp

    $sw = [Diagnostics.Stopwatch]::StartNew()
    if ($cscPath.EndsWith(".dll")) { $out = & dotnet $cscPath /noconfig "@$rsp" 2>&1 | Out-String }
    else { $out = & $cscPath /noconfig "@$rsp" 2>&1 | Out-String }
    $code = $LASTEXITCODE
    $sw.Stop()

    $errors = ($out -split "`r?`n") | Where-Object { $_ -match ': error ' }
    $warnings = ($out -split "`r?`n") | Where-Object { $_ -match ': warning ' }
    $out | Set-Content -Encoding utf8 (Join-Path $OutDir "$name.log")

    "{0}: exit={1} errors={2} warnings={3} time={4:n1}s" -f $name, $code, $errors.Count, $warnings.Count, $sw.Elapsed.TotalSeconds
    $errors | Select-Object -First 40
    if ($code -ne 0) { throw "$name failed (see $OutDir\$name.log)" }
}

function Get-Sources([string]$dir, [string[]]$exclude = @()) {
    Get-ChildItem -Path $dir -Recurse -Filter *.cs -File |
        Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } |
        Where-Object { $f = $_.FullName; -not ($exclude | Where-Object { $f -like $_ }) } |
        Sort-Object FullName | Select-Object -ExpandProperty FullName
}

"compiler: $cscPath"
"repo:     $Repo"

Invoke-Csc "Ultima" @("/target:library", "/out:`"$OutDir\Ultima.dll`"") (Get-Sources "$Repo\Ultima")

# ConnectionLog.cs lives under Scripts but is compiled into the server, as in Server.csproj.
$serverSrc = @(Get-Sources "$Repo\Server") + "$Repo\Scripts\Misc\ConnectionLog.cs"
Invoke-Csc "ServUO" @("/target:exe", "/main:Server.Core", "/out:`"$OutDir\ServUO.exe`"", "/reference:`"$OutDir\Ultima.dll`"",
    "/win32manifest:`"$Repo\Server\ServUO.exe.manifest`"") $serverSrc

if (-not $SkipScripts) {
    if ($Harness) { $ExtraScripts += (Get-Sources "$PSScriptRoot\Harness") }

    $scriptRefs = @("System.Web", "System.Windows.Forms", "System.Data.DataSetExtensions") | ForEach-Object { "/reference:`"$fx\$_.dll`"" }
    $scriptSrc = @(Get-Sources "$Repo\Scripts" @("$Repo\Scripts\Misc\ConnectionLog.cs")) + $ExtraScripts
    Invoke-Csc "Scripts" (@("/target:library", "/out:`"$OutDir\Scripts.dll`"", "/reference:`"$OutDir\ServUO.exe`"",
        "/reference:`"$OutDir\Ultima.dll`"") + $scriptRefs) $scriptSrc
}
