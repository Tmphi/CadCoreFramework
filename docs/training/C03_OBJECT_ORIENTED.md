> **🎓 Học viện CadCoreFramework** · [Lộ trình](README.md) · [← C02](C02_DATA_AND_COLLECTIONS.md) · **C03** · [C04 →](C04_CSHARP_IN_THIS_PROJECT.md)

# C03 — Hướng Đối Tượng Qua Code Dự Án

> **Cấp độ:** 🌰 Nhập môn · **Cần trước:** [C02](C02_DATA_AND_COLLECTIONS.md) · **Thời lượng:** tuần 3–4
>
> **⏱ Đọc:** ≈ 40 phút · **Thực hành:** [bài tập §10](#10-bài-tập) · **Nhãn mục:** mọi mục đều 🎯, vì Giai đoạn 0 đã lọc theo [quy tắc 20/80](C00_START_HERE.md#1-quy-tắc-2080-học-đúng-phần-dự-án-thật-sự-dùng) ([README §5](README.md#5-quy-ước-đọc))

Toàn bộ dự án được tổ chức thành **class** (hơn 100 khai báo) và **interface** (20). Module này dạy bạn đọc và viết chúng. Mỗi khái niệm đi kèm đúng chỗ nó xuất hiện trong dự án, và kết thúc bằng việc đọc trọn một file thật: [`GridSpec.cs`](../../Samples/GridMarker/GridSpec.cs).

**Sau module này bạn phải làm được:**
1. Giải thích từng dòng của `GridSpec.cs` ([§9](#9-đọc-có-hướng-dẫn-gridspeccs)).
2. Viết một class **bất biến** có `Validate()` — bài [E1](#10-bài-tập).
3. Giải thích vì sao `GridMarkerService` **nhận** `ICadContext` qua constructor thay vì tự tạo, và điều đó giúp gì cho unit test.

---

## Mục lục

1. [Class và object](#1-class-và-object)
2. [Property và constructor](#2-property-và-constructor)
3. [Đóng gói: đối tượng tự giữ luật của mình](#3-đóng-gói-đối-tượng-tự-giữ-luật-của-mình)
4. [static: hàm không cần đối tượng](#4-static-hàm-không-cần-đối-tượng)
5. [namespace, using và phạm vi truy cập](#5-namespace-using-và-phạm-vi-truy-cập)
6. [Interface: hợp đồng và bản giả](#6-interface-hợp-đồng-và-bản-giả)
7. [Kế thừa: abstract, virtual, override](#7-kế-thừa-abstract-virtual-override)
8. [Nhận phụ thuộc qua constructor](#8-nhận-phụ-thuộc-qua-constructor)
9. [Đọc có hướng dẫn: GridSpec.cs](#9-đọc-có-hướng-dẫn-gridspeccs)
10. [Bài tập](#10-bài-tập)

---

## 1. Class và object

**Class** là *bản thiết kế*: mô tả một loại đối tượng có những dữ liệu gì và làm được gì. **Object** (đối tượng) là *một thứ cụ thể* làm theo bản thiết kế đó, tạo bằng `new` — từ khóa đứng thứ hai trong bảng 20/80 (439 dòng).

```csharp
var cell = new GridCell(0, 0, 100, 50, "A1");          // new: tạo một ĐỐI TƯỢNG từ class
var other = new GridCell(1, 2, 140, 30, "B3");
Console.WriteLine($"{cell.Label} tại ({cell.CenterX}, {cell.CenterY})");    // A1 tại (100, 50)
Console.WriteLine($"{other.Label} ở hàng {other.Row}, cột {other.Column}");  // B3 ở hàng 1, cột 2

public sealed class GridCell                            // class: BẢN THIẾT KẾ
{
    public int Row { get; }                              // property chỉ đọc
    public int Column { get; }
    public double CenterX { get; }
    public double CenterY { get; }
    public string Label { get; }

    public GridCell(int row, int column, double centerX, double centerY, string label)   // constructor
    {
        Row = row;
        Column = column;
        CenterX = centerX;
        CenterY = centerY;
        Label = label;
    }
}
```

Một class, hai object: `cell` và `other` có cùng "hình dạng" nhưng dữ liệu riêng. 🔎 `GridCell` thật nằm trong [`GridPlanner.cs`](../../Samples/GridMarker/GridPlanner.cs); `GridPlanner.Plan` tạo một `GridCell` cho **mỗi** ô của lưới.

---

## 2. Property và constructor

**Property** là "mặt tiền" dữ liệu của object. Ba dạng gặp trong dự án:

| Dạng | Ý nghĩa | Ví dụ thật |
| :--- | :--- | :--- |
| `{ get; }` | chỉ đọc; chỉ gán được trong constructor | `public int Rows { get; }` — [`GridSpec`](../../Samples/GridMarker/GridSpec.cs) |
| `{ get; set; }` | đọc và ghi từ bên ngoài | `public string Label { get; set; } = string.Empty;` — `GridMarkerTag` trong [`GridMarkerService.cs`](../../Samples/GridMarker/GridMarkerService.cs) |
| `=> biểu thức` | **tính** mỗi lần đọc, không lưu | `public int CellCount => Rows * Columns;` |

Thử gán vào property chỉ đọc: `cell.Label = "Z9";` — trình biên dịch chặn:

```text
error CS0200: Property or indexer 'GridCell.Label' cannot be assigned to -- it is read only
```

**Constructor** là hàm đặc biệt, **trùng tên class**, không có kiểu trả về, chạy đúng một lần khi `new`. Việc của nó là đưa object vào trạng thái hợp lệ ngay từ đầu. Nó nhận tham số mặc định như hàm thường: `new GridSpec(3, 4)` dùng `spacing = 20`, `layerName = "SAMPLE_GRID"`…

> **Field hay property?** *Field* là biến bên trong object, thường `private` và tên bắt đầu bằng `_` (`private readonly ICadContext _cadContext;`). *Property* là thứ cho bên ngoài xem. Quy ước của dự án: dữ liệu nội bộ là field `private`, thứ công khai là property.

---

## 3. Đóng gói: đối tượng tự giữ luật của mình

**Đóng gói** (encapsulation) nghĩa là dữ liệu và **luật** của dữ liệu ở cùng một chỗ. Người dùng class không cần biết luật — chỉ cần hỏi `IsValid`:

```csharp
var ok = new PanelSpec("MSB-01", 800);
var bad = new PanelSpec("", 200, height: 0);
Console.WriteLine($"{ok.Name}: {ok.Area} mm², hợp lệ = {ok.IsValid}");
foreach (string error in bad.Validate())
    Console.WriteLine($"- {error}");

public sealed class PanelSpec
{
    public const double MinWidth = 300;                 // hằng số của class

    public string Name { get; }
    public double Width { get; }
    public double Height { get; }

    public PanelSpec(string name, double width, double height = 2000)
    {
        Name = name ?? string.Empty;
        Width = width;
        Height = height;
    }

    public double Area => Width * Height;               // property TÍNH TOÁN, viết rút gọn bằng =>

    public IReadOnlyList<string> Validate()             // method: một hành động của đối tượng
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Name)) errors.Add("Tên tủ không được để trống.");
        if (Width < MinWidth) errors.Add($"Chiều rộng phải ≥ {MinWidth}.");
        if (Height <= 0) errors.Add("Chiều cao phải > 0.");
        return errors;
    }

    public bool IsValid => Validate().Count == 0;
}
```

```text
MSB-01: 1600000 mm², hợp lệ = True
- Tên tủ không được để trống.
- Chiều rộng phải ≥ 300.
- Chiều cao phải > 0.
```

Ba quyết định thiết kế, giống hệt `GridSpec`:
- **Bất biến** (immutable): mọi property chỉ có `get`. Tạo xong không ai đổi được, nên truyền đi đâu cũng an toàn — không có chuyện "ai đó sửa mất số hàng giữa chừng".
- **`sealed`**: không cho class khác kế thừa ([§7](#7-kế-thừa-abstract-virtual-override)). Class dữ liệu không cần mở rộng thì niêm phong lại.
- **Luật nằm trong class**: lệnh `SAMPLE_GRID` và palette Grid Marker đều gọi `spec.Validate()` — luật chỉ viết **một** lần ([R05 — Golden Sample](../reference/R05_GOLDEN_SAMPLE.md#3-nguyên-tắc-2--validate-một-lần-ở-model)).

---

## 4. static: hàm không cần đối tượng

Thành viên `static` thuộc về **class**, không thuộc object nào — gọi qua tên class, không cần `new`:

```csharp
Console.WriteLine(PanelMath.TotalWidth(3, 600, 50));    // 1900  ← gọi qua TÊN CLASS, không cần new

public static class PanelMath                            // static class: chỉ chứa hàm, không có trạng thái
{
    public static double TotalWidth(int count, double width, double gap)
        => count * width + (count - 1) * gap;
}
```

Bạn đã dùng `static` từ bài đầu: `Console.WriteLine`, `string.Join`, `Math.Abs`. 🔎 Trong dự án (307 dòng có `static`):
- [`GridPlanner`](../../Samples/GridMarker/GridPlanner.cs) là `public static class GridPlanner`: chỉ **tính** (nhận `GridSpec`, trả danh sách ô), không giữ dữ liệu, nên không cần object.
- Mọi class chứa *extension method* trong `Extensions/` đều là `static class` ([C04 §5](C04_CSHARP_IN_THIS_PROJECT.md#5-extension-method-thêm-hàm-cho-kiểu-có-sẵn)).
- `const` luôn là static: viết `GridSpec.MaxDimension`, không cần object.

> ⚠️ **Bẫy:** field `static` mà **thay đổi được** là dữ liệu dùng chung của *cả tiến trình*. Trong AutoCAD, mọi bản vẽ đang mở chạy chung một tiến trình, nên một biến static "bản vẽ hiện tại" sẽ bị các bản vẽ ghi đè lẫn nhau ([T01 §1](T01_AUTOCAD_NET_FOUNDATIONS.md#1-plugin-sống-ở-đâu-autocad-net-và-appdomain-dùng-chung)). Quy tắc: static cho **hàm thuần** và **hằng số**; trạng thái thì để trong object.

---

## 5. namespace, using và phạm vi truy cập

Năm dòng đầu của [`GridSpec.cs`](../../Samples/GridMarker/GridSpec.cs):

```csharp
using System.Collections.Generic;              // dùng List<T>, IReadOnlyList<T> mà không cần gõ tên đầy đủ
using CadCoreFramework.Utils;                  // dùng SymbolNameRules

namespace CadCoreFramework.Samples.GridMarker  // "địa chỉ" của các class khai báo bên trong
{
```

- **namespace** gom các class thành nhóm, tránh trùng tên. Trong dự án, namespace **khớp thư mục**: file `Samples/GridMarker/GridSpec.cs` ở namespace `CadCoreFramework.Samples.GridMarker`.
- **using** cho phép gọi tắt. Không có `using System.Collections.Generic;` thì phải viết `System.Collections.Generic.List<string>`.
- **using alias** đặt tên cho một kiểu cụ thể khi nhiều kiểu trùng tên. AutoCAD có hai class `Application` (trong `Autodesk.AutoCAD.ApplicationServices` và `….Core`), WPF có thêm `System.Windows.Application`. Vì vậy [`CadContext.cs`](../../Core/CadContext.cs) viết: `using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;`.

**Phạm vi truy cập** — ai được dùng một thành viên:

| Từ khóa | Ai dùng được | Ví dụ thật |
| :--- | :--- | :--- |
| `public` | mọi nơi | `public sealed class GridSpec` |
| `internal` | chỉ trong cùng project (DLL) — và project test, nhờ `InternalsVisibleTo` trong [`AssemblyInfo.cs`](../../Properties/AssemblyInfo.cs) | constructor `internal LoggerService(string logDirectory, LogSeverity minimumLevel, Func<DateTimeOffset> clock)` dành cho test |
| `protected` | class đó và class **con** | `protected CadCommandBase()`, `protected void TrackSubscription(…)` |
| `private` | chỉ bên trong class đó | `private readonly ICadContext _cadContext;` |

💡 **20/80:** bắt đầu bằng `private`, chỉ mở rộng khi thật sự có người cần dùng. Thứ gì đã `public` thì rất khó thu hồi, vì code của người khác đã dựa vào nó.

---

## 6. Interface: hợp đồng và bản giả

**Interface** liệt kê những gì một class **phải có**, không nói làm thế nào. Class nào "ký" hợp đồng (`: ITên`) thì phải viết đủ. Nhờ vậy, code chỉ cần biết hợp đồng, không cần biết class cụ thể — và ta **thay được** bản thật bằng bản giả:

```csharp
var morning = new FakeClock(new DateTime(2026, 10, 14, 8, 0, 0));
var evening = new FakeClock(new DateTime(2026, 10, 14, 19, 0, 0));
Console.WriteLine(Greeting(morning));          // Chào buổi sáng
Console.WriteLine(Greeting(evening));          // Chào buổi tối
Console.WriteLine(Greeting(new SystemClock()) != ""); // True — đồng hồ thật cũng dùng được

string Greeting(IClock clock)                   // chỉ biết "một thứ có Now", không biết là đồng hồ nào
    => clock.Now.Hour < 12 ? "Chào buổi sáng" : clock.Now.Hour < 18 ? "Chào buổi chiều" : "Chào buổi tối";

public interface IClock                         // interface: HỢP ĐỒNG — "phải có những gì"
{
    DateTime Now { get; }
}

public sealed class SystemClock : IClock        // ": IClock" = cam kết thực hiện hợp đồng
{
    public DateTime Now => DateTime.Now;
}

public sealed class FakeClock : IClock          // bản "giả" cho test: giờ do ta quyết định
{
    public FakeClock(DateTime now) { Now = now; }
    public DateTime Now { get; }
}
```

(`điều_kiện ? a : b` là **toán tử ba ngôi**: "nếu điều kiện đúng thì `a`, không thì `b`" — một `if/else` viết gọn trong một biểu thức.)

Không có `FakeClock` thì muốn kiểm câu "Chào buổi tối" bạn phải… đợi đến tối. Dự án gặp đúng bài toán đó với AutoCAD, ở quy mô lớn hơn:

| Hợp đồng | Bản thật (cần AutoCAD) | Bản giả (chạy trong unit test) |
| :--- | :--- | :--- |
| [`ICadContext`](../../Core/ICadContext.cs) — `Execute`, `TryExecute`, `ExecuteRead`, `ExecuteAsync`… | [`CadContext`](../../Core/CadContext.cs): lock bản vẽ, mở transaction, commit | [`FakeCadContext`](../../CadCoreFramework.Testing/Fakes/FakeCadContext.cs): đếm số lần gọi, ghi lại tham số, giả lập lỗi |

Vì [`GridMarkerService`](../../Samples/GridMarker/GridMarkerService.cs) chỉ biết `ICadContext`, test [`GridMarkerServiceTests`](../../CadCoreFramework.Tests/Samples/GridMarker/GridMarkerServiceTests.cs) đưa cho nó một `FakeCadContext`, nên chạy được trên máy không cài AutoCAD — kể cả máy CI của GitHub. Chi tiết: [T02 §4](T02_ARCHITECTURE_DEEP_DIVE.md#4-icadcontext-hợp-đồng-giao-dịch), [T05](T05_TESTING_STRATEGY.md).

💡 **20/80:** quy ước tên interface bắt đầu bằng **`I`** (`ICadContext`, `ILoggerService`, `IGridMarkerService`). Thấy chữ `I` đầu tên kiểu là biết đó là một hợp đồng.

---

## 7. Kế thừa: abstract, virtual, override

**Kế thừa** cho một class (con) nhận lại mọi thứ của class khác (cha) và bổ sung hoặc viết lại một phần:

```csharp
Shape[] shapes = { new Rect(3, 4), new Circle(1) };
foreach (Shape s in shapes)
    Console.WriteLine(s.Describe());

public abstract class Shape                     // abstract: không tạo trực tiếp được, chỉ để KẾ THỪA
{
    protected Shape(string name) { Name = name; }
    public string Name { get; }
    public abstract double Area();              // lớp con BẮT BUỘC viết
    public virtual string Describe() => $"{Name}: diện tích {Area():F1}";   // lớp con ĐƯỢC PHÉP viết lại
}

public sealed class Rect : Shape                // Rect KẾ THỪA Shape
{
    public Rect(double w, double h) : base("Chữ nhật") { W = w; H = h; }   // base(...): gọi constructor cha
    public double W { get; }
    public double H { get; }
    public override double Area() => W * H;
}

public sealed class Circle : Shape
{
    public Circle(double r) : base("Tròn") { R = r; }
    public double R { get; }
    public override double Area() => Math.PI * R * R;
    public override string Describe() => "⭕ " + base.Describe();          // gọi lại bản của lớp cha
}
```

```text
Chữ nhật: diện tích 12.0
⭕ Tròn: diện tích 3.1
```
(Máy định dạng Việt Nam in `12,0` và `3,1`.)

| Từ khóa | Nghĩa |
| :--- | :--- |
| `abstract class` | chỉ để kế thừa. `new Shape("x")` báo `error CS0144: Cannot create an instance of the abstract type or interface 'Shape'` |
| `abstract` method | không có thân; class con **bắt buộc** `override`. Quên thì báo `error CS0534: 'Triangle' does not implement inherited abstract member 'Shape.Area()'` |
| `virtual` method | có sẵn thân; class con **được phép** `override` |
| `base.X` / `: base(…)` | gọi phiên bản của class cha / constructor của class cha |
| `sealed` | cấm kế thừa tiếp |

🔎 Trong dự án:
- Mọi class chứa lệnh kế thừa [`CadCommandBase`](../../Commands/CadCommandBase.cs) (`public abstract class CadCommandBase`). Ví dụ `public class GridMarkerCommands : CadCommandBase` nhận sẵn `ExecuteCommand(…)`, `GetRequiredService<T>()`, `Logger`, `CadContext`… mà không phải tự viết ([T02 §5](T02_ARCHITECTURE_DEEP_DIVE.md#5-cadcommandbase-biên-giới-của-mọi-lệnh)).
- [`ViewModelBase`](../../ViewModels/ViewModelBase.cs) là `public abstract partial class ViewModelBase : ObservableObject, IBusyIndicator, IDisposable` — kế thừa **một** class (`ObservableObject`) và thực hiện **hai** interface. Nó có `public virtual void Dispose()` để ViewModel con viết thêm phần dọn dẹp của mình.
- Jig: `public abstract class CadDrawJigBase : DrawJig` viết đè `protected override SamplerStatus Sampler(JigPrompts prompts)` của AutoCAD ([`Utils/Jigs`](../../Utils/Jigs/CadDrawJigBase.cs)).

💡 **20/80:** C# chỉ cho kế thừa **một** class nhưng thực hiện **nhiều** interface. Dự án chỉ có 4 class `abstract`: phần lớn việc dùng lại code đi qua **interface + nhận phụ thuộc** ([§8](#8-nhận-phụ-thuộc-qua-constructor)), không qua cây kế thừa sâu.

---

## 8. Nhận phụ thuộc qua constructor

Một class cần class khác để làm việc (service vẽ cần `ICadContext`, cần logger…). Có hai cách:
- **Tự tạo:** `var ctx = new CadContext();` bên trong class → bị "hàn chết" vào AutoCAD thật, không test được.
- **Nhận qua constructor:** người tạo object đưa phụ thuộc vào → test đưa bản giả, AutoCAD đưa bản thật.

```csharp
var service = new GreetingService(new FakeClock(new DateTime(2026, 1, 1, 9, 0, 0)));
Console.WriteLine(service.Greet("Minh"));       // Chào buổi sáng, Minh!

public interface IClock { DateTime Now { get; } }

public sealed class FakeClock : IClock
{
    public FakeClock(DateTime now) { Now = now; }
    public DateTime Now { get; }
}

public sealed class GreetingService
{
    private readonly IClock _clock;              // readonly: chỉ gán trong constructor

    public GreetingService(IClock clock)         // NHẬN phụ thuộc qua constructor — không tự new
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public string Greet(string name)
        => $"{(_clock.Now.Hour < 12 ? "Chào buổi sáng" : "Xin chào")}, {name}!";
}
```

🔎 Đây đúng là hình dạng constructor của [`GridMarkerService`](../../Samples/GridMarker/GridMarkerService.cs):

```csharp
public GridMarkerService(ICadContext cadContext, IEventAggregator events, ILoggerService logger)
{
    _cadContext = cadContext ?? throw new ArgumentNullException(nameof(cadContext));
    _events = events ?? throw new ArgumentNullException(nameof(events));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
}
```

Vậy **ai** gọi constructor đó với đủ ba tham số? Một "nhà máy" có tên **DI container** (Dependency Injection). [`AppBootstrapper`](../../Core/AppBootstrapper.cs) khai báo một lần khi plugin khởi động:

```csharp
services.AddSingleton<ICadContext, CadContext>();
services.AddSingleton<Samples.GridMarker.IGridMarkerService, Samples.GridMarker.GridMarkerService>();
```

Nghĩa là: "ai cần `IGridMarkerService` thì tạo **một** `GridMarkerService` duy nhất (*singleton*), tự đưa `ICadContext`, `IEventAggregator`, `ILoggerService` vào constructor". `AddTransient` thì tạo object **mới** mỗi lần cần (dùng cho ViewModel). Chi tiết: [T02 §3](T02_ARCHITECTURE_DEEP_DIVE.md#3-dependency-injection).

> Không phải lúc nào cũng cần một interface. [`LoggerService`](../../Core/LoggerService.cs) nhận đồng hồ dưới dạng **hàm** `Func<DateTimeOffset> clock`: bản thật truyền `() => DateTimeOffset.Now`, test truyền một giờ cố định. Cú pháp `Func` và `=>` ở [C04 §1](C04_CSHARP_IN_THIS_PROJECT.md#1-delegate-hàm-là-một-giá-trị).

---

## 9. Đọc có hướng dẫn: GridSpec.cs

Mở [`Samples/GridMarker/GridSpec.cs`](../../Samples/GridMarker/GridSpec.cs) (107 dòng). Bạn đã học **mọi** thứ trong file này, trừ `NaN`, được giải thích ngay trong bảng:

| Dòng | Code | Khái niệm | Học ở |
| :---: | :--- | :--- | :--- |
| 1–2 | `using System.Collections.Generic;` | `using` | [§5](#5-namespace-using-và-phạm-vi-truy-cập) |
| 4 | `namespace CadCoreFramework.Samples.GridMarker` | namespace khớp thư mục | §5 |
| 6, 16–25 | `/// <summary>…` | *XML doc comment*: hiện ra khi rê chuột lên tên trong Visual Studio | — |
| 7–14 | `public enum GridLabelStyle { LetterNumber, Sequential }` | enum | [C02 §7](C02_DATA_AND_COLLECTIONS.md#7-enum-tập-lựa-chọn-có-tên) |
| 26 | `public sealed class GridSpec` | class, `sealed` | §1, §3 |
| 29, 32 | `public const int MaxDimension = 100;` | `const` | [C02 §8](C02_DATA_AND_COLLECTIONS.md#8-const-và-readonly) |
| 34–45 | `public int Rows { get; }` … | property chỉ đọc → **bất biến** | §2, §3 |
| 47–63 | `public GridSpec(int rows, int columns, double spacing = 20, …)` | constructor, tham số mặc định | §2, [C01 §8](C01_FIRST_PROGRAM.md#8-hàm-method) |
| 61 | `LayerName = layerName ?? string.Empty;` | `??` — không bao giờ để `LayerName` là null | [C02 §6](C02_DATA_AND_COLLECTIONS.md#6-null-giá-trị-không-có) |
| 66 | `public int CellCount => Rows * Columns;` | property tính toán | §2 |
| 72–99 | `public IReadOnlyList<string> Validate()` | method; `List` bên trong, `IReadOnlyList` ra ngoài | §3, [C02 §5](C02_DATA_AND_COLLECTIONS.md#5-trả-về-danh-sách-chỉ-đọc-ireadonlylist) |
| 77 | `errors.Add($"Số hàng phải trong khoảng 1–{MaxDimension}.");` | chuỗi nội suy | [C01 §5](C01_FIRST_PROGRAM.md#5-chuỗi-và-định-dạng-số--bẫy-định-dạng-việt-nam) |
| 80 | `if (Rows >= 1 && Columns >= 1 && CellCount > MaxCells)` | `&&` — chỉ báo "quá nhiều ô" khi số hàng/cột đã hợp lệ | [C01 §6](C01_FIRST_PROGRAM.md#6-ra-quyết-định-if-else-switch) |
| 84 | `if (!IsFinite(Spacing) \|\| !IsFinite(CellSize) \|\| !IsFinite(TextHeight))` | `!` và `\|\|`. **`NaN`** (*Not a Number*, "không phải số") là một giá trị đặc biệt của `double`, ví dụ kết quả của `0.0 / 0.0`. So sánh `<`, `<=`, `>`, `>=`, `==` với `NaN` đều trả `false` (chỉ `!=` trả `true`), nên luật `CellSize <= 0` ở dòng 86 không bắt được nó; phải hỏi thẳng bằng `double.IsNaN` | [C01 §6](C01_FIRST_PROGRAM.md#6-ra-quyết-định-if-else-switch) |
| 95 | `SymbolNameRules.ContainsInvalidCharacter(LayerName)` | gọi hàm `static` của class khác — cùng luật mà `EnsureLayer` dùng | §4 |
| 96 | `` "… (< > / \\ \" : ; ? * \| , = `)." `` | `\\` và `\"` trong chuỗi | [C02 E1](C02_DATA_AND_COLLECTIONS.md#10-bài-tập) |
| 102 | `public bool IsValid => Validate().Count == 0;` | property tính toán gọi method | §2 |
| 105 | `private static bool IsFinite(double value) => …` | hàm `private static` viết gọn bằng `=>`: chỉ dùng bên trong class, không cần đối tượng | §4, §5, [C01 §8](C01_FIRST_PROGRAM.md#8-hàm-method) |

Trả lời được ba câu này là bạn đã đọc *hiểu*, không chỉ đọc *thấy*:
1. Vì sao `GridSpec` không có `set` nào?
2. Vì sao file này **không** có kiểu nào của AutoCAD (`Point3d`, `ObjectId`)? (Đọc dòng 19–21.)
3. Vì sao `Validate()` trả về **danh sách** mà không ném lỗi ở lỗi đầu tiên? (Đọc dòng 68–70.)

🧪 **Tiếp theo, tự đọc** [`GridPlanner.cs`](../../Samples/GridMarker/GridPlanner.cs). Bạn đã biết gần hết; phần còn lại (`throw`, `ArgumentException`) nằm ở [C04 §7](C04_CSHARP_IN_THIS_PROJECT.md#7-exception-báo-lỗi-đúng-cách).

---

## 10. Bài tập

**E1.** Mở rộng `PanelSpec` ở §3: thêm hằng `MaxHeight = 2400`, luật "Chiều cao phải ≤ 2400.", và property `IsTall` (đúng khi cao hơn 2000). Kiểm với `new PanelSpec("MSB-02", 800, 2600)`.

<details><summary>Đáp án E1</summary>

```csharp
var tall = new PanelSpec("MSB-02", 800, 2600);
Console.WriteLine(tall.IsTall);                     // True
foreach (string e in tall.Validate()) Console.WriteLine(e);   // Chiều cao phải ≤ 2400.

public sealed class PanelSpec
{
    public const double MinWidth = 300;
    public const double MaxHeight = 2400;

    public PanelSpec(string name, double width, double height = 2000)
    {
        Name = name ?? string.Empty;
        Width = width;
        Height = height;
    }

    public string Name { get; }
    public double Width { get; }
    public double Height { get; }
    public bool IsTall => Height > 2000;

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Name)) errors.Add("Tên tủ không được để trống.");
        if (Width < MinWidth) errors.Add($"Chiều rộng phải ≥ {MinWidth}.");
        if (Height <= 0) errors.Add("Chiều cao phải > 0.");
        if (Height > MaxHeight) errors.Add($"Chiều cao phải ≤ {MaxHeight}.");
        return errors;
    }
}
```
`IsTall` là property tính toán: không lưu gì thêm, luôn đúng với `Height` hiện tại.
</details>

**E2.** Thêm class `Triangle : Shape` (đáy, chiều cao) vào ví dụ §7 và đưa nó vào mảng `shapes`. Trước khi viết `Area()`, build thử một lần để thấy lỗi CS0534.

<details><summary>Đáp án E2</summary>

```csharp
public sealed class Triangle : Shape
{
    public Triangle(double baseLength, double height) : base("Tam giác")
    {
        BaseLength = baseLength;
        Height = height;
    }

    public double BaseLength { get; }
    public double Height { get; }
    public override double Area() => 0.5 * BaseLength * Height;
}
```
Với `new Triangle(3, 4)` thêm vào mảng, dòng in thêm là `Tam giác: diện tích 6.0`. Vòng `foreach` **không cần sửa**: nó chỉ biết `Shape`, nên chạy được với mọi class con — đó là lợi ích của kế thừa (và của interface).
</details>

**E3.** Viết interface `IPanelRepository` (`void Add(string name)`, `IReadOnlyList<string> GetAll()`), một bản `InMemoryPanelRepository` dùng `List<string>`, và class `PanelReport` **nhận** `IPanelRepository` qua constructor, có hàm `Summary()` trả về `"2 tủ: MSB-01, DB-02"`.

<details><summary>Đáp án E3</summary>

```csharp
IPanelRepository repo = new InMemoryPanelRepository();
var report = new PanelReport(repo);
repo.Add("MSB-01");
repo.Add("DB-02");
Console.WriteLine(report.Summary());        // 2 tủ: MSB-01, DB-02

public interface IPanelRepository
{
    void Add(string name);
    IReadOnlyList<string> GetAll();
}

public sealed class InMemoryPanelRepository : IPanelRepository
{
    private readonly List<string> _names = new List<string>();
    public void Add(string name) => _names.Add(name);
    public IReadOnlyList<string> GetAll() => _names;
}

public sealed class PanelReport
{
    private readonly IPanelRepository _repository;
    public PanelReport(IPanelRepository repository) { _repository = repository; }
    public string Summary()
    {
        var names = _repository.GetAll();
        return $"{names.Count} tủ: {string.Join(", ", names)}";
    }
}
```
Sau này đổi sang lưu tủ vào bản vẽ (XRecord) hay file JSON, chỉ cần viết một class mới thực hiện `IPanelRepository` — `PanelReport` không phải sửa dòng nào. Câu hỏi thêm: constructor của `PanelReport` còn thiếu gì so với `GridMarkerService`? (Gợi ý: `?? throw`.)
</details>

---

## ✅ Tự kiểm tra C03

1. Class và object khác nhau thế nào? Lấy ví dụ bằng `GridSpec`.
2. `public int CellCount => Rows * Columns;` khác `public int CellCount { get; }` ở điểm nào?
3. Vì sao `GridPlanner` là `static class`, còn `GridMarkerService` thì không?
4. Một class có thể kế thừa bao nhiêu class? Thực hiện bao nhiêu interface?
5. Nếu `GridMarkerService` tự viết `_cadContext = new CadContext();` trong constructor, điều gì hỏng?

<details><summary>Gợi ý</summary>

1. `GridSpec` là bản thiết kế; `new GridSpec(3, 4)` và `new GridSpec(10, 10)` là hai object khác nhau làm theo cùng thiết kế.
2. Dạng `=>` tính lại mỗi lần đọc, luôn khớp `Rows`/`Columns`; dạng `{ get; }` lưu một giá trị gán trong constructor.
3. `GridPlanner` chỉ tính từ tham số, không có trạng thái, không phụ thuộc gì. `GridMarkerService` cần các phụ thuộc (`ICadContext`, logger…) nhận qua constructor, nên phải là object.
4. Một class; nhiều interface (ví dụ `ViewModelBase : ObservableObject, IBusyIndicator, IDisposable`).
5. Service bị gắn chặt vào AutoCAD thật: unit test không thay được bằng `FakeCadContext`, nên không test được nếu không mở AutoCAD.
</details>

---

**Tiếp theo:** [C04 — C# mà dự án dùng hằng ngày →](C04_CSHARP_IN_THIS_PROJECT.md)
