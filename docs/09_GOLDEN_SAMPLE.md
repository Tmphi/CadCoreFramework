# Phần 9: Golden Sample — Khuôn Mẫu Chuẩn

Các lệnh `DEMO_*` minh họa **từng API riêng lẻ**. Golden Sample thì khác: nó là **một tính năng hoàn chỉnh** đi qua đủ mọi tầng, viết đúng chuẩn, có test. Khi bắt đầu một tính năng mới, hãy **copy thư mục `Samples/GridMarker/`** và đổi tên — đừng copy từ demo.

**Tính năng:** vẽ một lưới các ô vuông đánh nhãn (A1, A2, …, B1…). Mỗi ô có khung + chữ, khung được gắn XData để đọc lại sau.

| Điểm vào | Cách dùng |
| :--- | :--- |
| Lệnh `SAMPLE_GRID` | Pick điểm bằng chuột, nhập số hàng/cột/khoảng cách trên command line |
| Lệnh `SHOW_PALETTE` | Palette WPF: nhập thông số + tọa độ gốc, bấm *Vẽ lưới* |

---

## 9.1 Bản đồ các tầng

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
                 │  ICadContext.Execute<T>(…, undoMarkName) + XData        │
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
| `GridMarkerCommands.cs` | Điểm vào command | ✅ | `smoke.scr` (AutoCAD thật) |
| `GridMarkerViewModel.cs` | Điểm vào palette | ❌ | xUnit + Mock service |
| `GridMarkerPalette.xaml(.cs)` | View | (WPF) | Chạy thử thủ công |

---

## 9.2 Nguyên tắc #1 — Functional Core, Imperative Shell

Code nằm **trong lambda** của `ICadContext.Execute` chỉ chạy được khi có AutoCAD thật. Nếu mọi logic đều nằm trong lambda, unit test chỉ kiểm được *"có gọi Execute không"* — xem `LayerServiceTests` để thấy giới hạn này.

Golden Sample đưa **mọi tính toán** ra `GridPlanner` — một hàm thuần nhận `GridSpec` và trả về danh sách `GridCell` (tọa độ + nhãn bằng `double`/`string`, không phải `Point3d`). Service chỉ còn là một vòng lặp mỏng:

```csharp
// GridMarkerService.DrawCells — toàn bộ code chạm AutoCAD
foreach (GridCell cell in cells)
{
    ObjectId frameId = scope.DrawRectangle(/* từ cell.CenterX/Y */);
    XDataHelper.SetMetadata(scope.Transaction, frame, XDataAppName, new GridMarkerTag { … });
    scope.DrawText(cell.Label, /* … */);
}
```

Nhờ vậy `GridPlannerTests` kiểm được *"ô B3 nằm ở (40, −20) và có nhãn B3"* — điều không thể làm nếu logic nằm trong lambda.

> **Quy tắc:** nếu một đoạn code không bắt buộc phải gọi AutoCAD API, nó không được nằm trong lambda của `Execute`.

---

## 9.3 Nguyên tắc #2 — Validate một lần, ở Model

`GridSpec.Validate()` trả về **danh sách** lỗi (không throw) để UI hiển thị tất cả lỗi cùng lúc. Cả Command và ViewModel đều gọi nó — không nơi nào tự viết lại luật.

- **ViewModel:** mỗi khi input đổi → `Revalidate()` → cập nhật `ValidationSummary`, `HasValidationErrors` và `DrawCommand.NotifyCanExecuteChanged()`. Nút *Vẽ* tự disable khi input sai.
- **Command:** sau khi prompt xong → `Validate()` → in từng lỗi bằng `notify.Warn` và dừng.
- **Planner:** gọi `Validate()` lần cuối và **throw** nếu sai — hàng rào bảo vệ khi có caller quên validate.

---

## 9.4 Nguyên tắc #3 — Hiểu đúng ngữ cảnh: Command vs Palette

| | Command (`SAMPLE_GRID`) | Palette (`GridMarkerViewModel`) |
| :--- | :--- | :--- |
| Ngữ cảnh | Command context | Application context (modeless) |
| Prompt người dùng | ✅ được — làm **trước** khi mở transaction | ❌ không — nhập tọa độ trên form |
| Lock document | `ICadContext` tự lo | `ICadContext` tự lo (bắt buộc ở đây) |
| Gom Undo | AutoCAD tự gom theo command | **Cần** `undoMarkName` |

Service dùng chung cho cả hai vì nó **không prompt** và luôn truyền `undoMarkName`. Đây là lý do `SampleDrawingService` cũ bị xóa: nó prompt bên trong service, nên gọi từ palette là sai ngữ cảnh và không test được.

