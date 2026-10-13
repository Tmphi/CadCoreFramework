# Changelog

All notable changes to **CadCoreFramework** are documented here.

Format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).
Versions follow [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

_Chưa có thay đổi._

---

## [1.0.0] — 2026-10-13

Bản phát hành đầu tiên: [GitHub Release v1.0.0](https://github.com/Tmphi/CadCoreFramework/releases/tag/v1.0.0) (`CadCoreFramework_v1.0.0_Setup.zip`). Gồm toàn bộ các nhóm thay đổi dưới đây, cùng với nền tảng template ban đầu (nhóm "Added (initial template baseline)" ở cuối mục).

### Changed (template hygiene)
- **Demo suite isolated in `Demos/`** (was `Commands/Demos/` + `Commands/DemoCommands.cs`), namespace `CadCoreFramework.Demos`. Its `CommandClass` registrations moved to `Demos/DemoAssemblyInfo.cs`. New MSBuild property **`IncludeDemos`** (default `true`): build with `/p:IncludeDemos=false` — or set it in the csproj — and every `DEMO_*` command drops out of the DLL. Golden Sample, `LayerManager` and `CommonControlsPalette` are unaffected.
- **`CadCommandBase` injection constructor** now takes an optional `ICommandHistoryService` — with all three services injected, a command no longer touches the static `AppBootstrapper.ServiceProvider`.
- **`dotnet new cad-plugin` produced `Author.csproj` / `Author.sln`** instead of the `-n` name: the `AuthorName` symbol replaced the same `"CadCoreFramework"` token as `sourceName`. Symbol removed; new parameters `--GitHubRepo owner/name` (README badge + CAD001 help link) and `--IncludeDemos false`. Analyzer/Benchmarks project GUIDs were missing from `guids` (every generated plugin shared them) — added. Local junk (`packages/`, `nuget.exe`, `build_log.txt`, `.claude/`, nested `bin/obj`) is now excluded. Verified: generated solution builds with `-warnaserror` and passes all tests.
- **`init-template.ps1` failed to parse on Windows PowerShell 5.1** (`#Requires -Version 5.1`): UTF-8 without BOM is read as cp1252, and the `0x94` byte of `—` is a curly quote → unterminated string. All `.ps1` scripts with non-ASCII text now carry a BOM (`.editorconfig` documents why). New `-GitHubRepo` / `-RemoveDemos` switches; the WiX `UpgradeCode` is now regenerated in sync with `PackageContents.xml`. Verified end-to-end on a copy: rename + build + tests pass.
- **MSB3270 under `-warnaserror`** (AnyCPU project referencing AMD64 `AcSeamless.dll`): `Tests` and `Testing` projects now `PlatformTarget=x64`, like the plugin. Debug and Release solution builds are 0 warnings again.
- **Integration runner (`tests/integration`) reported "passed" without ever running a command.** Four bugs: accoreconsole's UTF-16 stdout was never decoded (no marker could match), `;` comments in `smoke.scr` were sent to the command line, blank lines re-ran `NETLOAD` and swallowed the command names, the DLL path was written with doubled `\\`, and auto-discovery picked the last *character* of the path when only one AutoCAD is installed. All fixed; each command must now print its own success message, `Unable to load` / `Unknown command` / `[Lỗi …]` fail the run, and `/isolate` keeps `SECURELOAD 0` out of the real AutoCAD profile. **Known limitation (documented, exit code 3):** the plugin references `acmgd.dll`, and accoreconsole crashes with a stack overflow loading it (NETLOAD of `acmgd.dll` alone reproduces it) — headless testing needs a separate non-UI DLL.
- **Release bundle shipped another product's user guide:** `Deployment/HDSD.txt` advertised features that do not exist here (device library, Excel import/export), and `Install.bat` called the plugin an "electrical device manager" — both left over from the project this template was extracted from. Rewritten to the real command list; hard-coded `v2.0` removed. Install/Uninstall verified against a throwaway `APPDATA`.
- **`release.yml` (never run before):** the tag version was never passed to `package.ps1` (every release was `_v2.0_`), and the release zip re-compressed `dist\*`, nesting `package.ps1`'s own zip inside it. Now `-Version` comes from the tag and the single setup zip is published. `package.ps1` stamps that version into the bundle's `PackageContents.xml` (case-sensitive replace — a case-insensitive one corrupted `<?xml version=…?>`). Simulated locally with tag `v1.3.0`.
- `.gitattributes`: `*.bat` / `*.cmd` always checked out CRLF (cmd.exe mis-parses LF-only batch files).
- Docs: cookbook called a non-existent `DynamicBlockHelper.SetProperty` (→ `SetVisibilityState`); DevOps chapter described the CI badge as something still to add.
- README: "Tạo dự án mới từ template" section; learning-path table lists parts 8–9.
- **No more silent `catch { }` in core infrastructure:** COM `StartUndoMark`/`EndUndoMark` failures (`CadContext`, `UndoGroupManager`), overrule unregister failures (`OverruleManager`) and palette ViewModel dispose failures (`PaletteService`) are now logged. `CadContext`, `OverruleManager` and `UndoGroupManager` accept an optional `ILoggerService` (DI supplies it automatically).

### Fixed (first real AutoCAD run)
- **Plugin failed to initialize on AutoCAD 2024** with `MissingMethodException` in `Serilog.Sinks.File`. AutoCAD 2024 ships and preloads Serilog 2.9 / Serilog.Sinks.File 4.1; Serilog keeps AssemblyVersion 2.0.0.0 across 2.x/3.x, so the plugin was bound to AutoCAD's copy. **Serilog removed**; `LoggerService` is now a dependency-free file logger (same `log-yyyyMMdd.txt` files and line format, `FileShare.ReadWrite`, 31-day retention, never throws). 10 new tests.
- **net8.0-windows output contained only `CadCoreFramework.dll`** (no DI / MVVM / Newtonsoft) → would fail to NETLOAD on AutoCAD 2025+. Enabled `EnableDynamicLoading` for that TFM so dependencies are copied (AutoCAD assemblies still excluded).
- net48: explicit `Microsoft.CSharp` reference (needed by `dynamic` COM calls; previously arrived transitively via Serilog).
- **Palettes failed with "The component '…' does not have a resource identified by the URI '…'"** when the plugin is loaded from a byte array (anti-lock loader / hot-reload). WPF resolves the plugin assembly by name for BAML; the byte-loaded assembly is invisible to the CLR binder, so a second copy got loaded from disk. `AssemblyResolve` now first returns an already-loaded assembly with the same name + version (`AppBootstrapper.FindLoadedAssembly`, 5 tests).
- **`SHOW_UI_TEMPLATE` failed with "Provide value on StaticResourceExtension threw an exception"**: `CommonControlsPalette` referenced 33 `Styles.xaml` keys via `{StaticResource}`, but the styles are merged in code-behind *after* `InitializeComponent`. Switched those to `{DynamicResource}` and load styles right after `InitializeComponent` (no unstyled flash).
- **New `XamlPaletteTests`**: instantiate all 4 palettes for real on an STA thread (DI with mocks, no AutoCAD) — catches XAML/resource/DI-registration errors at test time. Verified it reproduces the exact `SHOW_UI_TEMPLATE` error on the old markup. Test project now `UseWPF`.

### Changed (Consistency pass — align the whole repo with the Golden Sample)
- **`ICadContext` error convention:** `Execute*` throws · new `TryExecute(Action)` / `TryExecute<T>(Func)` return `CadResult` and never throw. `Execute<T>` (which returned `CadResult` despite its name) is now `[Obsolete]` and forwards to `TryExecute<T>`; `CadContext` now has a single throwing core that every overload wraps.
- **`ExecuteRead`** locks the document when called from application context (palette / modeless), detected via `DocumentManager.IsApplicationContext`.
- **`LayerService.ToggleLayerVisibility`** (was `ToggleLayerVisibilityAsync`) — synchronous `TryExecute` + undo mark + regen, returns `CadResult<bool>`; no more sync work wrapped in `ExecuteAsync`. `LayerManagerViewModel` reports missing-layer / CAD-failure cases.
- **`scope.Ask*()`** prompt extensions marked `[Obsolete]` (they prompt while the transaction + lock are open); use `CadPromptHelper.Get*()` before `ICadContext`. Added `CadPromptHelper.GetAngle`.
- **Fail-loud service resolution** across `Commands/` and `Commands/Demos/`: `GetRequiredService<T>()` instead of `ServiceProvider?.GetService<T>()`; `CadCommandBase.CadContext` throws when DI is unavailable. Section banners / "HOÀN TẤT" footers moved inside `ExecuteCommand` so they only print on success (fixes `CORE_TEST_ALL` printing "passed" after a failure — relevant to `smoke.scr` log parsing).
- `PaletteService.TogglePalette` logs **and rethrows** instead of swallowing; `LayerManagerControl` / `DebugInspectorPaletteControl` fail loud when their ViewModel is missing; `DebugInspectorViewModel` takes `IEventAggregator` by constructor injection.
- `DEMO_UNDO_GROUP` used expression lambdas that silently bound to the non-throwing generic overload — now block lambdas.
- `.agents/rules/project_rules.md` updated to the Golden Sample conventions.

### Fixed (build) — clean build is now **0 warnings** on both TFMs
- **NU1608**: `AutoCAD.NET` 25.0.0 → 25.0.1 (25.0.0 pinned the pre-release `AutoCAD.NET.Core 25.0.0-V058`).
- `GenerateDocumentationFile=true` (CS1591 suppressed) so IDE0005 runs on build; removed ~190 unused `using` directives across 40+ files (`dotnet format style --diagnostics IDE0005`).
- Fixed malformed XML doc comments (CS1570: unescaped `&` / raw XAML in `<summary>`) and missing `<param>` tags (CS1573).
- `Templates\**` excluded from the plugin compile — the `cad-command` item-template source was being built into `CadCoreFramework.dll`.

### Added (tests)
- `CadCoreFramework.Tests` / `CadCoreFramework.Testing` multi-target **`net48;net8.0-windows`** — the suite now runs on both AutoCAD runtimes (CI uses `LogFilePrefix` so the two `.trx` files don't collide).
- `LayerManagerViewModelTests`, `DebugInspectorViewModelTests`, `TryExecute` tests; `LayerServiceTests` rewritten for the new API. 176 tests per TFM.

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

### Added (initial template baseline)
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
- File logging via `ILoggerService` / `LoggerService` (originally Serilog; replaced by a self-written file logger because AutoCAD 2024 preloads an incompatible Serilog — see "Fixed (first real AutoCAD run)").
- `ThemeManager` — automatic WPF theme switching following AutoCAD Dark/Light mode.

[Unreleased]: https://github.com/Tmphi/CadCoreFramework/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/Tmphi/CadCoreFramework/releases/tag/v1.0.0
