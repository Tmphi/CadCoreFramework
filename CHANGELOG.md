# Changelog

All notable changes to **CadCoreFramework** are documented here.

Format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).
Versions follow [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Added (Golden Sample — Grid Marker)
- **`Samples/GridMarker/`** — reference feature showing the canonical layering end-to-end: `GridSpec` (immutable model + validation) → `GridPlanner` (pure logic, no AutoCAD types) → `GridMarkerService` (thin AutoCAD shell: `Execute<T>` + UndoMark + XData, publishes `GridDrawnMessage`) → `GridMarkerCommands` (`SAMPLE_GRID`, prompts before the transaction) and `GridMarkerViewModel` / `GridMarkerPalette` (validation-driven `CanExecute`, enum status instead of `Brush`, `TrackSubscription`).
- **`docs/09_GOLDEN_SAMPLE.md`** — layer-by-layer walkthrough, linked from `00_TOC.md`.
- **`CadCommandBase.GetRequiredService<T>()`** — fail-loud service resolution for command classes.
- 56 tests under `CadCoreFramework.Tests/Samples/GridMarker/` (164 total). `SAMPLE_GRID` added to `tests/integration/smoke.scr`.

### Changed (Golden Sample)
- `SHOW_PALETTE` now opens the Grid Marker palette.

### Removed (Golden Sample)
- `ISampleDrawingService` / `SampleDrawingService`, `SampleViewModel`, `SamplePaletteControl` — superseded by the Golden Sample (they prompted from inside a service, wrapped sync work in `ExecuteAsync`, and kept WPF `Brush` in the ViewModel).

### Fixed
- `CadCommandBase.ExecuteWithScope[Async]` no longer reports success when `ICadContext` is unavailable; async variant writes errors to the originating document.
- `CadContext.ExecuteAsync` aborts if the captured document was closed during the async phase.
- `EventAggregator.Publish` invokes every subscriber even if one throws (then rethrows); subscription tokens are one-shot on Dispose.

### Added (Developer-ergonomics "ăn chơi" bundle)
- **`.editorconfig`** — extended with async-method naming rule and ~20 CA-rule severity tunings (noisy stylistic rules → `suggestion` or `none`; real-value rules stay `warning`). Analyzer config in main csproj: `EnforceCodeStyleInBuild=true`, `AnalysisLevel=latest`, `AnalysisMode=Recommended`. Clean build 115 warnings → 0.
- **`snippets/*.snippet`** — Visual Studio code snippets: `cadcmd`, `cadsvc`, `cadscope`, `cadvm`. Install docs in `snippets/README.md`.
- **`.vscode/cadcoreframework.code-snippets`** — same four snippets plus `cadasync`, `cadfilter`, `caddraw`, `cadguard` for VS Code / Cursor; picked up automatically when the workspace loads.
- **`Views/DebugInspectorPaletteControl.xaml`** + **`ViewModels/DebugInspectorViewModel.cs`** — WPF palette with three tabs: Command History (DataGrid), DI Services (lifetime/interface/impl), Event Subscribers (handler counts). Opens via new **`CORE_DEBUG`** command. Supported by `AppBootstrapper.RegisteredServices` snapshot and `EventAggregator.GetSubscriberSnapshot()`.
- **`docs/08_COOKBOOK.md`** — 11 copy-paste recipes (Title block A3, Batch PDF export, Purge+Audit, XRef attach, Free polyline jig, Dynamic block attribute update, Layer merge, Selection filter patterns, XData round-trip, Subscription Dispose, Async heavy work). Linked from `00_TOC.md`.
- **`CadCoreFramework.Benchmarks/`** — BenchmarkDotNet project. 3 suites: `CadResultBenchmarks` (Result vs raw vs throw/catch), `EventAggregatorBenchmarks` (publish at 1/10/100 subscribers), `CommandHistoryBenchmarks` (Record steady-state). Pure-C# only — no AutoCAD runtime required.
- **`Deployment/installer/`** — WiX 4 installer scaffold: `CadCoreFramework.wxs` + `build-msi.ps1` + README. Produces a per-user MSI that drops the bundle into `%AppData%\Autodesk\ApplicationPlugins`.
- **`CadCoreFramework.Analyzers/`** — custom Roslyn analyzer. Ships one rule: **CAD001** forbids `Application.DocumentManager.MdiActiveDocument` in classes under `Services/` and `ViewModels/`. Wired into the main project via `OutputItemType="Analyzer"` so the analyzer DLL is not shipped inside `CadCoreFramework.dll`.
- **`Templates/cad-command/`** — `dotnet new cad-command` item template. Generates `Commands/XxxCommand.cs` with `CadCommandBase` skeleton + `ExecuteWithScope` + inline reminder to register in `AssemblyInfo.cs`. Parameters: `--AutoCadCommandName`, `--UndoMark`.
- **`Core/EventAggregator.GetSubscriberSnapshot()`** — read-only subscriber count per message type (used by Debug Inspector).
- **`Core/AppBootstrapper.RegisteredServices`** — snapshot of the `IServiceCollection` used to build the provider (used by Debug Inspector).

### Added (Final 10/10 polish)
- `CadCoreFramework.Tests/LayerServiceTests.cs` — 6 control-flow tests for `LayerService` using `FakeCadContext` (verify ExecuteRead/ExecuteAsync call counts, undoMarkName, exception propagation).
- `CadCoreFramework.Tests/PathProviderTests.cs` — 3 pure-C# tests for `PathProvider`.
- `CadCoreFramework.Tests/FileSystemServiceTests.cs` — 6 pure-C# tests for `FileSystemService` (file I/O round-trips, idempotency, missing-path handling).
- `tests/integration/smoke.scr` + `run-integration-tests.ps1` + `README.md` — accoreconsole-driven integration test scaffolding that NETLOADs the plugin and runs CORE_INFO, CORE_TEST_ALL, DRAW_CIRCLE_SAMPLE; parses the console log for failure markers. Runs locally (self-hosted CI) because GitHub Actions `windows-latest` lacks AutoCAD.

### Changed (Final 10/10 polish)
- `Core/CadContext.cs` — `ExecuteCore` and `ExecuteCore<T>` now open `CurrentSpace` with `OpenMode.ForRead`; `AppendEntityToCurrentSpace` lazy-upgrades to `ForWrite` only when a caller actually writes. Removes unnecessary write locks on pure-read Execute lambdas.
- `Utils/BlockHelper.cs` — `InsertBlock` lazy-upgrades `space` to `ForWrite` to match the new CadContext default.
- `CadCoreFramework.csproj` — `<Nullable>annotations</Nullable>` enabled on the main project. Lets public API use `?` / non-null annotations without flow-analysis noise from the un-annotated AutoCAD API.
- Nullable annotations added to: `AppBootstrapper.ServiceProvider`, `ICadContext.Execute`/`ExecuteAsync`/`ExecuteBatch` (undoMarkName, progress), `IEventAggregator.Subscribe`/`Unsubscribe`, `ICommandHistoryService.Record`, `ViewModelBase.RunSafe`/`RunSafeAsync` (errorPrefix), `Guard.NotNull`/`NotEmpty`, `SelectionFilterBuilder.OfTypes`.
- `CadCoreFramework.Testing/Fakes/FakeCadScope.cs` — properties switched from `Document?` to `Document` + `null!` to match the (correctly non-nullable) `ICadScope` interface contract.
- Tests pass: 90 → 105 (added 15 service/path/filesystem tests).

### Added (Tier 3 — Template polish & docs sync)
- `init-template.ps1` — extended with `-ProjectName` parameter for full namespace/file/directory rename workflow when cloning without `dotnet new`; GUID regeneration now also refreshes all project and solution GUIDs in the `.sln` file.
- `docs/06_DEVOPS_AND_STANDARDS.md` — new §6.3 CI/CD section documenting the GitHub Actions workflows; §6.2 testing section updated with `FakeCadContext` example and coverage-report commands.
- `docs/01_GETTING_STARTED.md` — Bước 1 now uses `dotnet build` instead of the removed `build.ps1` reference.

### Added (Tier 2 — Tooling & safety)
- `Core/CadContextGuard.cs` — `[ThreadStatic]` sentinel that detects and rejects nested `CadContext.Execute` / `ExecuteRead` calls on the same thread with a clear diagnostic message.
- `Core/CadContext.cs` — `CadContextGuard` wired into all three entry points: `ExecuteCore` (void), `ExecuteCore<T>`, and `ExecuteRead`.
- `CadCoreFramework.Tests/CadContextGuardTests.cs` — 5 unit tests covering `IsActive`, `Enter`, `Exit`, nested-throw, and re-enter-after-exit.
- `.template.config/template.json` — `dotnet new cad-plugin` template manifest: `sourceName` drives namespace/file renaming, `guids` array triggers GUID regeneration, `sources.modifiers.exclude` omits build artifacts.
- `.config/dotnet-tools.json` — ReportGenerator 5.3.8 declared as a local dotnet tool.
- `.github/workflows/build-and-test.yml` — `dotnet tool restore` + `--collect:"XPlat Code Coverage"` + `reportgenerator` HTML/Cobertura + coverage artifact upload.

### Added (Tier 1 — Infrastructure)
- `.github/workflows/build-and-test.yml` — CI pipeline: build Release + run tests on `windows-latest`, upload `.trx` test-result artifact.
- `.github/workflows/release.yml` — release pipeline: triggered by `v*` tags, packages `.bundle` and creates GitHub Release.
- `CadCoreFramework.Testing/` — new project with `FakeCadContext` and `FakeCadScope` test doubles implementing `ICadContext` / `ICadScope` without requiring AutoCAD runtime.
- `CadCoreFramework.Tests/FakeCadContextTests.cs` — 7 demonstration tests showing injection of exceptions, `LastUndoMarkName`, `ExecuteCount`, async and batch overloads.
- `Commands/CadCommandBase.cs` — `ExecuteCommand`, `ExecuteWithScope`, `ExecuteWithScopeAsync` helpers with `[CallerMemberName]` auto-capture; `CommandHistory` auto-wired via `AppBootstrapper.ServiceProvider` lazy lookup.

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
