# API Reference

Tài liệu tham khảo nhanh các hàm và lớp cốt lõi của CadCoreFramework. Chữ ký dưới đây khớp với code v1.0.0. Đọc kỹ cách các lớp này phối hợp ở [T02: Mổ xẻ kiến trúc](training/T02_ARCHITECTURE_DEEP_DIVE.md).

## 1. Extension methods trên `ICadScope` (`CadScope*Extensions`)

Bộ hàm vẽ một dòng, nằm trong namespace `CadCoreFramework.Extensions` (chia theo file `CadScopeLineExtensions`, `CadScopeTextExtensions`, `CadScopeBlockExtensions`, `CadScopeDimensionExtensions`, `CadScopeLayerExtensions`, `CadScopeQueryExtensions`). Đặc điểm chung:
- Là extension của `ICadScope`, nên **chạy bên trong transaction của scope** mà `ICadContext.Execute*` đã mở. Các hàm này **không** tự mở transaction hay lock.
- Gọi `SetDatabaseDefaults`, gán **layer** (tự `EnsureLayer` nếu chưa có) và **màu ACI** khi được truyền.
- Tham số `configure` (`Action<T>`) để chỉnh mọi thuộc tính khác trước khi append.
- Tọa độ là **WCS**. Trả `ObjectId`, hoặc `ObjectId.Null` khi input không hợp lệ.

**Phương thức nổi bật:**
- `ObjectId DrawLine(Point3d startPoint, Point3d endPoint, string layer = null, short? colorIndex = null, LineWeight lineWeight = ByLayer, Action<Line> configure = null)`
- `ObjectId DrawCircle(Point3d center, double radius, …)`
- `ObjectId DrawRectangle(Point3d corner1, Point3d corner2, string layer = null, short? colorIndex = null, double lineWidth = 0, Action<Polyline> configure = null)`
- `ObjectId DrawPolyline(IEnumerable<Point2d> points, bool isClosed = true, …)` / `DrawPolyline(IEnumerable<PolylineVertex> vertices, …)` (có bulge)
- `ObjectId DrawText(string text, Point3d position, double height = 2.5, …, TextHorizontalMode justification = TextLeft, Action<DBText> configure = null)`
- `ObjectId DrawMText(string text, Point3d location, double textHeight = 2.5, double width = 0, AttachmentPoint attachment = BottomLeft, string layer = null, short? colorIndex = null, string textStyle = null, Action<MText> configure = null)`
- `ObjectId InsertBlock(string blockName, Point3d position, double rotation = 0, double scale = 1, Dictionary<string,string> attributes = null, Dictionary<string,object> dynamicProperties = null, string layer = null)`: tạo đủ `AttributeReference`; trả `ObjectId.Null` (kèm cảnh báo) nếu block không có
- `ObjectId DrawHatch(ObjectId boundaryId, …)`, `DrawTable(…)`, `CreateGroup(…)`, `CopyEntity(…)`, `MirrorEntity(…)`
- `ObjectId EnsureLayer(string layerName, short colorIndex = 7, string linetypeName = null)`, `EnsureTextStyle`, `EnsureLinetype`, `EnsureDimStyle`
- `void SetXRecord<T>(ObjectId entityId, string key, T data)`, `T GetXRecord<T>(…)`, `void RemoveXRecord(…)`
- `List<T> GetEntities<T>(string layer = null)`. Các `Ask*()` đã **[Obsolete]** (prompt trong transaction), dùng `CadPromptHelper` trước khi mở transaction.

## 2. EventAggregator (`IEventAggregator`)

Pub/sub trong tiến trình. Giữ **tham chiếu mạnh** tới subscriber (không phải WeakReference), nên **bắt buộc** Dispose subscription khi không còn dùng. Trong ViewModel, dùng `TrackSubscription(...)`.
- `IDisposable? Subscribe<T>(Action<T>? action)`: Dispose một lần là gỡ (one-shot).
- `void Unsubscribe<T>(Action<T>? action)`
- `void Publish<T>(T message)`: gọi **đồng bộ** trên thread hiện tại; gọi đủ mọi subscriber rồi mới ném lại lỗi (1 lỗi giữ nguyên kiểu, nhiều lỗi thành `AggregateException`).

