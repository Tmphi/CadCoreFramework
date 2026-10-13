> **🎓 Học viện CadCoreFramework** · [Lộ trình](README.md) · [← T03](T03_PITFALLS_CASEBOOK.md) · **T04** · [T05 →](T05_TESTING_STRATEGY.md)

# T04 — Debug & Chẩn Đoán

Debug plugin AutoCAD khó hơn debug ứng dụng thường. Code chạy *bên trong* một tiến trình lớn của hãng khác, crash nhiều khi không có stack trace, và lỗi phụ thuộc vào bản vẽ, phiên bản AutoCAD, plugin khác đang chạy. Module này trang bị **quy trình** chẩn đoán, không chỉ công cụ.

> [!IMPORTANT]
> **Quy trình chẩn đoán chuẩn** (đã dùng để tìm ra [A5](T03_PITFALLS_CASEBOOK.md#a5-accoreconsole-crash-0xc00000fd-khi-netload-plugin), [A6](T03_PITFALLS_CASEBOOK.md#a6-integration-test-báo-passed-dù-không-lệnh-nào-chạy), [A13](T03_PITFALLS_CASEBOOK.md#a13-palette-luôn-theme-sáng-dù-autocad-đang-tối)):
> 1. **Tái hiện** ổn định, ghi lại các bước.
> 2. **Đọc dữ liệu thật** (log, output, sysvar, metadata), đừng đoán.
> 3. **Thu hẹp**: bỏ dần biến số cho tới khi còn nhỏ nhất vẫn lỗi.
> 4. **Đặt giả thuyết → thí nghiệm để bác bỏ** nó.
> 5. Sửa, rồi **chứng minh** bản sửa (test fail trước, pass sau).
> 6. Ghi **incident report** ([§8](#8-mẫu-báo-cáo-sự-cố-incident-report)) và thêm mục vào T03 nếu đáng.

---

## 1. Hộp đồ nghề chẩn đoán

| Công cụ | Dùng cho | Mục |
| :--- | :--- | :---: |
| Visual Studio, Attach to Process `acad.exe` | Breakpoint, xem biến, bắt exception lúc ném | §3 |
| File log `%AppData%\CadCoreFramework\Logs\` | Lịch sử khởi động, lỗi đã bắt | §4 |
| Lệnh `CORE_DEBUG` (Debug Inspector) | Lịch sử lệnh, DI services, subscriber | §4 |
| Event Viewer (Application log) | Crash không có thông báo | §5 |
| accoreconsole `/isolate` | Thí nghiệm an toàn: sysvar, script | §5.3 |
| Grep / reflection metadata DLL | Kiểm chứng API có tồn tại | §6 |
| `AppDomain.GetAssemblies()`, Fusion log | Lỗi nạp DLL, xung đột version | §7 |

---

## 2. Vòng lặp phát triển: build → nạp → thử

```powershell
dotnet build CadCoreFramework.csproj -c Debug -f net48     # chỉ plugin, nhanh hơn build cả solution
```

- Output: `bin\Debug\net48\CadCoreFramework_<timestamp>.dll` + bản copy `CadCoreFramework.dll` (và `.pdb`).
- Nạp bằng `NETLOAD`, hoặc bằng tool nạp từ byte array của nhóm (không khóa file, nạp lại được nhiều lần).
- Nhờ tên assembly có timestamp, **mỗi lần build là một assembly mới**, nên nạp lại được mà không tắt AutoCAD ([T02 §2.1](T02_ARCHITECTURE_DEEP_DIVE.md#21-hot-reload-tên-assembly-có-timestamp)).

> [!WARNING]
> **Nạp lại không "thay thế" bản cũ.** Bản cũ vẫn nằm trong AppDomain: `IExtensionApplication` của nó đã chạy, event nó hook vẫn còn, lệnh cùng tên bị bản mới đè. Sau nhiều lần nạp, hành vi có thể "lạ". **Khởi động lại AutoCAD** khi:
> - Sửa code khởi động (`AppBootstrapper`), event, Overrule, Ribbon.
> - Thấy hành vi không giải thích được.
> - Trước khi xác nhận một bản sửa là đúng (luôn kiểm trên phiên AutoCAD sạch).

---

## 3. Gắn debugger vào AutoCAD

### 3.1 Attach to Process

1. Mở AutoCAD, nạp plugin.
2. Visual Studio → **Debug → Attach to Process…** → chọn `acad.exe`.
3. **Code type:** `Managed (.NET Framework 4.x)` (AutoCAD 2021–2024). Nếu sau này làm AutoCAD 2025+ thì chọn `Managed (.NET Core, .NET 5+)`.
4. Đặt breakpoint, gõ lệnh trong AutoCAD.

### 3.2 Khởi chạy AutoCAD từ Visual Studio (tùy chọn)

Tạo `Properties/launchSettings.json` (file cá nhân, **không commit**):

```json
{
  "profiles": {
    "AutoCAD 2024": {
      "commandName": "Executable",
      "executablePath": "C:\\Program Files\\Autodesk\\AutoCAD 2024\\acad.exe",
      "commandLineArgs": "/nologo"
    }
  }
}
```

F5 sẽ mở AutoCAD và gắn debugger sẵn. Sau đó `NETLOAD` như thường.

### 3.3 Breakpoint không dính?

| Dấu hiệu | Kiểm tra |
| :--- | :--- |
| *"The breakpoint will not currently be hit. No symbols have been loaded"* | **Debug → Windows → Modules**: assembly `CadCoreFramework_<timestamp>` có trong danh sách không, symbol đã nạp chưa |
| Nạp bằng byte array | Tool phải nạp kèm PDB (`Assembly.Load(rawAssembly, rawSymbolStore)`), hoặc chỉ cho VS đường dẫn PDB bằng chuột phải module → *Load Symbols*. Khi cần debug sâu, dùng `NETLOAD` trực tiếp cho đơn giản |
| Đang chạy bản cũ | Có nhiều `CadCoreFramework_*` trong Modules. Khởi động lại AutoCAD |

### 3.4 Bắt exception bị nuốt

**Debug → Windows → Exception Settings** → tích **Common Language Runtime Exceptions**. Debugger dừng ngay khi exception **được ném**, kể cả khi sau đó nó bị một `catch` nuốt mất. Đây là cách nhanh nhất để tìm "thay đổi biến mất không lỗi" ([T03 B3](T03_PITFALLS_CASEBOOK.md#b3-thay-đổi-biến-mất-không-có-lỗi)).

> [!CAUTION]
> Không để breakpoint dừng **bên trong một transaction đang mở** quá lâu rồi thao tác với AutoCAD (click, đổi tab). Bạn đang giữ lock và transaction. Đừng "Stop debugging" giữa transaction rồi tiếp tục dùng phiên AutoCAD đó cho việc quan trọng.

---

## 4. Log, Debug Inspector, CommandHistory

### 4.1 File log

```powershell
# Xem log hôm nay, tự cập nhật khi có dòng mới
Get-Content "$env:APPDATA\CadCoreFramework\Logs\log-$(Get-Date -Format yyyyMMdd).txt" -Tail 30 -Wait
```

Định dạng: `2026-10-13 21:41:23.456 +07:00 [ERR] Ngoại lệ hệ thống khi thực thi lệnh.`, kèm stack trace đầy đủ ở dòng sau.

| Thấy trong log | Ý nghĩa |
| :--- | :--- |
| `[INF] CadCoreFramework initialized. Version: …` | `InitializeCore` chạy hết. Nếu **không** có dòng này, khởi động đã lỗi |
| `[INF] EventService khởi tạo thành công.` | EventService đã hook DocumentManager |
| `[ERR] Ngoại lệ AutoCAD khi thực thi lệnh.` + `Autodesk.AutoCAD.Runtime.Exception: eXxx` | Lỗi API trong một lệnh. Tra [T01 §14](T01_AUTOCAD_NET_FOUNDATIONS.md#14-phụ-lục-errorstatus-thường-gặp) |
| `[WRN] StartUndoMark '…' thất bại …` | COM UndoMark không khả dụng. Lệnh vẫn chạy nhưng không gom Undo |
| `[ERR] Một subscriber của GridDrawnMessage ném exception.` | Bản vẽ **đã commit**. Lỗi ở phía người nhận message |

Mức log: bản **Debug** ghi từ `DBG`, bản **Release** ghi từ `INF`.

### 4.2 Debug Inspector: lệnh `CORE_DEBUG`

| Tab | Cho biết | Dùng khi |
| :--- | :--- | :--- |
| Command History | 50 lệnh gần nhất: tên, thời gian, thành công/thất bại | "Lệnh vừa rồi có chạy không, mất bao lâu?" |
| DI Services | Mọi service đã đăng ký: interface, implementation, lifetime | "Service X có được đăng ký không?" |
| Event Subscribers | Số subscriber theo từng loại message | Nghi rò rỉ subscription: con số tăng mãi sau mỗi lần mở/đóng palette |

---

## 5. Đọc lỗi AutoCAD & phân tích crash

### 5.1 Exception của AutoCAD

`Autodesk.AutoCAD.Runtime.Exception` mang `ErrorStatus`. **Luôn đọc `ErrorStatus` trước, message sau**: tên status nói lên nguyên nhân chính xác hơn. Bảng tra: [T01 §14](T01_AUTOCAD_NET_FOUNDATIONS.md#14-phụ-lục-errorstatus-thường-gặp). `CadCommandBase` in dạng `[Lỗi AutoCAD] LockViolation: eLockViolation`.

### 5.2 Crash không có thông báo

```powershell
# Các sự cố của ứng dụng trong 2 giờ qua
Get-WinEvent -FilterHashtable @{ LogName = 'Application'; StartTime = (Get-Date).AddHours(-2) } |
  Where-Object { $_.ProviderName -in 'Application Error', '.NET Runtime' } |
  Select-Object TimeCreated, ProviderName, @{ n = 'Message'; e = { $_.Message.Substring(0, [Math]::Min(600, $_.Message.Length)) } }
```

| Exception code | Ý nghĩa | Nghi ngờ |
| :--- | :--- | :--- |
| `0xC00000FD` | **Stack overflow** | Đệ quy vô hạn (vd. `AssemblyResolve` tự gọi lại chính nó); nạp `AcMgd` trong accoreconsole ([A5](T03_PITFALLS_CASEBOOK.md#a5-accoreconsole-crash-0xc00000fd-khi-netload-plugin)) |
| `0xE0434352` | Exception .NET không được bắt | Exception thoát khỏi event handler / callback không có try/catch |
| `0xC0000005` | Access violation (truy cập bộ nhớ sai) | Dùng đối tượng đã đóng/hủy, gọi API từ thread nền, Overrule còn treo lúc thoát |

Entry `.NET Runtime` thường có kèm **stack trace managed**, nên đọc nó trước.

> [!TIP]
> **Dump khi cần điều tra sâu:** Windows Error Reporting có thể tự lưu file dump khi `acad.exe` crash (khóa registry `LocalDumps` của WER). Cấu hình này cần quyền admin và ảnh hưởng toàn máy. Chỉ bật khi thật cần, ghi lại cách tắt, và thống nhất với người quản lý máy.

### 5.3 Thí nghiệm an toàn với accoreconsole

Không làm hỏng profile AutoCAD thật nhờ `/isolate`. Ví dụ hỏi giá trị sysvar (đây chính là cách phát hiện lỗi `DARKMODE`):

```powershell
$scr = "$env:TEMP\sysvars.scr"
Set-Content $scr -Encoding ASCII -Value @(
  '(foreach v (list "DARKMODE" "COLORTHEME" "BACKGROUNDPLOT" "SECURELOAD") (princ (strcat "\nSV|" v "|" (vl-princ-to-string (getvar v)))))',
  '(princ)', 'QUIT', 'Y')
[Console]::OutputEncoding = [Text.Encoding]::Unicode           # output accoreconsole là UTF-16
& "C:\Program Files\Autodesk\AutoCAD 2024\accoreconsole.exe" /isolate Lab "$env:TEMP\acprofile" /s $scr /l en-US |
  Where-Object { $_ -match '^SV\|' }
```

Kết quả trên máy kiểm chứng: `DARKMODE|nil`, `COLORTHEME|0`, `BACKGROUNDPLOT|2`, `SECURELOAD|1`.

---

## 6. Kiểm chứng API bằng metadata

Trước khi viết (hoặc tin) "AutoCAD có API X", hãy kiểm tra. Đây là cách [A14](T03_PITFALLS_CASEBOOK.md#a14-changelog-mô-tả-một-api-không-tồn-tại) được phát hiện.

**Cách nhanh (tồn tại hay không):** tên method/kiểu nằm trong metadata dạng chuỗi.

```bash
# Git Bash
grep -c -a "ExecuteInCommandContextAsync" ~/.nuget/packages/autocad.net.core/24.0.0/lib/*/AcCoreMgd.dll
```

**Cách chính xác (chữ ký, enum):** reflection-only trong Windows PowerShell 5.1.

```powershell
$core = (Get-ChildItem "$env:USERPROFILE\.nuget\packages\autocad.net.core\24.0.0\lib" -Directory)[0].FullName
$model = (Get-ChildItem "$env:USERPROFILE\.nuget\packages\autocad.net.model\24.0.0\lib" -Directory)[0].FullName
[AppDomain]::CurrentDomain.add_ReflectionOnlyAssemblyResolve({ param($s, $e)
  $n = (New-Object Reflection.AssemblyName($e.Name)).Name
  foreach ($d in $core, $model) { $p = Join-Path $d "$n.dll"; if (Test-Path $p) { return [Reflection.Assembly]::ReflectionOnlyLoadFrom($p) } }
  [Reflection.Assembly]::ReflectionOnlyLoad($e.Name) })
$a = [Reflection.Assembly]::ReflectionOnlyLoadFrom("$core\AcCoreMgd.dll")
$a.GetType('Autodesk.AutoCAD.ApplicationServices.DocumentCollection').GetMethods() |
  Where-Object Name -like 'ExecuteIn*' | ForEach-Object { $_.ToString() }
[Enum]::GetNames($a.GetType('Autodesk.AutoCAD.Runtime.CommandFlags')) -join ', '
```

Đầu ra thật: `ExecutionResult ExecuteInCommandContextAsync(Func<Object,Task>, Object)` cùng danh sách 25 `CommandFlags` (xem [T01 §6.3](T01_AUTOCAD_NET_FOUNDATIONS.md#63-commandflags)).

---

## 7. Chẩn đoán nạp assembly

| Triệu chứng | Nguyên nhân thường gặp |
| :--- | :--- |
| `FileNotFoundException: Could not load file or assembly 'X'` | DLL X không nằm cạnh plugin (bundle thiếu file, bản net8 thiếu `EnableDynamicLoading`), hoặc nạp từ byte array mà resolve không tìm được |
| `MissingMethodException` / `TypeLoadException` ngay khi khởi động | **Xung đột version với host**: AutoCAD đã nạp DLL cùng identity ([A1](T03_PITFALLS_CASEBOOK.md#a1-serilog-missingmethodexception-ngay-khi-khởi-động)) |
| `InvalidCastException` giữa hai kiểu cùng tên | Hai bản của **cùng một assembly** được nạp ([A2](T03_PITFALLS_CASEBOOK.md#a2-palette-lỗi-does-not-have-a-resource-identified-by-the-uri)) |

Bước kiểm tra đầu tiên: liệt kê assembly đã nạp, *trong* AutoCAD.

```csharp
// Đặt tạm trong một lệnh chẩn đoán (đừng commit), hoặc trong Immediate Window khi đang debug
foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
{
    var n = asm.GetName();
    string loc = asm.IsDynamic ? "(dynamic)" : (string.IsNullOrEmpty(asm.Location) ? "(byte array)" : asm.Location);
    Editor.WriteMessage($"\n{n.Name} {n.Version}  {loc}");
}
```

Đọc kết quả: tìm **cùng tên xuất hiện hai lần** (A2), hoặc **version khác bản plugin mong đợi** (A1). Với .NET Framework còn có *Fusion log* (`fuslogvw.exe` trong Windows SDK) ghi chi tiết từng lần bind. Công cụ này cần quyền admin và làm chậm máy khi bật, nên nhớ tắt sau khi dùng.

---

## 8. Mẫu báo cáo sự cố (incident report)

Mọi lỗi đáng kể phải có báo cáo theo mẫu dưới, đính kèm PR sửa lỗi hoặc lưu ở issue.

```markdown
## [INC-YYYYMMDD-nn] <Tiêu đề ngắn: triệu chứng>

**Mức độ:** Crash / Mất dữ liệu / Sai kết quả / Khó chịu
**Phạm vi:** AutoCAD <phiên bản>, plugin <version/commit>, bản vẽ <đặc điểm>

### Triệu chứng
<Thông điệp nguyên văn, ảnh chụp, dòng log>

### Tái hiện
1. …
2. …
Tỉ lệ tái hiện: 10/10

### Gốc rễ
<Cơ chế thật sự. Kèm bằng chứng: log, output thí nghiệm, đoạn code>

### Vì sao không bị phát hiện sớm hơn
<Test nào thiếu, giả định nào sai>

### Sửa
<PR #, mô tả thay đổi>

### Phòng ngừa
<Test mới / analyzer / quy tắc / mục checklist. Thêm vào T03 nếu tổng quát>
```

Mục "Vì sao không bị phát hiện sớm hơn" là mục **quan trọng nhất** với Senior: nó biến một lỗi thành một cải tiến cho quy trình.

---

## 9. Cây quyết định: triệu chứng → nguyên nhân

| # | Triệu chứng | Kiểm tra theo thứ tự |
| :---: | :--- | :--- |
| 1 | Gõ lệnh báo `Unknown command` | ① Log có `initialized` không ② lệnh có `[assembly: CommandClass]` không ③ có nạp đúng file DLL không (Debug sinh nhiều bản) ④ `SECURELOAD` có chặn không |
| 2 | MessageBox *"CadCoreFramework — Lỗi Khởi Tạo"* | Đọc exception: `MissingMethod`/`TypeLoad` → §7 (xung đột host); `FileNotFound` → thiếu DLL |
| 3 | Palette không mở, lỗi resource URI | [A2](T03_PITFALLS_CASEBOOK.md#a2-palette-lỗi-does-not-have-a-resource-identified-by-the-uri) (nạp byte array) |
| 4 | Palette lỗi `StaticResourceExtension` | [A3](T03_PITFALLS_CASEBOOK.md#a3-show_ui_template-lỗi-provide-value-on-staticresourceextension), đổi sang `DynamicResource` |
| 5 | Palette lỗi "ViewModel chưa được đăng ký" | Thêm `AddTransient<TViewModel>()` trong `ConfigureServices` |
| 6 | Palette sai màu / trắng / không đổi theo theme | ① code-behind có gọi `ThemeManager.Attach(this)` sau `InitializeComponent()` không ② XAML dùng `{DynamicResource}` và key có trong **cả hai** file theme (chạy `XamlPaletteTests`) ③ log có cảnh báo "Không đọc được sysvar COLORTHEME" không. Lịch sử: B-01 (DARKMODE), B-02 (hai hệ màu), B-03 (key không tồn tại), B-23 — sửa ở PR #6 |
| 7 | Lệnh chạy xong nhưng **không thấy gì** | ① đang ở Layout (`CurrentSpace` là Paper Space) ② layer tắt/đóng băng ③ vẽ ở bản vẽ khác (B6) ④ lỗi bị nuốt trong lambda nên rollback (B3) ⑤ từ palette: chưa regen (`autoRegen`) ⑥ chỉ là zoom (`ZOOM E`) |
| 8 | Ctrl+Z gỡ từng phần nhỏ | Thao tác từ palette không có `undoMarkName` ([T01 §11](T01_AUTOCAD_NET_FOUNDATIONS.md#11-undo)) |
| 9 | AutoCAD treo | ① prompt trong transaction (B4) ② `.Result`/`.Wait()` (B9) ③ vòng lặp dài trên main thread ④ hộp thoại modal bị che sau cửa sổ |
| 10 | `eLockViolation` | Ghi từ application context không qua `ICadContext` (B1) |
| 11 | *"Nested CadContext.Execute detected"* | Service gọi Service bên trong lambda; publish message *bên trong* transaction ([T02 §4.6](T02_ARCHITECTURE_DEEP_DIVE.md#46-cadcontextguard-chặn-lồng-nhau)) |
| 12 | Chạy trên máy dev, lỗi trên máy người dùng | ① bundle thiếu DLL ② đường dẫn không tin cậy / `SECURELOAD` ③ phiên bản AutoCAD ngoài `SeriesMin/Max` ④ code giả định đường dẫn (`Assembly.Location`, B30) ⑤ quyền ghi thư mục |
| 13 | Crash | §5.2: đọc exception code. Nghi B8 (thread), B5/B7 (đối tượng đã đóng), B25 (Overrule), B26 (finalizer) |
| 14 | *"… được gọi từ thread N, không phải main thread của AutoCAD"* | Gọi `ICadContext` từ thread nền: `Task.Run`, `System.Timers.Timer`, `ConfigureAwait(false)`, hoặc phần sau `await` không có `SynchronizationContext` ([T03 B8](T03_PITFALLS_CASEBOOK.md#b8-gọi-autocad-api-từ-thread-nền)). Đây là guard `CadThread` báo đúng chỗ — đừng tắt nó |
| 15 | `ArgumentException` *"Tên ký hiệu '…' chứa ký tự không hợp lệ"* | Tên layer/block/style từ người dùng hoặc dữ liệu ngoài chứa `< > / \ " : ; ? * \| , = `` ` ``; validate ở tầng Model (`SymbolNameRules`) trước khi vẽ |
| 16 | Lệnh async **không vẽ gì** khi người dùng chuyển bản vẽ trong lúc chờ (không chuyển thì chạy đúng) | Mở log hôm nay: `eNotApplicable` tại `Editor.WriteMessage` / `Regen` / `Get*` → code gọi Editor của bản vẽ không còn active trong giai đoạn vẽ ([T03 B31](T03_PITFALLS_CASEBOOK.md#b31-gọi-editor-của-bản-vẽ-không-active)). Lỗi **không hiện** trên màn hình là manh mối: thông báo cũng bị ghi vào bản vẽ không active |

---

## ✅ Tự kiểm tra T04

1. Người dùng báo "thỉnh thoảng palette vẽ lưới vào sai bản vẽ". Bạn tái hiện thế nào, và nghi ngờ đoạn code nào đầu tiên?
2. Breakpoint trong `GridMarkerService.Draw` không dừng, dù lưới vẫn được vẽ. Ba khả năng?
3. Vì sao "Vì sao không bị phát hiện sớm hơn" là mục quan trọng nhất của incident report?

<details><summary>Gợi ý đáp án</summary>

1. Mở 2 bản vẽ, bấm vẽ từ palette rồi chuyển tab *trong lúc* thao tác đang chạy (nếu có async). Nghi các đoạn đọc `MdiActiveDocument` sau `await`, hoặc service không dùng document đã chụp ([B6](T03_PITFALLS_CASEBOOK.md#b6-vẽ-nhầm-bản-vẽ-sau-await)).
2. (a) Đang chạy một bản assembly cũ hơn (nhiều `CadCoreFramework_*` trong Modules); (b) symbol không nạp được (nạp byte array không kèm PDB); (c) đang attach sai loại code (.NET Framework vs .NET Core).
3. Vì nó trả lời câu hỏi "quy trình nào thủng", từ đó sinh ra test/analyzer/checklist ngăn **cả một lớp lỗi**, không chỉ một lỗi.
</details>

---

**Tiếp theo:** [T05 — Chiến lược kiểm thử →](T05_TESTING_STRATEGY.md)
