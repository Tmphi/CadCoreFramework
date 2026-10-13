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

**Hai cách mở một bảng mà bạn có thể sẽ ghi:**
1. Mở `ForRead`, kiểm tra `Has(name)`, chỉ khi cần thêm mới thì `UpgradeOpen()`. Rẻ hơn khi phần lớn lần gọi không ghi gì. Framework dùng cách này cho `CurrentSpace` (upgrade lười trong `AppendEntityToCurrentSpace`), cho `RegAppTable` (`XDataHelper.EnsureRegApp`) và — từ PR #6 — cho mọi `Ensure*` trong [`CadScopeLayerExtensions.cs`](../Extensions/CadScopeLayerExtensions.cs):
```csharp
var layerTable = (LayerTable)scope.Transaction.GetObject(scope.Database.LayerTableId, OpenMode.ForRead);
if (layerTable.Has(layerName)) return layerTable[layerName];
layerTable.UpgradeOpen();   // chỉ khi thật sự thêm layer mới
```
2. Mở `ForWrite` ngay từ đầu. Đơn giản khi gần như chắc chắn sẽ ghi.

Trong **một** transaction, cả hai cách đều đúng. (Trên v1.0.0, `EnsureLayer` mở `ForWrite` ngay và comment giải thích bằng lỗi `eVetoed` — không phải lý do thật, xem [T09 B-19](training/T09_SENIOR_PLAYBOOK.md#3-backlog-kỹ-thuật-đã-kiểm-chứng).) Lỗi bạn **thực sự** hay gặp là sửa đối tượng đang mở `ForRead`, gây `eNotOpenForWrite` ([T01 §4.2](training/T01_AUTOCAD_NET_FOUNDATIONS.md#42-openmode)). Tên layer cũng được validate trước (`SymbolNameRules`): tên sai ném `ArgumentException` nêu rõ ký tự, thay vì `eInvalidInput` khó hiểu của AutoCAD.

---

## 3.3 Ví Dụ Thực Chiến Tủ Điện (Full Flow)

Thay vì gọi lẻ tẻ, dưới đây là một luồng thực tế áp dụng Framework kết hợp: Prompt -> Layer -> Vẽ Khung -> Vẽ Text. Đoạn code này có **prompt**, nên thuộc về **lệnh** (bên trong `ExecuteCommand`), không thuộc về Service ([Phần 9 §9.4](09_GOLDEN_SAMPLE.md)).

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

---

## 3.4 Hiệu Ứng Di Chuột (Interactive Jigs)

Khi bạn muốn hiển thị một cái Khung hình chữ nhật bám theo con trỏ chuột trước khi người dùng click (giống hệt lệnh `RECTANG` mặc định của CAD), bạn phải dùng `EntityJig`.

Dự án cung cấp sẵn [`RectangleJig.cs`](../Utils/Jigs/RectangleJig.cs).

### Giải phẫu cơ chế Jig
Mỗi lần rê chuột, `Editor.Drag` gọi **lần lượt** (tuần tự, trên main thread, không song song) hai hàm:
1. **`Sampler(JigPrompts prompts)`**: lấy toạ độ chuột hiện tại. Nếu không đổi so với lần trước, trả `SamplerStatus.NoChange` để AutoCAD khỏi vẽ lại.
2. **`Update()`**: nếu toạ độ đã đổi, cập nhật hình học của entity tạm (ở đây là khung chữ nhật).

Cách gọi Jig (constructor thật: `RectangleJig(Point3d corner1, Polyline polyline, string promptMessage = …)`):
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
