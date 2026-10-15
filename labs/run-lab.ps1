#Requires -Version 5.1
<#
.SYNOPSIS
    Chấm tự động lab L02–L06 trên AutoCAD THẬT (accoreconsole), không mở giao diện AutoCAD.

.DESCRIPTION
    1. Build labs\Labs.csproj (bài làm của bạn; -Solution: lời giải mẫu trong labs\LoiGiai).
    2. Mỗi lab chạy trong một phiên accoreconsole /isolate riêng (profile trong %TEMP%: KHÔNG chạm
       profile/registry AutoCAD của bạn). L03 chạy mỗi thí nghiệm một phiên, vì TN2 và TN3 làm AutoCAD dừng hẳn.
    3. Đọc các dòng LAB|… mà lệnh LAB_GRADE_* in ra (labs\Grader\Grade.cs) và in kết quả.

    EXIT CODES: 0 = không có kiểm tra nào FAIL · 1 = có FAIL / thiếu kết quả / build lỗi · 2 = không có accoreconsole.

.PARAMETER Lab
    L02, L03, L04, L05, L06, hoặc All (mặc định).

.PARAMETER Only
    Chỉ với L03: chạy vài thí nghiệm, ví dụ -Only TN2,TN3.

.PARAMETER Solution
    Chấm lời giải mẫu thay cho bài làm (người bảo trì dùng để kiểm chính bộ chấm).

.PARAMETER SaveSample
    Lưu bản vẽ mẫu (LAB_SAMPLE) ra labs\out\lab-sample.dwg để mở trong AutoCAD, rồi dừng.

.PARAMETER FrameworkRoot
    Thư mục chứa mã nguồn framework (Core\, Utils\, Extensions\) để chấm. Mặc định: repo chứa labs\.
    Lab sửa backlog (L05 phần 2, L06): trỏ vào worktree tạo từ v1.0.0 (T10 quy tắc 6).

.EXAMPLE
    .\labs\run-lab.ps1 -Lab L02
.EXAMPLE
    .\labs\run-lab.ps1 -Lab L03 -Only TN2
.EXAMPLE
    .\labs\run-lab.ps1 -Lab L06 -FrameworkRoot ..\cadcore-lab
