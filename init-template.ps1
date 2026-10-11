#Requires -Version 5.1
<#
.SYNOPSIS
    Khởi tạo dự án từ template CadCoreFramework: tái tạo tất cả GUID để tránh xung đột
    khi nhiều instance của template cùng chạy trên một máy AutoCAD.

.DESCRIPTION
    Script này thay thế:
      - ProductCode  trong Deployment/PackageContents.xml
      - UpgradeCode  trong Deployment/PackageContents.xml
      - Guid          trong Properties/AssemblyInfo.cs

    Chạy một lần duy nhất sau khi clone/copy repo để đặt danh tính riêng cho dự án.

.EXAMPLE
    .\init-template.ps1
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDir = $PSScriptRoot

function New-UpperGuid { return [System.Guid]::NewGuid().ToString("D").ToUpper() }

# ──────────────────────────────────────────────────────────────────────────────
#  1. PackageContents.xml — ProductCode & UpgradeCode
# ──────────────────────────────────────────────────────────────────────────────
$pkgFile = Join-Path $scriptDir "Deployment\PackageContents.xml"
if (-not (Test-Path $pkgFile)) {
    Write-Error "Không tìm thấy: $pkgFile"
}

$pkgContent = Get-Content $pkgFile -Raw -Encoding UTF8

$newProduct = New-UpperGuid
$newUpgrade = New-UpperGuid

# Thay thế ProductCode="{...}" và UpgradeCode="{...}"
$pkgContent = $pkgContent -replace 'ProductCode="\{[0-9A-Fa-f\-]+\}"', "ProductCode=""{$newProduct}"""
$pkgContent = $pkgContent -replace 'UpgradeCode="\{[0-9A-Fa-f\-]+\}"', "UpgradeCode=""{$newUpgrade}"""

[System.IO.File]::WriteAllText($pkgFile, $pkgContent, [System.Text.Encoding]::UTF8)
Write-Host "PackageContents.xml — ProductCode  : {$newProduct}"
Write-Host "PackageContents.xml — UpgradeCode  : {$newUpgrade}"

# ──────────────────────────────────────────────────────────────────────────────
#  2. AssemblyInfo.cs — Guid
# ──────────────────────────────────────────────────────────────────────────────
$asmFile = Join-Path $scriptDir "Properties\AssemblyInfo.cs"
if (-not (Test-Path $asmFile)) {
    Write-Warning "Không tìm thấy: $asmFile — bỏ qua."
} else {
    $asmContent = Get-Content $asmFile -Raw -Encoding UTF8
    $newAssemblyGuid = [System.Guid]::NewGuid().ToString("D")

    # Thay thế [assembly: Guid("...")]
    $asmContent = $asmContent -replace '(\[assembly:\s*Guid\(")[0-9A-Fa-f\-]+("\)\])', "`${1}$newAssemblyGuid`${2}"

    [System.IO.File]::WriteAllText($asmFile, $asmContent, [System.Text.Encoding]::UTF8)
    Write-Host "AssemblyInfo.cs     — Guid         : $newAssemblyGuid"
}

Write-Host ""
Write-Host "Xong! Hãy mở solution và build lại dự án."
