#Requires -Version 5.1
<#
.SYNOPSIS
    Initializes a cloned CadCoreFramework template: regenerates GUIDs and optionally
    renames the entire project to your own name.

.DESCRIPTION
    Run once after cloning the repository directly (without `dotnet new`).

    Without -ProjectName: regenerates only the hard-coded GUIDs (ProductCode,
    UpgradeCode, Assembly Guid, solution GUID, project GUIDs) so that each
    cloned copy has a unique identity on AutoCAD.

    With -ProjectName: additionally replaces every occurrence of "CadCoreFramework"
    (in file contents, file names, and directory names) with your chosen name,
    then removes template-only artifacts (.template.config, code_review_report.md,
    and this script itself).

    If you used `dotnet new cad-plugin`, the template engine handled all of this
    automatically — you do NOT need to run this script.

.PARAMETER ProjectName
    Optional. New project/assembly name (e.g. "MyElectricalPlugin").
    Must start with a letter and contain only letters, digits, underscores, or dots.

.PARAMETER GitHubRepo
    Optional. "owner/name" of your GitHub repo — replaces the YOUR_ORG/YOUR_REPO
    placeholder in the README CI badge and the CAD001 analyzer help link.

.PARAMETER RemoveDemos
    Optional. Deletes the Demos/ folder (all DEMO_* commands). Same effect as
    `dotnet new cad-plugin --IncludeDemos false`.

.EXAMPLE
    # Regenerate GUIDs only (keep "CadCoreFramework" as the name):
    .\init-template.ps1

.EXAMPLE
    # Full rename + GUID regeneration, real project without demos:
    .\init-template.ps1 -ProjectName "MyElectricalPlugin" -GitHubRepo "acme/electrical-plugin" -RemoveDemos
