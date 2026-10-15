> **📘 Tra cứu CadCoreFramework** · [Mục lục tài liệu](../README.md) · [← R04](R04_UI.md) · **R05** · [R06 →](R06_STANDARDS.md)

# R05 — Golden Sample: Khuôn Mẫu Chuẩn

> **Dùng khi:** bắt đầu một tính năng mới, hoặc phân vân "code này nên nằm ở tầng nào" · **Học bài bản:** [T02 §13](../training/T02_ARCHITECTURE_DEEP_DIVE.md#13-golden-sample-theo-dấu-một-lệnh-từ-đầu-đến-cuối) (theo dấu một lệnh từ đầu đến cuối), [T08 §2](../training/T08_RECIPES_EXTENDING.md#2-thêm-một-tính-năng-trọn-vẹn), lab L07 ở [T10](../training/T10_LABS_AND_ASSESSMENT.md)

Các lệnh `DEMO_*` minh họa **từng API riêng lẻ**. Golden Sample thì khác: nó là **một tính năng hoàn chỉnh** đi qua đủ mọi tầng, viết đúng chuẩn, có test. Khi bắt đầu một tính năng mới, hãy **copy thư mục [`Samples/GridMarker/`](../../Samples/GridMarker/)** và đổi tên. Đừng copy từ demo.

> Các lệnh `DEMO_*` nằm gọn trong thư mục `Demos/`. Khi dựng dự án thật từ template, đặt `<IncludeDemos>false</IncludeDemos>` trong `CadCoreFramework.csproj` (hoặc build `dotnet build -p:IncludeDemos=false`) để loại toàn bộ demo khỏi DLL, không cần sửa file nào khác.

**Tính năng:** vẽ một lưới các ô vuông đánh nhãn (A1, A2, …, B1…). Mỗi ô có khung + chữ, khung được gắn XData để đọc lại sau.

| Điểm vào | Cách dùng |
| :--- | :--- |
| Lệnh `SAMPLE_GRID` | Pick điểm bằng chuột, nhập số hàng/cột/khoảng cách trên command line |
| Lệnh `SHOW_PALETTE` | Palette WPF: nhập thông số + tọa độ gốc, bấm *Vẽ lưới* |

---

## Mục lục

1. [Bản đồ các tầng](#1-bản-đồ-các-tầng)
2. [Nguyên tắc #1 — Functional Core, Imperative Shell](#2-nguyên-tắc-1--functional-core-imperative-shell)
3. [Nguyên tắc #2 — Validate một lần, ở Model](#3-nguyên-tắc-2--validate-một-lần-ở-model)
4. [Nguyên tắc #3 — Hiểu đúng ngữ cảnh: Command vs Palette](#4-nguyên-tắc-3--hiểu-đúng-ngữ-cảnh-command-vs-palette)
5. [Nguyên tắc #4 — Lỗi là giá trị, không phải bất ngờ](#5-nguyên-tắc-4--lỗi-là-giá-trị-không-phải-bất-ngờ)
6. [Nguyên tắc #5 — Giao tiếp lỏng qua EventAggregator](#6-nguyên-tắc-5--giao-tiếp-lỏng-qua-eventaggregator)
7. [Nguyên tắc #6 — ViewModel không biết WPF](#7-nguyên-tắc-6--viewmodel-không-biết-wpf)
8. [Test](#8-test)
9. [Checklist khi copy làm tính năng mới](#9-checklist-khi-copy-làm-tính-năng-mới)

---

## 1. Bản đồ các tầng

```
                 ┌──────────────────────┐       ┌───────────────────────────┐
  Command line → │ GridMarkerCommands   │       │ GridMarkerPalette (XAML)  │ ← Palette
                 │  prompt → validate   │       │  chỉ có Binding           │
                 └──────────┬───────────┘       └─────────────┬─────────────┘
                            │                                 │ DataContext
                            │                   ┌─────────────▼─────────────┐
                            │                   │ GridMarkerViewModel       │
                            │                   │  validate, CanExecute,    │
                            │                   │  Status (enum)            │
                            │                   └─────────────┬─────────────┘
                            ▼                                 ▼
                 ┌─────────────────────────────────────────────────────────┐
                 │ GridSpec.Validate()   +   GridPlanner.Plan()            │  ← LOGIC THUẦN
                 │ (không có kiểu AutoCAD — test được 100%)                │
                 └──────────────────────────┬──────────────────────────────┘
                                            ▼
                 ┌─────────────────────────────────────────────────────────┐
                 │ IGridMarkerService → GridMarkerService                  │  ← VỎ AUTOCAD
                 │  ICadContext.TryExecute<T>(…, undoMarkName) + XData     │
                 │  publish GridDrawnMessage sau khi commit                │
                 └─────────────────────────────────────────────────────────┘
```

| File | Tầng | Phụ thuộc AutoCAD? | Test bằng |
| :--- | :--- | :---: | :--- |
| `GridSpec.cs` | Model + validate | ❌ | xUnit thuần |
| `GridPlanner.cs` | Logic thuần | ❌ | xUnit thuần — **kiểm kết quả** |
| `GridDrawnMessage.cs` | Message | ❌ | — |
| `IGridMarkerService.cs` | Hợp đồng | ❌ (chỉ kiểu plugin) | Mock |
| `GridMarkerService.cs` | Vỏ AutoCAD | ✅ | `FakeCadContext` — kiểm hợp đồng |
| `GridMarkerCommands.cs` | Điểm vào command | ✅ | Chạy thử thủ công trong AutoCAD ([T05 §7](../training/T05_TESTING_STRATEGY.md#7-những-gì-chỉ-autocad-thật-mới-kiểm-được)) |
| `GridMarkerViewModel.cs` | Điểm vào palette | ❌ | xUnit + Mock service |
| `GridMarkerPalette.xaml(.cs)` | View | (WPF) | `XamlPaletteTests` + chạy thử thủ công |

---

## 2. Nguyên tắc #1 — Functional Core, Imperative Shell

Code nằm **trong lambda** của `ICadContext.Execute` chỉ chạy được khi có AutoCAD thật. Nếu mọi logic đều nằm trong lambda, unit test chỉ kiểm được *"có gọi Execute không"*. `LayerServiceTests` cho thấy giới hạn này.

Golden Sample đưa **mọi tính toán** ra `GridPlanner`: một hàm thuần nhận `GridSpec` và trả về danh sách `GridCell` (tọa độ + nhãn bằng `double`/`string`, không phải `Point3d`). Service chỉ còn là một vòng lặp mỏng:

```csharp
// GridMarkerService.DrawCells (rút gọn) — toàn bộ code chạm AutoCAD
foreach (GridCell cell in cells)
{
    ObjectId frameId = scope.DrawRectangle(/* từ cell.CenterX/Y */);
    XDataHelper.SetMetadata(scope.Transaction, frame, XDataAppName, new GridMarkerTag { … });
    scope.DrawText(cell.Label, /* … */);
}
```

Nhờ vậy `GridPlannerTests` kiểm được *"ô B3 nằm ở (40, −20) và có nhãn B3"*, điều không thể làm nếu logic nằm trong lambda.

> **Quy tắc:** nếu một đoạn code không bắt buộc phải gọi AutoCAD API, nó không được nằm trong lambda của `Execute`.

---

## 3. Nguyên tắc #2 — Validate một lần, ở Model

`GridSpec.Validate()` trả về **danh sách** lỗi (không throw) để UI hiển thị tất cả lỗi cùng lúc. Cả Command và ViewModel đều gọi nó; không nơi nào tự viết lại luật.

- **ViewModel:** mỗi khi input đổi → `Revalidate()` → cập nhật `ValidationSummary`, `HasValidationErrors` và `DrawCommand.NotifyCanExecuteChanged()`. Nút *Vẽ* tự disable khi input sai.
- **Ô số trên palette** bind vào property **chuỗi** (`RowsText`, `SpacingText`…), không bind thẳng vào số. ViewModel đọc chuỗi bằng `ViewModels/NumberText`: dấu chấm thập phân như dòng lệnh, không phụ thuộc culture của máy. Ô nào không đọc được thành số thì `Revalidate()` báo lỗi của ô đó, **trước** các lỗi của `GridSpec.Validate()`. Đây là luật về cách *gõ* số trên UI, không phải luật của lưới. `GridSpec` vẫn tự chặn mọi giá trị sai, kể cả `NaN` và vô cực mà code nào đó truyền thẳng vào (B-34). Bind thẳng vào số thì WPF đọc `2,5` thành 25 và lặng lẽ giữ số cũ khi ô trống (B-31/B-32, [R04 §3](R04_UI.md#3-ô-nhập-số)).
- **Command:** sau khi prompt xong → `Validate()` → in từng lỗi bằng `notify.Warn` và dừng.
- **Planner:** gọi `Validate()` lần cuối và **throw** nếu sai: hàng rào bảo vệ khi có caller quên validate.

---

## 4. Nguyên tắc #3 — Hiểu đúng ngữ cảnh: Command vs Palette

| | Command (`SAMPLE_GRID`) | Palette (`GridMarkerViewModel`) |
| :--- | :--- | :--- |
| Ngữ cảnh | Command context | Application context (modeless) |
| Prompt người dùng | ✅ được — làm **trước** khi mở transaction | ❌ không — nhập tọa độ trên form |
| Lock document | `ICadContext` tự lo | `ICadContext` tự lo (bắt buộc ở đây) |
| Gom Undo | AutoCAD tự gom theo command | **Cần** `undoMarkName` |

Service dùng chung cho cả hai vì nó **không prompt** và luôn truyền `undoMarkName`. Đây là lý do `SampleDrawingService` cũ bị xóa: nó prompt bên trong service, nên gọi từ palette là sai ngữ cảnh và không test được. Hai ngữ cảnh khác nhau thế nào: [T01 §5](../training/T01_AUTOCAD_NET_FOUNDATIONS.md#5-document-lock-và-hai-ngữ-cảnh-thực-thi).

> **Quy tắc:** prompt thuộc về điểm vào (Command), không bao giờ thuộc về Service.

---

## 5. Nguyên tắc #4 — Lỗi là giá trị, không phải bất ngờ

- **Quy ước của `ICadContext`:** `Execute*` ném exception · `TryExecute*` trả `CadResult`, không bao giờ throw ([R02 §2](R02_ARCHITECTURE.md#2-hợp-đồng-của-icadcontext)).
- Service dùng `TryExecute<T>` → trả `CadResult<int>`. Lỗi AutoCAD (layer bị khóa, bản vẽ read-only) thành giá trị để caller quyết định.
- ⚠️ **Bẫy overload:** `Execute(scope => scope.DrawCircle(…))`: lambda biểu thức *trả về giá trị* nên C# chọn overload generic (trước đây là `Execute<T>`, nay đã `[Obsolete]` và có cảnh báo CS0618). Khi muốn ném lỗi, viết lambda dạng khối: `Execute(scope => { scope.DrawCircle(…); })`.
- **Command** gặp `!result.IsSuccess` → `throw` lại để `ExecuteCommand` in lỗi, ghi log, đánh dấu FAIL trong CommandHistory.
- **ViewModel** gặp `!result.IsSuccess` → `Status = Error` + thông báo. Exception bất ngờ được `RunSafe` bắt.
- Command lấy service bằng `GetRequiredService<T>()` (ném exception có thông điệp rõ) thay vì `ServiceProvider?.GetService<T>()` (âm thầm trả null → lệnh không làm gì).

---

## 6. Nguyên tắc #5 — Giao tiếp lỏng qua EventAggregator

Service publish `GridDrawnMessage` **sau khi** transaction commit. Palette subscribe để cập nhật số ô, kể cả khi lưới được vẽ từ lệnh `SAMPLE_GRID`.

```csharp
// GridMarkerViewModel constructor
TrackSubscription(events.Subscribe<GridDrawnMessage>(_ => RefreshCount()));
```

- `TrackSubscription` → tự gỡ khi ViewModel bị Dispose (PaletteService Dispose ViewModel khi đóng palette). Không leak.
- Service bọc `Publish` trong try/catch: bản vẽ **đã commit**, nên lỗi của một subscriber chỉ được ghi log, không biến thao tác thành "thất bại".

---

## 7. Nguyên tắc #6 — ViewModel không biết WPF

`SampleViewModel` cũ giữ `Brush` để tô màu trạng thái → ViewModel phụ thuộc WPF. Golden Sample dùng enum `GridMarkerStatus { Idle, Success, Error }` và để XAML tự chọn màu bằng `DataTrigger`:

```xml
<DataTrigger Binding="{Binding Status}" Value="{x:Static local:GridMarkerStatus.Error}">
    <Setter Property="Foreground" Value="{DynamicResource DangerBrush}"/>
</DataTrigger>
```

Code-behind chỉ làm hạ tầng: lấy ViewModel từ DI (fail-loud nếu thiếu), nạp `Styles.xaml` + theme lúc runtime, gọi `RefreshCount` khi `Loaded`.

---

## 8. Test

| File | Số test | Kiểm gì |
| :--- | :---: | :--- |
| `GridSpecTests` | 23 | Mọi luật validate, kể cả `NaN`/vô cực ở khoảng cách, cỡ ô, cao chữ; báo đủ nhiều lỗi cùng lúc |
| `GridPlannerTests` | 18 | Số ô, tọa độ, thứ tự, nhãn A…Z/AA…/AAA, spec sai |
| `GridMarkerServiceTests` | 8 | Một `TryExecute` + đúng undo mark, không mở transaction khi rỗng, publish khi thành công / không publish khi lỗi, subscriber lỗi không làm hỏng kết quả |
| `GridMarkerViewModelTests` | 23 | `CanExecute` theo validate, truyền đúng spec/cells, trạng thái thành công/lỗi/exception, message → refresh, Dispose → ngừng nghe; ô số: `2,5`, ô trống, `NaN` là lỗi, số ↔ chuỗi đồng bộ hai chiều |
| `NumberTextTests` | 26 | Đọc số như dòng lệnh AutoCAD: dấu chấm, không dấu phân cách hàng nghìn, chặn `NaN`/vô cực/tràn số, không phụ thuộc culture của máy |
| `XamlPaletteTests` (phần Grid Marker) | 2 + 1 | Gõ vào ô thật của palette: `2,5` và ô trống làm tắt nút Vẽ; cộng một test cho **mọi** palette: không TextBox nào bind thẳng vào property số |

Toàn bộ test chạy trên **cả `net48` và `net8.0-windows`** (hai runtime của AutoCAD 2021–2024 và 2025+). Cách viết từng loại test: [T05](../training/T05_TESTING_STRATEGY.md).

Tích hợp với AutoCAD thật: kịch bản `SAMPLE_GRID` với `0,0 · 2 hàng · 3 cột · khoảng cách 20` nằm trong `tests/integration/smoke.scr` **và** trong checklist smoke test thủ công ([T05 §7](../training/T05_TESTING_STRATEGY.md#7-những-gì-chỉ-autocad-thật-mới-kiểm-được)). Runner accoreconsole hiện **không** nạp được plugin, vì plugin tham chiếu `AcMgd` mà accoreconsole không nạp được `AcMgd` ([T03 A5](../training/T03_PITFALLS_CASEBOOK.md#a5-accoreconsole-crash-0xc00000fd-khi-netload-plugin)). Vì vậy bước kiểm tra trên AutoCAD thật hiện là thủ công.

---

## 9. Checklist khi copy làm tính năng mới

1. Copy `Samples/GridMarker/` → `Features/<TênTínhNăng>/` (hoặc thư mục bạn chọn), đổi tên class và namespace.
2. **Model:** khai báo input bất biến + `Validate()` trả danh sách lỗi.
3. **Planner:** đưa toàn bộ tính toán vào hàm thuần, không dùng kiểu AutoCAD.
4. **Service:** interface chỉ dùng kiểu plugin; implementation gọi `ICadContext` (`TryExecute` nếu muốn trả `CadResult`) với `undoMarkName`; không prompt (CAD002 sẽ báo), không `MdiActiveDocument` (CAD001), không ghi trong `ExecuteRead` (CAD003). Ba rule: [R06 §5](R06_STANDARDS.md#5-analyzer-cad001cad003).
5. **Command:** prompt → validate → plan → service → báo kết quả; bọc trong `ExecuteCommand`; dùng `GetRequiredService<T>()`. Thêm lệnh vào `Deployment/PackageContents.xml` (test `PackageManifestTests` kiểm); muốn có nút thì thêm một dòng vào `Commands/CadCoreRibbon.cs`.
6. **ViewModel:** không dùng kiểu WPF/AutoCAD; `CanExecute` gắn với validate; subscription qua `TrackSubscription`; nhận `ILoggerService? logger = null` và chuyển cho `base(logger)` để lỗi trong `RunSafe*` được log.
7. **Đăng ký:** `AppBootstrapper.ConfigureServices` (service Singleton, ViewModel Transient) + `[assembly: CommandClass]` trong `Properties/AssemblyInfo.cs`.
8. **Palette:** code-behind gọi `ThemeManager.Attach(this)` sau `InitializeComponent()`; XAML chỉ dùng key theme qua `{DynamicResource}`, không mã màu cứng. Ô số bind vào property chuỗi rồi đọc bằng `NumberText` ([§3](#3-nguyên-tắc-2--validate-một-lần-ở-model)); `EveryPaletteTextBox_BindsToAStringProperty` kiểm điều này cho mọi palette có trong mảng `Palettes`.
9. **Test:** Planner + Model (kết quả), Service (`FakeCadContext`), ViewModel (Mock), palette (`XamlPaletteTests` + thêm vào mảng `Palettes` để chạy test theme). Helper mới trong `Extensions/`/`Utils/` có hành vi AutoCAD đáng ngờ → thêm kiểm thử [HeadlessLab](../../tests/integration/README.md#headlesslab--kiểm-thử-headless-trên-autocad-thật). Thêm kịch bản của tính năng vào checklist smoke test thủ công ([T05 §7](../training/T05_TESTING_STRATEGY.md#7-những-gì-chỉ-autocad-thật-mới-kiểm-được)) và vào `smoke.scr` (mỗi lệnh kèm thông điệp thành công trong `$expected` của runner).

Checklist trước khi mở PR: [T08 §13](../training/T08_RECIPES_EXTENDING.md#13-checklist-trước-khi-mở-pr).
