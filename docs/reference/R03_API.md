> **📘 Tra cứu CadCoreFramework** · [Mục lục tài liệu](../README.md) · [← R02](R02_ARCHITECTURE.md) · **R03** · [R04 →](R04_UI.md)

# R03 — API: Vẽ, Tương Tác, Metadata Và Service

> **Dùng khi:** cần tra tên hàm, tham số mặc định, hoặc nhớ "dự án đã có sẵn cái này chưa" · **Học bài bản:** [T02 §6](../training/T02_ARCHITECTURE_DEEP_DIVE.md#6-extensions-và-utils-api-vẽ-một-dòng) (mẫu chung của hàm `Draw*`), [T01 §7](../training/T01_AUTOCAD_NET_FOUNDATIONS.md#7-editor-prompt-hệ-tọa-độ-selection) (prompt, tọa độ, selection)

Chữ ký dưới đây chép từ code hiện tại của `main`. Nếu thấy lệch: **code là sự thật**, hãy sửa trang này bằng một PR. Kiểu `ICadContext`, `CadResult` và lớp lệnh nằm ở [R02](R02_ARCHITECTURE.md); palette, ribbon, theme ở [R04](R04_UI.md).

---

## Mục lục

1. [Hỏi người dùng: `CadPromptHelper`](#1-hỏi-người-dùng-cadprompthelper)
2. [Vẽ và bảng ký hiệu trên `ICadScope`](#2-vẽ-và-bảng-ký-hiệu-trên-icadscope)
3. [Truy vấn và lọc chọn](#3-truy-vấn-và-lọc-chọn)
4. [Metadata: XData và XRecord](#4-metadata-xdata-và-xrecord)
5. [Block và dynamic block](#5-block-và-dynamic-block)
6. [Jig: hình chạy theo chuột](#6-jig-hình-chạy-theo-chuột)
7. [Entity, hình học, tọa độ](#7-entity-hình-học-tọa-độ)
8. [Service có sẵn](#8-service-có-sẵn)
9. [Tiện ích khác](#9-tiện-ích-khác)

---

## 1. Hỏi người dùng: `CadPromptHelper`

🔎 [`Utils/CadPromptHelper.cs`](../../Utils/CadPromptHelper.cs)

**Luật thép: HỎI trước, VẼ sau.** Prompt trong lúc đang mở transaction là giữ lock bản vẽ suốt thời gian người dùng chần chừ. Vì vậy prompt nằm ở **lệnh**, trước khi gọi `ICadContext`; Service và ViewModel không bao giờ prompt (analyzer **CAD002**, [R06 §5](R06_STANDARDS.md#5-analyzer-cad001cad003)).

```csharp
// PHA 1: Yêu cầu người dùng (Màn hình chưa bị khoá)
Point3d? p1 = CadPromptHelper.GetPoint("\nChọn điểm đầu:");
if (p1 == null) return; // Xử lý nhấn ESC hợp lệ

Point3d? p2 = CadPromptHelper.GetPoint("\nChọn điểm cuối:", p1);
if (p2 == null) return;

// PHA 2: Mở Transaction và Vẽ (Bản vẽ bị khoá)
_cadContext.Execute(scope => 
{
    scope.DrawLine(p1.Value, p2.Value);
});
```

| Hàm | Trả về | Khi ESC |
| :--- | :--- | :--- |
| `GetPoint(string message, Point3d? basePoint = null, bool convertToWcs = true)` | `Point3d?` | `null` |
| `GetCorner(string message, Point3d basePoint, bool convertToWcs = true)` | `Point3d?` | `null` |
| `GetDistance(string message, Point3d? basePoint = null)` | `double?` | `null` |
| `GetAngle(string message, Point3d? basePoint = null)` | `double?` | `null` |
| `GetString(string message, bool allowSpaces = true, string defaultVal = null)` | `string` | `null` |
| `GetKeywords(string message, string[] keywords, string defaultKeyword = null)` | `string` | `null` |
| `GetInteger(string message, int? defaultValue = null, int? lowerLimit = null, int? upperLimit = null)` | `int?` | `null` |
| `GetDouble(string message, double? defaultValue = null)` | `double?` | `null` |
| `GetEntity(string message, Type allowedType = null, string rejectMessage = …)` | `ObjectId` | `ObjectId.Null` |
| `GetSelection(string message = …, SelectionFilter filter = null)` / `GetSelection(string message, SelectionFilterBuilder)` | `ObjectId[]` | mảng rỗng |

- **Tọa độ:** `GetPoint`/`GetCorner` mặc định đổi điểm nhận được từ **UCS** (hệ người dùng có thể đã xoay) sang **WCS** (hệ gốc của database), và đổi `basePoint` theo chiều ngược lại. Mọi hàm `Draw*` nhận WCS, nên bạn truyền thẳng kết quả vào là đúng. Dùng `Editor.GetPoint` thô thì nhận UCS: hình sẽ lệch khi UCS xoay ([T01 §7.3](../training/T01_AUTOCAD_NET_FOUNDATIONS.md#73-ba-hệ-tọa-độ-wcs-ucs-ocs)).
- **ESC** khi đang prompt **không** ném exception: AutoCAD trả `PromptStatus.Cancel`, helper quy về giá trị "rỗng" ở cột cuối.
- Bảng chọn bề mặt UI (prompt, palette, hộp thoại, ribbon) và bẫy của từ khóa, giá trị mặc định: [U02](../training/U02_UI_IN_AUTOCAD.md).

> [!NOTE]
> Các hàm `scope.Ask*()` (`AskPoint`, `AskString`…) trong `CadScopeQueryExtensions` đã **`[Obsolete]`**: chúng prompt bên trong transaction. Dùng `CadPromptHelper` trước khi mở transaction.

---

## 2. Vẽ và bảng ký hiệu trên `ICadScope`

🔎 [`Extensions/`](../../Extensions/): `CadScopeLineExtensions`, `CadScopeTextExtensions`, `CadScopeBlockExtensions`, `CadScopeDimensionExtensions`, `CadScopeLayerExtensions`, `CadScopeQueryExtensions`, `XRecordExtensions` (namespace `CadCoreFramework.Extensions`)

Đặc điểm chung của mọi hàm `Draw*`:
- Là extension của `ICadScope`, nên chạy **bên trong** transaction mà `ICadContext.Execute*` đã mở. Các hàm này **không** tự mở transaction hay lock.
- Gọi `SetDatabaseDefaults`, gán **layer** (tự `EnsureLayer` nếu chưa có) và **màu ACI** khi được truyền.
- Tham số `configure` (`Action<T>`) để chỉnh mọi thuộc tính khác trước khi append.
- Tọa độ là **WCS**. Trả `ObjectId`, hoặc `ObjectId.Null` khi input không hợp lệ.

**Đường và hình** (`CadScopeLineExtensions`):

| Hàm | Tham số sau tham số hình học |
| :--- | :--- |
| `DrawLine(Point3d startPoint, Point3d endPoint, …)` | `string layer = null, short? colorIndex = null, LineWeight lineWeight = ByLayer, Action<Line> configure = null` |
| `DrawCircle(Point3d center, double radius, …)` | `Vector3d? normal = null, layer, colorIndex, Action<Circle> configure` |
| `DrawArc(Point3d center, double radius, double startAngleRad, double endAngleRad, …)` | `normal, layer, colorIndex, configure` |
| `DrawEllipse(Point3d center, Vector3d majorAxis, double minorToMajorRatio = 0.5, …)` | `normal, layer, colorIndex, configure` |
| `DrawRectangle(Point3d corner1, Point3d corner2, …)` | `layer, colorIndex, double lineWidth = 0, Action<Polyline> configure` |
| `DrawFilletRectangle(corner1, corner2, double filletRadius, …)` | `layer, colorIndex, lineWidth, configure` |
| `DrawRegularPolygon(Point3d center, double radius, int sides, double startAngleRad = 0, …)` | `layer, colorIndex, lineWidth, configure` |
| `DrawSlot(Point3d center, double length, double width, double rotationAngleRad = 0, …)` | `layer, colorIndex, lineWidth, configure` |
| `DrawPolyline(IEnumerable<Point2d> points, bool isClosed = true, double constantWidth = 0, double elevation = 0, …)` | `layer, colorIndex, configure` |
| `DrawPolyline(IEnumerable<PolylineVertex> vertices, bool isClosed = true, double elevation = 0, …)` | có bulge (cung) |
| `DrawSpline(IEnumerable<Point3d> fitPoints, …)` | `layer, colorIndex, configure` |
| `DrawWipeout(Point3d corner1, Point3d corner2, string layer = null)` | — |

**Chữ, ghi chú, kích thước** (`CadScopeTextExtensions`, `CadScopeDimensionExtensions`):
- `DrawText(string text, Point3d position, double height = 2.5, double rotation = 0, string layer = null, short? colorIndex = null, string textStyle = null, TextHorizontalMode justification = TextLeft, Action<DBText> configure = null)`
- `DrawMText(string text, Point3d location, double textHeight = 2.5, double width = 0, AttachmentPoint attachment = BottomLeft, string layer = null, short? colorIndex = null, string textStyle = null, Action<MText> configure = null)`
- `DrawMLeader(string text, Point3d arrowPoint, Point3d textPoint, string styleName = null, …)`
- `DrawAlignedDimension`, `DrawLinearDimension`, `DrawDiametricDimension`, `DrawRadialDimension`, `DrawJoggedRadialDimension`, `DrawAngularDimension`, `DrawOrdinateDimension`

**Block, bảng, hatch, nhóm, sao chép** (`CadScopeBlockExtensions`):
- `InsertBlock(string blockName, Point3d position, double rotation = 0, double scale = 1, Dictionary<string,string> attributes = null, Dictionary<string,object> dynamicProperties = null, string layer = null)`: tạo đủ `AttributeReference`; trả `ObjectId.Null` (kèm cảnh báo) nếu block không có
- `DrawTable(Point3d insertPoint, int rows, int columns, double rowHeight = 8, double colWidth = 30, string title = "BOM", string[,] data = null, …)`
- `DrawHatch(ObjectId boundaryId, string patternName = "SOLID", …)` (bật `autoRegen` khi gọi `Execute` để hatch hiện đúng)
- `CreateGroup(string baseGroupName, IEnumerable<ObjectId> entityIds, string description = "")`
- `CopyEntity(ObjectId sourceId, Vector3d displacement)` / `CopyEntity(sourceId, Point3d fromPoint, Point3d toPoint)`, `MirrorEntity(sourceId, Point3d mirrorLineStart, Point3d mirrorLineEnd, bool eraseSource = false)`

**Bảng ký hiệu** (`CadScopeLayerExtensions`):
- `EnsureLayer(string layerName, short colorIndex = 7, string linetypeName = null)`, `EnsureTextStyle(string styleName, string fontFile = "arial.ttf", double textHeight = 0)`, `EnsureLinetype(string linetypeName)`, `EnsureDimStyle(string styleName, Action<DimStyleTableRecord> configure = null)`: trả `ObjectId` của bản ghi có sẵn, hoặc tạo mới
- `SetLayerStatus(string layerName, bool? isLocked = null, bool? isFrozen = null, bool? isOff = null, bool? isPlottable = null)`, `GetAllLayerNames()`
- `HasBlock(string blockName)`, `CreateBlockDefinition(string blockName, IEnumerable<Entity> entities, Point3d basePoint = default, string description = "")`

Mọi `Ensure*` mở bảng `ForRead`, kiểm `Has(name)`, và chỉ `UpgradeOpen()` khi thật sự thêm bản ghi mới. Tên được kiểm trước bằng `SymbolNameRules`: tên sai ném `ArgumentException` nêu rõ ký tự, thay vì `eInvalidInput` khó hiểu của AutoCAD. Lỗi bạn **thực sự** hay gặp khi tự viết code bảng ký hiệu là sửa đối tượng đang mở `ForRead`, gây `eNotOpenForWrite` ([T01 §4.2](../training/T01_AUTOCAD_NET_FOUNDATIONS.md#42-openmode)).

**Ví dụ: một luồng đầy đủ trong thân lệnh** (prompt → layer → khung → chữ). Đoạn này có prompt nên thuộc về lệnh, bên trong `ExecuteCommand`, không thuộc về Service ([R05 §4](R05_GOLDEN_SAMPLE.md#4-nguyên-tắc-3--hiểu-đúng-ngữ-cảnh-command-vs-palette)):

```csharp
// Thân của một [CommandMethod], bên trong ExecuteCommand(() => { … })
void VeTuDien(string tenTu)
{
    // Bước 1: Hỏi tọa độ chèn
    Point3d? pt = CadPromptHelper.GetPoint("Chọn vị trí đặt tủ:");
    if (pt == null) return;

    // Bước 2: Bọc Transaction an toàn
    _cadContext.Execute(scope =>
    {
        // 2a. Đảm bảo Layer đã tồn tại (Tự tạo màu xanh)
        scope.EnsureLayer("ELEC_PANEL", colorIndex: 3);
        
        // 2b. Vẽ hình vuông 600x600 làm thân tủ
        var p2 = new Point3d(pt.Value.X + 600, pt.Value.Y + 600, 0);
        ObjectId rectId = scope.DrawRectangle(pt.Value, p2, layer: "ELEC_PANEL");
        
        // 2c. Viết Tên Tủ vào tâm
        var centerPt = new Point3d(pt.Value.X + 300, pt.Value.Y + 300, 0);
        scope.EnsureTextStyle("ARIAL_BOLD", "arialbd.ttf");
        scope.DrawMText(tenTu, centerPt, textHeight: 50, layer: "ELEC_PANEL", textStyle: "ARIAL_BOLD");

    }, undoMarkName: "Vẽ Tủ Điện"); // tham số thứ 2 theo vị trí là bool autoRegen → phải đặt TÊN tham số
}
```

Khi tính năng lớn dần, đưa phần tính tọa độ ra một hàm thuần để test được, như `GridPlanner` của [Golden Sample](R05_GOLDEN_SAMPLE.md#2-nguyên-tắc-1--functional-core-imperative-shell).

---

## 3. Truy vấn và lọc chọn

**Đọc entity trong không gian hiện tại:** `scope.GetEntities<T>(string layer = null)` trả `List<T>` (`T : Entity`).

**`SelectionFilterBuilder`** 🔎 [`Utils/SelectionFilterBuilder.cs`](../../Utils/SelectionFilterBuilder.cs): fluent API dựng `SelectionFilter` thay cho mảng `TypedValue` mã DXF thô.

| Hàm | Lọc theo | Ghi chú |
| :--- | :--- | :--- |
| `OfTypes(params string[])` | Kiểu DXF: `"LINE"`, `"LWPOLYLINE"`, `"INSERT"`… | Nhiều giá trị = OR |
| `OnLayers(params string[])` | **Mẫu** wildcard tên layer (`"EQUIP_*"`) | `# @ . ~ [ ] -` mang nghĩa wildcard: `"LAB#1"` **không** khớp layer `LAB#1` |
| `OnLayersExact(params string[])` | Tên layer **đúng nguyên văn** | Escape mọi ký tự wildcard |
| `WithBlockNames(params string[])` | Mẫu tên block (đi kèm `OfTypes("INSERT")`) | Không bắt được dynamic block đã chỉnh tham số |
| `WithBlockNamesExact(params string[])` | Tên block đúng nguyên văn | Cùng lưu ý dynamic block |
| `WithColor(short)` | Màu ACI | |
| `WithXData(string appName)` | Có XData của RegApp này | Cách nhanh để chỉ chọn entity do plugin tạo |
| `Custom(int dxfCode, object value)` | Mã DXF tùy ý | |
| `static EscapeWildcards(string)` | — | `"LAB#1"` → ``"LAB`#1"`` |
| `Build()` | — | Trả `SelectionFilter` |

```csharp
var filter = SelectionFilterBuilder.Create()
    .OfTypes("LINE", "CIRCLE")
    .OnLayers("DIMENSIONS")
    .Build();
```

Dynamic block đã chỉnh tham số mang tên ẩn danh `*U…`, nên lọc theo tên gốc sẽ bỏ sót nó. Cách xử lý: [T03 B12](../training/T03_PITFALLS_CASEBOOK.md#b12-lọc-block-theo-tên-bỏ-sót-dynamic-block). Lọc theo `ObjectClass` cho tốc độ: [T07](../training/T07_PERFORMANCE_AND_SCALE.md).

---

## 4. Metadata: XData và XRecord

| | XData — [`XDataHelper`](../../Utils/XDataHelper.cs) | XRecord — [`XRecordHelper`](../../Utils/XRecordHelper.cs) |
| :--- | :--- | :--- |
| Nằm ở | Gắn thẳng trên đối tượng, theo tên RegApp | Extension Dictionary của đối tượng, theo key |
| Định dạng | JSON chia khúc **250** ký tự (mã DXF 1000) | JSON chia khúc **2.000** ký tự (`DxfCode.Text`) |
| Giới hạn | **16 KB mỗi đối tượng**, gộp mọi ứng dụng cùng gắn XData lên đối tượng đó; helper chặn ở **15.000** ký tự JSON | Rất lớn |
| Lọc chọn được | ✅ `WithXData`, Overrule `SetXDataFilter` | ❌ |
| API | `XDataHelper.SetMetadata<T>(Transaction, DBObject, string appName, T)`, `GetMetadata<T>(DBObject, appName, T defaultValue = default)`, `HasMetadata`, `RemoveMetadata`, `EnsureRegApp`; hoặc extension `entity.SetMetadata(tr, appName, data)` / `GetMetadata<T>` / `HasMetadata` | `scope.SetXRecord<T>(ObjectId, string key, T)`, `scope.GetXRecord<T>(ObjectId, key, T defaultValue = default)`, `scope.RemoveXRecord(ObjectId, key)`; hoặc `XRecordHelper.SetMetadata/GetMetadata/RemoveMetadata(Transaction, DBObject, key, …)` |

Extension Dictionary là đối tượng con của entity, nên dữ liệu XRecord đi theo entity khi copy hoặc WBLOCK. Dữ liệu đặt trong NOD (cấp bản vẽ) thì không. Chọn loại nào, và cách đặt tên key/RegApp: [T08 §8](../training/T08_RECIPES_EXTENDING.md#8-lưu-metadata-xdata-hay-xrecord).

Cấu trúc mà `XRecordHelper.SetMetadata` tạo ra:

```text
(Entity) Khung của Tủ Điện
 └── Extension Dictionary            ← tạo nếu chưa có (CreateExtensionDictionary)
      └── "APP_TU_DIEN"  (Xrecord)    ← key do bạn truyền vào
           └── ResultBuffer: TypedValue(DxfCode.Text, "…2000 ký tự JSON…") × N khúc
```

```csharp
var tuDienModel = new { TenTu = "DB-01", CongSuat = "100 kW", ChieuCao = 2000 };

_cadContext.Execute(scope => 
{
    // Framework tự động tạo ExtensionDictionary và lưu JSON an toàn
    scope.SetXRecord(lineId, "APP_TU_DIEN", tuDienModel);

    // Giải mã ngược lại
    var data = scope.GetXRecord<dynamic>(lineId, "APP_TU_DIEN");
});
```

`SetXRecord`/`RemoveXRecord` bị chặn trong `ExecuteRead` ([R02 §2](R02_ARCHITECTURE.md#2-hợp-đồng-của-icadcontext)).

---

## 5. Block và dynamic block

- **Chèn block:** `scope.InsertBlock(…)` ([§2](#2-vẽ-và-bảng-ký-hiệu-trên-icadscope)) cho hầu hết trường hợp. Cấp thấp hơn: [`BlockHelper`](../../Utils/BlockHelper.cs) `InsertBlock(Transaction, BlockTableRecord space, string blockName, Point3d position, double rotation = 0, double scale = 1)`, `GetBlockAttributes(Transaction, BlockReference)`, `SetBlockAttribute(Transaction, BlockReference, string tag, string value)`.
- **Dynamic block:** [`DynamicBlockHelper`](../../Utils/DynamicBlockHelper.cs)

| Hàm | Làm gì |
| :--- | :--- |
| `GetEffectiveName(Transaction, BlockReference)` | Tên thật của block, kể cả khi tham chiếu mang tên ẩn danh `*U…` |
| `GetDynamicProperties(BlockReference)` | `Dictionary<string, object>` mọi tham số |
| `GetDynamicProperty<T>(BlockReference, string propertyName, T defaultValue = default)` | Đọc một tham số |
| `SetDynamicProperty(BlockReference, string propertyName, object value)` | Ghi một tham số, tự đổi `value` sang kiểu hiện tại của tham số; trả `false` nếu block không phải dynamic, không có tham số đó, hoặc tham số chỉ đọc |
| `GetVisibilityStates(BlockReference)` / `(BlockReference, string visibilityPropertyName)` | Danh sách visibility state |
| `GetVisibilityState(BlockReference)` / `SetVisibilityState(BlockReference, string stateName)` | Đọc / đặt visibility state |

```csharp
// Ghi thuộc tính (Kéo dài Block thêm 500)
DynamicBlockHelper.SetDynamicProperty(blockRef, "Distance1", 500.0);
```

`blockRef` phải đang mở `ForWrite` trong transaction. Tên tham số so sánh không phân biệt hoa thường. Bẫy và công thức đầy đủ: [T08 §10](../training/T08_RECIPES_EXTENDING.md#10-làm-việc-với-dynamic-block).

---

## 6. Jig: hình chạy theo chuột

🔎 [`Utils/Jigs/`](../../Utils/Jigs/): `RectangleJig`, `BlockPlacementJig`, và hai lớp nền `CadEntityJigBase<TEntity>` (một entity) và `CadDrawJigBase` (vẽ tự do)

Mỗi lần rê chuột, `Editor.Drag` gọi **lần lượt** (trên main thread) hai hàm:
1. **`Sampler(JigPrompts prompts)`**: lấy tọa độ chuột. Không đổi so với lần trước thì trả `SamplerStatus.NoChange` để AutoCAD khỏi vẽ lại.
2. **`Update()`**: cập nhật hình học của entity tạm.

Cách gọi (constructor thật: `RectangleJig(Point3d corner1, Polyline polyline, string promptMessage = …)`):

```csharp
Point3d? start = CadPromptHelper.GetPoint("Góc thứ nhất:");     // WCS
if (start == null) return;

var pline = new Polyline();                  // entity TẠM, chưa nằm trong database
var jig = new RectangleJig(start.Value, pline);
PromptResult result = Editor.Drag(jig);      // chạy vòng lặp Sampler/Update cho tới khi click hoặc ESC

if (result.Status != PromptStatus.OK)
{
    pline.Dispose();                         // BẮT BUỘC: đối tượng không vào DB thì phải tự Dispose
    return;
}
CadContext.Execute(scope => { scope.AppendEntityToCurrentSpace(pline); });   // chốt: đưa vào database
```

> [!NOTE]
> Ba điều đã kiểm chứng trên AutoCAD 2024, chi tiết ở [U03 §9](../training/U03_DRAWING_ON_UI.md#9-vẽ-tạm-lên-màn-hình-bản-vẽ):
> - `AcquirePoint` trong jig trả tọa độ **WCS**, còn `Editor.GetPoint` thô trả **UCS**.
> - `Sampler` nên trả `SamplerStatus.Cancel` khi `Status` khác `OK`. Các jig hiện có của dự án chưa kiểm điều này.
> - Muốn xem trước **cả kết quả** của lệnh (không chạy theo chuột) thì dùng transient graphics.

---

## 7. Entity, hình học, tọa độ

- **[`EntityExtensions`](../../Extensions/EntityExtensions.cs)** (hàm sửa entity cần entity đang mở `ForWrite`): `Move(Vector3d)` / `Move(Point3d from, Point3d to)`, `Rotate(Point3d basePoint, double angleRadians)`, `Rotate3D`, `Scale(Point3d basePoint, double scaleFactor)`, `Mirror(Point3d p1, Point3d p2)`, `CloneAndDisplace(Vector3d)`, `GetSafeExtents()` (`Extents3d?`, `null` thay vì ném), `SetHighlight`, `SetTransparency(byte alpha = 255)`, `SetLineWeight`, `SetLayer`, `SetColor`, `ExplodeToCurrentSpace(ICadScope)`, và metadata `SetMetadata`/`GetMetadata`/`HasMetadata` ([§4](#4-metadata-xdata-và-xrecord)).
- **[`GeometryExtensions`](../../Extensions/GeometryExtensions.cs)**: `MidPointTo`, `Offset(dx, dy[, dz])`, `ToPoint2d` / `ToPoint3d(z = 0)`, `AngleTo2D`, `Perpendicular2D`, `NormalizeSafe`; cho `Extents3d`: `Width`, `Height`, `Center`, `Expand(offset)`, `UnionWith(other)`.
- **[`CoordinateHelper`](../../Utils/CoordinateHelper.cs)**: `UcsToWcs` / `WcsToUcs` (điểm, vector, danh sách điểm), `GetCurrentMatrices()` trả `(ucsToWcs, wcsToUcs)`.
- Các file khác trong `Extensions/`: `BlockTableRecordExtensions`, `DocumentExtensions`, `LayoutExtensions`, `ObjectIdExtensions`, `SelectionSetExtensions`, `TransactionExtensions`.

---

## 8. Service có sẵn

Mọi service được đăng ký trong `AppBootstrapper.ConfigureServices`. Trong lệnh, lấy bằng `GetRequiredService<T>()`; trong Service hoặc ViewModel, nhận qua constructor.

**Hạ tầng** (`Core/`):

| Interface | Thành viên | Ghi chú |
| :--- | :--- | :--- |
| `ICadContext` | [R02 §2](R02_ARCHITECTURE.md#2-hợp-đồng-của-icadcontext) | |
| `INotificationService` | `Info`, `Warn`, `Error(message, ex = null)`, `Confirm(message, title)`, `Section(title)` | In ra command line của bản vẽ đang active |
| `ILoggerService` | `Debug`, `Info`, `Warning`, `Error(message, ex = null)`, `Performance(name, elapsed)` | Ghi `%AppData%\CadCoreFramework\Logs\log-yyyyMMdd.txt`, giữ 31 ngày |
| `IEventAggregator` | `Subscribe<T>(Action<T>)` → `IDisposable`, `Unsubscribe<T>`, `Publish<T>(T)` | Xem dưới bảng |
| `IEventService` | event `DocumentCreated`, `DocumentToBeDestroyed`, `ObjectAppended`, `ObjectErased`; `Initialize`, `Shutdown` | Sự kiện AutoCAD, an toàn với nhiều bản vẽ ([T02 §7.2](../training/T02_ARCHITECTURE_DEEP_DIVE.md#72-eventservice-sự-kiện-autocad-an-toàn-với-mdi)) |
| `IPaletteService` | `TogglePalette`, `ShowPalette`, `HidePalette`, `IsPaletteVisible`, `GetOrCreatePaletteSet`, `RunWithBusyAsync` | [R04 §1](R04_UI.md#1-palette-và-ribbon) |
| `IRibbonService` | `AddTab`, `RemoveTab`, `AddPanel`, `AddButton`, `AddSplitButton`, `RegisterBuildAction`, `InitializeRibbon`, `Shutdown` | [R04 §1](R04_UI.md#1-palette-và-ribbon) |
| `IProgressMeterService` | `Start(title, totalLimit)`, `SetLimit`, `MeterProgress`, `Stop` | Thanh tiến trình của AutoCAD |
| `IWindowManagerService` | `ShowModelessWindow`, `ShowModalDialog`, `ShowMessageBox` | Cửa sổ WPF thuộc về AutoCAD |
| `ICommandHistoryService` | `RecentCommands`, `Capacity`, `Record`, `Clear` | `CadCommandBase` tự ghi; xem bằng `CORE_DEBUG` |

`IEventAggregator` giữ **tham chiếu mạnh** tới subscriber (không phải `WeakReference`), nên **bắt buộc** Dispose subscription khi không còn dùng; trong ViewModel dùng `TrackSubscription(...)`. `Publish` gọi **đồng bộ** trên thread hiện tại, gọi đủ mọi subscriber rồi mới ném lại lỗi (một lỗi giữ nguyên kiểu, nhiều lỗi thành `AggregateException`).

**Nghiệp vụ** (`Services/`):

| Interface | Thành viên |
| :--- | :--- |
| `ILayerService` | `List<LayerModel> GetAllLayers()`, `CadResult<bool> ToggleLayerVisibility(string layerName, bool isOn)` |
| `IPlotService` | `PlotLayoutToPdf(Database db, string layoutName, string outputFilePath, string printerName = "DWG To PDF.pc3")`, `PlotMultipleLayouts(…)`, `GetAvailablePlotters()`, `GetMediaNames(plotterName)` |
| `ISettingsService` | `GetSetting<T>(key, defaultValue)`, `SetSetting<T>(key, value)`, `Save()` ([T08 §9](../training/T08_RECIPES_EXTENDING.md#9-thêm-thiết-lập-người-dùng)) |
| `IDialogService` | `ShowOpenFileDialog`, `ShowSaveFileDialog`, `ShowFolderBrowserDialog` |
| `IFileSystemService` | `FileExists`, `DirectoryExists`, `CreateDirectory`, `DeleteFile`, `DeleteDirectory`, `ReadAllText`, `WriteAllText`, `ReadAllBytes`, `WriteAllBytes`, `GetFiles` |
| `IDataFileService` | `ReadJson<T>`, `WriteJson<T>`, `ReadCsv(path, delimiter = null)`, `WriteCsv(path, sheet, delimiter = ',')`, `ReadExcel(path, sheetName = null)`, `WriteExcel(path, sheet, sheetName = "Sheet1")`, `GetExcelSheetNames`, `ReadTable`, `WriteTable` ([xem dưới bảng](#đọc-ghi-file-dữ-liệu-idatafileservice)) |
| `IPathProvider` | `GetPluginDirectory`, `GetAppDataDirectory`, `GetTemplatesDirectory` |

[`LayerService`](../../Services/LayerService.cs) là service ngắn nhất của dự án, đáng đọc trọn như mẫu: đọc bằng `ExecuteRead`, ghi bằng `TryExecute` kèm `undoMarkName` để dùng được từ palette. Phần đọc, nguyên văn:

```csharp
public List<LayerModel> GetAllLayers()
{
    var layers = new List<LayerModel>();

    // ExecuteRead: transaction chỉ đọc, tự lock khi được gọi từ Palette.
    _cadContext.ExecuteRead(scope =>
    {
        var layerTable = (LayerTable)scope.Transaction.GetObject(scope.Database.LayerTableId, OpenMode.ForRead);

        foreach (ObjectId layerId in layerTable)
        {
            var layerRecord = (LayerTableRecord)scope.Transaction.GetObject(layerId, OpenMode.ForRead);
            layers.Add(new LayerModel(
                name: layerRecord.Name,
                isOn: !layerRecord.IsOff, // IsOff = true nghĩa là đang Tắt
                colorIndex: layerRecord.Color.ColorIndex
            ));
        }
    });

    return layers;
}
```

ViewModel chỉ gọi `_layerService.GetAllLayers()` và gán vào property hiển thị, không đụng `Transaction` hay `OpenMode`. Thêm service mới: [T08 §3](../training/T08_RECIPES_EXTENDING.md#3-thêm-một-service).

### Đọc, ghi file dữ liệu: `IDataFileService`

[`IDataFileService`](../../Services/IDataFileService.cs) đọc và ghi JSON (Newtonsoft.Json), CSV (tự viết, [`Utils/CsvCodec.cs`](../../Utils/CsvCodec.cs)) và Excel `.xlsx` ([MiniExcel](https://github.com/mini-software/MiniExcel) 1.44.1, giấy phép Apache-2.0, không kéo theo DLL nào khác). Service không dùng API AutoCAD, gọi được ở mọi thread.

- **Không ném exception với lỗi thường gặp.** Mọi hàm trả `CadResult` / `CadResult<T>`. Thông điệp lỗi tiếng Việt đọc được ngay: không thấy file, file đang mở trong Excel, JSON hoặc CSV sai cấu trúc (kèm số dòng), `.xls` cũ, CSV không phải UTF-8. Lỗi người dùng tự sửa được ghi log mức Warning, lỗi lạ ghi mức Error kèm stack trace.
- **Ghi atomic.** Ghi ra tệp tạm rồi thay thế. Ghi thất bại (ví dụ file đang mở trong Excel) thì file cũ còn nguyên.
- **Đọc được file đang mở trong Excel.** Service mở file với `FileShare.ReadWrite`. Ghi vào file đó thì thất bại kèm lời nhắc đóng file.

Dữ liệu dạng bảng đi qua [`DataSheet`](../../Models/DataSheet.cs): một dòng tiêu đề (tên cột không rỗng, không trùng, không phân biệt hoa thường) và các dòng giá trị `object?`. Kiểu của ô phụ thuộc định dạng file, nên khi đọc hãy dùng `GetText` / `TryGetDouble` thay vì ép kiểu:

| Định dạng | Ghi | Đọc lại |
| :--- | :--- | :--- |
| `.xlsx` | số → ô số, `bool` → ô logic, `DateTime` → ô ngày, chuỗi giữ nguyên (`"001"` vẫn là chữ) | số → `double` (cả số nguyên), ngày → `DateTime`, ô rỗng → `null`. Dòng có dữ liệu đầu tiên là tiêu đề; bỏ dòng trống |
| `.csv` | UTF-8 **có BOM** (thiếu BOM thì Excel đọc tiếng Việt thành ký tự rác), số và ngày theo InvariantCulture (`12.5`, `2026-10-15`), CRLF | mọi ô là `string`, ô rỗng là `""`. Tự nhận dấu phân cách `,` `;` hoặc tab từ dòng tiêu đề |
| `.json` (`ReadTable`/`WriteTable`) | mảng đối tượng `[{"Cột": giá trị, …}]`, giữ thứ tự cột; `DateTime` ghi thành chuỗi như CSV | số → `double`, chuỗi dạng ngày **giữ nguyên là chuỗi**, giá trị lồng → chuỗi JSON |

`ReadTable` / `WriteTable` chọn định dạng theo đuôi file. Ví dụ dùng: lệnh `DEMO_ATTR_EXPORT` / `DEMO_ATTR_IMPORT` ([T08 §12.6](../training/T08_RECIPES_EXTENDING.md#126-xuất-nhập-bảng-attribute-excel-csv-json)).

> [!WARNING]
> **Dữ liệu cần đi qua Excel thì dùng `.xlsx`, đừng dùng CSV.** Đã kiểm với Excel 16 trên máy dùng dấu phẩy thập phân, mở file do service ghi:
> - CSV phân cách bằng `,`: Excel dùng `;` làm dấu phân cách danh sách, nên **cả dòng dồn vào cột A**.
> - CSV phân cách bằng `;`: Excel tách đúng cột nhưng tự đổi kiểu. `001` thành `1`, handle `2E5` thành `200000`, còn `12.5` thành chữ chứ không phải số.
> - Lưu lại bằng "CSV (Comma delimited)" thì file thành ANSI: `ReadCsv` từ chối và hướng dẫn lưu bằng "CSV UTF-8".
> - `.xlsx` do service ghi mở đúng: `001` và `2E5` vẫn là chữ, `12.5` là số.
>
> CSV hợp với việc trao đổi giữa các chương trình. Còn file mà người dùng sẽ mở và sửa bằng Excel thì dùng `.xlsx`.

---

## 9. Tiện ích khác

| Lớp | Thành viên |
| :--- | :--- |
| [`CadAsyncHelper`](../../Utils/CadAsyncHelper.cs) | `ExecuteSafeAsync(Func<Task<T>> backgroundTask, Action<T> mainThreadAction)` (việc nặng trên ThreadPool, kết quả được đưa tường minh về main thread), `ExecuteSafeAsync(Func<Task>, Action)`, `task.FireAndForget(Action<Exception> onError)` |
| [`CadApplicationHelper`](../../Utils/CadApplicationHelper.cs) | `GetSystemVariable` / `GetSystemVariable<T>(name, defaultValue)`, `SetSystemVariable`, `IsDarkColorTheme(colorTheme)`, `WriteToCommandLine`, `SendCommand` |
| [`Guard`](../../Utils/Guard.cs) | `NotNull<T>(value, paramName)`, `NotNull(ObjectId, paramName)`, `NotEmpty(string, paramName)`, `IsTrue(condition, paramName, message)` |
| [`SymbolNameRules`](../../Utils/SymbolNameRules.cs) | `IsValid`, `GetError`, `ContainsInvalidCharacter`, `Validate(name, paramName)` (ném `ArgumentException`) cho tên layer/style/block |
| [`TextStyleHelper`](../../Utils/TextStyleHelper.cs) | `EnsureTextStyle(Database, Transaction, styleName, fontName, textSize = 0)`, `SetCurrentTextStyle`, `GetAllTextStyleNames` |
| [`DimensionHelper`](../../Utils/DimensionHelper.cs) | `CreateAlignedDimension(p1, p2, dimLinePoint)`, `CreateLinearDimension(p1, p2, dimLinePoint, rotation = 0)`: tạo đối tượng, chưa append |
| [`CadTableBuilder`](../../Utils/TableHelper.cs) | Fluent: `Create()`, `SetTitle`, `SetPosition`, `SetRowHeight`, `SetTextHeight`, `AddColumn(header, width)`, `AddRow(params string[])`, `FromCollection<T>(items, rowSelector)`, `Build()` → `Table` |
| [`WpfCadHelper`](../../Utils/WpfCadHelper.cs) | `ShowDialogUnderCad(Window)`, `ShowUnderCad(Window)`: cửa sổ WPF có owner là AutoCAD |
