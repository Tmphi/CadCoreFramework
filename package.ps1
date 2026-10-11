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
$dataDir = Join-Path $contentsDir "Data"
$zipOutput = Join-Path $distDir "CadCoreFramework_v$($Version)_Setup.zip"

Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "  DONG GOI PHAT HANH CadCoreFramework v$Version" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

# 1. Chay build Release x64
Write-Host "`n[1/5] Bien dich cau hinh Release x64..." -ForegroundColor Green
& powershell.exe -ExecutionPolicy Bypass -File (Join-Path $workspaceRoot "build.ps1") -Configuration Release -Platform x64

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

New-Item -ItemType Directory -Path $contentsDir -Force | Out-Null
New-Item -ItemType Directory -Path $dataDir -Force | Out-Null

# 3. Sao chep DLL va thu vien phu thuoc
Write-Host "`n[3/5] Sao chep DLL va thu vien phu thuoc..." -ForegroundColor Green
$binRelease = Join-Path $workspaceRoot "bin\x64\Release"

# Copy CadCoreFramework DLL & PDB
Copy-Item (Join-Path $binRelease "CadCoreFramework.dll") -Destination $contentsDir -Force
if (Test-Path (Join-Path $binRelease "CadCoreFramework.pdb")) {
    Copy-Item (Join-Path $binRelease "CadCoreFramework.pdb") -Destination $contentsDir -Force
}

# Copy cac dll phu thuoc (tru AutoCAD dlls da co san trong AutoCAD)
Get-ChildItem -Path $binRelease -Filter "*.dll" | ForEach-Object {
    $name = $_.Name
    if (-not ($name.StartsWith("Ac", [System.StringComparison]::OrdinalIgnoreCase) -or
              $name.StartsWith("Ad", [System.StringComparison]::OrdinalIgnoreCase))) {
        Copy-Item $_.FullName -Destination $contentsDir -Force
        Write-Host "   + $name" -ForegroundColor Gray
    }
}

# Copy Data (device_library.json + Blocks)
$sourceData = Join-Path $workspaceRoot "Data"
if (Test-Path $sourceData) {
    # File JSON
    $sourceJson = Join-Path $sourceData "device_library.json"
    if (Test-Path $sourceJson) {
        Copy-Item $sourceJson -Destination $dataDir -Force
        Write-Host "   + Data/device_library.json" -ForegroundColor Gray
    }
    # Thu muc Blocks DWG
    $sourceBlocks = Join-Path $sourceData "Blocks"
    if (Test-Path $sourceBlocks) {
        Copy-Item $sourceBlocks -Destination (Join-Path $dataDir "Blocks") -Recurse -Force
        Write-Host "   + Data/Blocks/ (*.dwg)" -ForegroundColor Gray
    }
}

# 4. Sao chep PackageContents.xml, Install.bat, Uninstall.bat, HDSD.txt
Write-Host "`n[4/5] Sao chep file cau hinh bundle va script cai dat..." -ForegroundColor Green
$deployDir = Join-Path $workspaceRoot "Deployment"

Copy-Item (Join-Path $deployDir "PackageContents.xml") -Destination $bundleDir -Force
Copy-Item (Join-Path $deployDir "Install.bat") -Destination $releaseStaging -Force
Copy-Item (Join-Path $deployDir "Uninstall.bat") -Destination $releaseStaging -Force
Copy-Item (Join-Path $deployDir "HDSD.txt") -Destination $releaseStaging -Force

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