#>
param(
    [ValidatePattern('^[A-Za-z][A-Za-z0-9_.]*$')]
    [string]$ProjectName = '',

    [ValidatePattern('^$|^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$')]
    [string]$GitHubRepo = '',

    [switch]$RemoveDemos
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$SourceName = 'CadCoreFramework'
$Root = $PSScriptRoot

function New-UpperGuid { return [System.Guid]::NewGuid().ToString('D').ToUpper() }

# Extensions whose text content is rewritten during rename.
$TextExts = '*.cs','*.csproj','*.sln','*.xaml','*.md','*.json','*.xml',
            '*.ps1','*.yaml','*.yml','*.txt','*.config'

# Directories skipped entirely (binary output, git history, etc.)
$SkipDirs = '.git','bin','obj','dist','coverage','packages'

function Should-Skip([string]$path) {
    foreach ($d in $SkipDirs) {
        if ($path -like "*\$d\*" -or $path -like "*/$d/*") { return $true }
    }
    return $false
}

# ─── 1. Regenerate all hard-coded GUIDs ──────────────────────────────────────
Write-Host 'Regenerating GUIDs...'

# PackageContents.xml — ProductCode & UpgradeCode
$pkgFile = Join-Path $Root 'Deployment\PackageContents.xml'
if (Test-Path $pkgFile) {
    $c = Get-Content $pkgFile -Raw -Encoding UTF8
    $newProduct = New-UpperGuid
    $newUpgrade = New-UpperGuid
    $c = $c -replace 'ProductCode="\{[0-9A-Fa-f\-]+\}"', "ProductCode=""{$newProduct}"""
    $c = $c -replace 'UpgradeCode="\{[0-9A-Fa-f\-]+\}"', "UpgradeCode=""{$newUpgrade}"""
    [System.IO.File]::WriteAllText($pkgFile, $c, [System.Text.Encoding]::UTF8)
    Write-Host "  PackageContents.xml  ProductCode={$newProduct}"
    Write-Host "  PackageContents.xml  UpgradeCode={$newUpgrade}"

    # WiX installer shares the bundle's UpgradeCode — keep them in sync
    $wxsFile = Join-Path $Root "Deployment\installer\$SourceName.wxs"
    if (Test-Path $wxsFile) {
        $w = Get-Content $wxsFile -Raw -Encoding UTF8
        $w = $w -replace 'UpgradeCode="[0-9A-Fa-f\-]{36}"', "UpgradeCode=""$newUpgrade"""
        [System.IO.File]::WriteAllText($wxsFile, $w, [System.Text.Encoding]::UTF8)
        Write-Host "  $SourceName.wxs  UpgradeCode=$newUpgrade"
    }
}

# AssemblyInfo.cs — [assembly: Guid("...")]
$asmFile = Join-Path $Root 'Properties\AssemblyInfo.cs'
if (Test-Path $asmFile) {
    $c = Get-Content $asmFile -Raw -Encoding UTF8
    $newAsmGuid = [System.Guid]::NewGuid().ToString('D')
    $c = $c -replace '(\[assembly:\s*Guid\(")[0-9A-Fa-f\-]+("\)\])', "`${1}$newAsmGuid`${2}"
    [System.IO.File]::WriteAllText($asmFile, $c, [System.Text.Encoding]::UTF8)
    Write-Host "  AssemblyInfo.cs      Guid=$newAsmGuid"
}

# Solution file — project GUIDs and SolutionGuid
$slnFile = Get-ChildItem -Path $Root -Filter '*.sln' -File | Select-Object -First 1
if ($slnFile) {
    $c = Get-Content $slnFile.FullName -Raw -Encoding UTF8
    # Replace each project GUID occurrence with a fresh one
    $projectGuidPattern = '\{[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}\}'
    $matches = [regex]::Matches($c, $projectGuidPattern) |
               Select-Object -ExpandProperty Value -Unique |
               Where-Object { $_ -ne '{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}' }  # keep VS type GUID
    foreach ($old in $matches) {
        $new = '{' + (New-UpperGuid) + '}'
        $c = $c.Replace($old, $new)
    }
    [System.IO.File]::WriteAllText($slnFile.FullName, $c, [System.Text.Encoding]::UTF8)
    Write-Host "  $($slnFile.Name)  — $($matches.Count) project/solution GUIDs refreshed"
}

# ─── 1b. GitHub repo placeholder (README badge, analyzer help link) ──────────
if ($GitHubRepo) {
    Write-Host ""
    Write-Host "Setting GitHub repo → $GitHubRepo..."
    Get-ChildItem -Path $Root -Recurse -Include $TextExts -File |
        Where-Object { -not (Should-Skip $_.FullName) } |
        ForEach-Object {
            $raw = Get-Content -Path $_.FullName -Raw -Encoding UTF8 -ErrorAction SilentlyContinue
            if ($null -eq $raw -or -not $raw.Contains('YOUR_ORG/YOUR_REPO')) { return }
            [System.IO.File]::WriteAllText($_.FullName, $raw.Replace('YOUR_ORG/YOUR_REPO', $GitHubRepo), [System.Text.Encoding]::UTF8)
            Write-Host "  content  $($_.FullName.Substring($Root.Length + 1))"
        }
}

# ─── 1c. Demo suite (Demos/ is self-contained — nothing else references it) ──
if ($RemoveDemos) {
    $demos = Join-Path $Root 'Demos'
    if (Test-Path $demos) { Remove-Item $demos -Recurse -Force; Write-Host "  removed  Demos\ (DEMO_* commands)" }
}

# ─── 2. Full rename (only when -ProjectName is supplied) ─────────────────────
if ($ProjectName -and $ProjectName -ne $SourceName) {
    Write-Host ""
    Write-Host "Renaming '$SourceName' → '$ProjectName'..."

    # 2a. Rewrite file contents
    Get-ChildItem -Path $Root -Recurse -Include $TextExts -File |
        Where-Object { -not (Should-Skip $_.FullName) } |
        ForEach-Object {
            $raw = Get-Content -Path $_.FullName -Raw -Encoding UTF8 -ErrorAction SilentlyContinue
            if ($null -eq $raw) { return }
            $updated = $raw -replace [regex]::Escape($SourceName), $ProjectName
            if ($updated -ne $raw) {
                [System.IO.File]::WriteAllText($_.FullName, $updated, [System.Text.Encoding]::UTF8)
                Write-Host "  content  $($_.FullName.Substring($Root.Length + 1))"
            }
        }

    # 2b. Rename files
    Get-ChildItem -Path $Root -Recurse -File |
        Where-Object { (-not (Should-Skip $_.FullName)) -and ($_.Name -like "*$SourceName*") } |
        ForEach-Object {
            $newName = $_.Name -replace [regex]::Escape($SourceName), $ProjectName
            Rename-Item -LiteralPath $_.FullName -NewName $newName
            Write-Host "  file     $($_.Name)  →  $newName"
        }

    # 2c. Rename directories (deepest first to avoid path conflicts)
    Get-ChildItem -Path $Root -Recurse -Directory |
        Where-Object { (-not (Should-Skip $_.FullName)) -and ($_.Name -like "*$SourceName*") } |
        Sort-Object { $_.FullName.Length } -Descending |
        ForEach-Object {
            $newName = $_.Name -replace [regex]::Escape($SourceName), $ProjectName
            $newPath = Join-Path $_.Parent.FullName $newName
            if (-not (Test-Path $newPath)) {
                Rename-Item -LiteralPath $_.FullName -NewName $newName
                Write-Host "  dir      $($_.Name)\  →  $newName\"
            }
        }

    # 2d. Remove template-only artifacts
    Write-Host "Removing template artifacts..."
    foreach ($a in @('.template.config', 'code_review_report.md')) {
        $t = Join-Path $Root $a
        if (Test-Path $t) { Remove-Item $t -Recurse -Force; Write-Host "  removed  $a" }
    }

    # Remove this script last (it lives at $Root\init-template.ps1)
    $self = $MyInvocation.MyCommand.Path
    if (Test-Path $self) { Remove-Item $self -Force; Write-Host "  removed  init-template.ps1" }
}

Write-Host ""
$displayName = if ($ProjectName) { $ProjectName } else { $SourceName }
Write-Host "Done. Next: dotnet build $displayName.sln -c Release"
