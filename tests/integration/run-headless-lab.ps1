#Requires -Version 5.1
<#
.SYNOPSIS
    Chạy HEADLESS LAB: kiểm thử các helper lõi của framework trên AutoCAD THẬT (accoreconsole).

.DESCRIPTION
    Plugin chính tham chiếu AcMgd nên không nạp được trong accoreconsole (xem README.md cạnh file này).
    HeadlessLab.csproj LINK mã nguồn không phụ thuộc UI (Extensions/, Utils/) vào một DLL test chỉ tham
    chiếu AcDbMgd + AcCoreMgd. Script này:
      1. Build HeadlessLab (Debug, net48).
      2. Chạy accoreconsole với /isolate (profile riêng trong %TEMP%: SECURELOAD 0 KHÔNG chạm profile thật).
      3. Gọi LAB_ALL, đọc output UTF-16, yêu cầu: có LAB|COUNT|n, đủ n dòng PASS, có LAB|DONE.

    EXIT CODES: 0 = mọi kiểm thử PASS · 1 = có FAIL / thiếu kết quả / build lỗi · 2 = không có accoreconsole.

.PARAMETER AcCorePath
    Đường dẫn accoreconsole.exe. Bỏ trống: tự tìm bản AutoCAD 20xx mới nhất.

.EXAMPLE
    .\tests\integration\run-headless-lab.ps1
#>
param([string]$AcCorePath = '')

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$Project = Join-Path $PSScriptRoot 'HeadlessLab\HeadlessLab.csproj'
$Dll = Join-Path $PSScriptRoot 'HeadlessLab\bin\Debug\net48\CadCoreFramework.HeadlessLab.dll'

if (-not $AcCorePath) {
    # HeadlessLab là net48, build với API AutoCAD 2024 (R24): chỉ AutoCAD 2021–2024 (.NET Framework 4.8).
    # AutoCAD 2025+ chạy .NET 8, nên không bao giờ tự chọn nó, kể cả khi máy cài cả hai.
    # @(...): một bản cài → pipeline trả chuỗi đơn; $x[-1] sẽ là KÝ TỰ cuối (xem T03 A6).
    $candidates = @(Get-ChildItem 'C:\Program Files\Autodesk' -Directory -Filter 'AutoCAD 20*' -ErrorAction SilentlyContinue |
                    Where-Object { $_.Name -match '^AutoCAD (\d{4})$' -and [int]$Matches[1] -ge 2021 -and [int]$Matches[1] -le 2024 } |
                    Sort-Object Name | ForEach-Object { Join-Path $_.FullName 'accoreconsole.exe' } | Where-Object { Test-Path $_ })
    if ($candidates.Count -gt 0) { $AcCorePath = $candidates[-1] }
}
if (-not $AcCorePath -or -not (Test-Path $AcCorePath)) { Write-Host 'accoreconsole.exe of AutoCAD 2021–2024 not found (AutoCAD 2025+ runs .NET 8 and cannot load the net48 lab DLL).'; exit 2 }

Write-Host "Building HeadlessLab…"
& dotnet build $Project -c Debug --nologo -v q | Out-Null
if ($LASTEXITCODE -ne 0) { Write-Host 'HeadlessLab build FAILED.'; exit 1 }

$scr = Join-Path $env:TEMP 'cadcore_headless_lab.scr'
# KHÔNG comment, KHÔNG dòng trống trong .scr (T03 A6). Đường dẫn giữ nguyên "\".
# Hai 'No' ngay sau LAB_ALL: câu trả lời cho hai prompt THẬT của kiểm thử KEYWORDS_PROMPT_LISTS_ONCE (B-33).
Set-Content -Path $scr -Encoding ASCII -Value @('SECURELOAD', '0', 'NETLOAD', "`"$Dll`"", 'LAB_ALL', 'No', 'No', 'QUIT', 'Y')

$profileDir = Join-Path $env:TEMP 'CadCoreFramework_accoreconsole_profile'
# accoreconsole đặt bản vẽ mặc định (Drawing1.dwg) và plot.log của các kiểm thử in ấn vào THƯ MỤC HIỆN HÀNH
# → chạy trong thư mục tạm để không xả rác vào nơi người dùng gọi script (thường là gốc repo).
$workDir = Join-Path $env:TEMP 'CadCoreFramework_headless_lab_work'
New-Item -ItemType Directory -Force -Path $workDir | Out-Null
$logPath = Join-Path $PSScriptRoot 'headless-lab.log'
$previous = [Console]::OutputEncoding
Push-Location $workDir
try {
    [Console]::OutputEncoding = [System.Text.Encoding]::Unicode      # output accoreconsole là UTF-16 LE
    $output = & $AcCorePath /isolate CadCoreIntegration $profileDir /s $scr /l en-US 2>&1 | Out-String
    $exit = $LASTEXITCODE
}
finally {
    Pop-Location
    [Console]::OutputEncoding = $previous
    Remove-Item $scr -Force -ErrorAction SilentlyContinue
}
[System.IO.File]::WriteAllText($logPath, $output, [System.Text.Encoding]::UTF8)

$lines = @($output -split "`r?`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ -like 'LAB|*' })
$countLine = $lines | Where-Object { $_ -like 'LAB|COUNT|*' } | Select-Object -First 1
$results = @($lines | Where-Object { $_ -match '^LAB\|[^|]+\|(PASS|FAIL)' -and $_ -notlike 'LAB|COUNT|*' })
$fails = @($results | Where-Object { $_ -match '\|FAIL' })
$passes = @($results | Where-Object { $_ -match '\|PASS$' })

Write-Host ""
$results | ForEach-Object { Write-Host "  $_" }
Write-Host ""

$problems = @()
if ($exit -ne 0) { $problems += "accoreconsole exit code $exit" }
if (-not $countLine) { $problems += 'thiếu LAB|COUNT (DLL không nạp được hoặc LAB_ALL không chạy)' }
elseif ($passes.Count -ne [int]($countLine.Split('|')[2])) { $problems += "PASS $($passes.Count)/$($countLine.Split('|')[2])" }
if (-not ($lines -contains 'LAB|DONE')) { $problems += 'thiếu LAB|DONE' }
if ($fails.Count -gt 0) { $problems += "$($fails.Count) kiểm thử FAIL" }
# B-33: chỉ output mới cho thấy câu nhắc AutoCAD thật sự in ra — danh sách từ khóa phải xuất hiện đúng MỘT lần.
foreach ($prompt in 'LAB keyword prompt [Yes/No] <Yes>:', 'LAB scope keyword prompt [Yes/No] <Yes>:') {
    if ($output -notmatch [regex]::Escape($prompt)) { $problems += "B-33: không thấy câu nhắc `"$prompt`"" }
}
if ($output -match [regex]::Escape('[Yes/No] [Yes/No]')) { $problems += 'B-33: danh sách từ khóa bị in hai lần' }

if ($problems.Count -gt 0) {
    Write-Host "Headless lab FAILED — log: $logPath"
    $problems | ForEach-Object { Write-Host "  - $_" }
    exit 1
}
Write-Host "Headless lab passed ($($passes.Count) checks). Log: $logPath"
exit 0
