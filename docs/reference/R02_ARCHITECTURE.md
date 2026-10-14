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
            
            tr.Commit(); // QUÊN DÒNG NÀY → không crash, nhưng MỌI THAY ĐỔI BỊ HỦY ÂM THẦM (Dispose = Abort)
        }
        catch (Exception ex)
        {
            tr.Abort(); // Thừa: thoát khỏi using mà chưa Commit thì transaction tự Abort
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
> **Sự thật về Undo:** *Bên trong một lệnh*, AutoCAD tự gom mọi thay đổi của lệnh (kể cả nhiều transaction) thành **một** bước Ctrl+Z. Vấn đề nằm ở **application context** (nút bấm trên Palette): ở đó mỗi lần lock/transaction có thể thành một bước Undo riêng.
>
> **Cách Framework giải quyết:** AutoCAD .NET API **không có** hàm UndoMark managed (không tồn tại `Database.BeginUndoMark`). Framework dùng COM: `doc.GetAcadDocument()` (kiểu `dynamic`), gọi `StartUndoMark()` trước transaction và `EndUndoMark()` trong `finally`, khi bạn truyền `undoMarkName`. Đó là lý do mọi Service của Golden Sample luôn truyền `undoMarkName`: Service dùng chung cho cả lệnh lẫn palette. Chi tiết: [T01 §11](training/T01_AUTOCAD_NET_FOUNDATIONS.md#11-undo).

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
> Trong nhiều kịch bản nạp, CLR **không tự tìm được** DLL phụ thuộc cạnh plugin. Điển hình là khi plugin được nạp **từ byte array** (tool chống khóa DLL / hot-reload), lúc đó `Assembly.Location` rỗng nên không có thư mục để dò. Khi đó JIT ném `FileNotFoundException` và hủy luôn `Initialize()` trước khi dòng code đầu tiên kịp chạy!
>
> **Giải pháp:** Gắn sự kiện `AssemblyResolve` ở hàm vỏ, và **tách toàn bộ logic** dùng DI sang một hàm khác (`InitializeCore()`). Handler của dự án còn ưu tiên trả về **assembly đã nạp** khớp tên + version. Điều này sửa lỗi palette khi nạp bằng byte array ([T03 A2](training/T03_PITFALLS_CASEBOOK.md#a2-palette-lỗi-does-not-have-a-resource-identified-by-the-uri)). Chi tiết cơ chế phân giải: [T01 §12.4](training/T01_AUTOCAD_NET_FOUNDATIONS.md#124-phân-giải-assembly-phụ-thuộc).

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
            // ESC khi đang prompt → AutoCAD trả PromptStatus.Cancel (KHÔNG ném exception);
            // CadPromptHelper quy về null → chỉ cần return.
            // (Một số API khác ném Runtime.Exception với ErrorStatus.UserBreak khi bị ESC —
            //  ExecuteCommand bắt riêng trường hợp đó: không in lỗi, không ghi lịch sử lệnh.)
            var pt = CadPromptHelper.GetPoint("Chọn điểm:");
            if (pt == null) return;
            
            // Bắt đầu logic vẽ...
        });
    }
}
```
