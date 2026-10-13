# Integration Tests (accoreconsole.exe)

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
> lệnh không palette) thành một DLL riêng và NETLOAD DLL đó trong console. Cho tới khi đó, kiểm
> tra end-to-end bằng AutoCAD thật (GUI): nạp plugin rồi chạy đúng chuỗi lệnh trong `smoke.scr`.
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
