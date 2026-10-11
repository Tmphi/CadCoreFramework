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

Dù đã có trick, XData vẫn **bị giới hạn tổng dung lượng 16KB** trên toàn bộ bản vẽ. Do đó, **Không nên dùng XData cho dữ liệu phức tạp.**

### Cấu Trúc Cây Của XRecord (`XRecordExtensions.cs` - Khuyên Dùng)

**XRecord** khắc phục toàn bộ nhược điểm của XData. Nó lưu trong một từ điển mở rộng (`Extension Dictionary`) đính kèm vào Entity. XRecord không giới hạn kích thước, và không bị lệnh `PURGE` hay WBLOCK làm mất dữ liệu.

Cấu trúc cây thực tế trong database AutoCAD:
```text
(Entity) Line của Tủ Điện
 └── Extension Dictionary
      └── Tủ Mạng (Dictionary)
           └── Dữ Liệu Ứng Dụng (XRecord)
                └── JSON String (Không giới hạn độ dài)
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

    // Trả về danh sách C# thuần tuý, để UI (WPF) có thể Bind thẳng vào ComboBox
    public List<LayerModel> GetLayers()
    {
        var layers = new List<LayerModel>();

        // Gọi ExecuteRead (Không khoá bản vẽ, tốc độ nhanh)
        _cadContext.ExecuteRead(scope =>
        {
            var layerTable = (LayerTable)scope.Transaction.GetObject(scope.Database.LayerTableId, OpenMode.ForRead);
            foreach (ObjectId layerId in layerTable)
            {
                var ltr = (LayerTableRecord)scope.Transaction.GetObject(layerId, OpenMode.ForRead);
                layers.Add(new LayerModel { Name = ltr.Name, ColorIndex = ltr.Color.ColorIndex });
            }
        });

        return layers;
    }
}
```
Sau đó, trong WPF ViewModel, bạn chỉ cần gọi `_layerService.GetLayers()` và gán vào thuộc tính hiển thị, tuyệt đối không đụng gì tới `Transaction` hay `OpenMode`.
