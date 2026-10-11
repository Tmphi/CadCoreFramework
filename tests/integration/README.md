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
| 0    | All commands ran, no failure markers in log |
| 1    | `accoreconsole` returned non-zero OR log contains `eLockViolation` / `Fatal Error` / `[ERROR]` |
| 2    | `accoreconsole.exe` not found on this machine |

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
2. Append the command name on its own line to `smoke.scr`.
3. If the command writes a failure marker to stdout, the runner will catch it;
   otherwise write a self-check that calls `Editor.WriteMessage("[ERROR] ...")`
   on failure.
