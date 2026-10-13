# ==============================================================================
# PowerShell Build Script for CadCoreFramework AutoCAD Plugin
# Usage:
#   .\build.ps1                          # Debug build (net48 default)
#   .\build.ps1 -Configuration Release   # Release build
#   .\build.ps1 -Framework net8.0-windows # Build for AutoCAD 2025+
#   .\build.ps1 -RunTests                # Build + run xUnit tests
# ==============================================================================

param (
    [string]$Configuration = "Debug",
    [string]$Platform      = "x64",
    [string]$Framework     = "net48",
    [switch]$RunTests      = $false,
    [switch]$Coverage      = $false
)

$ErrorActionPreference = "Stop"

Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "  BUILDING CadCoreFramework" -ForegroundColor Cyan
Write-Host "  Configuration : $Configuration" -ForegroundColor Cyan
Write-Host "  Platform      : $Platform" -ForegroundColor Cyan
Write-Host "  Framework     : $Framework" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

# ─── 1. Locate MSBuild ────────────────────────────────────────────────────────
$vswherePath = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$msbuildPath = ""

if (Test-Path $vswherePath) {
    $msbuildPath = & $vswherePath -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -First 1
}

if (-not $msbuildPath -or -not (Test-Path $msbuildPath)) {
    Write-Host "[INFO] MSBuild not found via vswhere - falling back to 'dotnet msbuild'." -ForegroundColor Yellow
    $buildCmd  = "dotnet"
    $buildArgs = @("msbuild", "CadCoreFramework.csproj",
        "/p:Configuration=$Configuration",
        "/p:Platform=$Platform",
        "/p:TargetFramework=$Framework")
} else {
    Write-Host "[INFO] Using MSBuild: $msbuildPath" -ForegroundColor Green
    $buildCmd  = $msbuildPath
    $buildArgs = @("CadCoreFramework.csproj",
        "/p:Configuration=$Configuration",
        "/p:Platform=$Platform",
        "/p:TargetFramework=$Framework")
}

# ─── 2. Build ─────────────────────────────────────────────────────────────────
Write-Host "`n[STARTING BUILD]..." -ForegroundColor Green
& $buildCmd $buildArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host "`n[ERROR] Build failed with exit code $LASTEXITCODE." -ForegroundColor Red
    exit $LASTEXITCODE
}

$finalDll = Join-Path $PSScriptRoot "bin\$Platform\$Configuration\$Framework\CadCoreFramework.dll"
Write-Host "`n=================================================" -ForegroundColor Green
Write-Host "  BUILD SUCCESSFUL!" -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Green
Write-Host "DLL ready: $finalDll" -ForegroundColor Yellow

Write-Host "`nAvailable AutoCAD Commands:" -ForegroundColor Cyan
Write-Host "  CORE_INFO      - Check infrastructure status"      -ForegroundColor Gray
Write-Host "  CORE_TEST_ALL  - Test drawing, layers, XData"      -ForegroundColor Gray
Write-Host "  SHOW_PALETTE   - Toggle AutoCAD Dockable Palette"  -ForegroundColor Gray

# ─── 3. Run Tests (optional) ──────────────────────────────────────────────────
if ($RunTests) {
    Write-Host "`n=================================================" -ForegroundColor Cyan
    Write-Host "  RUNNING TESTS" -ForegroundColor Cyan
    Write-Host "=================================================" -ForegroundColor Cyan

    $testProject = Join-Path $PSScriptRoot "CadCoreFramework.Tests\CadCoreFramework.Tests.csproj"

    if ($Coverage) {
        Write-Host "[INFO] Collecting code coverage with coverlet..." -ForegroundColor Yellow
        $coverageDir = Join-Path $PSScriptRoot "coverage"
        dotnet test $testProject --configuration $Configuration --verbosity normal `
            --collect:"XPlat Code Coverage" `
            --results-directory $coverageDir
    } else {
        dotnet test $testProject --configuration $Configuration --verbosity normal
    }

    if ($LASTEXITCODE -ne 0) {
        Write-Host "`n[ERROR] One or more tests failed (exit code $LASTEXITCODE)." -ForegroundColor Red
        exit $LASTEXITCODE
    }

    Write-Host "`n[OK] All tests passed." -ForegroundColor Green

    if ($Coverage) {
        Write-Host "[INFO] Coverage report saved to: $coverageDir" -ForegroundColor Yellow
    }
}

Write-Host "`n=================================================" -ForegroundColor Green
Write-Host "  DONE" -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Green
