param(
    [switch]$Probe,
    [switch]$Once,
    [switch]$DryRun,
    [string]$Snapshot,
    [string]$Process = "HTGame",
    [string]$Title = "$([char]0x5F02)$([char]0x73AF)",
    [double]$HammerX = 0.0667,
    [double]$HammerY = 0.4620,
    [int]$DurationSec = 45,
    [int]$LoadingMs = 7000,
    [int]$MinMs = 250,
    [int]$MaxMs = 650,
    [string]$ClaimRegion = "0.50,0.72,0.22,0.12"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "src\YihuanRunner\YihuanRunner.csproj"

$runnerArgs = @(
    "--process", $Process,
    "--title", $Title,
    "--hammer-x", ([string]::Format([Globalization.CultureInfo]::InvariantCulture, "{0}", $HammerX)),
    "--hammer-y", ([string]::Format([Globalization.CultureInfo]::InvariantCulture, "{0}", $HammerY)),
    "--duration-sec", $DurationSec,
    "--loading-ms", $LoadingMs,
    "--min-ms", $MinMs,
    "--max-ms", $MaxMs,
    "--claim-region", $ClaimRegion
)

if ($Probe) { $runnerArgs += "--probe" }
if ($Once) { $runnerArgs += "--once" }
if ($DryRun) { $runnerArgs += "--dry-run" }
if ($Snapshot) { $runnerArgs += @("--snapshot", $Snapshot) }

dotnet run --project $project -- @runnerArgs
exit $LASTEXITCODE