> **Quy tắc:** prompt thuộc về điểm vào (Command), không bao giờ thuộc về Service.

---

## 9.5 Nguyên tắc #4 — Lỗi là giá trị, không phải bất ngờ

- Service dùng `Execute<T>` → trả `CadResult<int>`. Lỗi AutoCAD (layer bị khóa, bản vẽ read-only) thành giá trị để caller quyết định.
- **Command** gặp `!result.IsSuccess` → `throw` lại để `ExecuteCommand` in lỗi, ghi log, đánh dấu FAIL trong CommandHistory.
- **ViewModel** gặp `!result.IsSuccess` → `Status = Error` + thông báo. Exception bất ngờ được `RunSafe` bắt.
- Command lấy service bằng `GetRequiredService<T>()` (ném exception có thông điệp rõ) thay vì `ServiceProvider?.GetService<T>()` (âm thầm trả null → lệnh không làm gì).

---

## 9.6 Nguyên tắc #5 — Giao tiếp lỏng qua EventAggregator

Service publish `GridDrawnMessage` **sau khi** transaction commit. Palette subscribe để cập nhật số ô — kể cả khi lưới được vẽ từ lệnh `SAMPLE_GRID`.

```csharp
// GridMarkerViewModel constructor
TrackSubscription(events.Subscribe<GridDrawnMessage>(_ => RefreshCount()));
```

- `TrackSubscription` → tự gỡ khi ViewModel bị Dispose (PaletteService Dispose ViewModel khi đóng palette). Không leak.
- Service bọc `Publish` trong try/catch: bản vẽ **đã commit**, nên lỗi của một subscriber chỉ được ghi log, không biến thao tác thành "thất bại".

---

## 9.7 Nguyên tắc #6 — ViewModel không biết WPF

`SampleViewModel` cũ giữ `Brush` để tô màu trạng thái → ViewModel phụ thuộc WPF. Golden Sample dùng enum `GridMarkerStatus { Idle, Success, Error }` và để XAML tự chọn màu bằng `DataTrigger`:

```xml
<DataTrigger Binding="{Binding Status}" Value="{x:Static local:GridMarkerStatus.Error}">
    <Setter Property="Foreground" Value="{DynamicResource DangerBrush}"/>
</DataTrigger>
```

Code-behind chỉ làm hạ tầng: lấy ViewModel từ DI (fail-loud nếu thiếu), nạp `Styles.xaml` + theme lúc runtime, gọi `RefreshCount` khi `Loaded`.

---

## 9.8 Test

| File | Số test | Kiểm gì |
| :--- | :---: | :--- |
| `GridSpecTests` | 18 | Mọi luật validate, báo đủ nhiều lỗi cùng lúc |
| `GridPlannerTests` | 18 | Số ô, tọa độ, thứ tự, nhãn A…Z/AA…/AAA, spec sai |
| `GridMarkerServiceTests` | 8 | Một `Execute` + đúng undo mark, không mở transaction khi rỗng, publish khi thành công / không publish khi lỗi, subscriber lỗi không làm hỏng kết quả |
| `GridMarkerViewModelTests` | 12 | `CanExecute` theo validate, truyền đúng spec/cells, trạng thái thành công/lỗi/exception, message → refresh, Dispose → ngừng nghe |

Tích hợp với AutoCAD thật: `tests/integration/smoke.scr` chạy `SAMPLE_GRID` với `0,0 · 2 hàng · 3 cột · khoảng cách 20`.

---

## 9.9 Checklist khi copy làm tính năng mới

1. Copy `Samples/GridMarker/` → `Features/<TênTínhNăng>/` (hoặc thư mục bạn chọn), đổi tên class và namespace.
2. **Model:** khai báo input bất biến + `Validate()` trả danh sách lỗi.
3. **Planner:** đưa toàn bộ tính toán vào hàm thuần, không dùng kiểu AutoCAD.
4. **Service:** interface chỉ dùng kiểu plugin; implementation gọi `ICadContext` với `undoMarkName`; không prompt, không `MdiActiveDocument` (CAD001 sẽ báo).
5. **Command:** prompt → validate → plan → service → báo kết quả; bọc trong `ExecuteCommand`; dùng `GetRequiredService<T>()`.
6. **ViewModel:** không dùng kiểu WPF/AutoCAD; `CanExecute` gắn với validate; subscription qua `TrackSubscription`.
7. **Đăng ký:** `AppBootstrapper.ConfigureServices` (service Singleton, ViewModel Transient) + `[assembly: CommandClass]` trong `Properties/AssemblyInfo.cs`.
8. **Test:** Planner + Model (kết quả), Service (`FakeCadContext`), ViewModel (Mock). Thêm một bước vào `smoke.scr`.
