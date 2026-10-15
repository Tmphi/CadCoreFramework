> **🎓 Học viện CadCoreFramework** · [Lộ trình](README.md) · [← C01](C01_FIRST_PROGRAM.md) · **C02** · [C03 →](C03_OBJECT_ORIENTED.md)

# C02 — Dữ Liệu: Kiểu, Collection Và null

> **Cấp độ:** 🌰 Nhập môn · **Cần trước:** [C01](C01_FIRST_PROGRAM.md) · **Thời lượng:** tuần 2–3
>
> **⏱ Đọc:** ≈ 30 phút · **Thực hành:** [bài tập §10](#10-bài-tập) · **Nhãn mục:** mọi mục đều 🎯, vì Giai đoạn 0 đã lọc theo [quy tắc 20/80](C00_START_HERE.md#1-quy-tắc-2080-học-đúng-phần-dự-án-thật-sự-dùng) ([README §5](README.md#5-quy-ước-đọc))

Chương trình thật xử lý **nhiều** dữ liệu cùng lúc: danh sách layer, các ô của lưới, thuộc tính của block. Module này dạy cách chứa chúng (mảng, `List`, `Dictionary`), cách trả về an toàn (`IReadOnlyList`), và cách sống chung với `null` — nguồn lỗi lúc chạy số 1 của người mới.

**Sau module này bạn phải làm được:**
1. Giải thích vì sao một hàm nhận `Point3d` không thể làm đổi điểm của bạn, còn hàm nhận `Line` thì có thể.
2. Chọn đúng giữa mảng, `List<T>` và `Dictionary<TKey, TValue>` cho một bài toán.
3. Đọc được `layer?.Length ?? 0`, `short? colorIndex = null`, và biết khi nào được phép dùng `!`.

---

## Mục lục

1. [Giá trị và tham chiếu: struct và class](#1-giá-trị-và-tham-chiếu-struct-và-class)
2. [Mảng](#2-mảng)
3. [List: danh sách co giãn](#3-list-danh-sách-co-giãn)
4. [Dictionary: tra theo khóa](#4-dictionary-tra-theo-khóa)
5. [Trả về danh sách chỉ đọc: IReadOnlyList](#5-trả-về-danh-sách-chỉ-đọc-ireadonlylist)
6. [null: giá trị không có](#6-null-giá-trị-không-có)
7. [enum: tập lựa chọn có tên](#7-enum-tập-lựa-chọn-có-tên)
8. [const và readonly](#8-const-và-readonly)
9. [Làm việc với chuỗi](#9-làm-việc-với-chuỗi)
10. [Bài tập](#10-bài-tập)

---

## 1. Giá trị và tham chiếu: struct và class

Có hai loại kiểu dữ liệu, và chúng hành xử **khác nhau khi gán**:

<!-- doc-check: run -->
```csharp
var p1 = new PointValue(1, 2);
var p2 = p1;                 // struct: SAO CHÉP toàn bộ giá trị
p2.X = 99;
Console.WriteLine(p1.X);     // 1   ← p1 không đổi

var c1 = new PointRef(1, 2);
var c2 = c1;                 // class: c2 và c1 cùng trỏ tới MỘT đối tượng
c2.X = 99;
Console.WriteLine(c1.X);     // 99  ← đổi qua c2 thì c1 cũng thấy

struct PointValue
{
    public double X;
    public double Y;
    public PointValue(double x, double y) { X = x; Y = y; }
}

class PointRef
{
    public double X;
    public double Y;
    public PointRef(double x, double y) { X = x; Y = y; }
}
```

Hình dung: biến kiểu **struct** là *tờ giấy ghi sẵn con số* — đưa cho người khác thì họ nhận một bản photo. Biến kiểu **class** là *địa chỉ một căn nhà* — đưa địa chỉ cho người khác thì hai người cùng vào **một** căn nhà.

| | struct — kiểu giá trị | class — kiểu tham chiếu |
| :--- | :--- | :--- |
| `b = a` | sao chép toàn bộ | sao chép "địa chỉ"; hai biến cùng một đối tượng |
| Truyền vào hàm | hàm nhận bản sao — **không** làm đổi được biến của bạn | hàm nhận "địa chỉ" — **sửa được** đối tượng của bạn |
| Có sẵn trong .NET | `int`, `double`, `bool`, `char` | `string`, mảng, `List<T>`, `Dictionary<,>` |
| Trong AutoCAD | `Point3d`, `Vector3d`, `ObjectId` | `Line`, `Circle`, `Transaction`, `Database` |

(Các dòng AutoCAD đã kiểm trong `acdbmgd.dll` của AutoCAD 2024.)

💡 **20/80 — hệ quả trong dự án:**
- `Point3d` là struct và **chỉ đọc**: không có `p.X = 5`. Muốn điểm khác thì tạo điểm mới: `new Point3d(p.X + 10, p.Y, p.Z)`.
- `Line` là class: khi [`DrawLine`](../../Extensions/CadScopeLineExtensions.cs) đưa cái `Line` vừa tạo cho bạn qua tham số `configure`, bạn sửa **chính** đường thẳng sẽ được vẽ (cú pháp ở [C04 §1](C04_CSHARP_IN_THIS_PROJECT.md#1-delegate-hàm-là-một-giá-trị)).
- `string` là class nhưng **không bao giờ đổi được nội dung** (bất biến), nên dùng nó như một giá trị — xem bẫy ở [§9](#9-làm-việc-với-chuỗi).

---

## 2. Mảng

**Mảng** chứa một dãy phần tử cùng kiểu, số phần tử **cố định** từ lúc tạo:

<!-- doc-check: run -->
```csharp
int[] widths = { 600, 800, 600 };          // mảng: số phần tử CỐ ĐỊNH
Console.WriteLine(widths.Length);          // 3
Console.WriteLine(widths[0]);              // 600   ← chỉ số bắt đầu từ 0
widths[1] = 900;
Console.WriteLine(widths[1]);              // 900

try
{
    Console.WriteLine(widths[3]);          // chỉ số 3 không tồn tại (0, 1, 2)
}
catch (IndexOutOfRangeException ex)
{
    Console.WriteLine(ex.Message);         // Index was outside the bounds of the array.
}
```

(`try`/`catch` bắt lỗi để chương trình không dừng — học kỹ ở [C04 §7](C04_CSHARP_IN_THIS_PROJECT.md#7-exception-báo-lỗi-đúng-cách).)

🔎 Mảng hay gặp nhất trong dự án là **tham số `params`**: [`SelectionFilterBuilder`](../../Utils/SelectionFilterBuilder.cs) có `OnLayersExact(params string[] layerNames)`, nên bạn gọi `OnLayersExact("E-POWER", "E-LIGHT")` và C# tự gom các tên thành một mảng.

---

## 3. List: danh sách co giãn

`List<T>` đọc là "list của T": `List<string>` là danh sách chuỗi, `List<GridCell>` là danh sách ô lưới. Khác mảng, nó **thêm/bớt** được:

<!-- doc-check: run -->
```csharp
var layers = new List<string> { "0", "E-POWER" };   // List: thêm/bớt thoải mái
layers.Add("A-WALL");
Console.WriteLine(layers.Count);                    // 3
Console.WriteLine(layers[1]);                       // E-POWER
Console.WriteLine(layers.Contains("A-WALL"));       // True
layers.Remove("0");
foreach (string layer in layers)
{
    Console.WriteLine(layer);                       // E-POWER, rồi A-WALL
}
```

| Việc | Mảng | `List<T>` |
| :--- | :--- | :--- |
| Số phần tử | `.Length` | `.Count` |
| Thêm / bớt | không được | `.Add(x)`, `.Remove(x)`, `.Insert(i, x)` |
| Tìm | `Array.IndexOf(a, x)` | `.Contains(x)`, `.IndexOf(x)` |

🔎 Hai dòng bạn sẽ đọc đi đọc lại: [`GridSpec.Validate`](../../Samples/GridMarker/GridSpec.cs) bắt đầu bằng `var errors = new List<string>();` rồi `errors.Add(…)` cho từng lỗi; [`GridPlanner.Plan`](../../Samples/GridMarker/GridPlanner.cs) tạo `var cells = new List<GridCell>(spec.CellCount);` — con số trong ngoặc là **sức chứa ban đầu**, giúp list khỏi phải nới rộng nhiều lần khi đã biết trước số ô.

---

## 4. Dictionary: tra theo khóa

`Dictionary<TKey, TValue>` lưu các cặp **khóa → giá trị**, tra cứu theo khóa rất nhanh — như tra từ điển theo từ:

<!-- doc-check: run -->
```csharp
var attributes = new Dictionary<string, string>    // từ điển: tra theo KHÓA
{
    ["TAG"] = "D-01",
    ["WIDTH"] = "900",
};
attributes["HEIGHT"] = "2100";                      // thêm (hoặc ghi đè) một cặp khóa–giá trị
Console.WriteLine(attributes["TAG"]);               // D-01
Console.WriteLine(attributes.Count);                // 3

if (attributes.TryGetValue("COLOR", out string? color))   // tra an toàn, không ném lỗi
    Console.WriteLine(color);
else
    Console.WriteLine("Không có COLOR");

var palettes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["Grid Marker"] = 1 };
Console.WriteLine(palettes.ContainsKey("GRID MARKER"));   // True  ← không phân biệt hoa/thường
```

> ⚠️ **Bẫy:** đọc `attributes["COLOR"]` khi khóa **không có** sẽ ném `KeyNotFoundException`. Không chắc khóa có tồn tại thì dùng `TryGetValue` — trả `true`/`false` và đưa giá trị ra qua `out`.

🔎 [`PaletteService`](../../Core/PaletteService.cs) giữ các palette đã mở trong `new Dictionary<string, PaletteSet>(StringComparer.OrdinalIgnoreCase)` và kiểm tra bằng `if (_palettes.TryGetValue(name, out PaletteSet existingPs)) return existingPs;`. `OrdinalIgnoreCase` vì tên trong AutoCAD (layer, block, palette) **không phân biệt hoa/thường**: "Grid Marker" và "GRID MARKER" phải là một.

---

## 5. Trả về danh sách chỉ đọc: IReadOnlyList

Bên trong hàm, bạn cần `List` để `Add`. Nhưng khi **trả ra ngoài**, dự án trả về một kiểu "chỉ đọc", để người gọi không vô tình sửa kết quả:

<!-- doc-check: run -->
```csharp
IReadOnlyList<string> errors = Validate(0, 200);
Console.WriteLine(errors.Count);                 // 2
foreach (string e in errors)
    Console.WriteLine(e);                        // Số hàng phải ≥ 1.  rồi  Số cột phải ≤ 100.

IReadOnlyList<string> Validate(int rows, int columns)
{
    var list = new List<string>();               // bên trong: List để Add
    if (rows < 1) list.Add("Số hàng phải ≥ 1.");
    if (columns > 100) list.Add("Số cột phải ≤ 100.");
    return list;                                 // bên ngoài: chỉ cho ĐỌC
}
```

Thử thêm `errors.Add("x");` sau dòng đầu tiên — trình biên dịch chặn ngay:

```text
error CS1061: 'IReadOnlyList<string>' does not contain a definition for 'Add' and no accessible extension method 'Add' …
```

| Kiểu | Làm được gì | Gặp ở |
| :--- | :--- | :--- |
| `IEnumerable<T>` | chỉ **duyệt** bằng `foreach` (và LINQ, [C04 §3](C04_CSHARP_IN_THIS_PROJECT.md#3-linq-hỏi-đáp-trên-danh-sách)) | tham số của hàm nhận "bất kỳ danh sách nào" |
| `IReadOnlyList<T>` | duyệt + `.Count` + lấy theo chỉ số `[i]` | `GridSpec.Validate()`, `GridPlanner.Plan(…)` |
| `List<T>` | tất cả, kể cả sửa | **bên trong** một hàm hoặc class |

(Các kiểu bắt đầu bằng chữ `I` là **interface** — [C03 §6](C03_OBJECT_ORIENTED.md#6-interface-hợp-đồng-và-bản-giả). Hiện chỉ cần biết: `List<T>` "là một" `IReadOnlyList<T>`, nên `return list;` hợp lệ.)

---

## 6. null: giá trị không có

`null` nghĩa là "biến này **không trỏ tới** đối tượng nào". Gọi một thứ trên `null` là lỗi lúc chạy hay gặp nhất: `NullReferenceException`.

<!-- doc-check: run -->
```csharp
string? layer = null;                             // dấu ? : biến này ĐƯỢC PHÉP null
Console.WriteLine(layer?.Length);                 // (dòng trống) ← ?. trả null thay vì ném lỗi
Console.WriteLine(layer?.Length ?? 0);            // 0  ← ?? : giá trị thay thế khi bên trái là null
string name = layer ?? "0";
Console.WriteLine(name);                          // 0

short? colorIndex = null;                         // kiểu số "có thể null" — như colorIndex của DrawLine
Console.WriteLine(colorIndex.HasValue);           // False
colorIndex = 1;
Console.WriteLine(colorIndex.Value);              // 1

try
{
    Console.WriteLine(layer!.Length);             // ! : "tôi chắc nó không null" — nhưng nó null…
}
catch (NullReferenceException)
{
    Console.WriteLine("NullReferenceException!");
}
```

| Cú pháp | Đọc là | Số dòng trong dự án |
| :--- | :--- | :---: |
| `string?`, `short?` | "có thể là null" | — |
| `a?.B` | "nếu `a` khác null thì lấy `B`, không thì null" | 238 |
| `a ?? b` | "`a`, nhưng nếu `a` null thì `b`" | 44 |
| `x ?? throw new …` | "nếu null thì báo lỗi ngay" | 15 — constructor của service, palette |
| `a!` | "tôi **chắc chắn** `a` không null" — trình biên dịch tin bạn, không kiểm | 2 |

🔎 Ba cách dự án dùng `null`:
- **null = "không chỉ định, dùng mặc định":** `DrawLine(…, string layer = null, short? colorIndex = null, …)` — không truyền layer thì vẽ trên layer hiện hành.
- **null = lỗi cấu hình, báo ngay:** constructor của [`GridMarkerService`](../../Samples/GridMarker/GridMarkerService.cs): `_cadContext = cadContext ?? throw new ArgumentNullException(nameof(cadContext));`.
- **`!` chỉ sau khi đã tự kiểm tra:** [`SymbolNameRules`](../../Utils/SymbolNameRules.cs) viết `!string.IsNullOrEmpty(name) && name!.IndexOfAny(InvalidChars) >= 0` — vế trái đã chắc `name` khác null.

> ⚠️ **Bẫy:** dự án đặt `<Nullable>annotations</Nullable>` trong `CadCoreFramework.csproj`: được viết `string?` để **ghi chú ý định**, nhưng trình biên dịch **không cảnh báo** khi bạn quên kiểm null (vì AutoCAD API chưa ghi chú null, bật cảnh báo sẽ ra hàng trăm cảnh báo vô ích). Chương trình console luyện tập của bạn thì *có* cảnh báo CS8602 — hãy để nó dạy bạn thói quen kiểm null trước khi vào dự án.

---

## 7. enum: tập lựa chọn có tên

`enum` đặt **tên** cho một nhóm lựa chọn cố định, thay cho các "con số ma thuật" khó hiểu:

<!-- doc-check: run -->
```csharp
GridLabelStyle style = GridLabelStyle.Sequential;
Console.WriteLine(style);            // Sequential
Console.WriteLine((int)style);       // 1   ← enum thực chất là số: LetterNumber = 0, Sequential = 1

switch (style)
{
    case GridLabelStyle.LetterNumber: Console.WriteLine("A1, A2…"); break;
    case GridLabelStyle.Sequential:   Console.WriteLine("1, 2, 3…"); break;
}

enum GridLabelStyle { LetterNumber, Sequential }
```

🔎 `GridLabelStyle` thật nằm ở đầu [`GridSpec.cs`](../../Samples/GridMarker/GridSpec.cs). Dự án chỉ tự khai báo 3 enum, nhưng bạn **dùng** enum của AutoCAD liên tục: `OpenMode.ForRead` / `OpenMode.ForWrite` ([T01 §4](T01_AUTOCAD_NET_FOUNDATIONS.md#4-transaction-trái-tim-của-mọi-thao-tác)), `LineWeight.ByLayer` (tham số mặc định của `DrawLine`), `CommandFlags.Modal`.

---

## 8. const và readonly

Cả hai đều nghĩa là "không được gán lại", nhưng khác thời điểm:

| | `const` | `readonly` |
| :--- | :--- | :--- |
| Giá trị biết khi nào | lúc **biên dịch** | lúc **chạy** — gán một lần (khi khai báo hoặc trong constructor) |
| Kiểu được phép | số, `bool`, `char`, `string` | mọi kiểu |
| Ví dụ thật | `public const int MaxDimension = 100;` ([`GridSpec`](../../Samples/GridMarker/GridSpec.cs)) | `private readonly ICadContext _cadContext;` ([`GridMarkerService`](../../Samples/GridMarker/GridMarkerService.cs)) |

<!-- doc-check: run -->
```csharp
Console.WriteLine(Rules.MaxDimension);   // 100
Console.WriteLine(Rules.Invalid[0]);     // <
Rules.Invalid[0] = 'X';                  // ⚠️ vẫn được! readonly chỉ khóa BIẾN, không khóa NỘI DUNG mảng
Console.WriteLine(Rules.Invalid[0]);     // X

static class Rules
{
    public const int MaxDimension = 100;                           // const: giá trị cố định lúc biên dịch
    public static readonly char[] Invalid = { '<', '>', '*' };     // readonly: chỉ gán một lần
}
```

> ⚠️ **Bẫy:** `readonly` không làm nội dung **bên trong** đối tượng thành chỉ đọc. Vì vậy [`SymbolNameRules`](../../Utils/SymbolNameRules.cs) khai báo mảng ký tự cấm là **`private`** `static readonly char[] InvalidChars` — bên ngoài không với tới để sửa.

💡 **20/80:** field nào chỉ gán trong constructor thì **luôn** thêm `readonly` (dự án có 56 dòng `private readonly`). Lỡ tay gán lại ở chỗ khác, trình biên dịch báo lỗi cho bạn.

---

## 9. Làm việc với chuỗi

Các thao tác chuỗi dùng nhiều nhất trong dự án:

<!-- doc-check: run -->
```csharp
string input = "  e-power  ";
string clean = input.Trim().ToUpperInvariant();     // bỏ khoảng trắng hai đầu, viết hoa
Console.WriteLine(clean);                           // E-POWER
Console.WriteLine(string.IsNullOrWhiteSpace("   ")); // True
Console.WriteLine("A*B".IndexOfAny(new[] { '*', '?' }));   // 1   ← vị trí ký tự cấm đầu tiên (-1 nếu không có)

string[] parts = "A1,A2,B1".Split(',');
Console.WriteLine(parts.Length);                    // 3
Console.WriteLine(string.Join(" | ", parts));       // A1 | A2 | B1

var sb = new System.Text.StringBuilder();           // ghép chuỗi nhiều lần: dùng StringBuilder
sb.Append("B");
sb.Insert(0, "A");
Console.WriteLine(sb.ToString());                   // AB
```

| Hàm | Làm gì | Trong dự án |
| :--- | :--- | :--- |
| `string.IsNullOrWhiteSpace(s)` | `null`, rỗng, hay toàn khoảng trắng? | 51 dòng — kiểm tra đầu vào |
| `s.Trim()` | bỏ khoảng trắng hai đầu | chuẩn hóa tên người dùng nhập |
| `s.IndexOfAny(chars)` | vị trí ký tự đầu tiên thuộc `chars`, `-1` nếu không có | `SymbolNameRules` tìm ký tự cấm |
| `string.Join(", ", list)` | nối danh sách thành một chuỗi | 9 dòng — ví dụ gộp thông báo lỗi trong `GridPlanner.Plan` |
| `StringBuilder` | ghép chuỗi nhiều lần mà không tạo chuỗi mới mỗi lần | `GridPlanner.ToRowLetters` |

Hai cái bẫy hay gặp:

<!-- doc-check: run -->
```csharp
string name = "  E-POWER ";
name.Trim();                        // ⚠️ kết quả bị bỏ đi — chuỗi KHÔNG BAO GIỜ tự đổi
Console.WriteLine($"[{name}]");     // [  E-POWER ]
name = name.Trim();                 // phải GÁN lại
Console.WriteLine($"[{name}]");     // [E-POWER]

Console.WriteLine("E-Power" == "E-POWER");                                                  // False
Console.WriteLine(string.Equals("E-Power", "E-POWER", StringComparison.OrdinalIgnoreCase)); // True
```

> ⚠️ **Bẫy:** so sánh **tên trong AutoCAD** (layer, block…) bằng `==` là sai, vì AutoCAD coi "E-Power" và "E-POWER" là một. Dùng `StringComparison.OrdinalIgnoreCase` (25 dòng trong dự án). Viết hoa để so sánh thì dùng `ToUpperInvariant()`, không dùng `ToUpper()` — bản không "Invariant" đổi theo định dạng khu vực của máy, cùng loại bẫy với [C01 §5](C01_FIRST_PROGRAM.md#5-chuỗi-và-định-dạng-số--bẫy-định-dạng-việt-nam).

---

## 10. Bài tập

Chấm tự động trong [`exercises/`](../../exercises/README.md): `dotnet test exercises/Exercises.Tests --filter FullyQualifiedName~C02.E1` (đổi `E1` theo bài). Test của E3 còn so bài làm với `GridSpec.Validate` thật.

**E1.** Cho `names = { "E-POWER", "A*B", "", "Lưới trục", "LAYER:1", "M.EQUIP" }`. Dùng `foreach`, `List<string>`, `string.IsNullOrWhiteSpace` và `IndexOfAny` để giữ lại các tên **hợp lệ** — không rỗng, không chứa ký tự cấm `` < > / \ " : ; ? * | , = ` `` (đúng danh sách trong [`SymbolNameRules`](../../Utils/SymbolNameRules.cs)). In kết quả cách nhau dấu phẩy.

<details><summary>Đáp án E1</summary>

<!-- doc-check: run output -->
```csharp
var names = new List<string> { "E-POWER", "A*B", "", "Lưới trục", "LAYER:1", "M.EQUIP" };
char[] invalid = { '<', '>', '/', '\\', '"', ':', ';', '?', '*', '|', ',', '=', '`' };

var valid = new List<string>();
foreach (string name in names)
{
    if (!string.IsNullOrWhiteSpace(name) && name.IndexOfAny(invalid) < 0)
        valid.Add(name);
}
Console.WriteLine(string.Join(", ", valid));
```

```text
E-POWER, Lưới trục, M.EQUIP
```
Chú ý `'\\'` là **một** ký tự `\` (trong `'…'` và `"…"`, dấu `\` phải gõ hai lần). Tiếng Việt, dấu cách và dấu chấm đều hợp lệ trong tên layer.
</details>

**E2.** Cho mảng layer của các entity: `{ "E-POWER", "A-WALL", "E-POWER", "0", "E-POWER", "A-WALL" }`. Dùng `Dictionary<string, int>` đếm số entity trên mỗi layer.

<details><summary>Đáp án E2</summary>

<!-- doc-check: run output -->
```csharp
string[] entityLayers = { "E-POWER", "A-WALL", "E-POWER", "0", "E-POWER", "A-WALL" };
var counts = new Dictionary<string, int>();
foreach (string layer in entityLayers)
{
    counts.TryGetValue(layer, out int current);   // không có khóa → current = 0
    counts[layer] = current + 1;
}
foreach (var pair in counts)
    Console.WriteLine($"{pair.Key}: {pair.Value}");
```

```text
E-POWER: 3
A-WALL: 2
0: 1
```
Thứ tự in ra thường trùng thứ tự thêm vào, nhưng `Dictionary` **không cam kết** thứ tự. Cần thứ tự cố định thì sắp xếp ([C04 §3](C04_CSHARP_IN_THIS_PROJECT.md#3-linq-hỏi-đáp-trên-danh-sách)). Nâng cao: tên layer không phân biệt hoa/thường — tạo dictionary thế nào?
</details>

**E3.** Viết lại bài C01-E4 thành hàm `IReadOnlyList<string> Validate(int rows, int columns)` **trả về** danh sách lỗi thay vì in ngay. Kiểm với `(0, 120)`, `(60, 50)`, `(3, 4)`.

<details><summary>Đáp án E3</summary>

<!-- doc-check: run output -->
```csharp
foreach (string error in Validate(0, 120))
    Console.WriteLine(error);
Console.WriteLine(Validate(60, 50).Count);   // 1 (chỉ lỗi tổng số ô)
Console.WriteLine(Validate(3, 4).Count);     // 0

IReadOnlyList<string> Validate(int rows, int columns)
{
    const int MaxDimension = 100, MaxCells = 2500;
    var errors = new List<string>();
    if (rows < 1 || rows > MaxDimension)
        errors.Add($"Số hàng phải trong khoảng 1–{MaxDimension}.");
    if (columns < 1 || columns > MaxDimension)
        errors.Add($"Số cột phải trong khoảng 1–{MaxDimension}.");
    if (rows >= 1 && columns >= 1 && rows * columns > MaxCells)
        errors.Add($"Tổng số ô ({rows * columns}) vượt quá giới hạn {MaxCells}.");
    return errors;
}
```

```text
Số hàng phải trong khoảng 1–100.
Số cột phải trong khoảng 1–100.
1
0
```
Bây giờ mở [`GridSpec.Validate`](../../Samples/GridMarker/GridSpec.cs) — bạn đã viết gần đúng bản thật. Vì sao trả về **danh sách** lỗi tốt hơn ném lỗi ở lỗi đầu tiên? (Gợi ý: palette hiện **mọi** lỗi cùng lúc cho người dùng sửa một lần.)
</details>

---

## ✅ Tự kiểm tra C02

1. `Point3d a = …; Point3d b = a;` rồi bạn tạo lại `b = new Point3d(5, 0, 0)`. `a` có đổi không? Nếu thay `Point3d` bằng `Line` và sửa `b.StartPoint` thì sao?
2. Khi nào chọn mảng, khi nào chọn `List<T>`, khi nào chọn `Dictionary<TKey, TValue>`?
3. Vì sao `GridSpec.Validate()` trả về `IReadOnlyList<string>` mà không trả `List<string>`?
4. `string? s = null; int n = s?.Length ?? -1;` — `n` bằng bao nhiêu? Dòng `int m = s!.Length;` thì sao?
5. Đoạn `name.ToUpperInvariant();` (đứng một mình) có làm `name` thành chữ hoa không?

<details><summary>Gợi ý</summary>

1. Không — `Point3d` là struct, `b` là bản sao riêng. Với `Line` (class), `b` và `a` cùng một đối tượng, nên sửa `b.StartPoint` thì `a` cũng thấy.
2. Mảng: số phần tử cố định (thường là tham số `params`). `List<T>`: danh sách thêm/bớt được. `Dictionary`: cần tra nhanh theo một khóa (tên → đối tượng).
3. Để người gọi không sửa được kết quả; bên trong hàm vẫn dùng `List` để `Add`.
4. `n = -1`. Dòng thứ hai ném `NullReferenceException` lúc chạy — `!` chỉ "bịt miệng" trình biên dịch, không làm `s` hết null.
5. Không. Chuỗi bất biến: phải viết `name = name.ToUpperInvariant();`.
</details>

---

**Tiếp theo:** [C03 — Hướng đối tượng qua code dự án →](C03_OBJECT_ORIENTED.md)
