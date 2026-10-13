# Integration Tests (accoreconsole.exe)

Thư mục này có HAI bộ kiểm thử chạy trên AutoCAD THẬT:

| Bộ | Chạy | Kiểm gì | Trạng thái |
| :-- | :-- | :-- | :-- |
| **HeadlessLab** | `.\tests\integration\run-headless-lab.ps1` | Helper của framework (Extensions, Utils, PlotService) trên AutoCAD thật, không UI | ✅ chạy được (AutoCAD 2024) |
| Smoke (plugin đầy đủ) | `.\tests\integration\run-integration-tests.ps1` | Toàn bộ plugin qua `smoke.scr` | ⛔ plugin không nạp được trong accoreconsole (xem "Giới hạn") |

## HeadlessLab — kiểm thử headless trên AutoCAD thật

Plugin chính tham chiếu `acmgd.dll` (PaletteSet, Ribbon) nên không nạp được trong accoreconsole.
`HeadlessLab/` là một DLL **chỉ dùng cho kiểm thử** (không phát hành), **link lại** (không copy) mã
nguồn không phụ thuộc UI — `Extensions/**`, `Utils/**` (trừ `WpfCadHelper.cs`), một số file `Core/`
và `Services/PlotService.cs` — và chỉ tham chiếu `AcDbMgd` + `AcCoreMgd`. Plugin vẫn là MỘT DLL
(ADR-001 không đổi); lab luôn kiểm đúng code đang có trong repo.

Runner: build lab → chạy `accoreconsole /isolate` với profile riêng trong `%TEMP%` (**không** đụng
profile/registry AutoCAD của bạn, kể cả `SECURELOAD`) → `NETLOAD` → lệnh `LAB_ALL` → đọc log.
Mỗi kiểm thử in đúng một dòng `LAB|tên|PASS` hoặc `LAB|tên|FAIL|lý do`; chạy trong transaction riêng
và **rollback** (kiểm thử in ấn chạy trên database đã commit — bản vẽ bị bỏ khi thoát không lưu).

```powershell
.\tests\integration\run-headless-lab.ps1          # log: tests\integration\headless-lab.log
```

Exit code: `0` mọi kiểm thử PASS · `1` có FAIL / thiếu kết quả / build lỗi · `2` không có accoreconsole.
CI (GitHub Actions) không có AutoCAD nên chỉ **biên dịch** lab (nó nằm trong `CadCoreFramework.sln`):
ai làm `Utils/`/`Extensions/` phụ thuộc UI sẽ làm hỏng build ngay.

Các kiểm thử hiện có (16, đều PASS trên AutoCAD 2024 — mục backlog ở `docs/training/T09` §3):

| Kiểm thử | Bảo vệ |
| :-- | :-- |
| `INITIALIZE_RUNS_ON_COMMAND_THREAD` | Giả định của `Core/CadThread` (B-06) |
| `COLORTHEME_DRIVES_DARK_MODE` | Theme đọc `COLORTHEME`; `DARKMODE` không tồn tại (B-01) |
| `XRECORD_REMOVE_ERASES` | `RemoveMetadata` Erase Xrecord (B-09) |
| `COPY_KEEPS_ATTRIBUTES`, `MIRROR_KEEPS_ATTRIBUTES` | Bản sao có attribute RIÊNG (B-13) |
| `COPY_KEEPS_METADATA` | Bản sao giữ Extension Dictionary (XRecord) + XData (B-13) |
| `ATTRIBUTE_REALIGNED_AFTER_SET` | Attribute căn giữa được căn lại sau khi đổi giá trị (B-25) |
| `ENSURE_LAYER_REJECTS_INVALID_NAME` | Tên ký hiệu sai → `ArgumentException` rõ ràng (B-16) |
| `CREATE_BLOCKDEF_EXISTING_DISPOSES` | Entity không dùng tới được Dispose (B-12) |
| `DRAW_CONFIGURE_THROWS_DISPOSES` | `configure` ném lỗi → entity được Dispose (B-14) |
| `FILTER_ONLAYERS_IS_WILDCARD`, `FILTER_EXACT_MATCHES_LITERALLY` | Wildcard của filter / `OnLayersExact` (B-17) |
| `PLOT_LAYOUT_TO_PDF`, `PLOT_MULTIPLE_LAYOUTS_TO_PDF`, `PLOT_UNKNOWN_LAYOUT_FAILS_CLEANLY` | In PDF thật, khôi phục `BACKGROUNDPLOT` + layout hiện hành (B-10) |

**Thêm kiểm thử:** thêm một dòng `("TÊN", scope => { … Assert(điều_kiện, "lý do"); })` vào `Tests` trong
`HeadlessLab/LabCommands.cs` (hoặc vào `DocumentTests` nếu cần database đã commit). Viết kiểm thử để nó
**FAIL trên code cũ** trước khi sửa — đó là bằng chứng lỗi có thật. Code mới cần được lab kiểm thì phải
không phụ thuộc `AcMgd` (nếu là file mới ngoài `Extensions/`/`Utils/`, thêm `<Compile Include … Link=…>`).

