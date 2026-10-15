> **🎓 Học viện CadCoreFramework** · [Lộ trình](README.md) · [← C00](C00_START_HERE.md) · **C01** · [C02 →](C02_DATA_AND_COLLECTIONS.md)

# C01 — Chương Trình Đầu Tiên & Tư Duy Lập Trình

> **Cấp độ:** 🌰 Nhập môn · **Cần trước:** [C00 §3](C00_START_HERE.md#3-chuẩn-bị-môi-trường) (đã cài .NET SDK) · **Thời lượng:** tuần 1–2
>
> **⏱ Đọc:** ≈ 35 phút · **Thực hành:** [bài tập §10](#10-bài-tập) · **Nhãn mục:** mọi mục đều 🎯, vì Giai đoạn 0 đã lọc theo [quy tắc 20/80](C00_START_HERE.md#1-quy-tắc-2080-học-đúng-phần-dự-án-thật-sự-dùng) ([README §5](README.md#5-quy-ước-đọc))

Module này dạy những viên gạch đầu tiên: chạy một chương trình, đọc lỗi, lưu dữ liệu vào biến, ra quyết định bằng `if`, lặp bằng vòng lặp, và gom code thành hàm. Đây là phần **tầng 1** của bảng 20/80 — dòng nào trong dự án cũng có.

**Sau module này bạn phải làm được:**
1. Tạo, chạy, sửa một chương trình console; đọc một lỗi biên dịch và tìm ra dòng sai.
2. Giải thích vì sao `7 / 2` ra `3` và vì sao `double.Parse("1.5")` có thể ra `15`.
3. Viết lại được vòng lặp lồng nhau và hàm `ToRowLetters` của [`GridPlanner`](../../Samples/GridMarker/GridPlanner.cs).

---

## Mục lục

1. [Lập trình, C# và .NET là gì](#1-lập-trình-c-và-net-là-gì)
2. [Chương trình đầu tiên](#2-chương-trình-đầu-tiên)
3. [Đọc lỗi biên dịch](#3-đọc-lỗi-biên-dịch)
4. [Biến và kiểu dữ liệu](#4-biến-và-kiểu-dữ-liệu)
5. [Chuỗi và định dạng số — bẫy định dạng Việt Nam](#5-chuỗi-và-định-dạng-số--bẫy-định-dạng-việt-nam)
6. [Ra quyết định: if, else, switch](#6-ra-quyết-định-if-else-switch)
7. [Vòng lặp: for, foreach, while](#7-vòng-lặp-for-foreach-while)
8. [Hàm (method)](#8-hàm-method)
9. [Debug: xem chương trình chạy từng bước](#9-debug-xem-chương-trình-chạy-từng-bước)
10. [Bài tập](#10-bài-tập)

---

## 1. Lập trình, C# và .NET là gì

**Máy tính chỉ làm đúng từng bước bạn ghi ra** — không hơn, không kém, không "hiểu ý". Một **chương trình** là danh sách các bước đó, viết bằng một **ngôn ngữ lập trình** mà con người đọc được.

- **C#** (đọc là "xi-sáp") là ngôn ngữ của dự án này.
- **.NET** là nền tảng chạy code C#. Bạn viết file `.cs` → **trình biên dịch** (compiler) kiểm tra và dịch nó → ra file `.exe` (chương trình chạy độc lập) hoặc `.dll` (thư viện để chương trình khác nạp vào).
- CadCoreFramework là một **`.dll`**: AutoCAD nạp nó vào bên trong mình (chi tiết ở [T01 §1](T01_AUTOCAD_NET_FOUNDATIONS.md#1-plugin-sống-ở-đâu-autocad-net-và-appdomain-dùng-chung)). Trong giai đoạn 0 bạn viết `.exe` console — đơn giản hơn, không cần AutoCAD, nhưng **cùng một ngôn ngữ C#**.

> 💡 **20/80:** biên dịch (build) là lúc máy **kiểm tra** code của bạn. Lỗi bắt được ở bước này là lỗi rẻ nhất — sửa ngay, không ai bị ảnh hưởng.

---

## 2. Chương trình đầu tiên

```powershell
cd $HOME\hoc-csharp                # thư mục luyện tập đã tạo ở C00 §3
dotnet new console -n BaiTap01     # tạo dự án mới tên BaiTap01
cd BaiTap01
dotnet run                         # biên dịch + chạy
```

Mở `Program.cs`, xóa hết nội dung, gõ:

```csharp
Console.OutputEncoding = System.Text.Encoding.UTF8;   // hiển thị đúng tiếng Việt trong cửa sổ console
Console.WriteLine("Xin chào CadCoreFramework!");
Console.WriteLine("Chương trình đầu tiên của tôi.");
```

Chạy lại `dotnet run`. Ba điều cần để ý:
- Mỗi **câu lệnh** kết thúc bằng dấu `;`.
- `Console.WriteLine(...)` in một dòng ra màn hình. `Console` là một **class** có sẵn của .NET, `WriteLine` là một **hàm** của nó (C03 sẽ nói rõ).
- Phần sau `//` là **comment**: máy bỏ qua, chỉ để người đọc hiểu.

> Chương trình console mới tạo dùng kiểu viết **"top-level statements"**: gõ thẳng câu lệnh, không cần bọc trong class. Code của dự án thì luôn nằm trong class và `namespace` — bạn sẽ gặp ở [C03](C03_OBJECT_ORIENTED.md).

---

## 3. Đọc lỗi biên dịch

Người mới sợ lỗi; người có kinh nghiệm **đọc** lỗi. Mỗi lỗi có dạng:

```text
Program.cs(2,19): error CS0103: The name 'colums' does not exist in the current context
     │      │ │          │      └── mô tả (tiếng Anh)
     │      │ │          └── mã lỗi: tra trên mạng với "CS0103"
     │      │ └── cột 19
     │      └── dòng 2
     └── file
```

Bốn lỗi người mới gặp nhiều nhất — thông báo dưới đây là **thông báo thật** của trình biên dịch .NET 8:

| Code sai | Thông báo | Nghĩa là |
| :--- | :--- | :--- |
| `int rows = 3` (thiếu `;`) rồi xuống dòng `Console.WriteLine(rows);` | `error CS1003: Syntax error, ',' expected` | Thiếu dấu `;` ở **cuối dòng trước**. Trình biên dịch "đoán" sai cách sửa (nó gợi ý `,`) nhưng **vị trí** thì đúng |
| `Console.WriteLine(colums);` (gõ nhầm `columns`) | `error CS0103: The name 'colums' does not exist in the current context` | Dùng một cái tên chưa khai báo — thường do gõ nhầm |
| `int rows = "3";` | `error CS0029: Cannot implicitly convert type 'string' to 'int'` | Gán **chữ** vào biến kiểu **số** |
| `string? layer = null;` rồi `layer.Length` | `warning CS8602: Dereference of a possibly null reference.` | Chỉ là *cảnh báo*, nhưng chương trình sẽ hỏng lúc chạy nếu `layer` thật sự null ([C02 §6](C02_DATA_AND_COLLECTIONS.md#6-null-giá-trị-không-có)) |

> ⚠️ **Bẫy:** sửa lỗi **đầu tiên** trước. Một dấu `;` thiếu có thể sinh ra cả chục lỗi phía sau; sửa nó xong, phần lớn số còn lại tự biến mất. Và: dự án build với `-warnaserror` (cảnh báo cũng tính là lỗi, [T06](T06_BUILD_RELEASE_DEVOPS.md)) — tập thói quen **không để lại cảnh báo** ngay từ bây giờ.

---

## 4. Biến và kiểu dữ liệu

**Biến** là một cái hộp có **tên** và **kiểu**, chứa một giá trị:

```csharp
int rows = 3;                     // số nguyên
int columns = 4;
double spacing = 20.5;            // số thực (có phần thập phân)
bool isValid = true;              // đúng / sai
string layer = "SAMPLE_GRID";     // chuỗi ký tự
var cellCount = rows * columns;   // var: trình biên dịch tự suy ra kiểu (ở đây là int)

Console.WriteLine(cellCount);     // 12
Console.WriteLine(layer);         // SAMPLE_GRID
Console.WriteLine(isValid);       // True
Console.WriteLine(spacing * 2);   // 41
```

| Kiểu | Chứa gì | Ví dụ trong dự án |
| :--- | :--- | :--- |
| `int` | số nguyên | `Rows`, `Columns` của [`GridSpec`](../../Samples/GridMarker/GridSpec.cs) |
| `double` | số thực | tọa độ, `Spacing`, `TextHeight` |
| `bool` | `true` / `false` | `IsValid`, `IsReadOnly` |
| `string` | văn bản | tên layer, thông báo lỗi |

`var` **không** phải "kiểu gì cũng được": trình biên dịch nhìn vế phải để suy ra kiểu, sau đó kiểu cố định. `var` xuất hiện 353 dòng trong dự án — chủ yếu khi vế phải đã nói rõ kiểu (`var errors = new List<string>();`).

**Đặt tên** theo quy ước của dự án (C# phân biệt hoa/thường: `rows` và `Rows` là hai tên khác nhau):

| Loại | Kiểu viết | Ví dụ |
| :--- | :--- | :--- |
| Biến cục bộ, tham số | `camelCase` | `cellCount`, `originX` |
| Class, hàm, property | `PascalCase` | `GridSpec`, `Validate`, `CellCount` |
| Field riêng của class | `_camelCase` | `_cadContext` |

Tính toán — có hai cái bẫy:

```csharp
Console.WriteLine(7 / 2);       // 3     ← chia hai số NGUYÊN: bỏ phần lẻ
Console.WriteLine(7 % 2);       // 1     ← phần dư
Console.WriteLine(7 / 2.0);     // 3.5   ← có một số thực → kết quả là số thực
Console.WriteLine(0.1 + 0.2 == 0.3);                    // False ← số thực có sai số rất nhỏ
Console.WriteLine(Math.Abs((0.1 + 0.2) - 0.3) < 1e-9);  // True  ← so sánh "gần bằng" với dung sai
```

> ⚠️ **Bẫy CAD:** **đừng so sánh `double` bằng `==`**. Tọa độ trong bản vẽ luôn có sai số rất nhỏ; so sánh hai điểm phải dùng dung sai (`Math.Abs(a - b) < 1e-9`). AutoCAD có sẵn khái niệm `Tolerance` cho việc này.

---

## 5. Chuỗi và định dạng số — bẫy định dạng Việt Nam

```csharp
string name = "SAMPLE_GRID";
int count = 12;
Console.WriteLine("Layer " + name + " có " + count + " ô");   // nối chuỗi bằng +
Console.WriteLine($"Layer {name} có {count} ô");               // nội suy: dấu $ và {biến}

double width = 1234.5678;
Console.WriteLine($"Rộng: {width:F2}");                        // :F2 = 2 chữ số thập phân

string path = @"C:\Users\Public\Drawing1.dwg";                 // @: dấu \ là chính nó, không cần gõ \\
Console.WriteLine(path);
Console.WriteLine("Dòng 1\nDòng 2");                           // \n: xuống dòng
```

Kết quả (máy định dạng Mỹ in `1234.57`, máy định dạng Việt Nam in `1234,57`):

```text
Layer SAMPLE_GRID có 12 ô
Layer SAMPLE_GRID có 12 ô
Rộng: 1234.57
C:\Users\Public\Drawing1.dwg
Dòng 1
Dòng 2
```

💡 **20/80:** viết chuỗi bằng **nội suy `$"…{biến}…"`** — dự án dùng nó 180 dòng, gần như mọi thông báo lỗi đều viết như vậy:

🔎 [`GridSpec.Validate`](../../Samples/GridMarker/GridSpec.cs): `errors.Add($"Số hàng phải trong khoảng 1–{MaxDimension}.");`

### Bẫy định dạng Việt Nam

Máy tính dùng **định dạng khu vực** (culture) của Windows khi đổi số ↔ chữ. Ở Việt Nam, dấu **phẩy** là dấu thập phân và dấu **chấm** là dấu phân cách hàng nghìn — ngược với Mỹ. Hậu quả có thật:

```csharp
using System.Globalization;

var vi = new CultureInfo("vi-VN");
double a = double.Parse("1.5", vi);                            // định dạng VN: '.' là dấu phân cách HÀNG NGHÌN
double b = double.Parse("1.5", CultureInfo.InvariantCulture);  // định dạng bất biến: '.' là dấu thập phân
Console.WriteLine(a == 15);                                    // True  ← "1.5" bị hiểu thành 15!
Console.WriteLine(b == 1.5);                                   // True
Console.WriteLine(1.5.ToString(CultureInfo.InvariantCulture)); // 1.5 trên mọi máy
```

> ⚠️ **Bẫy:** đọc số từ **file**, Excel, mạng (dữ liệu dùng chung giữa các máy) thì **luôn** truyền `CultureInfo.InvariantCulture`. Không thì cùng một file, máy định dạng Mỹ đọc ra 1,5 mm, máy định dạng Việt Nam đọc ra 15 mm. Còn số **hiển thị cho người dùng** thì để mặc định (theo máy của họ). 🔎 Dự án làm đúng điều này: [`CadApplicationHelper.IsDarkColorTheme`](../../Utils/CadApplicationHelper.cs) đổi số bằng `CultureInfo.InvariantCulture`.

Dự án cũng từng dính một biến thể của bẫy này: ô số trên palette Grid Marker đọc theo định dạng **Mỹ**, nên người dùng gõ `2,5` thì thành **25** (backlog B-31, [U01 §5](U01_WPF_MVVM_BASICS.md#5-ba-bẫy-khi-nhập-số)). Bản sửa đọc mọi ô số bằng `CultureInfo.InvariantCulture`, trong [`ViewModels/NumberText.cs`](../../ViewModels/NumberText.cs). Bài học chung: chỗ nào **đổi chữ thành số**, hãy tự hỏi "đang dùng định dạng của ai?".

---

## 6. Ra quyết định: if, else, switch

`if` là từ khóa dùng nhiều nhất trong dự án (488 dòng):

```csharp
int rows = 120;
const int MaxDimension = 100;              // const: hằng số, không gán lại được

if (rows < 1 || rows > MaxDimension)       // || là HOẶC, && là VÀ, ! là PHỦ ĐỊNH
{
    Console.WriteLine($"Số hàng phải trong khoảng 1–{MaxDimension}.");
}
else if (rows > 50)
{
    Console.WriteLine("Lưới khá lớn, vẽ sẽ lâu hơn.");
}
else
{
    Console.WriteLine("OK");
}
// In ra: Số hàng phải trong khoảng 1–100.
```

| So sánh | Nghĩa |
| :--- | :--- |
| `==` / `!=` | bằng / khác (chú ý: **hai** dấu `=`; một dấu `=` là **gán**) |
| `<` `<=` `>` `>=` | nhỏ hơn, nhỏ hơn hoặc bằng… |
| `&&` `\|\|` `!` | VÀ, HOẶC, PHỦ ĐỊNH |

`switch` chọn một nhánh theo giá trị — gọn hơn chuỗi `if/else if` dài:

```csharp
string style = "Sequential";
switch (style)
{
    case "Sequential":
        Console.WriteLine("Đánh số liên tục: 1, 2, 3…");
        break;
    case "LetterNumber":
    default:
        Console.WriteLine("Chữ + số: A1, A2, B1…");
        break;
}
// In ra: Đánh số liên tục: 1, 2, 3…
```

🔎 `GridPlanner.BuildLabel` trong [`GridPlanner.cs`](../../Samples/GridMarker/GridPlanner.cs) dùng đúng một `switch` như vậy để chọn cách đánh nhãn ô.

---

## 7. Vòng lặp: for, foreach, while

```csharp
for (int i = 0; i < 3; i++)                  // khởi tạo; điều kiện; bước tăng
{
    Console.WriteLine($"Lần thứ {i}");
}

string[] layers = { "0", "E-POWER", "A-WALL" };
foreach (string layer in layers)             // duyệt từng phần tử
{
    Console.WriteLine(layer);
}

int n = 10;
while (n > 0)                                // lặp khi điều kiện còn đúng
{
    Console.Write($"{n} ");
    n -= 3;                                  // viết tắt của n = n - 3
}
Console.WriteLine();
```

```text
Lần thứ 0
Lần thứ 1
Lần thứ 2
0
E-POWER
A-WALL
10 7 4 1
```

| Vòng lặp | Dùng khi | Trong dự án |
| :--- | :--- | :--- |
| `foreach` | duyệt **mọi** phần tử của một danh sách | 67 dòng — phổ biến nhất |
| `for` | cần biết **chỉ số** (thứ tự) | 19 dòng — ví dụ lặp hàng/cột của lưới |
| `while` | lặp tới khi một điều kiện thay đổi | 4 dòng — `ToRowLetters` |

Vòng lặp **lồng nhau** — chính là cách `GridPlanner.Plan` duyệt từng hàng, rồi từng cột trong hàng:

```csharp
string[] rowNames = { "A", "B", "C" };
for (int row = 0; row < 3; row++)
{
    for (int col = 0; col < 4; col++)
    {
        Console.Write($"{rowNames[row]}{col + 1} ");
    }
    Console.WriteLine();
}
```

```text
A1 A2 A3 A4
B1 B2 B3 B4
C1 C2 C3 C4
```

⚠️ **Bẫy:** chỉ số bắt đầu từ **0**. Hàng thứ nhất là `row = 0`, nên nhãn phải là `col + 1` để ra "A1" chứ không phải "A0".

---

## 8. Hàm (method)

**Hàm** gom một nhóm câu lệnh thành một khối có tên, nhận **tham số** vào và trả **kết quả** ra. Viết một lần, gọi nhiều lần:

```csharp
Console.WriteLine(Area(3, 4));             // 12
Console.WriteLine(Perimeter(3, 4));        // 14
Console.WriteLine(Label(0, 0));            // A1
Console.WriteLine(Label(1, 2, "G-"));      // G-B3
Console.WriteLine(Label(col: 3, row: 2));  // C4  ← tham số đặt tên, thứ tự tùy ý

double Area(double width, double height) => width * height;     // thân hàm rút gọn bằng =>

double Perimeter(double width, double height)
{
    return 2 * (width + height);
}

string Label(int row, int col, string prefix = "")              // prefix có giá trị mặc định
{
    char letter = (char)('A' + row);                             // 'A' + 1 = 'B'
    return $"{prefix}{letter}{col + 1}";
}
```

| Khái niệm | Trong ví dụ | Trong dự án |
| :--- | :--- | :--- |
| Kiểu trả về | `double`, `string`; `void` = không trả gì | `GridPlanner.Plan` trả `IReadOnlyList<GridCell>` |
| Tham số mặc định | `string prefix = ""` | constructor `GridSpec(int rows, int columns, double spacing = 20, …)` |
| Tham số đặt tên | `Label(col: 3, row: 2)` | test viết `new GridSpec(2, 3, spacing: 20)` |
| Thân rút gọn `=>` | `Area(...) => width * height` | `public int CellCount => Rows * Columns;` (220 dòng dùng `=>`) |

💡 **20/80:** một hàm nên làm **một việc** và có tên nói đúng việc đó (`Validate`, `Plan`, `ToRowLetters`). Hàm ngắn, tên rõ thì đọc code như đọc câu văn.

---

## 9. Debug: xem chương trình chạy từng bước

Đoán nhầm chương trình đang làm gì là nguồn gốc của hầu hết lỗi. **Debugger** cho bạn dừng chương trình giữa chừng và nhìn giá trị từng biến.

| Việc | Visual Studio / VS Code |
| :--- | :--- |
| Đặt / bỏ **breakpoint** (điểm dừng) | Bấm vào lề trái của dòng, hoặc `F9` |
| Chạy có debug | `F5` |
| Chạy **qua** dòng hiện tại | `F10` (Step Over) |
| Đi **vào bên trong** hàm được gọi | `F11` (Step Into) |
| Xem giá trị biến | Cửa sổ **Locals** / **Watch**, hoặc rê chuột lên tên biến |

🧪 **Thử ngay:** làm bài E3 ở §10, đặt breakpoint ở dòng `int rem = …`, gọi `ToRowLetters(26)`, nhấn `F10` liên tục và ghi ra giấy giá trị `n`, `rem`, `result` qua từng vòng. Bạn sẽ thấy vì sao 26 ra "AA". Đây cũng là kỹ năng bạn dùng để debug plugin trong AutoCAD ở [T04](T04_DEBUGGING_TROUBLESHOOTING.md).

---

## 10. Bài tập

Làm vào các dự án console riêng (`dotnet new console -n BaiTapC01E1`…). Đáp án ở khối thu gọn — **tự làm trước**.

**E1.** Tủ điện rộng 600, cao 2100 (mm). In diện tích và chu vi, 2 chữ số thập phân.

<details><summary>Đáp án E1</summary>

```csharp
double width = 600, height = 2100;
Console.WriteLine($"Diện tích: {width * height:F2}");
Console.WriteLine($"Chu vi: {2 * (width + height):F2}");
```

```text
Diện tích: 1260000.00
Chu vi: 5400.00
```
(Máy định dạng Việt Nam in `1260000,00` và `5400,00`.)
</details>

**E2.** In bảng nhãn lưới 4 hàng × 5 cột theo kiểu `A1 … D5`, **không** dùng mảng `rowNames` (gợi ý: `(char)('A' + row)` như hàm `Label` ở §8).

<details><summary>Đáp án E2</summary>

Thay `rowNames[row]` trong ví dụ vòng lặp lồng nhau ở §7 bằng `(char)('A' + row)`, và đổi giới hạn thành `row < 4`, `col < 5`. Dòng cuối phải là `D1 D2 D3 D4 D5`.
</details>

**E3.** Viết hàm `ToRowLetters(int index)` đổi số hàng (bắt đầu từ 0) thành chữ kiểu Excel: 0 → A, 25 → Z, 26 → AA, 701 → ZZ, 702 → AAA. Chỉ dùng `while`, `%`, `/` và nối chuỗi. Sau đó so với bản thật trong [`GridPlanner.cs`](../../Samples/GridMarker/GridPlanner.cs).

<details><summary>Đáp án E3</summary>

```csharp
foreach (int i in new[] { 0, 25, 26, 701, 702 })
{
    Console.WriteLine($"{i} → {ToRowLetters(i)}");
}

string ToRowLetters(int index)
{
    string result = "";
    int n = index + 1;                 // "hệ cơ số 26 không có số 0": A=1 … Z=26
    while (n > 0)
    {
        int rem = (n - 1) % 26;
        result = (char)('A' + rem) + result;
        n = (n - 1) / 26;
    }
    return result;
}
```

```text
0 → A
25 → Z
26 → AA
701 → ZZ
702 → AAA
```
Bản trong dự án dùng `StringBuilder` thay cho nối chuỗi ([C02 §9](C02_DATA_AND_COLLECTIONS.md#9-làm-việc-với-chuỗi)) và kiểm `index < 0` để ném lỗi ([C04 §7](C04_CSHARP_IN_THIS_PROJECT.md#7-exception-báo-lỗi-đúng-cách)).
</details>

**E4.** Cho `rows = 60`, `columns = 50`. In đúng các thông báo lỗi theo 3 luật của `GridSpec`: hàng và cột phải trong 1–100; tổng số ô không quá 2500.

<details><summary>Đáp án E4</summary>

```csharp
int rows = 60, columns = 50;
const int MaxDimension = 100, MaxCells = 2500;

if (rows < 1 || rows > MaxDimension)
    Console.WriteLine($"Số hàng phải trong khoảng 1–{MaxDimension}.");
if (columns < 1 || columns > MaxDimension)
    Console.WriteLine($"Số cột phải trong khoảng 1–{MaxDimension}.");
if (rows * columns > MaxCells)
    Console.WriteLine($"Tổng số ô ({rows * columns}) vượt quá giới hạn {MaxCells}.");
```

```text
Tổng số ô (3000) vượt quá giới hạn 2500.
```
Câu chữ giống hệt [`GridSpec.Validate`](../../Samples/GridMarker/GridSpec.cs) — so sánh hai bản. ([C02](C02_DATA_AND_COLLECTIONS.md) sẽ cho các thông báo vào một danh sách thay vì in ngay.)
</details>

---

## ✅ Tự kiểm tra C01

1. `Console.WriteLine(10 / 4);` và `Console.WriteLine(10 / 4.0);` in ra gì? Vì sao khác nhau?
2. Trình biên dịch báo `error CS0103` ở dòng 7. Bạn kiểm tra gì trước tiên?
3. Bạn đọc tọa độ từ một file CSV do đồng nghiệp ở nước ngoài gửi. Dòng code nào cần `CultureInfo.InvariantCulture`?
4. Khi nào dùng `foreach`, khi nào dùng `for`?
5. Viết hàm `bool IsInRange(int value, int min, int max)` trong một dòng bằng `=>`.

<details><summary>Gợi ý</summary>

1. `2` và `2.5` (máy VN in `2,5`). Chia hai số nguyên thì bỏ phần lẻ; có một vế là số thực thì ra số thực.
2. Tên ở dòng 7 có gõ đúng không (hoa/thường, thiếu chữ)? Biến đó đã được khai báo **trước** dòng 7 chưa?
3. Dòng `double.Parse(…)` (hoặc `TryParse`) đọc số từ file: `double.Parse(text, CultureInfo.InvariantCulture)`.
4. `foreach` khi chỉ cần từng phần tử; `for` khi cần chỉ số (hàng thứ mấy, cột thứ mấy) hoặc lặp một số lần nhất định.
5. `bool IsInRange(int value, int min, int max) => value >= min && value <= max;`
</details>

---

**Tiếp theo:** [C02 — Dữ liệu: kiểu, collection và null →](C02_DATA_AND_COLLECTIONS.md)
