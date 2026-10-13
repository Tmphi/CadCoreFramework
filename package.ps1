# ==============================================================================
# Script tu dong build Release va dong goi thanh CadCoreFramework_v2.0_Setup.zip
# Chuan Autodesk ApplicationPlugins (.bundle)
# ==============================================================================

param (
    [string]$Version = "2.0"
)

$ErrorActionPreference = "Stop"

$workspaceRoot = $PSScriptRoot
$distDir = Join-Path $workspaceRoot "dist"
$releaseStaging = Join-Path $distDir "CadCoreFramework_Release"
$bundleDir = Join-Path $releaseStaging "CadCoreFramework.bundle"
$contentsDir = Join-Path $bundleDir "Contents"
$zipOutput = Join-Path $distDir "CadCoreFramework_v$($Version)_Setup.zip"

Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "  DONG GOI PHAT HANH CadCoreFramework v$Version" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

# 1. Build Release x64 - both target frameworks
Write-Host "`n[1/5] Bien dich cau hinh Release x64 (net48 + net8.0-windows)..." -ForegroundColor Green
& dotnet build (Join-Path $workspaceRoot "CadCoreFramework.csproj") -c Release -p:Platform=x64 --nologo

if ($LASTEXITCODE -ne 0) {
    throw "[ERROR] Build Release that bai!"
}

# 2. Chuan bi thu muc staging
Write-Host "`n[2/5] Tao cau truc thu muc phat hanh..." -ForegroundColor Green
if (Test-Path $releaseStaging) {
    Remove-Item $releaseStaging -Recurse -Force
}
if (Test-Path $zipOutput) {
    Remove-Item $zipOutput -Force
}

$net48Dir = Join-Path $contentsDir "net48"
$net8Dir  = Join-Path $contentsDir "net8.0-windows"
New-Item -ItemType Directory -Path $net48Dir -Force | Out-Null
New-Item -ItemType Directory -Path $net8Dir  -Force | Out-Null

# 3. Sao chep DLL va thu vien phu thuoc
Write-Host "`n[3/5] Sao chep DLL va thu vien phu thuoc..." -ForegroundColor Green

# Helper: copy plugin DLL + non-AutoCAD deps from a TFM output folder
function Copy-TfmOutput([string]$srcDir, [string]$destDir) {
    if (-not (Test-Path $srcDir)) {
        throw "[ERROR] Thu muc output khong ton tai: $srcDir"
    }

    # Copy main DLL & PDB
    Copy-Item (Join-Path $srcDir "CadCoreFramework.dll") -Destination $destDir -Force
    $pdb = Join-Path $srcDir "CadCoreFramework.pdb"
    if (Test-Path $pdb) { Copy-Item $pdb -Destination $destDir -Force }

    # Copy third-party deps only (skip AutoCAD's own Ac*/Ad* assemblies)
    Get-ChildItem -Path $srcDir -Filter "*.dll" | ForEach-Object {
        $name = $_.Name
        if (-not ($name.Equals("CadCoreFramework.dll", [System.StringComparison]::OrdinalIgnoreCase) -or
                  $name.StartsWith("Ac", [System.StringComparison]::OrdinalIgnoreCase) -or
                  $name.StartsWith("Ad", [System.StringComparison]::OrdinalIgnoreCase))) {
            Copy-Item $_.FullName -Destination $destDir -Force
            Write-Host "   + $name" -ForegroundColor Gray
        }
    }
}

$binBase = Join-Path $workspaceRoot "bin\x64\Release"
Copy-TfmOutput (Join-Path $binBase "net48")          $net48Dir
Copy-TfmOutput (Join-Path $binBase "net8.0-windows") $net8Dir

# 4. Sao chep PackageContents.xml, Install.bat, Uninstall.bat, HDSD.txt
Write-Host "`n[4/5] Sao chep file cau hinh bundle va script cai dat..." -ForegroundColor Green
$deployDir = Join-Path $workspaceRoot "Deployment"

Copy-Item (Join-Path $deployDir "PackageContents.xml") -Destination $bundleDir -Force

# Dong dau version vao manifest cua bundle (AppVersion + Version cua tung ComponentEntry)
# de AutoCAD Autoloader nhan dung ban moi khi cap nhat. "2.0" -> "2.0.0".
$parts = @($Version.Split('.'))
while ($parts.Count -lt 3) { $parts += '0' }
$semver = ($parts[0..2] -join '.')
$manifest = Join-Path $bundleDir "PackageContents.xml"
$xml = [System.IO.File]::ReadAllText($manifest)
# -creplace (phan biet hoa/thuong): -replace se an ca <?xml version="1.0"?> -> manifest hong.
$xml = $xml -creplace 'AppVersion="[^"]*"', "AppVersion=""$semver""" -creplace '(\s)Version="[^"]*"', "`$1Version=""$semver"""
[System.IO.File]::WriteAllText($manifest, $xml, (New-Object System.Text.UTF8Encoding($false)))
Write-Host "   PackageContents.xml version = $semver" -ForegroundColor Gray

foreach ($f in @("Install.bat", "Uninstall.bat", "HDSD.txt")) {
    $src = Join-Path $deployDir $f
    if (Test-Path $src) { Copy-Item $src -Destination $releaseStaging -Force }
}

# 5. Nen toan bo releaseStaging thanh ZIP
Write-Host "`n[5/5] Nen goi phat hanh ZIP..." -ForegroundColor Green
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($releaseStaging, $zipOutput, [System.IO.Compression.CompressionLevel]::Optimal, $false)

$zipItem = Get-Item $zipOutput
$zipSizeKb = [math]::Round(($zipItem.Length / 1024.0), 2)

Write-Host "`n=================================================" -ForegroundColor Green
Write-Host "  DONG GOI HOAN TAT THANH CONG!" -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Green
Write-Host "File cai dat da san sang:" -ForegroundColor White
Write-Host "--> $zipOutput ($zipSizeKb KB)" -ForegroundColor Yellow
Write-Host "Nguoi dung chi can giai nen file zip va chay 'Install.bat' de cai dat 1-click vao AutoCAD." -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Green
