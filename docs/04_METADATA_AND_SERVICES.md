# Phần 4: Siêu Dữ Liệu & Nghiệp Vụ (Metadata & Services)

Bên cạnh việc vẽ hình, giá trị của một phần mềm chuyên ngành (BIM/Enterprise CAD) nằm ở lượng "Dữ liệu vô hình" (Metadata) được lưu trữ phía sau.

---

## 4.1 XData vs XRecord

### Thuật toán "Băm JSON" của XData (`XDataHelper.cs`)
**XData (Extended Data)** là cấu trúc dữ liệu cổ điển, rất dễ bị phần mềm khác can thiệp. Đặc biệt, nó có một giới hạn vật lý kinh khủng: **Chỉ lưu tối đa 255 ký tự** cho mỗi đoạn Text (DxfCode.ExtendedDataAsciiString - Mã DXF 1000).

> [!NOTE]
> **Under the hood:** Nếu bạn serialize (biến đổi) một Model C# phức tạp thành JSON (ví dụ một danh sách toạ độ), độ dài JSON dễ dàng vượt mốc 255 ký tự. Nếu cố lưu thẳng vào XData, AutoCAD sẽ quăng lỗi nổ tung.
> 
> Trong [`XDataHelper.cs`](../Utils/XDataHelper.cs), Framework sử dụng thuật toán Chunking: Nó cắt nhỏ chuỗi JSON dài thành mảng các chuỗi con 250 ký tự. Sau đó nối liên tiếp các DxfCode 1000 để nhét vào XData. Khi đọc ra, nó ráp các đoạn này lại và giải mã JSON về object C#. 

Dù đã có trick, XData vẫn **bị giới hạn 16 KB trên mỗi đối tượng** (gộp mọi ứng dụng cùng gắn XData lên đối tượng đó, *không phải* trên toàn bản vẽ). `XDataHelper` chặn ở 15.000 ký tự JSON để chừa chỗ. Do đó, **không nên dùng XData cho dữ liệu lớn.** Điểm mạnh riêng của XData: lọc chọn được (`SelectionFilterBuilder.WithXData`) và Overrule lọc được (`SetXDataFilter`).

### Cấu Trúc Cây Của XRecord (`XRecordExtensions.cs` - Khuyên Dùng cho dữ liệu lớn)

**XRecord** nằm trong từ điển mở rộng (`Extension Dictionary`) của chính Entity. `XRecordHelper` chia JSON thành nhiều khúc 2.000 ký tự, nên dung lượng thực tế rất lớn. Extension Dictionary là đối tượng con của entity, nên đi theo entity khi copy (deep clone) hoặc WBLOCK entity đó. Ngược lại, dữ liệu đặt trong NOD (cấp bản vẽ) thì không đi theo entity. So sánh đầy đủ: [T08 §8](training/T08_RECIPES_EXTENDING.md#8-lưu-metadata-xdata-hay-xrecord).

Cấu trúc mà `XRecordHelper.SetMetadata` thực sự tạo ra:
```text
(Entity) Khung của Tủ Điện
 └── Extension Dictionary            ← tạo nếu chưa có (CreateExtensionDictionary)
      └── "APP_TU_DIEN"  (Xrecord)    ← key do bạn truyền vào
           └── ResultBuffer: TypedValue(DxfCode.Text, "…2000 ký tự JSON…") × N khúc
```

**Sử dụng cực kỳ đơn giản qua Framework:**
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

---

## 4.2 Thao Tác Với Dynamic Blocks (`DynamicBlockHelper`)

Block Động (Dynamic Block) chứa các thông số tuỳ biến như Khoảng cách kéo giãn (Distance), Góc quay (Angle), Hiển thị (Visibility State). Thay vì duyệt qua mảng thuộc tính rườm rà, dùng [`DynamicBlockHelper.cs`](../Utils/DynamicBlockHelper.cs).

```csharp
// Ghi thuộc tính (Kéo dài Block thêm 500)
DynamicBlockHelper.SetDynamicProperty(blockRef, "Distance1", 500.0);
```

---

## 4.3 Code Mẫu Thực Chiến: Giao Tiếp Database với Dịch Vụ C#

Hãy nhìn vào cách cấu trúc một Service chuẩn mực (ví dụ quản lý toàn bộ cấu hình Layer của dự án) tách biệt với AutoCAD API.
Xem file [`LayerService.cs`](../Services/LayerService.cs).

```csharp
public class LayerService : ILayerService
{
    private readonly ICadContext _cadContext;

    // Tiêm (Inject) ICadContext vào
    public LayerService(ICadContext cadContext)
    {
        _cadContext = cadContext;
    }

    // ĐỌC: trả về danh sách C# thuần tuý, để UI (WPF) có thể Bind thẳng vào DataGrid
    public List<LayerModel> GetAllLayers()
    {
        var layers = new List<LayerModel>();

        // ExecuteRead: transaction chỉ đọc; tự lock khi được gọi từ Palette (application context)
        _cadContext.ExecuteRead(scope =>
        {
            var layerTable = (LayerTable)scope.Transaction.GetObject(scope.Database.LayerTableId, OpenMode.ForRead);
            foreach (ObjectId layerId in layerTable)
            {
                var ltr = (LayerTableRecord)scope.Transaction.GetObject(layerId, OpenMode.ForRead);
                layers.Add(new LayerModel(ltr.Name, !ltr.IsOff, ltr.Color.ColorIndex));
            }
        });

        return layers;
    }

    // GHI: TryExecute trả CadResult (không throw) + undo mark → dùng được từ Palette
    public CadResult<bool> ToggleLayerVisibility(string layerName, bool isOn)
        => _cadContext.TryExecute(scope =>
        {
            var lt = (LayerTable)scope.Transaction.GetObject(scope.Database.LayerTableId, OpenMode.ForRead);
            if (!lt.Has(layerName)) return false;
            var ltr = (LayerTableRecord)scope.Transaction.GetObject(lt[layerName], OpenMode.ForWrite);
            ltr.IsOff = !isOn;
            return true;
        }, autoRegen: true, undoMarkName: "Layer On/Off");
}
```
Sau đó, trong WPF ViewModel, bạn chỉ cần gọi `_layerService.GetAllLayers()` và gán vào thuộc tính hiển thị, tuyệt đối không đụng gì tới `Transaction` hay `OpenMode`.

> **Quy ước lỗi của `ICadContext`:** `Execute*` ném exception · `TryExecute*` trả `CadResult`. Xem thêm [Phần 9: Golden Sample](09_GOLDEN_SAMPLE.md).
