#Requires -Version 5.1
<#
.SYNOPSIS
    Builds an MSI installer for CadCoreFramework from the packaged .bundle.

.DESCRIPTION
    Steps the script performs:
      1. Verify dist\CadCoreFramework.bundle exists (run package.ps1 first otherwise).
      2. Verify the WiX 4+ global tool is installed; install it if missing.
      3. Run `wix build` producing dist\CadCoreFramework.msi.

    The installer is per-user and drops the bundle into
    %APPDATA%\Autodesk\ApplicationPlugins\CadCoreFramework.bundle. AutoCAD
    auto-loads it on next startup — no NETLOAD, no admin rights.

.PARAMETER Version
    MSI product version. Must be bumped between releases or MajorUpgrade
    will refuse to install over the previous version.

.EXAMPLE
    .\Deployment\installer\build-msi.ps1 -Version 1.0.0.0
#>
param(
    [string]$Version = '1.0.0.0'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot    = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$WxsFile     = Join-Path $PSScriptRoot 'CadCoreFramework.wxs'
$BundleDir   = Join-Path $RepoRoot 'dist\CadCoreFramework.bundle'
$OutMsi      = Join-Path $RepoRoot 'dist\CadCoreFramework.msi'

# ─── 1. Prerequisites ────────────────────────────────────────────────────────
if (-not (Test-Path $BundleDir)) {
    Write-Error "Bundle not found at $BundleDir. Run .\package.ps1 first."
    exit 1
}

# WiX 4+ ships as a .NET global tool.
$wix = (Get-Command wix -ErrorAction SilentlyContinue)
if (-not $wix) {
    Write-Host 'wix tool not found — installing globally...'
    dotnet tool install --global wix | Out-Null
    # PATH refresh for this session only.
    $env:PATH = "$env:USERPROFILE\.dotnet\tools;$env:PATH"
}

# ─── 2. Build ────────────────────────────────────────────────────────────────
Write-Host "Building $OutMsi (version $Version)..."
& wix build $WxsFile `
    -arch x64 `
    -bindvariable "BundleSourceDir=$BundleDir" `
    -define "ProductVersion=$Version" `
    -o $OutMsi
if ($LASTEXITCODE -ne 0) {
    Write-Error 'wix build failed.'
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "Done. MSI at: $OutMsi"
Write-Host "Install:    msiexec /i `"$OutMsi`""
Write-Host "Uninstall:  msiexec /x `"$OutMsi`""
