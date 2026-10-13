> **🎓 Học viện CadCoreFramework** · [Lộ trình](README.md) · [← T06](T06_BUILD_RELEASE_DEVOPS.md) · **T07** · [T08 →](T08_RECIPES_EXTENDING.md)

# T07 — Hiệu Năng & Quy Mô

> **Cấp độ:** 🌳 Middle → 🏔️ Senior · **Cần trước:** [T01](T01_AUTOCAD_NET_FOUNDATIONS.md), [T02](T02_ARCHITECTURE_DEEP_DIVE.md); LINQ và thực thi trì hoãn ([C04 §3–4](C04_CSHARP_IN_THIS_PROJECT.md#3-linq-hỏi-đáp-trên-danh-sách)) · **Trong lộ trình:** tuần 10 ([README §3](README.md#3-lộ-trình-đào-tạo))

Plugin chạy nhanh với bản vẽ 50 entity có thể "đơ" với bản vẽ 200.000 entity của khách hàng. Module này dạy **mô hình chi phí** của AutoCAD API, các kỹ thuật framework đã áp dụng, và cách **đo** thay vì đoán.

> [!IMPORTANT]
> Tài liệu này **không đưa ra con số thời gian cụ thể**. Chi phí phụ thuộc máy, phiên bản AutoCAD và bản vẽ. Hãy tự đo bằng Lab L13 ([T10](T10_LABS_AND_ASSESSMENT.md)) rồi ghi số liệu của bạn vào PR. Quy tắc: **không tối ưu khi chưa có số đo trước/sau.**

---

## 1. Mô hình chi phí (tương đối)

| Thao tác | Chi phí | Ghi chú |
| :--- | :---: | :--- |
| Đọc `ObjectId.ObjectClass`, `IsErased`, `Handle` | 🟢 rất rẻ | Không mở đối tượng |
| `tr.GetObject(id, ForRead)` | 🟡 vừa | Mở đối tượng. Lặp hàng trăm nghìn lần thì đáng kể |
| `tr.GetObject(id, ForWrite)` | 🟠 hơn | Ghi undo, gửi thông báo, có thể kích hoạt reactor/event |
| Tạo + append entity | 🟠 | `SetDatabaseDefaults`, append, đăng ký với transaction |
| `StartTransaction` + `Commit` | 🟠 | Rẻ một lần, **đắt khi lặp mỗi entity một lần** |
| `LockDocument` | 🟡 | Tương tự transaction: gom lại |
| `Editor.Regen()` | 🔴 đắt | Tính lại hiển thị **toàn bản vẽ** |
| `Editor.GetSelection` / `SelectAll` có filter | 🟢–🟡 | Lọc trong lõi C++, nhanh hơn tự duyệt rồi mở từng entity |
| Event `ObjectAppended` có handler | 🟡 × N | Bắn cho **mỗi** entity (INSERT/PASTE hàng nghìn đối tượng) |
| COM interop (UndoMark) | 🟡 | Một lần mỗi `Execute`, chấp nhận được |

---

## 2. Lọc trước khi mở: ObjectClass

🔎 [`GridMarkerService.CountMarkers`](../../Samples/GridMarker/GridMarkerService.cs):

```csharp
RXClass polylineClass = RXObject.GetClass(typeof(Polyline));
foreach (ObjectId id in scope.CurrentSpace)
{
    if (id.ObjectClass != polylineClass) continue;              // 🟢 không mở đối tượng
    var entity = (Entity)scope.Transaction.GetObject(id, OpenMode.ForRead);   // 🟡 chỉ mở cái cần
    if (XDataHelper.HasMetadata(entity, XDataAppName)) count++;
}
```

- So sánh `!=` là **khớp chính xác** lớp. Muốn gồm cả lớp con: `id.ObjectClass.IsDerivedFrom(RXObject.GetClass(typeof(Curve)))`.
- Lọc theo tên DXF: `id.ObjectClass.DxfName == "LWPOLYLINE"`.
- Cần lọc theo layer / XData / loại cùng lúc trên **toàn bản vẽ**: `SelectAll(filter)` thường tốt hơn duyệt tay, vì lọc diễn ra trong lõi C++.

---

## 3. Chi phí ẩn trong vòng lặp

Đọc lại [`GridMarkerService.DrawCells`](../../Samples/GridMarker/GridMarkerService.cs) và chuỗi gọi bên dưới, với **N ô**:

| Việc trong mỗi vòng | Từ đâu | Số lần | Cải thiện khi N lớn |
| :--- | :--- | :---: | :--- |
| `EnsureLayer(spec.LayerName)` | `DrawRectangle(layer:)` và `DrawText(layer:)` → `ApplyCommonProperties` | **2N** (+1 đầu hàm) | Gọi `EnsureLayer` **một lần** trước vòng lặp, sau đó gán `entity.Layer` qua `configure`, hoặc thêm overload không ensure |
| `EnsureRegApp` | `XDataHelper.SetMetadata` | N | Đăng ký RegApp một lần trước vòng lặp |
| `GetObject(frameId, ForWrite)` để gắn XData | Service | N | Gắn XData qua `configure` *trước* append (cần RegApp đã có và helper nhận `Database` tường minh) |
| Serialize JSON metadata | `XDataHelper` | N | Chấp nhận được (pure CPU) |

Với N = 6 (smoke test) thì không đáng kể. Với N = 2.500 (`GridSpec.MaxCells`) thì đáng đo. Đây chính là nội dung **Lab L13**.

> [!TIP]
> `GridSpec.MaxCells = 2500` không phải giới hạn kỹ thuật. Đó là **hàng rào sản phẩm**, chặn việc người dùng gõ nhầm `1000 × 1000` tạo ra một triệu entity. Mọi tính năng sinh hàng loạt entity nên có hàng rào tương tự ở tầng Model.

---

## 4. Xử lý nhiều bản vẽ: side database

Đếm/sửa dữ liệu trong 200 file DWG **không cần mở chúng lên giao diện**. Đọc file vào một `Database` độc lập (side database) nhanh hơn nhiều so với mở Document, và không làm phiền người dùng.

```csharp
// Mẫu đọc (đã kiểm chứng chữ ký API với AcDbMgd 24.0)
public static int CountBlockRefs(string dwgPath, string effectiveName)
{
    using (var db = new Database(false, true))              // buildDefaultDrawing=false, noDocument=true
    {
        db.ReadDwgFile(dwgPath, FileOpenMode.OpenForReadAndAllShare, true, null);
        db.CloseInput(true);                                 // đọc hết vào bộ nhớ, nhả file

        int count = 0;
        using (var tr = db.TransactionManager.StartTransaction())
        {
            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
            RXClass brClass = RXObject.GetClass(typeof(BlockReference));
            foreach (ObjectId id in ms)
            {
                if (id.ObjectClass != brClass) continue;
                var br = (BlockReference)tr.GetObject(id, OpenMode.ForRead);
                if (DynamicBlockHelper.GetEffectiveName(tr, br) == effectiveName) count++;
            }
            tr.Commit();
        }
        return count;
    }
}
```

| Quy tắc | Vì sao |
| :--- | :--- |
| `using` cho `Database` | Đối tượng C++ lớn, phải giải phóng tường minh ([T01 §9](T01_AUTOCAD_NET_FOUNDATIONS.md#9-bộ-nhớ-và-vòng-đời-đối-tượng)) |
| Không cần `LockDocument` | Không có Document. Side database không thuộc ai |
| **Không dùng `ICadContext`** cho side database | `ICadContext` gắn với Document đang mở. Side database là một kịch bản khác, tự quản lý transaction như mẫu trên |
| Ghi entity mới vào side database | Một số API ngầm dùng `HostApplicationServices.WorkingDatabase` (static, gán được). Tạm gán `WorkingDatabase = db` trong `try` và **khôi phục trong `finally`** |
| Lưu lại | `db.SaveAs(path, DwgVersion.Current)` |
| Lỗi một file không được dừng cả lô | Bọc mỗi file trong try/catch, ghi log, tiếp tục; báo cáo tổng kết cuối lô |
| File đang bị người khác mở | `FileOpenMode.OpenForReadAndAllShare` cho phép đọc; ghi đè thì cần quyền độc quyền |
| Chèn block từ file thư viện | `db.WblockCloneObjects(ids, ownerId, idMap, DuplicateRecordCloning.Ignore, false)` |

---

## 5. Batch và tiến trình

| Lựa chọn | Ưu | Nhược |
| :--- | :--- | :--- |
| **Một** transaction cho cả lô (`ExecuteBatch`, hoặc vòng lặp trong một `Execute`) | Nhanh nhất; nguyên tử; **một** bước Undo | Lỗi ở phần tử cuối thì rollback tất cả; bộ nhớ tăng theo kích thước lô |
| Chia lô (vd. 1.000 / transaction) | Giới hạn bộ nhớ; lỗi chỉ mất một lô | Nhiều bước Undo (gom bằng `UndoGroupManager`); mất tính nguyên tử |
| Mỗi phần tử một transaction | Đơn giản | **Chậm nhất**, nhiều bước Undo. Tránh |

Tiến trình: [`IProgressMeterService`](../../Core/ProgressMeterService.cs) (thanh trạng thái AutoCAD), hoặc `IProgress<int>` của `ExecuteBatch`. **Không** dùng `Application.DoEvents()` để "làm UI mượt" ([T01 §8.4](T01_AUTOCAD_NET_FOUNDATIONS.md#84-những-thứ-cần-tránh)).

---

## 6. Đồ họa và regen

- `autoRegen` của `ICadContext` mặc định **false**. Chỉ bật khi màn hình **không tự cập nhật** sau thao tác, ví dụ đổi trạng thái On/Off layer từ palette (`LayerService.ToggleLayerVisibility` bật vì lý do này). Regen là toàn bản vẽ.
- Nhẹ hơn regen: `tr.TransactionManager.QueueForGraphicsFlush()` (đẩy đồ họa của đối tượng vừa sửa) và `doc.Editor.UpdateScreen()`. Cả hai API đều tồn tại trong AutoCAD 2024.
- Vẽ hàng nghìn entity rồi regen **một lần** ở cuối, không regen mỗi entity.

---

## 7. Tính toán nặng và async

Mô hình hai giai đoạn của [`ExecuteAsync`](../../Core/CadContext.cs):
1. **Giai đoạn chuẩn bị** (ngoài lock, có thể ngoài main thread): tính toán thuần (Planner), đọc file, gọi API. Kết quả là dữ liệu thuần (`List<GridCell>`).
2. **Giai đoạn vẽ** (main thread, trong lock + transaction): chỉ là vòng lặp "vẽ những gì đã tính".

Muốn chạy phần tính toán trên thread nền, chỉ đưa **dữ liệu thuần** sang `Task.Run`, không đưa đối tượng AutoCAD ([T01 §8.1](T01_AUTOCAD_NET_FOUNDATIONS.md#81-quy-tắc-tuyệt-đối)). Logic thuần của Golden Sample đã sẵn sàng cho việc này: `GridPlanner.Plan` không chạm AutoCAD.

---

## 8. Đo đạc

| Công cụ | Đo gì | Cách |
| :--- | :--- | :--- |
| `CommandHistory` / `CORE_DEBUG` | Thời gian **mỗi lệnh** (tự động) | Chạy lệnh, xem tab Command History |
| `ILoggerService.Performance(name, elapsed)` | Một đoạn bất kỳ | `var sw = Stopwatch.StartNew(); …; _logger.Performance("DrawCells", sw.Elapsed);` → dòng `[PERF] DrawCells took 12.5ms` |
| BenchmarkDotNet ([`CadCoreFramework.Benchmarks`](../../CadCoreFramework.Benchmarks/README.md)) | **Code thuần** (Planner, EventAggregator, CadResult) | `dotnet run --project CadCoreFramework.Benchmarks -c Release -- --filter *Grid*` |
| Visual Studio Profiler (attach `acad.exe`) | Điểm nóng thật trong AutoCAD | Diagnostic Tools → CPU Usage khi attach |

Phương pháp đo trong AutoCAD đáng tin:
1. **Bản Release**, không attach debugger khi đo thời gian.
2. Chạy **lần đầu** (bỏ, vì JIT và cache) rồi lấy trung bình **5 lần** sau.
3. Cùng một bản vẽ, cùng trạng thái (đóng/mở palette như nhau).
4. Ghi: máy, phiên bản AutoCAD, kích thước bản vẽ, số trước/sau.

---

## 9. Bộ nhớ

- Danh sách `ObjectId` (struct nhỏ) **rẻ**. Lưu hàng trăm nghìn cái không vấn đề.
- **Không** giữ `DBObject`/`Entity` trong collection sau transaction.
- `ResultBuffer`, side `Database`, `PlotEngine`: `using`.
- Subscriber EventAggregator bị quên là **rò rỉ bộ nhớ thật** (tham chiếu mạnh). Kiểm bằng `CORE_DEBUG` (tab Event Subscribers).

---

## ✅ Tự kiểm tra T07

1. Lệnh đếm số cửa trong bản vẽ 100.000 entity mất 8 giây. Ba câu hỏi đầu tiên bạn đặt ra?
2. Vì sao vẽ 2.500 ô trong **một** transaction lại được ưu tiên hơn chia nhỏ, dù rủi ro rollback cả lô?
3. Bạn cần sửa 300 file DWG trên server. Dùng `ICadContext` được không? Vì sao?

<details><summary>Gợi ý đáp án</summary>

1. (a) Có đang `GetObject` **mọi** entity thay vì lọc `ObjectClass` trước không? (b) Có dùng `SelectAll(filter)` được không? (c) Có regen, event handler hay serialize nào chạy trong vòng lặp không? Và trên hết: đo xem thời gian thật sự nằm ở đâu.
2. Nguyên tử (thất bại thì không để lại nửa lưới), **một** bước Undo, và nhanh nhất. 2.500 entity không phải kích thước đáng lo về bộ nhớ.
3. Không. `ICadContext` làm việc với Document đang mở (lock, editor, undo). File trên server đọc bằng side database (`ReadDwgFile`), tự quản lý transaction, không lock, không undo.
</details>

---

**Tiếp theo:** [T08 — Công thức mở rộng dự án →](T08_RECIPES_EXTENDING.md)
