# Changelog

All notable changes to **CadCoreFramework** are documented here.

Format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).
Versions follow [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Added
- `CircleData` model replacing the empty, Vietnamese-named `HinhTron` class.
- `ICommandHistoryService` / `CommandHistoryService` — tracks executed command names, durations, and success state in a fixed-capacity (50) thread-safe in-memory buffer.
- `XRecordExtensions` — `SetXRecord<T>`, `GetXRecord<T>`, `RemoveXRecord` extension methods on `ICadScope` for a consistent XRecord API aligned with `CadScopeDrawingExtensions`.
- `SelectionFilterBuilder.WithXData(appName)` — filters entities by XData RegApp namespace in one fluent call.
- `.gitignore` covering Visual Studio artefacts, build outputs, NuGet packages, machine-specific `.csproj` files, and OS junk files.
- `InternalsVisibleTo("CadCoreFramework.Tests")` in `AssemblyInfo.cs`.
- `AppBootstrapper.SetServiceProviderForTesting()` (DEBUG-only) for xUnit DI isolation.
- Real xUnit tests: `XDataHelperTests` (7 cases) and `SelectionFilterBuilderTests` (8 cases) — run without AutoCAD.

### Changed
- **`EventService`** — fixed lambda event subscription bug. Delegate references are now stored in `Dictionary<Document, Handler>` enabling proper `UnhookDocument` via `-=`. Previously, anonymous lambdas could not be unsubscribed, causing permanent event handler accumulation over long sessions.
- **`CadContext`** — replaced brittle COM `dynamic` UndoMark (`AcadApplication.StartUndoMark()`) with the managed `Database.BeginUndoMark()` / `EndUndoMark()` API. `EndUndoMark` is now in a `finally` block to guarantee pairing even when the action throws.
- **`.csproj`** — multi-target `net48` (AutoCAD 2021–2024) + `net8.0-windows` (AutoCAD 2025+) with version-gated AutoCAD NuGet references (24.x vs 25.x). Removed hard-coded `HintPath` pointing to `C:\Program Files\Autodesk\AutoCAD 2021\`.
- **`AppBootstrapper`** — `OverruleManager` and `ICommandHistoryService` registered in the DI container. `OverruleManager.Dispose()` now called explicitly in `Terminate()` before the DI container is disposed.
- `CadCoreFramework.Tests.csproj` — upgraded from bare skeleton to full xUnit + FluentAssertions + Moq setup.
- All comments and XML documentation translated from Vietnamese to English.

### Removed
- `CadCoreFramework-DESKTOP-JFVJFBQ.csproj` — machine-specific VS conflict file.
- `Models/HinhTron.cs` — replaced by `Models/CircleData.cs`.

### Fixed
- `EventService` lambda leak — handlers were permanently attached to `Database.ObjectAppended` / `ObjectErased` and could never be removed.
- `CadContext` COM UndoMark — silently failed on non-standard AutoCAD installations; now uses the stable managed API.
- Build portability — `HintPath` for `Newtonsoft.Json` pointed to a machine-specific path, breaking CI builds and developer onboarding.

---

## [1.0.0] — Initial release

### Added
- `AppBootstrapper` — `IExtensionApplication` entry point with proper `AssemblyResolve` ordering to prevent JIT FileNotFoundException.
- `ICadContext` / `CadContext` — transaction management abstraction with `Execute`, `ExecuteRead`, and `ExecuteAsync` (2-phase async pattern).
- `CadCommandBase` — base class for all command handlers with built-in error catching, distinguishing `UserBreak` (ESC) from real exceptions.
- `RibbonService` — programmatic Ribbon Tab/Panel/Button management with auto-rebuild after `CUI` / `MENULOAD` commands.
- `EventService` — MDI-aware document event aggregation.
- `PaletteService` — dockable WPF Palette lifecycle management with `IsBusy` integration.
- `XDataHelper` — JSON-over-XData with multi-chunk support and Unicode-safe ASCII escaping.
- `XRecordHelper` — JSON-over-XRecord (no 16 KB limit).
- `CadScopeDrawingExtensions` — 800+ line library of one-liner drawing methods (`DrawLine`, `DrawRectangle`, `DrawMText`, `EnsureLayer`, …).
- `SelectionFilterBuilder` — fluent `SelectionFilter` builder.
- `DynamicBlockHelper` — reads and writes Dynamic Block attribute values.
- `CadPromptHelper` — wraps `Editor.GetPoint`, `GetString`, `GetEntity`, etc.
- WPF MVVM stack: `ViewModelBase`, `IBusyIndicator`, `ViewModelLocator`.
- WPF Hot-Reload trick via timestamped `AssemblyName` in Debug builds.
- Jig system: `CadDrawJigBase`, `CadEntityJigBase`, `BlockPlacementJig`, `RectangleJig`.
- `OverruleManager` — safe registration and cleanup of AutoCAD Overrules.
- Serilog file logging via `ILoggerService` / `LoggerService`.
- `ThemeManager` — automatic WPF theme switching following AutoCAD Dark/Light mode.