#>
param(
    [ValidateSet('L02', 'L03', 'L04', 'L05', 'L06', 'All')][string]$Lab = 'All',
    [ValidateSet('TN1', 'TN2', 'TN3', 'TN4', 'TN5', 'TN6')][string[]]$Only = @(),
    [switch]$Solution,
    [switch]$SaveSample,
    [string]$FrameworkRoot = '',
    [string]$AcCorePath = '',
    [int]$TimeoutSec = 120
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Project = Join-Path $PSScriptRoot 'Labs.csproj'
$Dll = Join-Path $PSScriptRoot 'bin\Debug\net48\CadCoreFramework.Labs.dll'
$OutDir = Join-Path $PSScriptRoot 'out'

if (-not $AcCorePath) {
    # @(...): một bản cài → pipeline trả chuỗi đơn; $x[-1] sẽ là KÝ TỰ cuối (xem T03 A6).
    $candidates = @(Get-ChildItem 'C:\Program Files\Autodesk' -Directory -Filter 'AutoCAD 20*' -ErrorAction SilentlyContinue |
                    Sort-Object Name | ForEach-Object { Join-Path $_.FullName 'accoreconsole.exe' } | Where-Object { Test-Path $_ })
    if ($candidates.Count -gt 0) { $AcCorePath = $candidates[-1] }
}
if (-not $AcCorePath -or -not (Test-Path $AcCorePath)) { Write-Host 'Không tìm thấy accoreconsole.exe (cần AutoCAD 2021 trở lên, bản đầy đủ).'; exit 2 }

$useSolutions = if ($Solution) { 'true' } else { 'false' }
if ($FrameworkRoot) {
    if (-not (Test-Path (Join-Path $FrameworkRoot 'Core\CadContext.cs'))) { Write-Host "Không thấy Core\CadContext.cs trong '$FrameworkRoot'."; exit 1 }
    $FrameworkRoot = (Resolve-Path $FrameworkRoot).Path.TrimEnd('\')   # "…\" ngay trước dấu " làm hỏng tham số dòng lệnh
} else {
    $FrameworkRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
}
Write-Host "Build labs ($(if ($Solution) { 'lời giải mẫu' } else { 'bài làm' })), framework: $FrameworkRoot"
& dotnet build $Project -c Debug --nologo -v q "-p:UseSolutions=$useSolutions" "-p:FrameworkRoot=$FrameworkRoot" | Out-Host
if ($LASTEXITCODE -ne 0) { Write-Host 'Build labs LỖI: sửa lỗi biên dịch ở trên rồi chạy lại.'; exit 1 }
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

# Một phiên accoreconsole: NETLOAD DLL lab, chạy các dòng lệnh, thoát không lưu.
# KHÔNG comment, KHÔNG dòng trống trong .scr (T03 A6).
function Invoke-Session([string]$Name, [string[]]$Lines) {
    $scr = Join-Path $env:TEMP "cadcore_lab_$Name.scr"
    Set-Content -Path $scr -Encoding ASCII -Value (@('SECURELOAD', '0', 'NETLOAD', "`"$Dll`"") + $Lines + @('QUIT', 'Y'))
    $profileDir = Join-Path $env:TEMP 'CadCoreFramework_accoreconsole_profile'
    # accoreconsole ghi Drawing1.dwg / log vào thư mục hiện hành → chạy trong thư mục tạm.
    $workDir = Join-Path $env:TEMP 'CadCoreFramework_lab_work'
    New-Item -ItemType Directory -Force -Path $workDir | Out-Null
    $info = New-Object System.Diagnostics.ProcessStartInfo $AcCorePath
    $info.Arguments = "/isolate CadCoreLabs `"$profileDir`" /s `"$scr`" /l en-US"
    $info.WorkingDirectory = $workDir
    $info.UseShellExecute = $false
    $info.RedirectStandardOutput = $true
    $info.StandardOutputEncoding = [System.Text.Encoding]::Unicode   # output accoreconsole là UTF-16 LE
    # stdin là một pipe để MỞ: script hết dòng mà lệnh còn chờ nhập (bài làm hỏi thừa điểm) thì accoreconsole
    # chờ tới hết giờ rồi bị dừng. Nếu stdin đóng (EOF), accoreconsole crash ngay tại prompt.
    $info.RedirectStandardInput = $true
    $process = [System.Diagnostics.Process]::Start($info)
    $reading = $process.StandardOutput.ReadToEndAsync()
    $timedOut = -not $process.WaitForExit($TimeoutSec * 1000)
    if ($timedOut) { $process.Kill(); $process.WaitForExit() }
    $text = $reading.Result
    # AutoCAD crash (thí nghiệm L03 cố ý gây ra) mở hộp thoại Error Report (CER, senddmp.exe) đòi gửi báo cáo cho
    # Autodesk. Đóng đúng hộp thoại do phiên này mở: không gửi gì, và không để nó giữ khóa DLL lab.
    Get-CimInstance Win32_Process -Filter "Name='senddmp.exe'" -ErrorAction SilentlyContinue |
        Where-Object { $_.ParentProcessId -eq $process.Id } |
        ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
    Remove-Item $scr -Force -ErrorAction SilentlyContinue
    $log = Join-Path $OutDir "$Name.log"
    [System.IO.File]::WriteAllText($log, $text, [System.Text.Encoding]::UTF8)
    [pscustomobject]@{ Name = $Name; Text = $text; TimedOut = $timedOut; Log = $log }
}

if ($SaveSample) {
    $dwg = Join-Path $OutDir 'lab-sample.dwg'
    Remove-Item $dwg -Force -ErrorAction SilentlyContinue
    $session = Invoke-Session 'sample' @('LAB_SAMPLE', 'FILEDIA', '0', '_.SAVEAS', '2018', "`"$dwg`"")
    $note = ($session.Text -split "`r?`n" | Where-Object { $_ -like 'LAB_SAMPLE*' } | Select-Object -First 1)
    if ($note) { Write-Host $note.Trim() }
    if (Test-Path $dwg) { Write-Host "Đã lưu bản vẽ mẫu: $dwg"; exit 0 }
    Write-Host "Không lưu được bản vẽ mẫu. Log: $($session.Log)"; exit 1
}

# Mỗi phiên: tên, các dòng gõ vào accoreconsole, và (L03) thông báo fatal error mà thí nghiệm PHẢI gây ra.
$sessions = @()
$labs = if ($Lab -eq 'All') { @('L02', 'L03', 'L04', 'L05', 'L06') } else { @($Lab) }
foreach ($l in $labs) {
    switch ($l) {
        'L03' {
            $tns = if ($Only.Count -gt 0) { $Only } else { @('TN1', 'TN2', 'TN3', 'TN4', 'TN5', 'TN6') }
            foreach ($tn in $tns) {
                $crash = switch ($tn) { 'TN2' { 'eNotOpenForWrite' } 'TN3' { '.' } default { $null } }
                $sessions += [pscustomobject]@{ Name = "L03-$tn"; Lines = @("LAB_GRADE_L03_$tn"); Crash = $crash }
            }
        }
        # Hai điểm cho Run, hai điểm cho RunRaw — khớp TypedStart/TypedEnd trong Grader\GradeL04.cs.
        'L04' { $sessions += [pscustomobject]@{ Name = 'L04'; Lines = @('LAB_GRADE_L04', '10,0', '20,0', '10,0', '20,0'); Crash = $null } }
        default { $sessions += [pscustomobject]@{ Name = $l; Lines = @("LAB_GRADE_$l"); Crash = $null } }
    }
}

$pass = 0; $fail = 0; $skip = 0
foreach ($s in $sessions) {
    Write-Host ""
    Write-Host "── $($s.Name) " -NoNewline; Write-Host ('─' * [Math]::Max(1, 50 - $s.Name.Length))
    $session = Invoke-Session $s.Name $s.Lines
    $lines = @($session.Text -split "`r?`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ -like 'LAB|*' })
    $countLine = $lines | Where-Object { $_ -like 'LAB|COUNT|*' } | Select-Object -First 1
    $results = @($lines | Where-Object { $_ -match '^LAB\|[^|]+\|(PASS|FAIL|SKIP)' })
    $begun = @($lines | Where-Object { $_ -match '^LAB\|[^|]+\|BEGIN$' } | ForEach-Object { $_.Split('|')[1] })
    $fatal = ($session.Text -split "`r?`n" | Where-Object { $_ -match '(INTERNAL|FATAL) ERROR' } | Select-Object -First 1)

    foreach ($r in $results) {
        $parts = $r.Split('|', 4)
        $note = if ($parts.Count -gt 3) { " — $($parts[3])" } else { '' }
        switch ($parts[2]) {
            'PASS' { $pass++; Write-Host "  [PASS] $($parts[1])$note" -ForegroundColor Green }
            'SKIP' { $skip++; Write-Host "  [SKIP] $($parts[1])$note" -ForegroundColor Yellow }
            'FAIL' { $fail++; Write-Host "  [FAIL] $($parts[1])$note" -ForegroundColor Red }
        }
    }
    # Thí nghiệm đã bắt đầu (BEGIN) mà không in kết quả: AutoCAD đã dừng giữa chừng.
    foreach ($id in @($begun | Where-Object { $b = $_; -not ($results | Where-Object { $_.Split('|')[1] -eq $b }) })) {
        $fatalText = if ($fatal) { $fatal.Trim() } else { 'không thấy thông báo lỗi' }
        if ($s.Crash -and $fatal -and $fatal -match $s.Crash) {
            $pass++; Write-Host "  [PASS] $id — AutoCAD dừng hẳn: $fatalText (fatal error: try/catch không bắt được)" -ForegroundColor Green
        } else {
            $fail++; Write-Host "  [FAIL] $id — AutoCAD dừng: $fatalText" -ForegroundColor Red
        }
        $results += "LAB|$id|DONE"
    }

    $problems = @()
    if ($session.TimedOut) { $problems += "hết $TimeoutSec giây mà lệnh chấm chưa xong: bài làm còn chờ nhập? (hỏi nhiều điểm hơn số dòng runner gõ; L04: đúng 2 điểm mỗi hàm)" }
    elseif (-not $countLine) {
        $problems += $(if ($fatal) { "AutoCAD dừng trước khi chấm: $($fatal.Trim())" } else { 'không thấy LAB|COUNT: DLL không nạp được hoặc lệnh chấm không chạy' })
    } elseif ($results.Count -lt [int]$countLine.Split('|')[3]) {
        $why = if ($fatal) { "AutoCAD dừng: $($fatal.Trim())" } else { 'lệnh chấm dừng giữa chừng' }
        $problems += "chỉ có $($results.Count)/$($countLine.Split('|')[3]) kết quả: $why"
    } elseif ($begun.Count -eq 0 -and -not ($lines | Where-Object { $_ -like 'LAB|END|*' })) {
        $problems += 'thiếu LAB|END: lệnh chấm không chạy hết'
    }
    foreach ($p in $problems) { $fail++; Write-Host "  [FAIL] $p — log: $($session.Log)" -ForegroundColor Red }
}

Write-Host ""
Write-Host "Kết quả: $pass đạt, $fail chưa đạt, $skip bỏ qua. Log từng phiên: $OutDir"
if ($fail -gt 0) { exit 1 }
exit 0
