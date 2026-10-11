# API Reference

Tài liệu tham khảo nhanh các hàm và lớp cốt lõi của CadCoreFramework.

## 1. CadScopeDrawingExtensions

Bộ mở rộng trên `ICadScope` hỗ trợ vẽ nhanh 1-dòng. Tất cả đều tự động:
- Khởi tạo đối tượng hình học với DatabaseDefaults.
- Bọc trong transaction an toàn.
- Cấu hình Layer, Color, Linetype (chỉ cần truyền tên chuỗi).
- Action `<T>` (configure lambda) cho phép cấu hình chi tiết (vd. `LineWeight`).

**Phương thức nổi bật:**
- `ObjectId DrawLine(Point3d p1, Point3d p2, string layer, ...)`
- `ObjectId DrawCircle(Point3d center, double radius, string layer, ...)`
- `ObjectId DrawRectangle(Point3d p1, Point3d p2, string layer, ...)`
- `ObjectId DrawMText(string text, Point3d location, ...)`
- `ObjectId DrawBlockReference(Point3d position, string blockName, ...)`

## 2. EventAggregator (IEventAggregator)

Cơ chế WeakReference an toàn, IDisposable để tránh memory leak.
- `IDisposable Subscribe<T>(Action<T> action)`
- `void Unsubscribe<T>(Action<T> action)`
- `void Publish<T>(T message)`

## 3. Lọc chọn (SelectionFilterBuilder)

Fluent API tạo TypeValue array cho AutoCAD SelectionFilter.
```csharp
var filter = SelectionFilterBuilder.Create()
    .OfTypes("LINE", "CIRCLE")
    .OnLayers("DIMENSIONS")
    .Build();
```

## 4. Metadata (XData & XRecord)

- **XDataHelper**: Ghi dữ liệu JSON <= 16KB.
- **XRecordHelper**: Ghi dữ liệu lớn không giới hạn.

## 5. UI (Ribbon & Palette)

- **RibbonService**: Tự động tạo Tab & Panel, dọn dẹp event khi Shutdown.
- **PaletteService**: Quản lý PaletteSet (Dockable) với WPF UserControl.

## 6. Utilities

- **CadPromptHelper**: Wrappers cho Editor inputs (GetPoint, GetString, GetDistance).
- **CoordinateHelper**: Tính toán MidPoint, Distance, Transform.
- **GeometryExtensions**: Các phương thức IsAlmostEqualTo (so sánh sai số), Transform, v.v.
- **Guard**: Lớp hỗ trợ Validation (NotNull, NotEmpty, IsTrue).
- **DimensionHelper**: Trợ giúp Dimension.
- **TableHelper (CadTableBuilder)**: Hỗ trợ Fluent tạo CAD Table.
- **TextStyleHelper**: Quản lý TextStyle trong Database.
