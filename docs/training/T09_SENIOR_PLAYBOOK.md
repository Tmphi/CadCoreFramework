> **🎓 Học viện CadCoreFramework** · [Lộ trình](README.md) · [← T08](T08_RECIPES_EXTENDING.md) · **T09** · [T10 →](T10_LABS_AND_ASSESSMENT.md)

# T09 — Sổ Tay Senior

Senior không chỉ viết code đúng. Senior **giữ cho cả hệ thống đúng theo thời gian**: biết vì sao kiến trúc như hiện tại, nhận ra khi nào nó cần đổi, review để lỗi không lọt vào, và quản lý danh sách nợ kỹ thuật một cách trung thực.

---

## 1. ADR: Vì sao kiến trúc như hiện tại

*Architecture Decision Record*: mỗi quyết định ghi **bối cảnh → quyết định → hệ quả**. Muốn đổi một quyết định, viết ADR mới thay thế, đừng âm thầm làm khác.

| ADR | Quyết định | Bối cảnh / lý do | Hệ quả (cả tốt lẫn xấu) |
| :---: | :--- | :--- | :--- |
| 001 | **Một DLL duy nhất** chứa cả lõi lẫn UI | Nạp đơn giản (một file, tool nạp byte array của nhóm); người dùng chỉ cài một thứ | ✅ Đơn giản. ❌ Tham chiếu `AcMgd` nên plugin **không chạy headless** (B-20 — giảm nhẹ bằng ADR-015); không tách được lõi để tái dùng |
| 002 | Mọi thao tác bản vẽ đi qua **`ICadContext`** (lock + transaction + undo mark) | Loại bỏ lỗi lock/commit/undo lặp lại khắp nơi; test được bằng Fake | ✅ Nguyên tử, nhất quán. ❌ Thêm một lớp trừu tượng; side database phải xử lý riêng |
| 003 | **`Execute*` ném, `TryExecute*` trả `CadResult`** | Tên phải nói lên hành vi; `Execute<T>` cũ trả Result "âm thầm" gây bẫy overload | ✅ Rõ ràng. Lambda biểu thức vẫn có thể chọn nhầm overload (giờ thành lỗi build vì `[Obsolete]` + `-warnaserror`) |
| 004 | **Prompt chỉ ở điểm vào** (Command); Service không prompt | Service dùng chung cho palette (application context, không prompt được); prompt trong transaction treo AutoCAD | ✅ Service test được, dùng được mọi nơi. ❌ Palette cần cách nhập khác (form, gửi lệnh) |
| 005 | **Functional core / imperative shell** (Spec, Planner thuần; Service mỏng) | Code trong lambda không test kết quả được | ✅ Test kết quả 100% cho logic. ❌ Thêm vài class cho mỗi tính năng |
| 006 | **MS DI**; service locator **chỉ ở biên** (lệnh, code-behind) | AutoCAD/WPF tạo instance bằng constructor không tham số | ✅ Bên trong hoàn toàn constructor injection. ❌ Biên phụ thuộc static `ServiceProvider` |
| 007 | **Logger tự viết** thay Serilog | AutoCAD 2024 nạp sẵn Serilog cùng AssemblyVersion ([A1](T03_PITFALLS_CASEBOOK.md#a1-serilog-missingmethodexception-ngay-khi-khởi-động)) | ✅ Không xung đột host. ❌ Ít tính năng (không structured logging) |
| 008 | **Tên assembly có timestamp** ở Debug | .NET Framework không gỡ assembly; cần nạp lại khi dev | ✅ Không phải tắt AutoCAD. ❌ Mọi pack URI phải dựng lúc runtime; nhiều bản cũ còn trong AppDomain |
| 009 | `AssemblyResolve` **ưu tiên assembly đã nạp** | Nạp byte array làm WPF nạp bản thứ hai ([A2](T03_PITFALLS_CASEBOOK.md#a2-palette-lỗi-does-not-have-a-resource-identified-by-the-uri)) | ✅ Palette chạy khi nạp byte array. Không bao giờ "ép" version khác |
| 010 | Demo tách bằng **công tắc MSBuild** `IncludeDemos`, không tách project | Giữ một DLL (ADR-001) và quy trình nạp hiện có của nhóm | ✅ Không đổi cách nạp. ❌ Demo vẫn cùng assembly khi bật |
| 011 | **Analyzer** biến quy ước thành lỗi build: CAD001 (không `MdiActiveDocument` trong Service/ViewModel), CAD002 (không prompt trong Service/ViewModel), CAD003 (không ghi trong `ExecuteRead`) | Quy ước dễ bị quên; analyzer biến quy ước thành lỗi build (`-warnaserror`) | ✅ Tự động, có unit test (`AnalyzerTests`). ❌ Chỉ thấy lời gọi trực tiếp, không theo vào method được gọi |
| 012 | **Multi-target `net48;net8.0-windows`** | Sẵn đường cho AutoCAD 2025+ | ✅ Build/test cả hai. ❌ net8 **chưa chạy thật** (B-21); mọi API phải có ở cả hai |
| 013 | **0 warning, CI `-warnaserror`** | Warning bị lờ đi thì sẽ có lỗi thật lẫn trong đó | ✅ Chặn sớm. ❌ Phải sửa cả warning "vô hại" (MSB3270…) |
| 014 | **Phát hành theo tag**, version bundle đóng dấu từ tag | Một nguồn sự thật cho version; tái lập được | ✅ Tự động. ❌ Workflow ít chạy nên dễ có lỗi tiềm ẩn ([A10](T03_PITFALLS_CASEBOOK.md#a10-release-workflow-lỗi-403-resource-not-accessible-by-integration), [A11](T03_PITFALLS_CASEBOOK.md#a11-packageps1-làm-hỏng-xml-version-khi-đóng-dấu-version)) |
| 015 | **HeadlessLab**: DLL *chỉ cho kiểm thử* link lại mã không-UI (`Extensions/`, `Utils/`, `PlotService`…), chạy trong accoreconsole — thay vì tách DLL lõi phát hành | Cần bằng chứng trên AutoCAD thật cho helper (B-20) mà không đổi quy trình nạp một DLL của nhóm (ADR-001) | ✅ 16 kiểm thử thật; đã bắt lỗi mà unit test không thể (B-28, B-13 đính chính). CI biên dịch lab nên `Utils/`/`Extensions/` không được phụ thuộc UI. ❌ Không phủ `CadContext`, lệnh, palette; chỉ chạy trên máy có AutoCAD |

**Mẫu ADR mới** (đặt trong PR thay đổi kiến trúc, hoặc `docs/adr/NNN-ten.md` nếu nhóm muốn tách riêng):

```markdown
# ADR-0NN: <Tên quyết định>
- Trạng thái: Đề xuất | Chấp nhận | Thay thế bởi ADR-0MM
- Ngày: YYYY-MM-DD · Người đề xuất: …
## Bối cảnh
<Vấn đề, ràng buộc, số liệu>
## Các lựa chọn đã cân nhắc
1. … (ưu / nhược)
2. …
## Quyết định
<Chọn gì, vì sao>
## Hệ quả
<Tốt, xấu, việc phải làm theo; ảnh hưởng tới quy trình nạp / phát hành / người dùng>
```

---

## 2. Checklist code review

Người review đi theo thứ tự này. Người viết PR **tự chạy checklist trước khi gửi**.

### A. Đúng với AutoCAD
- [ ] Mọi thao tác **ghi** đi qua `ICadContext` (không `LockDocument`/`StartTransaction` thủ công, trừ side database)
- [ ] **Không prompt** trong lambda / Service / ViewModel
- [ ] Không đọc `MdiActiveDocument` sau `await`; code async chụp `Document` trước
- [ ] Không gọi AutoCAD API ngoài main thread; không `ConfigureAwait(false)` trước code chạm AutoCAD
- [ ] Không giữ `DBObject` ngoài transaction; tham chiếu bền dùng `Handle`
- [ ] Đối tượng không vào DB được `Dispose`; `ResultBuffer`/side `Database`/`PlotEngine` trong `using`
- [ ] Bảng ký hiệu: `Has()` trước indexer; tên được validate
- [ ] Event: gỡ đúng delegate; không sửa DB trong handler sự kiện Database; handler có try/catch
- [ ] Service truyền `undoMarkName`; `autoRegen` chỉ bật khi cần
- [ ] Không có `Execute` lồng nhau (publish message *sau* `TryExecute`)

### B. Kiến trúc
- [ ] Phụ thuộc đúng chiều ([T02 §1](T02_ARCHITECTURE_DEEP_DIVE.md#1-bản-đồ-dự-án-và-quy-tắc-phụ-thuộc)); ViewModel không kiểu WPF/AutoCAD
- [ ] Logic nằm **ngoài** lambda (Planner/Spec); Service là vỏ mỏng
- [ ] Lỗi nghiệp vụ dự đoán được → `CadResult`; lỗi lập trình → exception
- [ ] Đăng ký DI đúng lifetime (service Singleton, ViewModel Transient); `CommandClass` đầy đủ
- [ ] Không thêm dependency mới mà chưa qua [chính sách dependency](T06_BUILD_RELEASE_DEVOPS.md#7-chính-sách-dependency)

### C. Chất lượng
- [ ] Build 0 warning; test xanh hai TFM; test mới **fail trên code cũ** (nếu là sửa lỗi)
- [ ] Test kết quả cho logic, test hợp đồng cho service, test XAML cho palette mới
- [ ] Không `catch {}` nuốt lỗi; lỗi được log ở biên
- [ ] Thông điệp người dùng rõ ràng, tiếng Việt nhất quán

### D. Vận hành và tài liệu
- [ ] Mô tả PR có *Summary* + *Verification* (smoke test nào, kết quả)
- [ ] CHANGELOG `[Unreleased]`; T-module / Phần 1–9 cập nhật nếu đổi quy ước
- [ ] Mọi tên API mới trong tài liệu **tồn tại** (kiểm metadata, xem [T04 §6](T04_DEBUGGING_TROUBLESHOOTING.md#6-kiểm-chứng-api-bằng-metadata))
- [ ] Thay đổi mục backlog thì cập nhật bảng §3

---

## 3. Backlog kỹ thuật đã kiểm chứng

Lập tại **v1.0.0** (2026-10-13); cột **Trạng thái** cập nhật sau **PR #6** (cùng ngày). Mức độ: 🔴 cao · 🟠 trung bình · 🟡 thấp · 🔵 kiến trúc/tài liệu. Cột **Lab** chỉ bài trong [T10](T10_LABS_AND_ASSESSMENT.md) dùng mục này làm đề: các lab đó giờ **làm trên tag `v1.0.0`** rồi so với PR #6.

Bằng chứng "Lab `TÊN`" là kiểm thử [HeadlessLab](../../tests/integration/README.md#headlesslab--kiểm-thử-headless-trên-autocad-thật) chạy trên AutoCAD 2024 thật; "test `TÊN`" là unit test trong `CadCoreFramework.Tests`. "*(FAIL trên v1.0.0)*" nghĩa là kiểm thử đã được chạy trên code cũ và thất bại: lỗi có thật, không phải suy đoán.

| Mã | Mức | Vấn đề | Bằng chứng | Hướng sửa | Trạng thái | Lab |
| :---: | :---: | :--- | :--- | :--- | :--- | :---: |
| B-01 | 🔴 | `IsDarkMode` đọc sysvar `DARKMODE` (**không tồn tại**), nên luôn coi là theme sáng; `ThemeManager` nghe nhầm biến | `(getvar "DARKMODE")` → `nil`, `(getvar "COLORTHEME")` → `0` trên AutoCAD 2024 ([A13](T03_PITFALLS_CASEBOOK.md#a13-palette-luôn-theme-sáng-dù-autocad-đang-tối)) | Đọc `COLORTHEME` (0 = tối); nghe `COLORTHEME`; log khi sysvar không đọc được | ✅ PR #6 — hàm thuần `CadApplicationHelper.IsDarkColorTheme`; lab `COLORTHEME_DRIVES_DARK_MODE`, test `IsDarkColorTheme_MapsColorThemeValues` | L12 |
| B-02 | 🟠 | Hai hệ màu song song: `Styles.xaml` cố định màu tối riêng; `Cad*` của ThemeManager dùng ở vài chỗ | Đọc `Themes/Styles.xaml` | Một nguồn màu duy nhất: brush trong `Styles.xaml` tham chiếu key theme bằng `DynamicResource` | ✅ PR #6 — `Styles.xaml` chỉ còn Style; màu ở hai file theme **cùng bộ key** (thêm `OnAccentBrush`, `SurfaceAltBrush`, `SurfaceHoverBrush`, `OverlayBrush`); test `StylesDictionary_DefinesNoColors`, `ThemeDictionaries_DefineExactlyTheSameKeys` (bắt được `CadComboBox` thiếu ở theme sáng) | L12 |
| B-03 | 🟡 | `LayerManagerControl.xaml` dùng 3 key không tồn tại | Script so khớp key ([T02 §10](T02_ARCHITECTURE_DEEP_DIVE.md#10-thememanager-styles-và-hệ-màu)) | Dùng key có thật; thêm test "mọi `DynamicResource` đều có định nghĩa" | ✅ PR #6 — test `EveryResourceKeyReferencedByPalettesAndStyles_Resolves` (4 palette × 2 theme) | L12 |
| B-04 | 🟠 | `TogglePalette`/`ShowPalette` tạo `PaletteSet` **không Guid**, nên AutoCAD không nhớ vị trí | `PaletteService.cs` | Thêm tham số Guid cho Toggle/Show; tạm thời dùng `GetOrCreatePaletteSet(name, guid)` trước ([T08 §4](T08_RECIPES_EXTENDING.md#4-thêm-một-palette)) | ✅ PR #6 — mọi PaletteSet có Guid ổn định từ `[assembly: Guid]` + tên (`Core/PaletteGuids`, UUID v8/SHA-256); test `PaletteGuidsTests`. Vị trí được nhớ qua lần khởi động: **cần kiểm trong GUI** | L08 |
| B-05 | 🟠 | Không ai dựng Ribbon; `AddButton` không idempotent; thứ tự đăng ký build action dễ sai | grep `AddTab`/`RegisterBuildAction` | `RemoveTab` + build idempotent + đăng ký trước `InitializeRibbon` ([T08 §6](T08_RECIPES_EXTENDING.md#6-thêm-ribbon-tab)) | ✅ PR #6 — `RemoveTab`; `AddButton` bỏ nút trùng lệnh; dựng ngay khi Ribbon sẵn sàng bất kể thứ tự gọi; tab "CadCore" (`Commands/CadCoreRibbon.cs`). **Cần kiểm trong GUI** | L11 |
| B-06 | 🟠 | `ExecuteAsync` không xác nhận có `SynchronizationContext` của main thread | `CadContext.cs` so với `CadAsyncHelper.cs` | Chụp và kiểm tra `SynchronizationContext.Current` khi bắt đầu; ném lỗi rõ ràng nếu null | ✅ PR #6 — kiểm `SynchronizationContext` khi bắt đầu; thêm `Core/CadThread`: mọi điểm vào `ICadContext` (và sau `await`) ném lỗi rõ nếu không ở main thread; test `CadThreadTests`, lab `INITIALIZE_RUNS_ON_COMMAND_THREAD` | L10 |
| B-07 | 🟡 | `ExecuteRead` commit transaction; `IsReadOnly` không chặn `GetObject(…, ForWrite)` | `CadContext.cs` | Ghi rõ trong XML doc; cân nhắc analyzer cấm `ForWrite` trong lambda `ExecuteRead` | ✅ PR #6 — XML doc + analyzer **CAD003**; test `AnalyzerTests.CAD003_*` | L03 |
| B-08 | 🟡 | `DEMO_ASYNC_AWAIT` vẽ vào `MdiActiveDocument` **sau** `await` | `Demos/DemoServicesCommands.cs` | Chụp `Document` trước `await` hoặc dùng `ExecuteAsync`; demo phải dạy mẫu đúng | ✅ PR #6 — dùng `ExecuteWithScopeAsync`; thông báo sau `await` ghi qua `scope.Editor` | L10 |
| B-09 | 🟡 | `XRecordHelper.RemoveMetadata` chỉ `Remove` khỏi dictionary, không `Erase` Xrecord (để lại đối tượng mồ côi) | `Utils/XRecordHelper.cs` | Mở Xrecord `ForWrite` rồi `Erase()` | ✅ PR #6 — lab `XRECORD_REMOVE_ERASES` *(FAIL trên v1.0.0)* | L06 |
| B-10 | 🟠 | `PlotService` không kiểm `ProcessPlotState`, không xử lý `BACKGROUNDPLOT` (mặc định **2**), không Dispose `PlotEngine` | Code + `getvar BACKGROUNDPLOT` → 2 | Kiểm trạng thái, tạm đặt `BACKGROUNDPLOT=0` rồi khôi phục, `using` engine | ✅ PR #6 — cùng B-28; lab `PLOT_LAYOUT_TO_PDF`, `PLOT_MULTIPLE_LAYOUTS_TO_PDF` (PDF 2 trang), `PLOT_UNKNOWN_LAYOUT_FAILS_CLEANLY` | (mở rộng L15) |
| B-11 | 🟡 | `PathProvider.GetPluginDirectory` hỏng khi nạp từ byte array (`Location == ""`, `Path.GetDirectoryName("")` ném) | Đã kiểm chứng trên .NET Framework; **hiện chưa có code sản phẩm gọi** | Kiểm tra `Location` rỗng, trả lỗi rõ ràng hoặc fallback | ✅ PR #6 — `PathProvider.DirectoryOf` ném `InvalidOperationException` nói rõ nguyên nhân; test `DirectoryOf_AssemblyLoadedFromBytes_ThrowsClearError` | — |
| B-12 | 🟡 | `CreateBlockDefinition`: block đã tồn tại thì entity truyền vào không được dùng cũng không được Dispose | `CadScopeLayerExtensions.cs` | Dispose các entity không dùng, hoặc ghi rõ quyền sở hữu trong XML doc | ✅ PR #6 — cả hai; lab `CREATE_BLOCKDEF_EXISTING_DISPOSES` *(FAIL trên v1.0.0)* | — |
| B-13 | 🟠 | `CopyEntity`/`MirrorEntity` dùng `Clone()`: bản sao **mất Extension Dictionary** (metadata XRecord của framework). *Đính chính:* giả thuyết ban đầu "block mất attribute" **sai** — `Clone()` vẫn giữ attribute riêng cho bản sao | Lab `COPY_KEEPS_METADATA` *(FAIL trên v1.0.0)*; `COPY_KEEPS_ATTRIBUTES`, `MIRROR_KEEPS_ATTRIBUTES` PASS cả trước lẫn sau ([B19](T03_PITFALLS_CASEBOOK.md#b19-sao-chép-entity-mất-metadata-extension-dictionary)) | `DeepCloneObjects` + `TransformBy` | ✅ PR #6 | (L07 mở rộng) |
| B-14 | 🟡 | `Draw*`: `configure` chạy **trước** append; nếu nó ném lỗi thì entity rò rỉ | `CadScope*Extensions.cs` | `try { configure } catch { entity.Dispose(); throw; }` | ✅ PR #6 — `CadScopeDrawingHelpers.AppendNew` cho mọi `Draw*`; lab `DRAW_CONFIGURE_THROWS_DISPOSES` *(FAIL trên v1.0.0)* | — |
| B-15 | 🟡 | `ShowPalette` chỉ log lỗi, `TogglePalette` ném lại: chính sách lỗi không nhất quán | `PaletteService.cs` | Thống nhất: ném lại ở cả hai | ✅ PR #6 | — |
| B-16 | 🟡 | `Ensure*` không validate tên ký hiệu | `CadScopeLayerExtensions.cs`; lab `ENSURE_LAYER_REJECTS_INVALID_NAME` *(FAIL trên v1.0.0: `eInvalidInput`, không nói ký tự nào sai)* | `SymbolUtilityServices.ValidateSymbolName(name, false)` (API đã kiểm chứng) | ✅ PR #6 — chọn `Utils/SymbolNameRules` thay vì API trên: thuần .NET nên kiểm **trước** khi chạm bản vẽ, unit test được, và `GridSpec` (tầng Model) dùng chung một luật; áp cho `Ensure*`, `CreateBlockDefinition`, `EnsureRegApp`; test `SymbolNameRulesTests` | — |
| B-17 | 🟡 | `SelectionFilterBuilder` không escape wildcard: tên **hợp lệ** chứa ký tự là wildcard trong filter (`# @ . ~ [ ] -`) bị hiểu sai — layer `LAB#1` không khớp chính nó, `M.EQUIP` khớp cả `M-EQUIP` (`* ? ,` không hợp lệ trong tên ký hiệu nên không gặp) | Lab `FILTER_ONLAYERS_IS_WILDCARD` | Thêm overload/escape helper (`` ` `` trước ký tự đặc biệt) | ✅ PR #6 — `OnLayersExact`, `WithBlockNamesExact`, `EscapeWildcards`; lab `FILTER_EXACT_MATCHES_LITERALLY` | L05 |
| B-18 | 🔵 | CAD001 theo cú pháp, bỏ sót prompt-trong-Service qua `CadPromptHelper` | Analyzer source | Rule CAD002: cấm `CadPromptHelper`/`Editor.Get*` trong Services/ViewModels | ✅ PR #6 — analyzer **CAD002**; test `AnalyzerTests.CAD002_*` | (đồ án) |
| B-19 | 🔵 | Comment giải thích sai lý do: `EnsureLayer` (eVetoed), `CadContextGuard` (eLockViolation/deadlock) | So với hành vi AutoCAD ([T01 §4.3](T01_AUTOCAD_NET_FOUNDATIONS.md#43-transaction-lồng-nhau)) | Sửa comment theo lý do thật | ✅ PR #6 — guard: "một Execute = một đơn vị công việc"; `Ensure*` mở bảng ForRead, `UpgradeOpen` chỉ khi thêm | — |
| B-20 | 🔵 | Không thể integration test headless (một DLL tham chiếu `AcMgd`) | [A5](T03_PITFALLS_CASEBOOK.md#a5-accoreconsole-crash-0xc00000fd-khi-netload-plugin) | ADR tách DLL lõi không UI ([T05 §9](T05_TESTING_STRATEGY.md#9-integration-test-headless-hiện-trạng-và-hướng-đi)) | ◐ Một phần, PR #6 — **HeadlessLab** (ADR-015): DLL chỉ cho kiểm thử, link mã không-UI, chạy trong accoreconsole, 16 kiểm thử. Lệnh, palette, `CadContext` vẫn kiểm trong GUI | — |
| B-21 | 🔵 | AutoCAD 2025+ / `net8.0-windows` chưa chạy thật | — | **Ngoài phạm vi hiện tại** | ⏸ Ngoài phạm vi | — |
| B-22 | 🟡 | `SettingsService.Save` ghi không atomic; nhiều phiên AutoCAD thì phiên ghi sau thắng | `SettingsService.cs` | Ghi file tạm rồi `File.Replace`; đọc lại trước khi ghi nếu cần merge | ✅ PR #6 — `FileSystemService.WriteAllText` atomic; `Save` đọc lại tệp và chỉ áp khóa phiên này đã đổi, có named mutex giữa các tiến trình, ghi lỗi thì giữ khóa cho lần sau; test `TwoRealSessions_SavingDifferentKeys_BothKeysSurvive` | — |
| B-23 | 🟠 | Palette chỉ áp theme **một lần** lúc tạo; không ai nghe `ThemeChanged` | grep `ThemeChanged` | Palette đăng ký `ThemeChanged` và áp lại (`ApplyThemeTo`); gỡ khi đóng | ✅ PR #6 — `ThemeManager.Attach(palette)` (đăng ký khi Loaded, gỡ khi Unloaded); `ApplyTheme` luôn phát sự kiện; test `AttachedPalette_FollowsThemeChangesWhileLoaded_AndStopsAfterUnloaded`. Đổi theme lúc palette đang mở: **cần kiểm trong GUI** | L12 |
| B-24 | 🟡 | `RunSafe` (đồng bộ): `IsBusy` không kịp hiện; exception chỉ vào `ErrorMessage`, không log | `ViewModelBase.cs` | Cho phép inject logger/handler lỗi; tài liệu hóa rằng busy chỉ có ý nghĩa với async | ✅ PR #6 — `ViewModelBase(ILoggerService?)`, ViewModel con nhận logger tùy chọn từ DI; test `RunSafe_FailingAction_LogsExceptionWithPrefix` | — |
| B-25 | 🟡 | Attribute căn khác Left **lệch** sau `InsertBlock`/`SetBlockAttribute` khi chưa `AdjustAlignment` *(đã kiểm chứng: có thật — tâm X = 42.507 thay vì 20)* | Lab `ATTRIBUTE_REALIGNED_AFTER_SET` *(FAIL trên v1.0.0)* | Lab với block attribute căn giữa, rồi quyết định thêm `AdjustAlignment(db)` | ✅ PR #6 — `InsertBlock`/`SetBlockAttribute` căn lại sau khi gán giá trị (MText: `UpdateMTextAttribute`, còn lại `AdjustAlignment`) | (L07 mở rộng) |
| B-26 | 🟡 | `CommonControlsPalette` gán DataContext cho `Grid` qua `ViewModelLocator`, nên PaletteService không Dispose được ViewModel đó. Hiện ViewModel này không subscribe gì nên **tác động rất nhỏ**, nhưng là mẫu dễ bị copy sai | `CommonControlsPalette.xaml` (DataContext chỉ ở `Grid`) + `PaletteService.DisposeExistingContent` | Gán DataContext cho chính UserControl như các palette khác | ✅ PR #6 — lấy từ DI, fail-loud; bỏ `ViewModelLocator` | — |
| B-27 | 🟡 | `PackageContents.xml` chỉ liệt kê 3 lệnh trong `<Commands>` | File manifest | Chỉ quan trọng khi chuyển sang demand-load: liệt kê đủ hoặc bỏ | ✅ PR #6 — đủ 9 lệnh không phải demo; test `PackageManifestTests` | — |
| B-28 | 🟠 | `PlotLayoutToPdf`/`PlotMultipleLayouts` **luôn thất bại**: ép `StringCollection` sang `IEnumerable<string>` (`InvalidCastException`); layout giấy: `SetPlotCentered` ném `eInvalidInput` (vùng in `Layout`); `eLayoutNotCurrent` khi layout không hiện hành; `BeginDocument` nhận số trang vào tham số *copies*. Không lệnh nào gọi hai hàm này nên lỗi không lộ | Phát hiện bởi lab `PLOT_*` khi làm B-10 (2026-10-13) | `Cast<string>()`; vùng in `Extents`; chuyển layout hiện hành rồi trả lại; kiểm tên layout trước khi in; copies = 1 | ✅ PR #6 | (L15) |
| B-29 | 🟡 | Lỗi hiển thị chỉ lộ khi **nhìn** palette: cột Result của Debug Inspector luôn "True/False" (giá trị local thắng setter của DataTrigger); mọi chữ trong tab đang chọn bị đậm (FontWeight của `TabItem` lan xuống nội dung); nút Secondary "biến mất" trên nền cùng màu (template không vẽ viền); tab đang chọn nền trắng ở theme tối | Render offscreen 4 palette × 2 theme (2026-10-13) | Giá trị mặc định đặt trong Style; template `TabItem` chỉ tô header; template nút vẽ viền | ✅ PR #6 | L12 |

> [!NOTE]
> **Quy tắc trung thực của backlog:** mục "cần kiểm chứng" phải ghi rõ như vậy (B-25 trước PR #6). Không nâng mức độ để gây chú ý, không hạ mức để đỡ việc. Khi sửa xong, ghi PR và ngày, đừng xóa dòng ngay: giữ một phiên bản để người sau biết lịch sử. Giả thuyết sai cũng phải **đính chính công khai** (B-13): ghi rõ điều đã tin, điều đã kiểm, và kết quả.

---

## 4. Lộ trình kiến trúc

| Giai đoạn | Việc | Mục |
| :--- | :--- | :--- |
| ~~Ngắn hạn~~ ✅ | Theme (B-01/02/03/23), Guid palette (B-04), Ribbon (B-05), lỗi đúng/sai (B-09, B-12…B-17, B-25), comment (B-19), context async (B-06) | Xong ở PR #6 (2026-10-13) |
| ~~Trung hạn~~ ✅ | Headless (B-20 → HeadlessLab, ADR-015), CAD002/CAD003 (B-18/B-07), settings atomic (B-22), PlotService (B-10, B-28) | Xong ở PR #6; B-20 một phần |
| **Tiếp theo** | Kiểm trong GUI các mục chỉ GUI mới thấy (vị trí palette B-04, Ribbon B-05, đổi theme trực tiếp B-23); mở rộng HeadlessLab tới `CadContext` (cần tách phần COM undo mark khỏi `AcMgd`) | Mỗi mục mới vào backlog phải kèm kiểm thử FAIL trên code cũ |
| **Dài hạn** | Kiểm chứng AutoCAD 2025+/net8 khi vào phạm vi (B-21); báo cáo lỗi có cấu trúc (telemetry nội bộ, chỉ khi người dùng đồng ý); đa ngôn ngữ thông điệp; ngân sách hiệu năng cho từng tính năng | Theo nhu cầu sản phẩm |

---

## 5. Trách nhiệm của Senior

| Mảng | Việc cụ thể |
| :--- | :--- |
| **Kiến trúc** | Giữ ADR; review mọi thay đổi chạm `Core/`; quyết định khi nào phá vỡ quy ước |
| **Chất lượng** | Duy trì 0 warning + test xanh; mở rộng analyzer; checklist review |
| **Phát hành** | Chạy checklist release ([T06 §5.3](T06_BUILD_RELEASE_DEVOPS.md#53-checklist-phát-hành)); quyết định PATCH/MINOR/MAJOR |
| **Sự cố** | Chủ trì chẩn đoán; incident report; cập nhật T03 |
| **Dependency** | Phê duyệt mọi package mới ([T06 §7](T06_BUILD_RELEASE_DEVOPS.md#7-chính-sách-dependency)) |
| **Tri thức** | Giữ bộ tài liệu này đúng; mentoring; đánh giá lên cấp ([T10 §4](T10_LABS_AND_ASSESSMENT.md#4-đề-đánh-giá-lên-cấp)) |
| **Ưu tiên** | Cân giữa tính năng mới và trả nợ backlog; nói "chưa" có lý do |

### Kỹ năng phi kỹ thuật

- **Viết cho người sau:** commit giải thích *vì sao*; PR có *Verification*; comment ghi lý do, không chép lại code.
- **Trung thực về mức độ chắc chắn:** "đã kiểm chứng trên AutoCAD 2024" khác "chắc là chạy". Ghi rõ điều chưa kiểm chứng (net8; B-25 trước khi có lab). Kiểm chứng xong mà giả thuyết sai thì nói thẳng là sai (B-13).
- **Tái hiện trước khi tranh luận:** báo cáo lỗi của người dùng phải có các bước tái hiện, trước khi bàn cách sửa.
- **Thu nhỏ thay đổi:** PR nhỏ, một mục đích, dễ review, dễ revert.
- **Biết khi nào dừng:** một tối ưu không có số đo là đoán; một abstraction chưa có ca dùng thứ hai là quá sớm.

---

## ✅ Tự kiểm tra T09

1. Chọn một ADR bạn **không đồng ý**. Viết ADR thay thế (bối cảnh, lựa chọn, hệ quả).
2. Đặt mình vào lúc phát hành v1.0.0 (mọi mục §3 còn mở). Bạn có 3 ngày. Chọn mục backlog nào để làm, vì sao? (Có nhiều đáp án đúng; mentor chấm *lập luận*.)
3. Một PR "tối ưu" `EnsureLayer` nhưng không có số đo. Bạn review thế nào?

<details><summary>Gợi ý</summary>

2. Một lựa chọn hợp lý: B-01 + B-23 (lỗi người dùng nhìn thấy, sửa nhỏ, cần kiểm trực quan), rồi B-04 (trải nghiệm hằng ngày). Lý do: tác động lên người dùng / công sức, rủi ro thấp, có cách kiểm chứng rõ.
3. Yêu cầu số đo trước/sau theo phương pháp ở [T07 §8](T07_PERFORMANCE_AND_SCALE.md#8-đo-đạc), trên kích thước dữ liệu thực tế (vd. `MaxCells`). Không có số đo thì không merge với lý do "tối ưu".
</details>

---

**Tiếp theo:** [T10 — Bài lab & đánh giá →](T10_LABS_AND_ASSESSMENT.md)
