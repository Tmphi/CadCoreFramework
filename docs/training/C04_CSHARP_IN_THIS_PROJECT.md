> **🎓 Học viện CadCoreFramework** · [Lộ trình](README.md) · [← C03](C03_OBJECT_ORIENTED.md) · **C04** · [C05 →](C05_ASYNC_TESTING_GIT.md)

# C04 — C# Mà Dự Án Dùng Hằng Ngày

> **Cấp độ:** 🌰 Nhập môn · **Cần trước:** [C03](C03_OBJECT_ORIENTED.md) · **Thời lượng:** tuần 4–5
>
> **⏱ Đọc:** ≈ 55 phút · **Thực hành:** [bài tập §13](#13-bài-tập) · **Nhãn mục:** mọi mục đều 🎯, vì Giai đoạn 0 đã lọc theo [quy tắc 20/80](C00_START_HERE.md#1-quy-tắc-2080-học-đúng-phần-dự-án-thật-sự-dùng) ([README §5](README.md#5-quy-ước-đọc))

Bạn đã biết biến, vòng lặp, class, interface. Module này dạy phần còn lại của bảng 20/80 — những thứ khiến code dự án trông "lạ" với người mới: `scope => …`, `.Where(…).ToList()`, `this ICadScope`, `<T>`, `using (…)`, `+=`, `[CommandMethod]`. Học xong, bạn **giải mã được từng ký tự** của các dòng code thật ở [§12](#12-giải-mã-dòng-code-thật).

**Sau module này bạn phải làm được:**
1. Giải thích `_cadContext.TryExecute(scope => DrawCells(scope, spec, cells), undoMarkName: UndoMarkName)` — kể cả **khi nào** lambda chạy.
2. Viết một extension method, một hàm generic, và một class phát sự kiện.
3. Chỉ ra vì sao `RibbonService` phải gọi `.ToList()` trước khi xóa tab.

> [!WARNING]
> **Dự án viết bằng C# 10** (`<LangVersion>10.0</LangVersion>` trong `CadCoreFramework.csproj`) vì AutoCAD 2024 chạy trên .NET Framework 4.8. Console luyện tập (.NET 8) mặc định dùng C# 12, nên có cú pháp chạy được ở console mà **không build** trong dự án — hay gặp nhất là collection expression `int[] widths = [600, 800];` và primary constructor `class Panel(string name)`:
>
> ```text
> error CS8936: Feature 'collection expressions' is not available in C# 10.0. Please use language version 12.0 or greater.
> ```
>
> Mọi ví dụ trong C01–C05 chỉ dùng cú pháp C# 10. Muốn luyện đúng như dự án, thêm `<LangVersion>10.0</LangVersion>` vào file `.csproj` của bài tập.

---

## Mục lục

1. [Delegate: hàm là một giá trị](#1-delegate-hàm-là-một-giá-trị)
2. [Lambda và closure](#2-lambda-và-closure)
3. [LINQ: hỏi đáp trên danh sách](#3-linq-hỏi-đáp-trên-danh-sách)
4. [LINQ chạy lúc nào: thực thi trì hoãn](#4-linq-chạy-lúc-nào-thực-thi-trì-hoãn)
5. [Extension method: thêm hàm cho kiểu có sẵn](#5-extension-method-thêm-hàm-cho-kiểu-có-sẵn)
6. [Generic: một code cho nhiều kiểu](#6-generic-một-code-cho-nhiều-kiểu)
7. [Exception: báo lỗi đúng cách](#7-exception-báo-lỗi-đúng-cách)
8. [using và IDisposable: dọn dẹp chắc chắn](#8-using-và-idisposable-dọn-dẹp-chắc-chắn)
9. [Event: báo tin cho người đăng ký](#9-event-báo-tin-cho-người-đăng-ký)
10. [Attribute: nhãn cho công cụ đọc](#10-attribute-nhãn-cho-công-cụ-đọc)
11. [Cú pháp nhỏ hay gặp: is, nameof, tuple](#11-cú-pháp-nhỏ-hay-gặp-is-nameof-tuple)
12. [Giải mã dòng code thật](#12-giải-mã-dòng-code-thật)
13. [Bài tập](#13-bài-tập)

---

## 1. Delegate: hàm là một giá trị

Trong C#, một **hàm** có thể được cất vào biến và truyền đi như một con số. Kiểu của biến đó gọi là **delegate**. Hai delegate dùng nhiều nhất là `Action` và `Func`:

<!-- doc-check: run -->
```csharp
Action sayHi = () => Console.WriteLine("Xin chào");          // Action: hàm KHÔNG trả về gì
Action<string> log = msg => Console.WriteLine($"[LOG] {msg}");
Func<int, int, int> add = (a, b) => a + b;                   // Func<vào, vào, RA>: kiểu cuối là kết quả
Func<double, bool> isPositive = x => x > 0;

sayHi();                              // Xin chào
log("Bắt đầu vẽ");                    // [LOG] Bắt đầu vẽ
Console.WriteLine(add(2, 3));         // 5
Console.WriteLine(isPositive(-1));    // False
```

| Delegate | Nhận | Trả về | Ví dụ thật |
| :--- | :--- | :--- | :--- |
| `Action` | không | không | `ExecuteCommand(Action action, …)` — [`CadCommandBase`](../../Commands/CadCommandBase.cs) |
| `Action<T>` | một `T` | không | `Execute(Action<ICadScope> cadAction, …)` — [`ICadContext`](../../Core/ICadContext.cs) |
| `Func<TResult>` | không | `TResult` | `Func<DateTimeOffset> clock` — [`LoggerService`](../../Core/LoggerService.cs) |
| `Func<T, TResult>` | một `T` | `TResult` | `TryExecute<T>(Func<ICadScope, T> cadFunc, …)` — `ICadContext` |

Delegate mạnh nhất khi làm **tham số**: hàm nhận một *hành động* từ người gọi và tự quyết định **lúc nào** chạy nó:

<!-- doc-check: run output -->
```csharp
RunInTransaction(scope => Console.WriteLine($"  vẽ bằng {scope}"));
RunInTransaction(scope =>
{
    Console.WriteLine($"  vẽ đường thẳng bằng {scope}");
    Console.WriteLine($"  vẽ đường tròn bằng {scope}");
});

void RunInTransaction(Action<string> work)    // nhận một HÀNH ĐỘNG làm tham số
{
    Console.WriteLine("Mở transaction");
    work("scope#1");                           // chạy hành động người gọi đưa vào
    Console.WriteLine("Commit");
}
```

```text
Mở transaction
  vẽ bằng scope#1
Commit
Mở transaction
  vẽ đường thẳng bằng scope#1
  vẽ đường tròn bằng scope#1
Commit
```

💡 **20/80:** đây **chính là** cách `ICadContext.Execute` làm việc — framework mở lock và transaction, gọi lambda của bạn với một `ICadScope`, rồi commit. Bạn chỉ viết phần "vẽ gì", phần "mở/đóng" không bao giờ bị quên. Code thật ở [§8](#8-using-và-idisposable-dọn-dẹp-chắc-chắn).

---

## 2. Lambda và closure

**Lambda** là cách viết một hàm ngắn **tại chỗ**, không cần đặt tên. Dấu `=>` đọc là "đi tới":

| Lambda | Nghĩa |
| :--- | :--- |
| `() => Refresh()` | không tham số |
| `x => x > 0` | một tham số (không cần ngoặc) |
| `(a, b) => a + b` | nhiều tham số |
| `scope => { …; …; }` | thân nhiều câu lệnh, đặt trong `{ }` |
| `_ => RefreshCount()` | có tham số nhưng **không dùng** — đặt tên `_` (*discard*) |

Lambda dùng được biến **bên ngoài** nó — gọi là **closure** (bao đóng). Nó "bắt" chính **biến**, không phải bản sao giá trị lúc tạo:

<!-- doc-check: run -->
```csharp
int count = 0;
Action increment = () => count++;   // lambda "bắt" (capture) biến count ở bên ngoài
increment();
increment();
Console.WriteLine(count);           // 2

var actions = new List<Action>();
for (int i = 0; i < 3; i++)
    actions.Add(() => Console.Write($"{i} "));
foreach (Action a in actions) a();  // 3 3 3   ← cả ba lambda cùng bắt MỘT biến i (lúc chạy i đã là 3)
Console.WriteLine();

var fixedActions = new List<Action>();
for (int i = 0; i < 3; i++)
{
    int copy = i;                   // mỗi vòng lặp một biến MỚI
    fixedActions.Add(() => Console.Write($"{copy} "));
}
foreach (Action a in fixedActions) a();   // 0 1 2
Console.WriteLine();
```

> ⚠️ **Bẫy:** lambda tạo trong vòng **`for`** mà chạy *sau đó* (khi bấm nút, khi transaction mở, sau `await`) sẽ thấy giá trị **cuối cùng** của biến đếm. Sao ra một biến mới trong thân vòng lặp như `copy`. Với `foreach` thì không bị: mỗi vòng `foreach` đã có biến riêng.

🔎 Closure có mặt khắp dự án. Trong [`GridMarkerService.Draw`](../../Samples/GridMarker/GridMarkerService.cs), lambda `scope => DrawCells(scope, spec, cells)` bắt `spec` và `cells` từ tham số của `Draw`, để khi framework gọi nó *bên trong* transaction thì vẫn biết cần vẽ gì.

---

## 3. LINQ: hỏi đáp trên danh sách

**LINQ** là bộ hàm "hỏi" một danh sách: lọc, biến đổi, đếm, tìm… Mỗi câu hỏi nhận một lambda:

<!-- doc-check: run -->
```csharp
var layers = new List<string> { "0", "E-POWER", "A-WALL", "E-LIGHT", "Defpoints" };

List<string> electrical = layers.Where(l => l.StartsWith("E-")).ToList();      // LỌC
Console.WriteLine(string.Join(", ", electrical));                              // E-POWER, E-LIGHT

int[] lengths = layers.Select(l => l.Length).ToArray();                       // BIẾN ĐỔI
Console.WriteLine(string.Join(", ", lengths));                                 // 1, 7, 6, 7, 9

Console.WriteLine(layers.Any(l => l == "Defpoints"));                          // True
Console.WriteLine(layers.Count(l => l.StartsWith("E-")));                      // 2
Console.WriteLine(lengths.Max());                                              // 9
Console.WriteLine(layers.FirstOrDefault(l => l.StartsWith("X")) ?? "(không có)"); // (không có)
Console.WriteLine(string.Join(", ", layers.OrderBy(l => l.Length)));          // 0, A-WALL, E-POWER, E-LIGHT, Defpoints
```

Các hàm LINQ dự án dùng nhiều nhất (số dòng có dùng, ngoài test):

| Hàm | Làm gì | Số dòng |
| :--- | :--- | :---: |
| `Select(x => …)` | biến **mỗi** phần tử thành một thứ khác | 24 |
| `Contains(x)` | có chứa `x` không | 21 |
| `ToList()` / `ToArray()` | chụp kết quả vào `List` / mảng mới | 19 / 10 |
| `Min(…)` / `Max(…)` | nhỏ nhất / lớn nhất | 15 / 9 |
| `FirstOrDefault(x => …)` | phần tử **đầu tiên** thỏa điều kiện, hoặc `null` (với số là `0`) nếu không có | 8 |
| `Where(x => …)` | giữ các phần tử thỏa điều kiện | 6 |
| `OrderBy(x => …)` | sắp xếp tăng dần theo khóa | 4 |
| `Any(x => …)` / `All(x => …)` / `Count(x => …)` | có phần tử nào / mọi phần tử / bao nhiêu phần tử thỏa | — |

> ⚠️ **Bẫy:** console của bạn bật *ImplicitUsings* nên tự có `using System.Linq;`. **Dự án không bật** — file nào dùng LINQ phải tự viết `using System.Linq;` ở đầu, không thì `Where`, `Select`… báo lỗi CS1061 như [§5](#5-extension-method-thêm-hàm-cho-kiểu-có-sẵn).

🔎 LINQ thật: [`ThemeManager.Attach`](../../Themes/ThemeManager.cs) kiểm `root.Resources.MergedDictionaries.All(d => d.Source != styles)` trước khi thêm styles; test [`GridPlannerTests`](../../CadCoreFramework.Tests/Samples/GridMarker/GridPlannerTests.cs) viết `cells.Select(c => (c.Row, c.Column)).Should().OnlyHaveUniqueItems()` — biến mỗi ô thành cặp (hàng, cột) rồi kiểm không có cặp nào trùng.

---

## 4. LINQ chạy lúc nào: thực thi trì hoãn

`Where`, `Select`, `OrderBy` **không chạy ngay**. Chúng chỉ tạo ra một *công thức*; công thức chạy khi có người duyệt kết quả (`foreach`, `Count()`, `ToList()`…). Điều này sinh ra một cái bẫy thật của dự án:

<!-- doc-check: run output -->
```csharp
var numbers = new List<int> { 1, 2, 3 };
IEnumerable<int> bigOnes = numbers.Where(n => n > 1);   // CHƯA chạy — mới là "công thức"
numbers.Add(10);
Console.WriteLine(bigOnes.Count());                      // 3   ← chạy LÚC NÀY nên thấy cả số 10

var tabs = new List<string> { "HOME", "CADCORE_TAB", "INSERT" };
try
{
    foreach (string t in tabs.Where(x => x == "CADCORE_TAB"))
        tabs.Remove(t);                                  // sửa danh sách khi ĐANG duyệt nó
}
catch (InvalidOperationException ex)
{
    Console.WriteLine(ex.Message);
}

foreach (string t in tabs.Where(x => x == "CADCORE_TAB").ToList())   // ToList: chụp kết quả TRƯỚC
    tabs.Remove(t);
Console.WriteLine(string.Join(", ", tabs));              // HOME, INSERT
```

```text
3
Collection was modified; enumeration operation may not execute.
HOME, INSERT
```

🔎 [`RibbonService`](../../Core/RibbonService.cs) gỡ tab của plugin đúng theo cách thứ hai:

<!-- doc-check: verbatim Core/RibbonService.cs -->
```csharp
foreach (RibbonTab tab in _ribbon.Tabs.Where(t => t.Id == tabId || t.Name == tabId).ToList())
    _ribbon.Tabs.Remove(tab);
```

Bỏ `.ToList()` thì vòng lặp đang đọc `_ribbon.Tabs` và xóa khỏi chính nó — lỗi như trên.

💡 **20/80:** cần **sửa** danh sách gốc, hoặc cần dùng kết quả **nhiều lần**, thì `.ToList()` trước.

---

## 5. Extension method: thêm hàm cho kiểu có sẵn

**Extension method** cho phép gọi một hàm *như thể* nó thuộc về một kiểu có sẵn, dù bạn không sửa được kiểu đó (như `int`, `string`, hay interface của framework). Ba điều kiện: **static class**, **static method**, và chữ **`this`** trước tham số đầu tiên:

<!-- doc-check: run -->
```csharp
Console.WriteLine(26.ToRowLetters());          // AA   ← gọi như thể int có sẵn hàm này
Console.WriteLine("  e-power ".Normalized());  // E-POWER

public static class MyExtensions                // extension method: static class + static method + "this"
{
    public static string ToRowLetters(this int index)
    {
        var sb = new System.Text.StringBuilder();
        for (int n = index + 1; n > 0; n = (n - 1) / 26)
            sb.Insert(0, (char)('A' + (n - 1) % 26));
        return sb.ToString();
    }

    public static string Normalized(this string text) => text.Trim().ToUpperInvariant();
}
```

`26.ToRowLetters()` thực ra là `MyExtensions.ToRowLetters(26)` — trình biên dịch tự đổi. LINQ ở §3 cũng là extension method (của `IEnumerable<T>`).

🔎 Dự án có 111 dòng khai báo extension method, gần hết trong thư mục `Extensions/`. Ví dụ [`CadScopeLineExtensions`](../../Extensions/CadScopeLineExtensions.cs):

<!-- doc-check: verbatim Extensions/CadScopeLineExtensions.cs -->
```csharp
public static ObjectId DrawLine(this ICadScope scope, Point3d startPoint, Point3d endPoint,
    string layer = null, short? colorIndex = null, LineWeight lineWeight = LineWeight.ByLayer,
    Action<Line> configure = null)
```

Nhờ chữ `this`, bên trong lambda của `Execute` bạn viết `scope.DrawLine(p1, p2, layer: "E-POWER")` thay vì `CadScopeLineExtensions.DrawLine(scope, p1, p2, …)`.

> ⚠️ **Bẫy:** extension method chỉ "hiện ra" khi file của bạn có `using` namespace chứa nó. Thiếu `using CadCoreFramework.Extensions;` thì `scope.DrawLine` báo lỗi y như khi namespace `Tools` chưa được `using`:
>
> ```text
> error CS1061: 'int' does not contain a definition for 'ToRowLetters' and no accessible extension method 'ToRowLetters' accepting a first argument of type 'int' could be found (are you missing a using directive or an assembly reference?)
> ```
>
> Câu cuối của thông báo đã gợi ý đúng: *are you missing a using directive?*

---

## 6. Generic: một code cho nhiều kiểu

`List<string>`, `List<int>`, `List<GridCell>` là **một** class viết một lần cho mọi kiểu. Chữ `T` trong `List<T>` là **tham số kiểu**: chỗ để người dùng điền kiểu vào. Bạn cũng viết được class và hàm generic:

<!-- doc-check: run -->
```csharp
Result<int> ok = Result<int>.Success(12);
Result<int> fail = Result<int>.Fail("Layer bị khóa");
Console.WriteLine(ok.IsSuccess ? $"Đã vẽ {ok.Value} ô" : ok.Error);       // Đã vẽ 12 ô
Console.WriteLine(fail.IsSuccess ? $"Đã vẽ {fail.Value} ô" : fail.Error); // Layer bị khóa

Console.WriteLine(FirstOr(new[] { "A1", "A2" }, "-"));    // A1
Console.WriteLine(FirstOr(new int[0], -1));               // -1  ← cùng một hàm, kiểu khác

T FirstOr<T>(IEnumerable<T> items, T fallback)              // hàm GENERIC: T là "một kiểu bất kỳ"
{
    foreach (T item in items) return item;
    return fallback;
}

public sealed class Result<T>                               // class GENERIC — giống CadResult<T>
{
    private Result(bool isSuccess, T? value, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }

    public static Result<T> Success(T value) => new Result<T>(true, value, null);
    public static Result<T> Fail(string error) => new Result<T>(false, default, error);
}
```

(`default` là "giá trị rỗng" của kiểu `T`: `0` với số, `null` với class. Constructor `private` + hai hàm `static` `Success`/`Fail` buộc người dùng tạo kết quả **đúng một trong hai** cách.)

🔎 `Result<T>` ở trên là bản rút gọn của [`CadResult<T>`](../../Core/CadResult.cs), có `public static CadResult<T> Success(T value)` và `public static CadResult<T> Fail(string message, Exception ex = null)`. `GridMarkerService.Draw` trả về `CadResult<int>`: thành công thì `Value` là số ô đã vẽ, thất bại thì `ErrorMessage` nói vì sao.

**Ràng buộc `where`** giới hạn kiểu được điền vào `T`:

| Khai báo thật | Nghĩa |
| :--- | :--- |
| `protected static T GetRequiredService<T>() where T : class` ([`CadCommandBase`](../../Commands/CadCommandBase.cs)) | `T` phải là class (hoặc interface) |
| `public abstract class CadEntityJigBase<TEntity> : EntityJig where TEntity : Entity` ([`Utils/Jigs`](../../Utils/Jigs/CadEntityJigBase.cs)) | `TEntity` phải là một `Entity` của AutoCAD (`Line`, `Circle`…) |
| `internal static ObjectId AppendNew<T>(ICadScope scope, T entity, Action<T> setup, …) where T : Entity` ([`CadScopeDrawingHelpers`](../../Extensions/CadScopeDrawingHelpers.cs)) | một hàm thêm **mọi loại** entity vào bản vẽ |

Thường bạn **không cần ghi** `<T>` khi gọi: `FirstOr(new[] { "A1" }, "-")` — trình biên dịch tự suy ra `T = string`.

---

## 7. Exception: báo lỗi đúng cách

**Exception** (ngoại lệ) là cách báo "có chuyện không làm tiếp được". `throw` ném lỗi ra; `try`/`catch` bắt lỗi lại; `finally` chạy **dù có lỗi hay không**:

<!-- doc-check: run output -->
```csharp
foreach (string input in new[] { "3", "", "abc" })
{
    try
    {
        Console.WriteLine($"Số hàng: {ParseRows(input)}");
    }
    catch (ArgumentException ex)                    // bắt ĐÚNG loại lỗi mình xử lý được
    {
        Console.WriteLine($"Lỗi nhập: {ex.Message}");
    }
    catch (FormatException)
    {
        Console.WriteLine($"'{input}' không phải là số.");
    }
    finally                                         // LUÔN chạy, dù có lỗi hay không
    {
        Console.WriteLine("  (xong một giá trị)");
    }
}

if (int.TryParse("12", out int rows))               // TryParse: không ném lỗi, trả true/false
    Console.WriteLine(rows + 1);                    // 13

int ParseRows(string text)
{
    if (string.IsNullOrWhiteSpace(text))
        throw new ArgumentException("Chưa nhập số hàng.", nameof(text));   // tự ném lỗi có thông điệp rõ
    return int.Parse(text);
}
```

```text
Số hàng: 3
  (xong một giá trị)
Lỗi nhập: Chưa nhập số hàng. (Parameter 'text')
  (xong một giá trị)
'abc' không phải là số.
  (xong một giá trị)
13
```

> ⚠️ **Khác biệt .NET:** trên .NET Framework 4.8 — môi trường của AutoCAD 2024 — cùng thông điệp đó in thành **hai dòng**: `Chưa nhập số hàng.` rồi `Parameter name: text` (đã chạy thử). Vì vậy đừng viết code hay test **so sánh nguyên văn** `ex.Message` của `ArgumentException`; kiểm bằng "có chứa" như các test của dự án.

💡 **20/80 — năm quy tắc exception của dự án:**
1. **Kiểm tra sớm, ném lỗi rõ ràng**, kèm tên tham số bằng `nameof`: [`GridPlanner.ToRowLetters`](../../Samples/GridMarker/GridPlanner.cs) mở đầu bằng `if (index < 0) throw new ArgumentOutOfRangeException(nameof(index), "Chỉ số hàng phải ≥ 0.");`.
2. **Lỗi "dự kiến được"** (người dùng nhập sai, layer bị khóa) thì **trả kết quả**, không ném: `int.TryParse`, `GridSpec.Validate()` trả danh sách lỗi, `TryExecute` trả `CadResult`.
3. **Chỉ bắt loại lỗi bạn xử lý được.** Bắt `Exception` chung chung chỉ ở **biên** — điểm vào lệnh, nút bấm — và phải ghi log. [`CadCommandBase.ExecuteCommand`](../../Commands/CadCommandBase.cs) làm đúng như vậy: `catch (Autodesk.AutoCAD.Runtime.Exception acEx)` rồi `catch (System.Exception ex)`, cả hai đều `Logger?.Error(…)`, và `finally` ghi lịch sử lệnh. Lý do: exception lọt ra khỏi lệnh có thể làm sập AutoCAD ([T01 §1](T01_AUTOCAD_NET_FOUNDATIONS.md#1-plugin-sống-ở-đâu-autocad-net-và-appdomain-dùng-chung)).
4. **Ném lại thì viết `throw;`**, không viết `throw ex;` — dạng sau xóa mất dấu vết (*stack trace*) của chỗ lỗi gốc, khiến log vô dụng.
5. **`catch … when (…)`** chỉ bắt khi điều kiện đúng: [`FileSystemService`](../../Services/FileSystemService.cs) có `catch (IOException) when (File.Exists(full)) { ReplaceFile(tmp, full); }` — chỉ xử lý trường hợp tiến trình khác vừa tạo file.

---

## 8. using và IDisposable: dọn dẹp chắc chắn

Có những đối tượng giữ **tài nguyên** phải trả lại: transaction của AutoCAD, khóa bản vẽ, file đang mở. Chúng thực hiện interface `IDisposable` (có hàm `Dispose()`). Khối `using` bảo đảm `Dispose()` **luôn** được gọi — kể cả khi có lỗi:

<!-- doc-check: run output -->
```csharp
using (var t1 = new FakeTransaction("T1"))
{
    Console.WriteLine("  làm việc…");
}                                                    // ← Dispose() tự được gọi ở đây

try
{
    using var t2 = new FakeTransaction("T2");        // dạng rút gọn: Dispose khi ra khỏi khối { }
    throw new InvalidOperationException("lỗi giữa chừng");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Bắt lỗi: {ex.Message}");
}

public sealed class FakeTransaction : IDisposable
{
    private readonly string _name;
    public FakeTransaction(string name) { _name = name; Console.WriteLine($"Mở {_name}"); }
    public void Dispose() => Console.WriteLine($"Đóng {_name} (Dispose)");
}
```

```text
Mở T1
  làm việc…
Đóng T1 (Dispose)
Mở T2
Đóng T2 (Dispose)
Bắt lỗi: lỗi giữa chừng
```

Để ý dòng thứ năm: `T2` được đóng **trước** khi vào `catch` — tài nguyên đã được trả lại ngay khi lỗi bay ra khỏi khối.

🔎 Đây là trái tim của [`CadContext`](../../Core/CadContext.cs) (rút gọn, `…` là phần lược bớt):

<!-- doc-check: skip rút gọn từ CadContext, "…" là phần lược bớt -->
```csharp
using (doc.LockDocument())
{
    …
    using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
    {
        …
        var scope = new CadScope(doc, tr, currentSpace, false);
        result = cadFunc(scope);     // ← lambda của bạn chạy ở đây (§1)
        tr.Commit();
    }
}
```

Lambda của bạn ném lỗi thì `tr.Commit()` không chạy, transaction bị `Dispose` khi chưa commit nên AutoCAD **hủy toàn bộ thay đổi**, rồi khóa bản vẽ được mở ra. Không bao giờ còn lại nửa bản vẽ hay một bản vẽ bị khóa vĩnh viễn. Bạn gần như **không bao giờ tự viết** hai khối `using` này, vì `ICadContext` viết hộ — nhưng phải hiểu chúng để đọc [T01 §4–5](T01_AUTOCAD_NET_FOUNDATIONS.md#4-transaction-trái-tim-của-mọi-thao-tác).

`IDisposable` cũng dùng cho việc **hủy đăng ký**: `EventAggregator.Subscribe<TMessage>(…)` trả về một `IDisposable`; [`ViewModelBase`](../../ViewModels/ViewModelBase.cs) gom chúng bằng `TrackSubscription(…)` và `Dispose()` hết khi palette đóng.

---

## 9. Event: báo tin cho người đăng ký

**Event** (sự kiện) cho một object **báo tin** cho những ai quan tâm mà không cần biết họ là ai. Bên quan tâm **đăng ký** bằng `+=` và **gỡ đăng ký** bằng `-=`:

<!-- doc-check: run -->
```csharp
var theme = new ThemeSource();
EventHandler<bool> onChanged = (sender, isDark) =>
    Console.WriteLine($"Palette đổi sang theme {(isDark ? "tối" : "sáng")}");

theme.Changed += onChanged;     // ĐĂNG KÝ nghe sự kiện
theme.Apply(true);              // Palette đổi sang theme tối
theme.Changed -= onChanged;     // GỠ đăng ký — phải dùng ĐÚNG delegate đã đăng ký
theme.Apply(false);             // (không in gì nữa)
Console.WriteLine("Xong");

public sealed class ThemeSource
{
    public event EventHandler<bool>? Changed;                          // khai báo sự kiện
    public void Apply(bool isDark) => Changed?.Invoke(this, isDark);   // phát: ?. vì có thể chưa ai nghe
}
```

> ⚠️ **Bẫy:** `-=` với một lambda **viết lại** không gỡ được gì — hai lambda giống hệt nhau vẫn là hai delegate khác nhau:
>
> ```csharp
> theme.Changed += (sender, isDark) => Console.WriteLine("Đã nhận tin");
> theme.Changed -= (sender, isDark) => Console.WriteLine("Đã nhận tin");   // ⚠️ lambda MỚI — không gỡ được gì
> theme.Apply(true);                                                      // Đã nhận tin  ← vẫn còn đăng ký!
> ```
>
> Muốn gỡ được thì cất delegate vào biến (`onChanged` ở trên). Quên gỡ thì đối tượng đăng ký **không bao giờ được giải phóng** (rò rỉ bộ nhớ) và handler vẫn chạy cho một palette đã đóng.

🔎 [`ThemeManager.Attach`](../../Themes/ThemeManager.cs) làm đúng như vậy cho mỗi palette:

<!-- doc-check: verbatim Themes/ThemeManager.cs -->
```csharp
EventHandler<bool> onThemeChanged = (_, __) => RunOnUiThread(root, () => ApplyThemeTo(root.Resources));
root.Loaded += (_, __) =>
{
    ThemeChanged -= onThemeChanged;   // Loaded có thể bắn nhiều lần (ẩn/hiện palette)
    ThemeChanged += onThemeChanged;
    ApplyThemeTo(root.Resources);     // theme có thể đã đổi trong lúc palette bị ẩn
};
root.Unloaded += (_, __) => ThemeChanged -= onThemeChanged;
```

Sự kiện của chính AutoCAD dùng cùng cú pháp: `Application.SystemVariableChanged += OnSystemVariableChanged;` (ThemeManager), `doc.CommandEnded += OnCommandEnded;` ([`RibbonService`](../../Core/RibbonService.cs)). Dự án có 34 dòng đăng ký/gỡ sự kiện; [`RibbonService`](../../Core/RibbonService.cs) còn ghi thẳng bài học vào comment: `// Lưu delegate reference thay vì dùng lambda anonymous — cho phép -= khi cleanup.` Quy tắc đăng ký/gỡ sự kiện của AutoCAD ở [T01 §10](T01_AUTOCAD_NET_FOUNDATIONS.md#10-events-sự-kiện).

---

## 10. Attribute: nhãn cho công cụ đọc

**Attribute** là cái nhãn trong ngoặc vuông `[…]` gắn lên class, method, property hay tham số. Bản thân nó không chạy gì; **một công cụ khác** đọc nhãn rồi hành động:

| Attribute thật | Ai đọc nó | Để làm gì |
| :--- | :--- | :--- |
| `[CommandMethod("SAMPLE_GRID")]` ([`GridMarkerCommands`](../../Samples/GridMarker/GridMarkerCommands.cs)) | AutoCAD, khi `NETLOAD` | đăng ký method thành lệnh gõ được trên dòng lệnh ([T01 §6](T01_AUTOCAD_NET_FOUNDATIONS.md#6-lệnh-command-đăng-ký-instance-commandflags)) |
| `[Fact]`, `[Theory]`, `[InlineData(…)]` | xUnit | đánh dấu một unit test ([C05 §2](C05_ASYNC_TESTING_GIT.md#2-unit-test-với-xunit)) |
| `[ObservableProperty] private int _rows = 3;` ([`GridMarkerViewModel`](../../Samples/GridMarker/GridMarkerViewModel.cs)) | *source generator* của CommunityToolkit.Mvvm, lúc build | **sinh code** property `Rows` biết báo cho giao diện khi giá trị đổi |
| `[RelayCommand(CanExecute = nameof(CanDraw))]` trên `private void Draw()` | cùng source generator | sinh `DrawCommand` — nút "▶ Vẽ lưới" của palette bind vào `{Binding DrawCommand}` |
| `[CallerMemberName] string callerName = null` (`ExecuteCommand`) | trình biên dịch | tự điền **tên method đang gọi** vào tham số |
| `[assembly: InternalsVisibleTo("CadCoreFramework.Tests")]` ([`AssemblyInfo.cs`](../../Properties/AssemblyInfo.cs)) | trình biên dịch | cho project test dùng được thành viên `internal` |

Vì source generator **viết thêm code** vào class, class đó phải khai báo `partial` ("class này có nhiều phần"): `public partial class GridMarkerViewModel : ViewModelBase`. Phần bạn viết có `_rows`; phần máy sinh ra có `Rows` và `DrawCommand`.

---

## 11. Cú pháp nhỏ hay gặp: is, nameof, tuple

<!-- doc-check: run output -->
```csharp
object value = 42;
if (value is int number && number > 10)      // is: kiểm tra kiểu VÀ lấy luôn giá trị
    Console.WriteLine($"Số lớn: {number}");  // Số lớn: 42

object content = "không phải ViewModel";
if (content is IDisposable disposable)       // giống PaletteService: DataContext is IDisposable vm
    disposable.Dispose();
else
    Console.WriteLine("Không cần Dispose");

var cells = new List<(int Row, int Column)> { (0, 0), (0, 1), (1, 0) };   // tuple: gói vài giá trị nhỏ
foreach (var (row, column) in cells)
    Console.Write($"({row},{column}) ");
Console.WriteLine();
Console.WriteLine(nameof(cells));            // cells  ← tên biến thành chuỗi (dùng trong thông báo lỗi)
```

```text
Số lớn: 42
Không cần Dispose
(0,0) (0,1) (1,0)
cells
```

| Cú pháp | Trong dự án |
| :--- | :--- |
| `x is Kiểu tên` (31 dòng) | [`PaletteService`](../../Core/PaletteService.cs): `if (kvp.Value?.DataContext is IDisposable vm)` — chỉ `Dispose` khi ViewModel có thực hiện `IDisposable` |
| `nameof(x)` (81 dòng) | `throw new ArgumentNullException(nameof(cadContext))`; `CanExecute = nameof(CanDraw)`. Đổi tên biến bằng công cụ *Rename* thì `nameof` đổi theo — chuỗi gõ tay thì không |
| tuple `(a, b)` | test: `cells.Select(c => (c.Row, c.Column))` |

---

## 12. Giải mã dòng code thật

Đây là bài kiểm tra của C04. Với mỗi dòng (đều chép **nguyên văn** từ dự án), che phần giải thích lại và tự nói nghĩa của **từng mảnh**.

**① [`GridMarkerService.cs`](../../Samples/GridMarker/GridMarkerService.cs)** — vẽ lưới:

<!-- doc-check: verbatim Samples/GridMarker/GridMarkerService.cs -->
```csharp
CadResult<int> result = _cadContext.TryExecute(
    scope => DrawCells(scope, spec, cells),
    undoMarkName: UndoMarkName);
```

<details><summary>Giải mã ①</summary>

- `CadResult<int>` — kết quả generic: thành công kèm số ô, hoặc thất bại kèm thông điệp (§6).
- `_cadContext` — field `private readonly` nhận qua constructor (C03 §8).
- `TryExecute(…)` — dạng `TryExecute<T>(Func<ICadScope, T> cadFunc, …)`: nhận một hàm "scope vào → giá trị ra" (§1). Không viết `<int>` vì trình biên dịch suy ra từ kiểu trả về của `DrawCells` (§6).
- `scope => DrawCells(scope, spec, cells)` — lambda, bắt `spec`, `cells` từ bên ngoài (§2). Nó **không chạy ở dòng này**: `TryExecute` gọi nó *sau khi* đã khóa bản vẽ và mở transaction (§8).
- `undoMarkName: UndoMarkName` — tham số đặt tên (C01 §8); `UndoMarkName` là `const` = `"Grid Marker"` (C02 §8), nên một lần Ctrl+Z xóa cả lưới.
</details>

**② [`SelectionFilterBuilder.cs`](../../Utils/SelectionFilterBuilder.cs)** — lọc chính xác theo tên layer:

<!-- doc-check: verbatim Utils/SelectionFilterBuilder.cs -->
```csharp
public SelectionFilterBuilder OnLayersExact(params string[] layerNames)
    => OnLayers(layerNames?.Select(EscapeWildcards).ToArray());
```

<details><summary>Giải mã ②</summary>

- `params string[] layerNames` — gọi bằng `OnLayersExact("E-POWER", "E-LIGHT")` (C02 §2).
- `=> …` — thân hàm một biểu thức (C01 §8).
- `layerNames?.Select(…)` — nếu `layerNames` là null thì cả biểu thức là null, không ném lỗi (C02 §6).
- `.Select(EscapeWildcards)` — LINQ biến đổi từng tên (§3); viết **tên hàm** thay cho lambda `n => EscapeWildcards(n)`, cùng nghĩa.
- `.ToArray()` — chụp kết quả thành mảng (§4).
- Kiểu trả về `SelectionFilterBuilder` — trả lại chính builder để gọi nối tiếp `.OnLayersExact(…).XYZ(…)` (kiểu "fluent").
</details>

**③ [`RibbonService.cs`](../../Core/RibbonService.cs)** — gỡ tab của plugin:

<!-- doc-check: verbatim Core/RibbonService.cs -->
```csharp
foreach (RibbonTab tab in _ribbon.Tabs.Where(t => t.Id == tabId || t.Name == tabId).ToList())
    _ribbon.Tabs.Remove(tab);
```

<details><summary>Giải mã ③</summary>

- `Where(t => t.Id == tabId || t.Name == tabId)` — lọc tab trùng Id **hoặc** trùng tên (§3, C01 §6).
- `.ToList()` — **bắt buộc**: chụp danh sách trước, vì thân vòng lặp xóa khỏi chính `_ribbon.Tabs` (§4).
</details>

**④ [`GridMarkerViewModel.cs`](../../Samples/GridMarker/GridMarkerViewModel.cs)** — tự cập nhật số ô khi có lưới mới được vẽ:

<!-- doc-check: verbatim Samples/GridMarker/GridMarkerViewModel.cs -->
```csharp
TrackSubscription(events.Subscribe<GridDrawnMessage>(_ => RefreshCount()));
```

<details><summary>Giải mã ④</summary>

- `events.Subscribe<GridDrawnMessage>(…)` — hàm generic: "đăng ký nhận tin loại `GridDrawnMessage`" (§6, §9).
- `_ => RefreshCount()` — lambda nhận tin nhắn nhưng không dùng nó, nên đặt tên `_` (§2).
- `Subscribe` trả về `IDisposable`; `TrackSubscription` cất lại để `Dispose()` (hủy đăng ký) khi palette đóng (§8).
</details>

**⑤ [`GridMarkerPalette.xaml.cs`](../../Samples/GridMarker/GridMarkerPalette.xaml.cs)** — gắn ViewModel cho palette:

<!-- doc-check: verbatim Samples/GridMarker/GridMarkerPalette.xaml.cs -->
```csharp
DataContext = AppBootstrapper.ServiceProvider?.GetService<GridMarkerViewModel>()
    ?? throw new InvalidOperationException(
        "GridMarkerViewModel chưa được đăng ký trong AppBootstrapper.ConfigureServices.");
```

<details><summary>Giải mã ⑤</summary>

- `AppBootstrapper.ServiceProvider` — property `static` (C03 §4): DI container của plugin (C03 §8).
- `?.GetService<GridMarkerViewModel>()` — nếu container chưa có (null) thì cả vế là null (C02 §6); `GetService<T>` là hàm generic (§6).
- `?? throw new InvalidOperationException(…)` — null thì báo lỗi **ngay** với thông điệp chỉ đúng chỗ cần sửa, thay vì hiện một palette "chết" (C02 §6, §7).
</details>

**⑥ [`CadScopeLineExtensions.cs`](../../Extensions/CadScopeLineExtensions.cs)** — khai báo hàm vẽ đường thẳng:

<!-- doc-check: verbatim Extensions/CadScopeLineExtensions.cs -->
```csharp
public static ObjectId DrawLine(this ICadScope scope, Point3d startPoint, Point3d endPoint,
    string layer = null, short? colorIndex = null, LineWeight lineWeight = LineWeight.ByLayer,
    Action<Line> configure = null)
```

<details><summary>Giải mã ⑥</summary>

- `static` + `this ICadScope scope` — extension method: gọi là `scope.DrawLine(…)` (§5).
- `ObjectId` — struct định danh đối tượng vừa vẽ (C02 §1).
- `Point3d startPoint` — struct chỉ đọc; hàm không thể làm đổi điểm của bạn (C02 §1).
- `string layer = null` — không truyền thì vẽ trên layer hiện hành (C02 §6).
- `short? colorIndex = null` — số có thể null: "không chỉ định màu" (C02 §6).
- `LineWeight lineWeight = LineWeight.ByLayer` — enum của AutoCAD, có giá trị mặc định (C02 §7).
- `Action<Line> configure = null` — tùy chọn: một hành động nhận `Line` vừa tạo để chỉnh thêm, ví dụ `configure: line => line.LinetypeScale = 2.0` (§1, C02 §1).
</details>

🧪 **Tự giải mã thêm** (không có đáp án — mang câu trả lời đến buổi gặp mentor): hai dòng liền nhau trong `GridMarkerViewModel.cs`:

<!-- doc-check: verbatim Samples/GridMarker/GridMarkerViewModel.cs -->
```csharp
private bool CanDraw() => !HasValidationErrors && !IsBusy;

[RelayCommand(CanExecute = nameof(CanDraw))]
```

---

## 13. Bài tập

**E1 — LINQ.** Cho `layers = { "0", "E-POWER", "A-WALL", "E-LIGHT", "Defpoints", "E-DATA", "A-DOOR" }`. Viết **mỗi câu một dòng**: (1) tên các layer bắt đầu bằng `"A-"`, xếp theo bảng chữ cái; (2) số layer có tên dài hơn 5 ký tự; (3) có layer nào chứa chữ `"LIGHT"` không; (4) danh sách tên viết thường.

<details><summary>Đáp án E1</summary>

<!-- doc-check: run output -->
```csharp
var layers = new List<string> { "0", "E-POWER", "A-WALL", "E-LIGHT", "Defpoints", "E-DATA", "A-DOOR" };

// 1. Tên các layer bắt đầu bằng "A-", xếp theo bảng chữ cái
Console.WriteLine(string.Join(", ", layers.Where(l => l.StartsWith("A-")).OrderBy(l => l)));
// 2. Số layer có tên dài hơn 5 ký tự
Console.WriteLine(layers.Count(l => l.Length > 5));
// 3. Có layer nào chứa chữ "LIGHT" không?
Console.WriteLine(layers.Any(l => l.Contains("LIGHT")));
// 4. Danh sách tên viết thường
Console.WriteLine(string.Join(", ", layers.Select(l => l.ToLowerInvariant())));
```

```text
A-DOOR, A-WALL
6
True
0, e-power, a-wall, e-light, defpoints, e-data, a-door
```
</details>

**E2 — Extension method.** Viết `bool IsValidSymbolName(this string name)`: đúng khi tên không rỗng, dài tối đa 255 ký tự, và không chứa ký tự cấm (danh sách ở C02-E1). Thử với `"E-POWER"`, `"A*B"`, `""`, `"Lưới trục"`, `"LAYER:1"`.

<details><summary>Đáp án E2</summary>

<!-- doc-check: run output -->
```csharp
foreach (string name in new[] { "E-POWER", "A*B", "", "Lưới trục", "LAYER:1" })
    Console.WriteLine($"'{name}' hợp lệ? {name.IsValidSymbolName()}");

public static class SymbolNameExtensions
{
    private static readonly char[] Invalid = { '<', '>', '/', '\\', '"', ':', ';', '?', '*', '|', ',', '=', '`' };

    public static bool IsValidSymbolName(this string name)
        => !string.IsNullOrWhiteSpace(name) && name.Length <= 255 && name.IndexOfAny(Invalid) < 0;
}
```

```text
'E-POWER' hợp lệ? True
'A*B' hợp lệ? False
'' hợp lệ? False
'Lưới trục' hợp lệ? True
'LAYER:1' hợp lệ? False
```
So với bản thật [`SymbolNameRules`](../../Utils/SymbolNameRules.cs): dự án chọn **static class thường** (`SymbolNameRules.IsValid(name)`) chứ không làm extension của `string`. Vì sao? (Gợi ý: một extension trên `string` sẽ hiện ra trong gợi ý code ở **mọi** chuỗi của mọi file có `using` namespace đó.)
</details>

**E3 — Event.** Viết class `DrawJob` có `event EventHandler<int>? Progress` và hàm `Run(int steps)` phát tiến độ phần trăm sau mỗi bước. Đăng ký, chạy `Run(4)`, gỡ đăng ký, chạy `Run(2)`.

<details><summary>Đáp án E3</summary>

<!-- doc-check: run output -->
```csharp
var job = new DrawJob();
EventHandler<int> onProgress = (sender, percent) => Console.WriteLine($"Tiến độ: {percent}%");

job.Progress += onProgress;
job.Run(4);                     // 25% 50% 75% 100%
job.Progress -= onProgress;
job.Run(2);                     // không in gì
Console.WriteLine("Xong");

public sealed class DrawJob
{
    public event EventHandler<int>? Progress;

    public void Run(int steps)
    {
        for (int i = 1; i <= steps; i++)
            Progress?.Invoke(this, i * 100 / steps);
    }
}
```

```text
Tiến độ: 25%
Tiến độ: 50%
Tiến độ: 75%
Tiến độ: 100%
Xong
```
Vì sao viết `i * 100 / steps` mà không viết `i / steps * 100`? (Gợi ý: [C01 §4](C01_FIRST_PROGRAM.md#4-biến-và-kiểu-dữ-liệu), chia số nguyên.)
</details>

---

## ✅ Tự kiểm tra C04

1. `Action<ICadScope>` và `Func<ICadScope, int>` khác nhau thế nào? Mỗi loại dùng ở hàm nào của `ICadContext`?
2. Lambda truyền vào `Execute` chạy **lúc nào**: lúc gọi `Execute`, hay lúc khác?
3. Vì sao `foreach (var t in list.Where(…)) list.Remove(t);` ném lỗi, còn thêm `.ToList()` thì không?
4. Bạn gõ `scope.DrawCircle(…)` và nhận lỗi CS1061. Kiểm tra gì trước tiên?
5. Viết `catch (Exception ex) { }` (rỗng) trong một service có vấn đề gì?
6. Vì sao `ThemeManager` cất lambda vào biến `onThemeChanged` thay vì viết thẳng lambda sau `+=`?

<details><summary>Gợi ý</summary>

1. `Action<ICadScope>` nhận scope, không trả gì — `Execute`. `Func<ICadScope, int>` nhận scope, trả về `int` — `TryExecute<T>` / `Execute<T>`.
2. Trong lúc `Execute` đang chạy, **sau khi** đã khóa bản vẽ và mở transaction, trước khi commit (§8).
3. Không có `.ToList()` thì vòng lặp vẫn đang đọc `list` trong lúc xóa khỏi nó. `.ToList()` duyệt hết trước, tạo danh sách riêng, nên xóa khỏi `list` thoải mái (§4).
4. File đã có `using CadCoreFramework.Extensions;` chưa (§5).
5. Lỗi bị "nuốt": không log, không báo, người dùng tưởng đã xong. Chỉ bắt lỗi ở biên và luôn ghi log (§7).
6. Để `-=` gỡ đúng delegate đã đăng ký; lambda viết lại là delegate khác, `-=` không gỡ được gì (§9).
</details>

---

**Tiếp theo:** [C05 — Async, unit test, Git và đồ án nhập môn →](C05_ASYNC_TESTING_GIT.md)
