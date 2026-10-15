> **🎓 Học viện CadCoreFramework** · [Lộ trình](README.md) · **C00** · [C01 →](C01_FIRST_PROGRAM.md)

# C00 — Bắt Đầu Từ Đây: Nhập Môn Lập Trình & C# Theo Quy Tắc 20/80

> **Cấp độ:** 🌰 Nhập môn · **Cần trước:** không gì cả — chưa từng lập trình cũng được · **Thời lượng:** 6 tuần × 8–10 giờ
>
> **⏱ Đọc:** ≈ 20 phút (phần 🎯 ≈ 15 phút) · **Thực hành:** [bài kiểm tra đầu vào](#6-bài-kiểm-tra-đầu-vào-bạn-nên-bắt-đầu-từ-đâu) · **Nhãn mục:** 🎯 cốt lõi · 🔍 đào sâu · 📖 tra cứu ([README §5](README.md#5-quy-ước-đọc))

**Giai đoạn 0** dành cho người **chưa từng viết một dòng code**. Bạn sẽ học C# (ngôn ngữ của dự án) theo **quy tắc 20/80**: dồn sức vào khoảng 20% tính năng mà chính dự án này dùng tới 80% thời gian, bỏ qua phần còn lại cho tới khi thật sự gặp. Không cần AutoCAD cho tới hết giai đoạn này.

**Sau giai đoạn 0 bạn làm được (đo được, mentor kiểm):**
1. Giải thích **từng dòng** của [`GridSpec.cs`](../../Samples/GridMarker/GridSpec.cs) và [`GridPlanner.cs`](../../Samples/GridMarker/GridPlanner.cs) — phần "bộ não" thuần C# của Golden Sample.
2. Tự viết một cặp Spec + Planner mới (đồ án [C05 §5](C05_ASYNC_TESTING_GIT.md#5-đồ-án-nhập-môn-planner-dãy-tủ-điện)) với ít nhất 10 test case, build 0 warning.
3. Đọc một thông báo lỗi biên dịch và tự sửa; đặt breakpoint, xem giá trị biến khi chương trình chạy.
4. Tạo branch, commit, mở Pull Request trên GitHub.

Làm được 4 điều đó là bạn đủ nền để vào [T01](T01_AUTOCAD_NET_FOUNDATIONS.md) với tư cách 🌱 Fresher.

---

## Mục lục

1. [Quy tắc 20/80: học đúng phần dự án thật sự dùng](#1-quy-tắc-2080-học-đúng-phần-dự-án-thật-sự-dùng) 🔍
2. [Lộ trình 6 tuần](#2-lộ-trình-6-tuần) 🎯
3. [Chuẩn bị môi trường](#3-chuẩn-bị-môi-trường) 🎯
4. [Cách học mỗi bài: quy trình 5 bước](#4-cách-học-mỗi-bài-quy-trình-5-bước) 🎯
5. [Quy ước trong giai đoạn 0](#5-quy-ước-trong-giai-đoạn-0) 🎯
6. [Bài kiểm tra đầu vào: bạn nên bắt đầu từ đâu?](#6-bài-kiểm-tra-đầu-vào-bạn-nên-bắt-đầu-từ-đâu) 🎯
7. [Sau giai đoạn 0](#7-sau-giai-đoạn-0) 🎯

---

## 1. Quy tắc 20/80: học đúng phần dự án thật sự dùng

C# có hàng trăm tính năng. Một dự án thật chỉ dùng **thường xuyên** một phần nhỏ trong số đó. Thay vì đọc một giáo trình C# từ đầu đến cuối, bạn học đúng phần mà dự án này cần — và học **trên chính code của dự án**.

Bảng dưới **đếm thật** trong 110 file `.cs` của dự án (không tính test, khoảng 10.500 dòng có chữ), ngày 2026-10-14. Con số là **số dòng có dùng** tính năng đó:

| Tầng | Tính năng (số dòng có dùng) | Học ở |
| :--- | :--- | :---: |
| 🥇 **Tầng 1** — gặp trên mọi trang | `if` (488) · `new` (439) · `var` (353) · `static` (307) · `?.` (238) · `=>` (220) · chuỗi `$"…"` (180) · `try`/`catch` (146) · `throw` (117) · extension method (111) · `class` (104) · attribute `[…]` (86) · `Action`/`Func` (82) · `nameof` (81) · property `{ get; }` (70) | C01–C04 |
| 🥈 **Tầng 2** — gặp hằng ngày | generic `<T>` (68) · `foreach` (67) · `readonly` (61) · LINQ (59) · `async`/`await` (47) · `IEnumerable`/`IReadOnlyList` (46) · `??` (44) · `List<>` (38) · sự kiện `+=`/`-=` (34) · `is` (31) · `const` (29) · `using (…)` (28) · `Dictionary<>` (22) · `interface` (20) · `for` (19) | C02–C05 |
| 🥉 **Tầng 3** — hiếm, học khi gặp | `yield` (9) · `enum` (3) · `switch` (2) · `struct` (1) | tra cứu khi cần |
| ⛔ **Chưa từng dùng** | `ref` · `goto` · `unsafe` · `record` | bỏ qua ở giai đoạn này |

> [!TIP]
> 💡 **20/80:** khoảng 30 tính năng ở tầng 1 và 2 đủ để bạn đọc được gần như mọi dòng trong dự án. Các module C01–C05 được sắp xếp đúng theo bảng này. Gặp một thứ ngoài bảng thì tra nó **lúc đó**, không học trước.

Đây là số đếm bằng **tìm kiếm văn bản**, nên chỉ là xấp xỉ (có thể lẫn một dòng comment) — dùng để so *thứ tự quan trọng*, không phải con số tuyệt đối. Muốn tự đếm lại (số liệu sẽ đổi khi code thay đổi)? Chạy trong thư mục gốc repo, bằng PowerShell:

```powershell
Get-ChildItem Commands,Core,Extensions,Services,Utils,ViewModels,Views,Samples,Models,Demos,Themes,Properties -Recurse -Filter *.cs |
    Select-String -Pattern '\bforeach \(' -CaseSensitive |
    Measure-Object
```

Quy tắc 20/80 cũng áp dụng cho **cách học**. 20% hoạt động đem lại 80% kết quả là: **gõ lại** ví dụ (không copy), **sửa** một chỗ và **đoán trước** kết quả, **đọc code thật**, và **viết test**. Đọc lý thuyết dài hay xem video thụ động thì đem lại rất ít.

---

## 2. Lộ trình 6 tuần

Mỗi tuần khoảng 8–10 giờ. Đầu ra mỗi tuần là thứ **chạy được** mà mentor kiểm tra được.

| Tuần | Đọc | Luyện tập | Đầu ra kiểm chứng được |
| :---: | :--- | :--- | :--- |
| 1 | [C01](C01_FIRST_PROGRAM.md) §1–6 | Cài đặt; chương trình đầu tiên; biến, chuỗi, điều kiện | Chương trình in đúng các thông báo kiểm tra lưới (bài C01-E4) |
| 2 | [C01](C01_FIRST_PROGRAM.md) §7–10, [C02](C02_DATA_AND_COLLECTIONS.md) §1–4 | Vòng lặp, hàm, debug; mảng, `List`, `Dictionary` | Bài tập C01 + C02-E2; tự debug được `ToRowLetters(26)` |
| 3 | [C02](C02_DATA_AND_COLLECTIONS.md) §5–9, [C03](C03_OBJECT_ORIENTED.md) §1–4 | `null`, `enum`, chuỗi; class, property, `static` | C02-E1, E3; class `PanelSpec` có `Validate()` (bài C03-E1) |
| 4 | [C03](C03_OBJECT_ORIENTED.md) §5–9, [C04](C04_CSHARP_IN_THIS_PROJECT.md) §1–4 | Interface, kế thừa, nhận phụ thuộc qua constructor; delegate, lambda, LINQ | Bài C03-E2, E3 + C04-E1 |
| 5 | [C04](C04_CSHARP_IN_THIS_PROJECT.md) §5–13, [C05](C05_ASYNC_TESTING_GIT.md) §1–2 | Extension, generic, exception, `using`, event; async; unit test | C04-E2, E3; giải mã được 6 dòng code thật (C04 §12); 5 unit test chạy xanh |
| 6 | [C05](C05_ASYNC_TESTING_GIT.md) §3–6 | Đọc solution dự án; Git & PR; đồ án | **PR đồ án nhập môn** + vượt bài đánh giá 🌰 → 🌱 ([C05 §6](C05_ASYNC_TESTING_GIT.md#6-đánh-giá-nhập-môn--fresher)) |

**Đã biết một ngôn ngữ lập trình khác** (Python, JavaScript, Java…)? Làm bài kiểm tra ở §6. Thường bạn chỉ cần 2 tuần: lướt C01–C02, làm kỹ C03–C05 (vì interface, generic, lambda, LINQ, `using` của C# có cách viết riêng).

---

## 3. Chuẩn bị môi trường

Giai đoạn 0 **không cần AutoCAD**. Mọi bài đều chạy được bằng .NET trên máy Windows bình thường.

| Thành phần | Phiên bản | Ghi chú |
| :--- | :--- | :--- |
| .NET SDK | 8 (LTS) trở lên | Bộ công cụ để biên dịch và chạy C#. Tải tại trang chính thức của Microsoft (dotnet.microsoft.com) |
| Trình soạn thảo | **Visual Studio 2022 Community** (miễn phí), workload ".NET desktop development" — *hoặc* **VS Code** + extension "C# Dev Kit" | Visual Studio dễ debug hơn cho người mới; dự án cũng dùng Visual Studio |
| Git | mới nhất | Lưu lịch sử thay đổi, làm việc nhóm ([C05 §4](C05_ASYNC_TESTING_GIT.md#4-git-và-pull-request)) |
| Tài khoản GitHub | — | Để tạo Pull Request |

Kiểm tra cài đặt và tạo chương trình đầu tiên (mở **PowerShell** hoặc **Terminal**):

```powershell
dotnet --version              # in ra 8.x hoặc mới hơn
git --version
mkdir $HOME\hoc-csharp        # thư mục luyện tập RIÊNG, ngoài repo dự án ($HOME = C:\Users\<tên bạn>)
cd $HOME\hoc-csharp
dotnet new console -n BaiTap01
cd BaiTap01
dotnet run                    # in ra: Hello, World!
```

Nếu `dotnet --version` báo "không nhận ra lệnh", hãy đóng rồi mở lại cửa sổ PowerShell sau khi cài SDK. Vẫn lỗi thì cài lại SDK.

---

## 4. Cách học mỗi bài: quy trình 5 bước

1. **Đọc** một mục (khoảng 10 phút). Đừng cố nhớ hết.
2. **Gõ lại** ví dụ vào `Program.cs` — gõ tay, không copy. Ngón tay nhớ cú pháp nhanh hơn mắt.
3. **Chạy** (`dotnet run`) và so với kết quả ghi trong bài.
4. **Sửa một chỗ**, viết ra giấy kết quả bạn **đoán**, rồi mới chạy. Đoán sai là lúc bạn học được nhiều nhất.
5. Mở file dự án ở mục 🔎 và **tìm đúng khái niệm đó** trong code thật.

Cuối mỗi module C01–C04 là phần **Bài tập**. Làm chúng trong [`exercises/`](../../exercises/README.md): mỗi bài có sẵn test chấm. Bạn biết ngay mình đúng hay sai, sai ở dòng nào, không phải đợi mentor:

```powershell
dotnet test exercises/Exercises.Tests --filter FullyQualifiedName~C01.E3
```

> [!IMPORTANT]
> **Kẹt quá 20 phút thì hỏi** — nhưng hỏi đúng cách: (1) bạn muốn làm gì, (2) **nguyên văn** thông báo lỗi (copy, đừng gõ lại), (3) bạn đã thử những gì. Câu hỏi "code không chạy" không ai trả lời được; câu hỏi có đủ 3 phần thường tự bạn trả lời được trước khi gửi.

---

## 5. Quy ước trong giai đoạn 0

| Ký hiệu | Ý nghĩa |
| :--- | :--- |
| 🧪 **Thử ngay** | Chạy ví dụ hoặc làm một thay đổi nhỏ |
| 🔎 **Đọc code dự án** | Mở file thật của CadCoreFramework, tìm khái niệm vừa học |
| ⚠️ **Bẫy** | Lỗi người mới hay mắc (nhiều cái chính dự án đã gặp) |
| 💡 **20/80** | Điều đáng nhớ nhất của mục |
| ✅ **Tự kiểm tra** | Câu hỏi cuối module; đáp án trong khối thu gọn — tự trả lời trước khi mở |

- **Mọi ví dụ là một chương trình hoàn chỉnh**: xóa hết nội dung `Program.cs` rồi gõ ví dụ vào. Tất cả đã được chạy thật trên .NET 8 với cú pháp C# 10 như dự án ([vì sao C# 10](C04_CSHARP_IN_THIS_PROJECT.md)); kết quả ghi trong comment `// …` và trong các khung kết quả là kết quả thật.
- Thêm dòng sau vào **đầu** `Program.cs` để cửa sổ console hiện đúng tiếng Việt (các ví dụ lược dòng này cho gọn):

  ```csharp
  Console.OutputEncoding = System.Text.Encoding.UTF8;
  ```

- Ví dụ in **số thập phân**: comment ghi theo dấu chấm (`3.5`). Máy đặt định dạng Việt Nam sẽ in dấu phẩy (`3,5`) — đó là đúng, không phải lỗi. Lý do và cái bẫy đi kèm: [C01 §5](C01_FIRST_PROGRAM.md#5-chuỗi-và-định-dạng-số--bẫy-định-dạng-việt-nam).

---

## 6. Bài kiểm tra đầu vào: bạn nên bắt đầu từ đâu?

Trả lời 8 câu **không tra cứu**, rồi mở đáp án.

1. `Console.WriteLine(7 / 2);` in ra gì?
2. `string[]` và `List<string>` khác nhau ở điểm quan trọng nào?
3. `string? name = null; Console.WriteLine(name?.Length ?? 0);` in ra gì?
4. *Class* và *object* khác nhau thế nào?
5. Một `interface` dùng để làm gì?
6. `layers.Where(l => l.StartsWith("E-")).ToList()` làm gì?
7. Vì sao viết `using (var tr = …) { … }` thay vì chỉ `var tr = …;`?
8. `await` có làm chương trình "đứng hình" trong lúc chờ không?

<details><summary>Đáp án và cách tính điểm</summary>

1. `3` — chia hai số nguyên thì bỏ phần lẻ.
2. Mảng có số phần tử **cố định**; `List` thêm/bớt được.
3. `0` — `?.` trả `null` thay vì ném lỗi, `??` thay `null` bằng `0`.
4. Class là **bản thiết kế**; object là **một thứ cụ thể** tạo từ bản thiết kế đó bằng `new`.
5. Định nghĩa một **hợp đồng** (phải có những gì) mà nhiều class có thể thực hiện; nhờ đó thay được bản thật bằng bản giả khi test.
6. Lọc ra các phần tử bắt đầu bằng `"E-"` và đưa kết quả vào một `List` mới.
7. Để `Dispose()` **chắc chắn** được gọi khi ra khỏi khối, kể cả khi có lỗi.
8. Không. `await` nhường thread cho việc khác trong lúc chờ, rồi chạy tiếp phần sau khi có kết quả.

**Mỗi câu đúng 1 điểm:**
- **0–3 điểm:** bắt đầu từ [C01](C01_FIRST_PROGRAM.md), đi đủ 6 tuần.
- **4–6 điểm:** lướt C01, bắt đầu học kỹ từ [C02](C02_DATA_AND_COLLECTIONS.md).
- **7–8 điểm:** đọc [C04](C04_CSHARP_IN_THIS_PROJECT.md) §12 (giải mã code thật), làm đồ án [C05 §5](C05_ASYNC_TESTING_GIT.md#5-đồ-án-nhập-môn-planner-dãy-tủ-điện), rồi vào thẳng [T01](T01_AUTOCAD_NET_FOUNDATIONS.md).
</details>

---

## 7. Sau giai đoạn 0

- **Lên cấp 🌰 → 🌱 Fresher** khi: làm xong bài tập C01–C05, đồ án nhập môn đạt DoD, và vượt phần vấn đáp ở [C05 §6](C05_ASYNC_TESTING_GIT.md#6-đánh-giá-nhập-môn--fresher) (cũng ghi trong [T10 §4](T10_LABS_AND_ASSESSMENT.md#4-đề-đánh-giá-lên-cấp)).
- **Tiếp theo:** [T01 — Nền tảng AutoCAD .NET](T01_AUTOCAD_NET_FOUNDATIONS.md) và [lộ trình 14 tuần](README.md#3-lộ-trình-đào-tạo) (Giai đoạn 1–3, gồm cả [nhánh UI](U00_UI_START_HERE.md) ở tuần 7–9). Từ T01 bạn cần AutoCAD 2024.
- Gặp thuật ngữ lạ: [Thuật ngữ — phần Lập trình & C#](GLOSSARY.md#a-lập-trình--c-cho-người-mới).

---

**Tiếp theo:** [C01 — Chương trình đầu tiên & tư duy lập trình →](C01_FIRST_PROGRAM.md)
