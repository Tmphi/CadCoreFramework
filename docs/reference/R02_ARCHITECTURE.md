> **📘 Tra cứu CadCoreFramework** · [Mục lục tài liệu](../README.md) · [← R01](R01_QUICKSTART.md) · **R02** · [R03 →](R03_API.md)

# R02 — Kiến Trúc Cốt Lõi

> **Dùng khi:** cần nhớ nhanh hợp đồng của `ICadContext`, cách plugin khởi động, hoặc lớp lệnh làm gì cho bạn · **Học bài bản:** [T02 — Mổ xẻ kiến trúc](../training/T02_ARCHITECTURE_DEEP_DIVE.md) (đọc code từng dòng), [T01 §4–5](../training/T01_AUTOCAD_NET_FOUNDATIONS.md#4-transaction-trái-tim-của-mọi-thao-tác) (transaction, lock)

Thư mục `Core/` gói sự phức tạp của AutoCAD API (lock, transaction, undo, luồng) sau vài hợp đồng nhỏ. Trang này tóm tắt các hợp đồng đó. Muốn biết từng dòng code bên trong, đọc T02.

---

## Mục lục

1. [Vì sao cần `ICadContext`](#1-vì-sao-cần-icadcontext)
2. [Hợp đồng của `ICadContext`](#2-hợp-đồng-của-icadcontext)
3. [Undo trong hai ngữ cảnh](#3-undo-trong-hai-ngữ-cảnh)
4. [Khởi động: AppBootstrapper và DI](#4-khởi-động-appbootstrapper-và-di)
5. [Lớp lệnh `CadCommandBase`](#5-lớp-lệnh-cadcommandbase)

---

## 1. Vì sao cần `ICadContext`

Đây là cách viết "cổ điển" để vẽ một đường thẳng bằng AutoCAD API thô:

```csharp
// ⛔ ANTI-PATTERN: tự lock, tự mở transaction, tự nhớ Commit
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

Mỗi dòng ở trên là một chỗ có thể sai: quên lock khi gọi từ palette, quên `AddNewlyCreatedDBObject`, quên `Commit`, dùng `MdiActiveDocument` sau khi người dùng đã đổi bản vẽ. Với `ICadContext`, cùng việc đó là:

```csharp
// ✅ Lock, transaction, Commit và undo do ICadContext lo
_cadContext.Execute(scope =>
{
    scope.DrawLine(new Point3d(0,0,0), new Point3d(10,10,0));
}, undoMarkName: "Vẽ Tường");
```

`scope` ([`ICadScope`](../../Core/ICadScope.cs)) cho bạn `Document`, `Database`, `Transaction`, `CurrentSpace`, `Editor`, `IsReadOnly` và `AppendEntityToCurrentSpace(entity)`. Các hàm vẽ một dòng (`DrawLine`, `EnsureLayer`…) là extension của `ICadScope`, liệt kê ở [R03 §2](R03_API.md#2-vẽ-và-bảng-ký-hiệu-trên-icadscope).

---

## 2. Hợp đồng của `ICadContext`

🔎 [`Core/ICadContext.cs`](../../Core/ICadContext.cs) · cài đặt: [`Core/CadContext.cs`](../../Core/CadContext.cs) · từng dòng: [T02 §4](../training/T02_ARCHITECTURE_DEEP_DIVE.md#4-icadcontext-hợp-đồng-giao-dịch)

| Phương thức | Dùng cho | Khi lỗi |
| :--- | :--- | :--- |
| `void Execute(Action<ICadScope>, bool autoRegen = false, string? undoMarkName = null)` | Ghi, trong lệnh | **Ném** exception |
| `CadResult TryExecute(Action<ICadScope>, …)` | Ghi, trong Service | Trả `CadResult`, **không ném** |
| `CadResult<T> TryExecute<T>(Func<ICadScope, T>, …)` | Ghi có kết quả, trong Service | Trả `CadResult<T>`, không ném |
| `void ExecuteRead(Action<ICadScope>)` | Chỉ đọc | Ném |
| `Task ExecuteAsync(Func<Task<Action<ICadScope>>>, …, CancellationToken)` | Chuẩn bị async (tải dữ liệu) rồi vẽ đồng bộ | Ném |
| `void ExecuteBatch<T>(IEnumerable<T>, Action<ICadScope, T>, IProgress<int>? = null, string undoMarkName = "Batch")` | Vẽ hàng loạt trong **một** transaction | Ném |
| ~~`CadResult<T> Execute<T>(Func<ICadScope, T>, …)`~~ | `[Obsolete]` (CS0618): tên cũ của `TryExecute<T>` | Trả `CadResult<T>`, không ném |

**Quy ước lỗi:** `Execute*` ném, `TryExecute*` trả `CadResult`. Dùng `Execute` trong lệnh (`CadCommandBase` bắt lỗi giúp bạn); dùng `TryExecute` trong Service để lỗi AutoCAD thành giá trị cho nơi gọi quyết định. `CadResult` có `IsSuccess`, `ErrorMessage`, `Exception` (và `Value` với `CadResult<T>`).

> [!WARNING]
> **Bẫy overload:** `Execute(scope => scope.DrawCircle(…))` là lambda *biểu thức trả về giá trị*, nên C# chọn overload generic `Execute<T>` đã obsolete: lỗi bị **nuốt** vào `CadResult` thay vì ném. Khi muốn ném lỗi, viết lambda dạng khối: `Execute(scope => { scope.DrawCircle(…); })`.

Những gì `ICadContext` làm cho bạn, và những gì nó chặn:

| | `Execute` / `TryExecute` | `ExecuteRead` |
| :--- | :--- | :--- |
| Lock document | Luôn lock | Chỉ lock khi gọi từ application context (palette) |
| Transaction | Mở, Commit khi lambda xong, Abort khi lambda ném | Mở, Commit (rẻ hơn Abort) |
| Ghi | Được | **Hợp đồng là chỉ đọc**: `AppendEntityToCurrentSpace`/`SetXRecord`/`RemoveXRecord` bị chặn; tự mở `ForWrite` thì vẫn ghi được, nên analyzer **CAD003** báo lỗi lúc build ([R06 §5](R06_STANDARDS.md#5-analyzer-cad001cad003)) |
| Gọi từ thread khác main thread | Báo lỗi ngay (`CadThread`): `Execute` ném, `TryExecute` trả `CadResult` lỗi | Ném |
| Gọi lồng `Execute` trong `Execute` | Báo lỗi ngay (`CadContextGuard`, [T02 §4.6](../training/T02_ARCHITECTURE_DEEP_DIVE.md#46-cadcontextguard-chặn-lồng-nhau)), cùng quy ước ném / trả `CadResult` | Ném |

`ExecuteAsync` chạy giai đoạn vẽ trên bản vẽ đang active **lúc gọi**, kể cả khi người dùng đã chuyển bản vẽ trong lúc chờ. Vì thế Action vẽ chỉ được chạm database, không gọi `scope.Editor` ([T02 §4.4](../training/T02_ARCHITECTURE_DEEP_DIVE.md#44-executeasync-hai-giai-đoạn)).

---

## 3. Undo trong hai ngữ cảnh

**Bên trong một lệnh**, AutoCAD tự gom mọi thay đổi của lệnh (kể cả nhiều transaction) thành **một** bước Ctrl+Z. Vấn đề nằm ở **application context** (nút bấm trên palette): ở đó mỗi lần lock/transaction có thể thành một bước Undo riêng.

AutoCAD .NET API **không có** hàm undo mark managed (không tồn tại `Database.BeginUndoMark`). Khi bạn truyền `undoMarkName`, `CadContext` dùng COM: `doc.GetAcadDocument()` (kiểu `dynamic`), gọi `StartUndoMark()` trước transaction và `EndUndoMark()` trong `finally`. Đó là lý do mọi Service của Golden Sample luôn truyền `undoMarkName`: Service dùng chung cho cả lệnh lẫn palette. Chi tiết: [T01 §11](../training/T01_AUTOCAD_NET_FOUNDATIONS.md#11-undo).

> [!NOTE]
> `undoMarkName` là tham số thứ **ba**; tham số thứ hai theo vị trí là `bool autoRegen`. Luôn gọi bằng tên: `undoMarkName: "…"`.

---

## 4. Khởi động: AppBootstrapper và DI

🔎 [`Core/AppBootstrapper.cs`](../../Core/AppBootstrapper.cs) · từng bước: [T02 §2–3](../training/T02_ARCHITECTURE_DEEP_DIVE.md#2-khởi-động-và-tắt-appbootstrapper)

`AppBootstrapper` cài `IExtensionApplication`, nên AutoCAD gọi `Initialize()` của nó một lần khi `NETLOAD`. Dự án dùng container DI của Microsoft (`Microsoft.Extensions.DependencyInjection`); mọi service được đăng ký trong `ConfigureServices` (service `AddSingleton`, ViewModel `AddTransient`).

Dòng đầu tiên của `Initialize()` không được dời đi:

```csharp
public void Initialize()
{
    // BẮT BUỘC: Đăng ký AssemblyResolve TRƯỚC KHI gọi bất cứ code nào
    // sử dụng các DLL phụ thuộc (DI, MVVM Toolkit, Newtonsoft...).
    // Lý do: CLR cố gắng JIT-compile InitializeCore() trước khi chạy nó,
    // và trong lúc đó sẽ tìm các DLL. Nếu chưa register handler → FileNotFoundException.
    AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

    // AutoCAD luôn gọi Initialize trên main thread → ghi nhận để ICadContext chặn lời gọi từ thread khác.
    CadThread.CaptureMainThread();

    try
    {
        InitializeCore();
    }
    // … catch: hiện MessageBox + in lỗi ra command line, không để lỗi khởi động làm sập AutoCAD
}
```

> [!WARNING]
> **Vì sao phải tách `InitializeCore()`:** trước khi chạy một method, JIT biên dịch nó và nạp mọi kiểu mà nó dùng. Nếu `Initialize()` dùng thẳng `ServiceCollection`, JIT đi tìm DLL của DI **trước khi** dòng đăng ký `AssemblyResolve` kịp chạy. Trong nhiều kịch bản nạp, CLR không tự tìm được DLL cạnh plugin; điển hình là khi plugin được nạp **từ byte array** (tool chống khóa DLL), lúc đó `Assembly.Location` rỗng. Kết quả là `FileNotFoundException` và `Initialize()` chết trước dòng đầu tiên.
>
> Handler của dự án còn ưu tiên trả về **assembly đã nạp** khớp tên + version; điều này sửa lỗi palette khi nạp bằng byte array ([T03 A2](../training/T03_PITFALLS_CASEBOOK.md#a2-palette-lỗi-does-not-have-a-resource-identified-by-the-uri)). Cơ chế phân giải: [T01 §12.4](../training/T01_AUTOCAD_NET_FOUNDATIONS.md#124-phân-giải-assembly-phụ-thuộc).

---

## 5. Lớp lệnh `CadCommandBase`

🔎 [`Commands/CadCommandBase.cs`](../../Commands/CadCommandBase.cs) · sơ đồ bắt lỗi: [T02 §5](../training/T02_ARCHITECTURE_DEEP_DIVE.md#5-cadcommandbase-biên-giới-của-mọi-lệnh)

Mọi lớp lệnh kế thừa `CadCommandBase` và khai báo `[assembly: CommandClass(typeof(…))]` trong [`Properties/AssemblyInfo.cs`](../../Properties/AssemblyInfo.cs). Thiếu dòng đó thì AutoCAD báo `Unknown command`.

| Thành viên | Làm gì |
| :--- | :--- |
| `bool ExecuteCommand(Action)` | Bọc thân lệnh: bắt và in lỗi, ghi log, ghi lịch sử lệnh. ESC (`ErrorStatus.UserBreak`) thì im lặng, không ghi lịch sử |
| `void ExecuteWithScope(Action<ICadScope>, string undoMarkName = null)` | Rút gọn của `ExecuteCommand(() => CadContext.Execute(…))`, cho lệnh nhỏ không prompt |
| `Task<bool> ExecuteWithScopeAsync(Func<Task<Action<ICadScope>>>, string undoMarkName = null)` | Bản async; chụp `Document` trước `await`, báo lỗi lên bản vẽ đang active |
| `static T GetRequiredService<T>()` | Lấy service từ DI, ném lỗi rõ ràng nếu thiếu. Gọi **bên trong** `ExecuteCommand` để lỗi được bắt |
| `Document`, `Database`, `Editor` | Đọc `MdiActiveDocument` mỗi lần truy cập. An toàn trong lệnh đồng bộ, **không** dùng sau `await` |
| `Logger`, `CadContext`, `CommandHistory` | Lấy lười từ DI |

Hai lệnh có thật trong [`SampleCommands.cs`](../../Commands/SampleCommands.cs), nguyên văn:

```csharp
[CommandMethod("CORE_INFO")]
public void CoreInfoCommand()
{
    ExecuteCommand(() =>
    {
        Editor?.WriteMessage("\n[CadCoreFramework] Hạ tầng plugin đã sẵn sàng.");
    });
}
```

```csharp
[CommandMethod("DRAW_CIRCLE_SAMPLE")]
public void DrawCircleSample()
{
    ExecuteWithScope(scope =>
    {
        ObjectId circleId = scope.DrawCircle(new Point3d(0, 0, 0), radius: 10);
        GetRequiredService<INotificationService>().Info($"Đã vẽ đường tròn (ID: {circleId})");
    });
}
```

Lệnh **có prompt** thì prompt trước, trong `ExecuteCommand`, rồi mới gọi Service. ESC khi đang prompt **không** ném exception: AutoCAD trả `PromptStatus.Cancel`, `CadPromptHelper` quy về `null`, và lệnh chỉ cần `return`. Mẫu đầy đủ là `SAMPLE_GRID` trong [`GridMarkerCommands.cs`](../../Samples/GridMarker/GridMarkerCommands.cs), giải thích ở [R05 §4](R05_GOLDEN_SAMPLE.md#4-nguyên-tắc-3--hiểu-đúng-ngữ-cảnh-command-vs-palette). Checklist thêm lệnh mới: [T08 §1](../training/T08_RECIPES_EXTENDING.md#1-thêm-một-lệnh).
