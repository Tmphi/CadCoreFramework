# Phần 2: Kiến Trúc Cốt Lõi (Core Architecture)

Trái tim của **CadCoreFramework** nằm trong thư mục `Core`. Kiến trúc này nhằm cô lập hoàn toàn sự phức tạp và bất ổn định của AutoCAD API, tạo ra một môi trường lập trình C# thuần túy, an toàn và dễ bảo trì.

---

## 2.1 Quản Lý Giao Dịch An Toàn (`ICadContext` & `ICadScope`)

### Tại sao lại cần? (The Anti-Pattern)
AutoCAD quản lý dữ liệu theo cấu trúc Database phân cấp. Dưới đây là cách code "cổ điển" (Anti-pattern) mà các Lập trình viên mới thường viết để vẽ 1 đường thẳng:

```csharp
// ⛔ ANTI-PATTERN: Code thô sơ, nguy hiểm, dễ gây Fatal Error
Document doc = Application.DocumentManager.MdiActiveDocument;
using (doc.LockDocument()) // Phải tự LockDocument
{
    using (Transaction tr = doc.Database.TransactionManager.StartTransaction()) // Tự Start Transaction
    {
        try 
        {
            BlockTable bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
            BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
            
            Line myLine = new Line(new Point3d(0,0,0), new Point3d(10,10,0));
            btr.AppendEntity(myLine);
            tr.AddNewlyCreatedDBObject(myLine, true);
            
            tr.Commit(); // NẾU QUÊN GỌI DÒNG NÀY -> AutoCAD CRASH NGAY LẬP TỨC.
        }
        catch (Exception ex)
        {
            tr.Abort(); // Phải tự handle rollback
            throw;
        }
    }
}
```

### Giải pháp của Framework (The Pattern)
Toàn bộ đoạn code 20 dòng đầy cạm bẫy phía trên được Framework đóng gói vào duy nhất **1 Dòng Gọi Hàm** qua `ICadContext`.

```csharp
// ✅ PATTERN CHUẨN: Ngắn gọn, An toàn tuyệt đối
_cadContext.Execute(scope =>
{
    scope.DrawLine(new Point3d(0,0,0), new Point3d(10,10,0));
}, undoMarkName: "Vẽ Tường");
```

### Giải Phẫu Under-The-Hood của `CadContext`

Nếu bạn mò vào mã nguồn của lớp [`CadContext`](../Core/CadContext.cs), bạn sẽ thấy một kỹ thuật xử lý Undo cực kỳ đặc biệt liên quan đến **COM Object**.

> [!NOTE]
> **Sự thật về Undo trong .NET API:** Trong AutoCAD .NET thuần, `TransactionManager.StartTransaction()` KHÔNG HỀ gộp các thao tác lại thành một bước Undo. Nếu bạn chạy 1 vòng lặp vẽ 100 đường Line trong 1 Transaction, khi bấm **Ctrl+Z**, AutoCAD sẽ xoá từng đường Line một (100 lần nhấn Ctrl+Z).
> 
> **Cách Framework giải quyết:** Framework truy cập ngược xuống tầng sâu hơn của AutoCAD thông qua giao diện COM: `(AcadDocument)AcadApplication.ActiveDocument`. Bằng cách gọi `comDoc.StartUndoMark()` trước khi mở Transaction và `comDoc.EndUndoMark()` sau khi Commit, Framework ép AutoCAD phải gói gọn 100 đường Line đó thành đúng 1 bước Ctrl+Z. Bạn chỉ có thể tìm thấy kỹ thuật "đắt giá" này trong các dự án Enterprise.

---

## 2.2 Bootstrapper & Dependency Injection (DI)

AutoCAD .NET truyền thống sử dụng các class Static (tĩnh) khổng lồ, khiến code rất khó bảo trì và không thể viết Unit Test. Dự án này sử dụng container DI của Microsoft (`Microsoft.Extensions.DependencyInjection`).

Tất cả được cấu hình tại [`AppBootstrapper.cs`](../Core/AppBootstrapper.cs). Lớp này kế thừa `IExtensionApplication`, nên hàm `Initialize()` của nó sẽ tự động chạy ngay khi bạn gõ `NETLOAD`.

### JIT Compilation & Assembly Resolve (Bí Kíp "Cứu Mạng")

Hãy quan sát kỹ dòng đầu tiên trong hàm `Initialize()` của `AppBootstrapper`:
```csharp
public void Initialize()
{
    // BẮT BUỘC: Đăng ký AssemblyResolve TRƯỚC KHI gọi bất cứ code nào dùng thư viện ngoài.
    AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
    
    InitializeCore(); // Tách riêng hàm này!
}
```

> [!WARNING]
> **Under the hood (JIT Compiler):** Trình biên dịch JIT (Just-In-Time) của .NET hoạt động theo cơ chế "Quét trước khi chạy". Ngay khi nó chuẩn bị bước vào một hàm (ví dụ `Initialize()`), nó sẽ quét toàn bộ biến và lời gọi hàm bên trong. Nếu bên trong chứa một class thuộc file DLL khác (ví dụ `ServiceCollection` của Microsoft.DI), nó sẽ cố gắng tìm file DLL đó.
> Vì AutoCAD không tự động quét thư mục của Plugin để tìm file DLL phụ thuộc, JIT Compiler sẽ quăng ra lỗi `FileNotFoundException` và huỷ luôn hàm `Initialize()` trước khi cả đoạn code đầu tiên kịp chạy!
> 
> **Giải pháp:** Phải gắn sự kiện `AssemblyResolve` (Hướng dẫn AutoCAD biết chỗ tìm DLL) ở hàm vỏ, và **tách toàn bộ logic** sử dụng DI sang một hàm khác (`InitializeCore()`). Đây là kỹ thuật vô cùng quan trọng mà 90% Fresher đều mắc phải khi tự setup plugin.

---

## 2.3 Lớp Lệnh `CadCommandBase`

Mỗi khi bạn muốn tạo một lệnh gõ vào AutoCAD (ví dụ `VE_TU_DIEN`), bạn phải tạo một hàm gắn thẻ `[CommandMethod]`.
Hãy luôn kế thừa lớp [`CadCommandBase`](../Commands/CadCommandBase.cs) để hưởng lợi từ hệ thống bẫy lỗi tự động.

Ví dụ tại [`SampleCommands.cs`](../Commands/SampleCommands.cs):
```csharp
public class SampleCommands : CadCommandBase
{
    [CommandMethod("VE_TU_DIEN")]
    public void VeTuDienCommand()
    {
        // ExecuteCommand sẽ tự động bọc try-catch, bắt các lỗi văng CAD
        ExecuteCommand(() => 
        {
            // Nếu người dùng ấn ESC khi đang chọn điểm, AutoCAD sẽ quăng lỗi UserBreak.
            // Framework tự động nuốt (swallow) lỗi này và kết thúc lệnh êm đẹp.
            var pt = CadPromptHelper.GetPoint("Chọn điểm:");
            if (pt == null) return;
            
            // Bắt đầu logic vẽ...
        });
    }
}
```