## Smoke tests (plugin đầy đủ)

End-to-end smoke tests that exercise the real AutoCAD .NET API by loading the
built plugin into AutoCAD's headless console (`accoreconsole.exe`) and
dispatching a sequence of commands.

Unlike the unit tests in `CadCoreFramework.Tests/` (which use `FakeCadContext`
to run without AutoCAD), these tests validate the full stack:

- `AssemblyResolve` wiring in `AppBootstrapper`
- `CadContext.Execute` → `UpgradeOpen` → `AppendEntity` write path
- `EnsureLayer`, `DrawRectangle`, `DrawMText`, `DrawText`, `DrawCircle` extensions
- XData `SetMetadata` / `GetMetadata` round-trip

> ## ⚠️ Giới hạn hiện tại — plugin này KHÔNG nạp được trong accoreconsole
>
> `accoreconsole.exe` crash với `0xC00000FD` (stack overflow trong `clr.dll`) ngay khi NETLOAD
> plugin, trước cả `IExtensionApplication.Initialize()`. Nguyên nhân: plugin tham chiếu
> **`acmgd.dll`** (PaletteSet, Ribbon, WPF) — mà chính `acmgd.dll` cũng không nạp được trong
> console: `NETLOAD "…\AutoCAD 2024\acmgd.dll"` crash y hệt (kiểm chứng 2026-10-13, AutoCAD 2024).
> Runner trả **exit code 3** cho trường hợp này.
>
> Muốn chạy được, phải tách phần không-UI (chỉ `AcDbMgd` + `AcCoreMgd`: Core, Extensions, Services,
> lệnh không palette) thành một DLL riêng và NETLOAD DLL đó trong console. **HeadlessLab** (ở trên)
> là bước một phần theo hướng đó mà không đổi việc phát hành một DLL: kiểm helper headless, còn
> lệnh/palette/`CadContext` vẫn kiểm end-to-end bằng AutoCAD thật (GUI): nạp plugin rồi chạy đúng
> chuỗi lệnh trong `smoke.scr`.
>
> Trước đây runner báo "passed" dù chưa lệnh nào chạy: log UTF-16 không được giải mã, comment
> `;` trong `.scr` bị gửi vào command line, dòng trống lặp lại NETLOAD, và đường dẫn bị nhân đôi
> `\\`. Các lỗi đó đã sửa — runner giờ yêu cầu mỗi lệnh in đúng thông báo thành công của nó.

## Prerequisites

- Windows with **AutoCAD 2021 or later** installed (any edition that ships
  `accoreconsole.exe`, which is all modern full installs — not AutoCAD LT).
- Plugin built: `dotnet build -c Debug` (or Release).

## Running locally

```powershell
# Default: Debug / net48 (AutoCAD 2021–2024)
.\tests\integration\run-integration-tests.ps1

# AutoCAD 2025+:
.\tests\integration\run-integration-tests.ps1 -Configuration Release -TargetFramework net8.0-windows

# Point at a specific AutoCAD install:
.\tests\integration\run-integration-tests.ps1 -AcCorePath "C:\Program Files\Autodesk\AutoCAD 2024\accoreconsole.exe"
```

## Exit codes

| Code | Meaning |
| :--- | :------ |
| 0    | Every command printed its expected success message, no failure markers in log |
| 1    | `accoreconsole` returned non-zero, a failure marker (`Unable to load`, `Unknown command`, `[Lỗi AutoCAD]`, `[Lỗi Hệ Thống]`, `Fatal Error`…) was found, or an expected message is missing |
| 2    | `accoreconsole.exe` not found on this machine |
| 3    | Plugin references `acmgd.dll` and cannot load in accoreconsole (see "Giới hạn" above) |

## Running in CI

GitHub Actions `windows-latest` runners do **not** ship with AutoCAD, so these
tests cannot run on the default `build-and-test.yml` workflow. To gate releases
on integration tests, set up a **self-hosted runner** on a Windows machine that
has AutoCAD installed, label it `autocad`, and add a job like:

```yaml
integration:
  runs-on: [self-hosted, windows, autocad]
  needs: build
  steps:
    - uses: actions/checkout@v4
    - run: dotnet build -c Release
    - run: .\tests\integration\run-integration-tests.ps1 -Configuration Release
```

## Adding new tests

1. Add a new `[CommandMethod]` to `Commands/SampleCommands.cs` (or a new
   file registered in `Properties/AssemblyInfo.cs`).
2. Append the command name (and one line per prompt answer) to `smoke.scr`.
   **No comments, no blank lines** — in a `.scr` file `;` is sent to the command
   line and a blank line is ENTER (repeats the previous command).
3. Add the command's success message to `$expected` in `run-integration-tests.ps1`
   — a command that never prints it fails the run.
