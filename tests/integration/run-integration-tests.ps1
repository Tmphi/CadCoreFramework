#Requires -Version 5.1
<#
.SYNOPSIS
    Runs CadCoreFramework integration smoke tests against a real AutoCAD install
    via accoreconsole.exe (AutoCAD's headless command processor).

.DESCRIPTION
    GitHub Actions `windows-latest` runners do not ship with AutoCAD, so these
    tests cannot run in CI unless you host a self-hosted runner on a machine
    that has AutoCAD installed. For local development:

      1. Build the plugin in Debug or Release.
      2. Run this script. It auto-discovers accoreconsole.exe under
         "C:\Program Files\Autodesk\AutoCAD <year>\accoreconsole.exe"
         or you can pass -AcCorePath explicitly.
      3. The script copies smoke.scr → smoke.generated.scr with the DLL path
         substituted, runs accoreconsole, captures stdout, and scans the log
         for failure markers.

    EXIT CODES:
      0 = all commands dispatched, no failure markers detected in the log.
      1 = accoreconsole returned non-zero, OR a failure marker was found.
      2 = accoreconsole.exe not found on this machine.

.PARAMETER AcCorePath
    Optional path to accoreconsole.exe. If omitted, searches
    C:\Program Files\Autodesk\AutoCAD 20[21-30]\.

.PARAMETER Configuration
    'Debug' (default) or 'Release'. Picks which bin folder to NETLOAD from.

.PARAMETER TargetFramework
    Which multi-target output to test. Default: net48 (AutoCAD 2021–2024).
    Use 'net8.0-windows' for AutoCAD 2025+.

.EXAMPLE
    .\tests\integration\run-integration-tests.ps1

.EXAMPLE
    .\tests\integration\run-integration-tests.ps1 -Configuration Release -TargetFramework net8.0-windows
#>
param(
    [string]$AcCorePath = '',
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [ValidateSet('net48', 'net8.0-windows')]
    [string]$TargetFramework = 'net48'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$ScriptDir = $PSScriptRoot
$DllPath = Join-Path $RepoRoot "bin\$Configuration\$TargetFramework\CadCoreFramework.dll"

# ─── 1. Locate accoreconsole.exe ─────────────────────────────────────────────
if (-not $AcCorePath) {
    $candidates = Get-ChildItem 'C:\Program Files\Autodesk' -Directory -Filter 'AutoCAD 20*' -ErrorAction SilentlyContinue |
                  ForEach-Object { Join-Path $_.FullName 'accoreconsole.exe' } |
                  Where-Object { Test-Path $_ }
    if ($candidates) { $AcCorePath = $candidates[-1] }  # latest install
}

if (-not $AcCorePath -or -not (Test-Path $AcCorePath)) {
    Write-Error "accoreconsole.exe not found. Install AutoCAD or pass -AcCorePath explicitly."
    exit 2
}

if (-not (Test-Path $DllPath)) {
    Write-Error "Plugin DLL not found at $DllPath. Run 'dotnet build -c $Configuration' first."
    exit 1
}

Write-Host "accoreconsole : $AcCorePath"
Write-Host "Plugin DLL    : $DllPath"

# ─── 2. Generate script with DLL path substituted ───────────────────────────
$templatePath = Join-Path $ScriptDir 'smoke.scr'
$generatedPath = Join-Path $ScriptDir 'smoke.generated.scr'
$content = (Get-Content $templatePath -Raw) -replace '\{\{DLL_PATH\}\}', ($DllPath -replace '\\', '\\')
Set-Content -Path $generatedPath -Value $content -Encoding ASCII

# ─── 3. Create a scratch DWG for accoreconsole to open ──────────────────────
$scratchDwg = Join-Path $env:TEMP ("CadCoreFramework_integration_" + [guid]::NewGuid().ToString('N') + '.dwg')

try {
    # accoreconsole needs a drawing to be open — the simplest is to let it start
    # with a blank in-memory DWG via /i for the script and let the script create
    # entities in that session.
    $logPath = Join-Path $ScriptDir 'smoke.log'
    $args = @('/s', $generatedPath, '/l', 'en-US')

    Write-Host ""
    Write-Host "Running accoreconsole..."
    & $AcCorePath @args 2>&1 | Tee-Object -FilePath $logPath
    $exit = $LASTEXITCODE

    # ─── 4. Scan log for failure markers ────────────────────────────────────
    $log = Get-Content $logPath -Raw
    $failureMarkers = @('eLockViolation', 'Fatal Error', 'Unhandled Exception', '[ERROR]')
    $hits = @()
    foreach ($m in $failureMarkers) {
        if ($log -match [regex]::Escape($m)) { $hits += $m }
    }

    Write-Host ""
    if ($exit -ne 0) {
        Write-Error "accoreconsole exited with code $exit"
        exit 1
    }
    if ($hits.Count -gt 0) {
        Write-Error "Failure markers detected in log: $($hits -join ', ')"
        exit 1
    }

    Write-Host "Integration smoke tests passed."
    exit 0
}
finally {
    if (Test-Path $generatedPath) { Remove-Item $generatedPath -Force }
    if (Test-Path $scratchDwg) { Remove-Item $scratchDwg -Force -ErrorAction SilentlyContinue }
}
