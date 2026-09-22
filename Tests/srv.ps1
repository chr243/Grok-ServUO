param(
    [Parameter(Mandatory = $true)][ValidateSet("start", "stop")][string]$Action,
    [Parameter(Mandatory = $true)][string]$Rt,
    [switch]$First   # first boot of a fresh runtime: answer the owner-account prompts (admin / adminpw)
)

# Starts or stops a test runtime in the background: hidden window, stdout -> $Rt\stdout.log, pid in pid.txt.
$ErrorActionPreference = "Stop"

if ($Action -eq "stop") {
    if (Test-Path "$Rt\pid.txt") {
        $srvPid = [int](Get-Content "$Rt\pid.txt")
        Stop-Process -Id $srvPid -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 1
        "stopped pid $srvPid"
    }
    return
}

if ($First) {
    Set-Content -Encoding ascii "$Rt\answers.txt" "y`r`nadmin`r`nadminpw`r`n"
    $stdin = "$Rt\answers.txt"
} else {
    Set-Content -Encoding ascii "$Rt\empty.txt" ""
    $stdin = "$Rt\empty.txt"
}

$p = Start-Process -FilePath "$Rt\ServUO.exe" -WorkingDirectory $Rt -RedirectStandardInput $stdin `
    -RedirectStandardOutput "$Rt\stdout.log" -RedirectStandardError "$Rt\stderr.log" -WindowStyle Hidden -PassThru
Set-Content "$Rt\pid.txt" $p.Id

$sw = [Diagnostics.Stopwatch]::StartNew()
while ($sw.Elapsed.TotalSeconds -lt 180) {
    Start-Sleep -Milliseconds 500
    if ($p.HasExited) {
        Get-Content "$Rt\stdout.log" -Tail 30
        throw "server exited with code $($p.ExitCode)"
    }
    $log = Get-Content "$Rt\stdout.log" -Raw -ErrorAction SilentlyContinue
    if ($log -match 'Listening:') { break }
}

"pid {0} up in {1:0.0}s" -f $p.Id, $sw.Elapsed.TotalSeconds
(Get-Content "$Rt\stdout.log") | Where-Object { $_ -match 'done \(\d+ items|Listening:|[Ee]rror|[Ee]xception' } | Select-Object -First 8
