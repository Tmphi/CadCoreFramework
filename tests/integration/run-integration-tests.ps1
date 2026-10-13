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
      1 = accoreconsole returned non-zero, a failure marker was found, OR a command
          did not print its expected success message.
      2 = accoreconsole.exe not found on this machine.
      3 = plugin cannot load in accoreconsole because it references acmgd.dll (UI) —
          see README "Giới hạn". The current single-DLL plugin always hits this.

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
    # @(...): với MỘT bản cài, pipeline trả về chuỗi đơn → $candidates[-1] sẽ là ký tự cuối, không phải đường dẫn.
    $candidates = @(Get-ChildItem 'C:\Program Files\Autodesk' -Directory -Filter 'AutoCAD 20*' -ErrorAction SilentlyContinue |
                  Sort-Object Name |
                  ForEach-Object { Join-Path $_.FullName 'accoreconsole.exe' } |
                  Where-Object { Test-Path $_ })
    if ($candidates.Count -gt 0) { $AcCorePath = $candidates[-1] }  # latest install
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
# smoke.scr KHÔNG có comment / dòng trống: trong file .scr, ";" bị gửi thẳng vào command line
# và dòng trống = ENTER = lặp lại lệnh trước (NETLOAD lặp lại, nuốt tên các lệnh phía sau).
# Đường dẫn giữ nguyên "\" — NETLOAD không hiểu "\\".
$templatePath = Join-Path $ScriptDir 'smoke.scr'
$generatedPath = Join-Path $ScriptDir 'smoke.generated.scr'
$content = (Get-Content $templatePath -Raw).Replace('{{DLL_PATH}}', $DllPath)
Set-Content -Path $generatedPath -Value $content -Encoding ASCII

# ─── 3. Expected output — each command must PROVE it ran ────────────────────
# Chỉ quét lỗi là không đủ: NETLOAD thất bại hay lệnh không tồn tại vẫn cho exit code 0.
$expected = [ordered]@{
    'NETLOAD + DI (CORE_INFO)'  = 'Hạ tầng plugin đã sẵn sàng'
    'CORE_TEST_ALL'             = "Đã tạo Layer 'CORE_TEST'"
    'DRAW_CIRCLE_SAMPLE'        = 'Đã vẽ đường tròn'
    'SAMPLE_GRID (Golden)'      = "Đã vẽ 6 ô trên layer"
    'DEMO_HELP (Demos/)'        = 'DANH MỤC CÁC LỆNH MINH HỌA'
}
$failureMarkers = @('eLockViolation', 'Fatal Error', 'Unhandled Exception', '[ERROR]',
                    'Unable to load', 'Unknown command', '[Lỗi AutoCAD]', '[Lỗi Hệ Thống]')

$logPath = Join-Path $ScriptDir 'smoke.log'
$previousEncoding = [Console]::OutputEncoding
try {
    Write-Host ""
    Write-Host "Running accoreconsole..."
    # accoreconsole ghi stdout dạng UTF-16 LE — không đặt encoding này thì log thành "C o m m a n d"
    # và không marker nào khớp được (test luôn "pass").
    [Console]::OutputEncoding = [System.Text.Encoding]::Unicode
    # /isolate: profile + registry riêng trong %TEMP% → "SECURELOAD 0" của smoke.scr KHÔNG chạm
    # vào profile AutoCAD thật của bạn (SECURELOAD được lưu trong profile).
    $isolatedProfile = Join-Path $env:TEMP 'CadCoreFramework_accoreconsole_profile'
    $output = & $AcCorePath /isolate CadCoreIntegration $isolatedProfile /s $generatedPath /l en-US 2>&1 | Out-String
    $exit = $LASTEXITCODE
    [Console]::OutputEncoding = $previousEncoding

    [System.IO.File]::WriteAllText($logPath, $output, [System.Text.Encoding]::UTF8)

    # 0xC00000FD (stack overflow trong clr.dll) ngay sau NETLOAD = assembly kéo theo acmgd.dll.
    # acmgd.dll (PaletteSet, Ribbon, WPF) KHÔNG nạp được trong accoreconsole — NETLOAD riêng
    # acmgd.dll cũng crash y hệt (đã kiểm chứng trên AutoCAD 2024). Xem README "Giới hạn".
    if ($exit -eq -1073741571) {
        Write-Host ""
        Write-Host "accoreconsole crashed (0xC00000FD) while loading the plugin: the DLL references acmgd.dll"
        Write-Host "(UI: PaletteSet/Ribbon), which cannot load in the headless console. See tests/integration/README.md."
        exit 3
    }

    # ─── 4. Verify ──────────────────────────────────────────────────────────
    $problems = @()
    if ($exit -ne 0) { $problems += "accoreconsole exited with code $exit" }
    foreach ($m in $failureMarkers) {
        if ($output.Contains($m)) { $problems += "failure marker: $m" }
    }

    Write-Host ""
    foreach ($name in $expected.Keys) {
        if ($output.Contains($expected[$name])) {
            Write-Host "  PASS  $name"
        } else {
            Write-Host "  FAIL  $name  (missing: '$($expected[$name])')"
            $problems += "missing output for $name"
        }
    }

    Write-Host ""
    if ($problems.Count -gt 0) {
        Write-Host "Integration smoke tests FAILED — full log: $logPath"
        $problems | ForEach-Object { Write-Host "  - $_" }
        exit 1
    }

    Write-Host "Integration smoke tests passed ($($expected.Count) checks). Log: $logPath"
    exit 0
}
finally {
    [Console]::OutputEncoding = $previousEncoding
    if (Test-Path $generatedPath) { Remove-Item $generatedPath -Force }
}
