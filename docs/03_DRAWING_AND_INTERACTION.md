# Phần 3: Thao Tác Vẽ & Tương Tác (Drawing & Interaction)

Phần này hướng dẫn cách giao tiếp với người dùng và đẩy dữ liệu vào bản vẽ thông qua các thư viện mở rộng (`Extensions`) và tiện ích (`Utils`).

---

## 3.1 Tương Tác Người Dùng (`CadPromptHelper`)

Một lỗi cực kỳ phổ biến khiến AutoCAD bị "Not Responding" vô thời hạn là: **Yêu cầu người dùng chọn chuột trên màn hình KHI ĐANG MỞ TRANSACTION (Đang Lock Document).**

**Luật Thép:** Luôn phân tách 2 pha: HỎI trước -> VẼ sau.

Sử dụng [`CadPromptHelper`](../Utils/CadPromptHelper.cs):
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

> [!NOTE]
> **Under the hood (Hệ tọa độ):** Hàm `CadPromptHelper.GetPoint()` của framework tự động gọi hàm chuyển đổi toạ độ từ **UCS** (User Coordinate System - toạ độ do người dùng tự xoay) về **WCS** (World Coordinate System - toạ độ gốc tuyệt đối của CAD). Nếu bạn dùng code gốc của AutoCAD, đường vẽ của bạn sẽ bị lệch toạ độ nếu người dùng đang nghiêng bản vẽ.

---

## 3.2 Luồng Dữ Liệu `OpenMode`: `ForRead` vs `ForWrite`

Khi thao tác với Database trong `ICadScope`, bạn cần nắm vững chế độ mở (OpenMode).
- **`OpenMode.ForRead`**: Chỉ đọc dữ liệu. Rất an toàn, cho phép nhiều lệnh đọc cùng lúc.
- **`OpenMode.ForWrite`**: Ghi/Sửa dữ liệu. Khoá độc quyền đối tượng đó, người khác không can thiệp được.

**Anti-Pattern thường gặp (Lỗi eVetoed):**
Nhiều người có thói quen mở bảng Layer (`LayerTable`) bằng `ForRead`, kiểm tra xem Layer tồn tại chưa. Nếu chưa, họ gọi `UpgradeOpen()` để chuyển sang `ForWrite` rồi thêm Layer mới.
Nếu có một lệnh khác (hoặc Event listener) đang âm thầm khoá bảng Layer này, lệnh `UpgradeOpen()` sẽ thất bại và quăng lỗi `eVetoed` (Bị cấm quyền).

**Pattern của Framework:**
Hãy nhìn vào hàm `EnsureLayer()` trong [`CadScopeDrawingExtensions.cs`](../Extensions/CadScopeDrawingExtensions.cs). Framework mở bảng Layer bằng `ForWrite` **ngay từ đầu**:
```csharp
var layerTable = (LayerTable)scope.Transaction.GetObject(scope.Database.LayerTableId, OpenMode.ForWrite);
```
Nếu bị khoá, nó sẽ ném Exception ngay lập tức trước khi logic vẽ bắt đầu, giúp huỷ lệnh an toàn thay vì vẽ hỏng nửa chừng.

---

## 3.3 Ví Dụ Thực Chiến Tủ Điện (Full Flow)

Thay vì gọi lẻ tẻ, dưới đây là một luồng thực tế áp dụng Framework kết hợp: Prompt -> Layer -> Vẽ Khung -> Vẽ Text. 

```csharp
public void VeTuDien(string tenTu)
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
        scope.DrawMText(tenTu, centerPt, textHeight: 50, layer: "ELEC_PANEL", style: "ARIAL_BOLD");
        
    }, "Vẽ Tủ Điện"); // 1 Bước Ctrl+Z
}
```

---

## 3.4 Hiệu Ứng Di Chuột (Interactive Jigs)

Khi bạn muốn hiển thị một cái Khung hình chữ nhật bám theo con trỏ chuột trước khi người dùng click (giống hệt lệnh `RECTANG` mặc định của CAD), bạn phải dùng `EntityJig`.

Dự án cung cấp sẵn [`RectangleJig.cs`](../Utils/Jigs/RectangleJig.cs).

### Giải phẫu cơ chế Jig
Một vòng lặp Jig bao giờ cũng gồm 2 hàm chạy song song cực nhanh (hàng chục lần / giây) mỗi khi rê chuột:
1. **`Sampler(JigPrompts prompts)`**: Lấy toạ độ chuột hiện tại. Nếu chuột không di chuyển so với vòng lặp trước, trả về `SamplerStatus.NoChange` để CAD khỏi mất công vẽ lại.
2. **`Update()`**: Nếu chuột có di chuyển, cập nhật lại toạ độ các đỉnh của Entity (ở đây là cái Khung Hình Chữ Nhật) dựa trên toạ độ mới.

Cách gọi Jig:
```csharp
Point3d startPoint = ...; // Lấy điểm 1
var jig = new RectangleJig(startPoint);

// Editor.Drag sẽ chặn màn hình và chạy vòng lặp Sampler/Update ở trên
var result = Editor.Drag(jig);

if (result.Status == PromptStatus.OK)
{
    // Người dùng đã Click chuột lần 2 (Chốt)
    // Sau đó bạn gọi CadContext.Execute() để add hình chữ nhật đó vào Database
}
```