## 3. Lọc chọn (`SelectionFilterBuilder`)

Fluent API tạo mảng `TypedValue` cho `SelectionFilter`: `OfTypes`, `OnLayers`, `WithBlockNames`, `WithColor`, `WithXData`, `Custom`, `Build`.
```csharp
var filter = SelectionFilterBuilder.Create()
    .OfTypes("LINE", "CIRCLE")
    .OnLayers("DIMENSIONS")
    .Build();
```
Lưu ý: giá trị được ghép bằng dấu phẩy và **không** escape ký tự wildcard. `WithBlockNames` **không** bắt được dynamic block đã chỉnh tham số (tên ẩn danh `*U…`), xem [T03 B12](training/T03_PITFALLS_CASEBOOK.md#b12-lọc-block-theo-tên-bỏ-sót-dynamic-block).

## 4. Metadata (XData & XRecord)

- **`XDataHelper`** / `entity.SetMetadata(tr, appName, data)`: JSON chia khúc 250 ký tự trong XData. Chặn ở **15.000 ký tự** vì giới hạn XData là 16 KB **mỗi đối tượng**. Tự đăng ký RegApp.
- **`XRecordHelper`** / `scope.SetXRecord(...)`: JSON chia khúc 2.000 ký tự trong Xrecord của Extension Dictionary. Dùng cho dữ liệu lớn.
- Chọn loại nào: [T08 §8](training/T08_RECIPES_EXTENDING.md#8-lưu-metadata-xdata-hay-xrecord).

## 5. UI (Ribbon & Palette)

- **`RibbonService`**: API `AddTab/AddPanel/AddButton/AddSplitButton` + `RegisterBuildAction` (tự dựng lại sau `CUI`/`MENULOAD`); gỡ event khi Shutdown. Dự án v1.0.0 **chưa dựng tab nào**, công thức ở [T08 §6](training/T08_RECIPES_EXTENDING.md#6-thêm-ribbon-tab).
- **`PaletteService`**: `TogglePalette(name, Func<FrameworkElement> contentFactory, tabTitle)` (tạo content lười, Dispose ViewModel cũ khi thay), `ShowPalette`, `GetOrCreatePaletteSet(name, Guid?)` (Guid ổn định thì AutoCAD nhớ vị trí palette).

## 6. Utilities

- **`CadPromptHelper`**: `GetPoint`, `GetCorner`, `GetDistance`, `GetAngle`, `GetString`, `GetKeywords`, `GetInteger`, `GetDouble`, `GetEntity`, `GetSelection`. Hủy (ESC) thì trả `null` / `ObjectId.Null` / mảng rỗng. Điểm trả về được đổi **UCS → WCS**.
- **`CoordinateHelper`**: `UcsToWcs` / `WcsToUcs` (điểm, vector, danh sách điểm), `GetCurrentMatrices()`.
- **`GeometryExtensions`**: `MidPointTo`, `Offset`, `ToPoint2d` / `ToPoint3d`, `AngleTo2D`, `Perpendicular2D`, `NormalizeSafe`, và cho `Extents3d`: `Width`, `Height`, `Center`, `Expand`, `UnionWith`.
- **`Guard`**: `NotNull`, `NotEmpty`, `IsTrue`.
- **`DynamicBlockHelper`**: `GetEffectiveName`, `GetDynamicProperties`, `GetDynamicProperty<T>`, `SetDynamicProperty`, `GetVisibilityStates`, `GetVisibilityState`, `SetVisibilityState`.
- **`DimensionHelper`**: `CreateAlignedDimension`, `CreateLinearDimension`.
- **`TableHelper` (`CadTableBuilder`)**: Fluent `SetTitle/SetPosition/SetRowHeight/SetTextHeight/AddColumn/AddRow/FromCollection/Build`.
- **`TextStyleHelper`**: `EnsureTextStyle`, `SetCurrentTextStyle`, `GetAllTextStyleNames`.
- **`CadAsyncHelper`**: `ExecuteSafeAsync` (việc nặng trên ThreadPool, kết quả được marshal tường minh về main thread) và `FireAndForget(onError)`.
