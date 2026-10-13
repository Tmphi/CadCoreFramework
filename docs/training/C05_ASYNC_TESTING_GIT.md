> **🎓 Học viện CadCoreFramework** · [Lộ trình](README.md) · [← C04](C04_CSHARP_IN_THIS_PROJECT.md) · **C05** · [T01 →](T01_AUTOCAD_NET_FOUNDATIONS.md)

# C05 — Async, Unit Test, Git Và Đồ Án Nhập Môn

> **Cấp độ:** 🌰 Nhập môn → 🌱 Fresher · **Cần trước:** [C04](C04_CSHARP_IN_THIS_PROJECT.md) · **Thời lượng:** tuần 5–6

Module cuối của giai đoạn 0 dạy ba kỹ năng làm việc thật: **chờ mà không đứng hình** (`async`/`await`), **để máy kiểm code giùm** (unit test), và **làm việc nhóm** (Git, Pull Request). Sau đó bạn áp dụng tất cả vào đồ án nhập môn, rồi làm bài đánh giá lên 🌱 Fresher.

**Sau module này bạn phải làm được:**
1. Giải thích vì sao sau `await` code có thể đang chạy trên **thread khác**, và vì sao điều đó nguy hiểm với AutoCAD.
2. Viết unit test bằng xUnit + FluentAssertions, đọc được một test fail và chạy riêng một test.
3. Build và chạy test của dự án thật; tạo branch, commit, push và mở Pull Request.
4. Hoàn thành đồ án [§5](#5-đồ-án-nhập-môn-planner-dãy-tủ-điện) và vượt [§6](#6-đánh-giá-nhập-môn--fresher).

---

## Mục lục

1. [async và await: chờ mà không đứng hình](#1-async-và-await-chờ-mà-không-đứng-hình)
2. [Unit test với xUnit](#2-unit-test-với-xunit)
3. [Đọc solution của dự án](#3-đọc-solution-của-dự-án)
4. [Git và Pull Request](#4-git-và-pull-request)
5. [Đồ án nhập môn: Planner dãy tủ điện](#5-đồ-án-nhập-môn-planner-dãy-tủ-điện)
6. [Đánh giá nhập môn → Fresher](#6-đánh-giá-nhập-môn--fresher)

---

## 1. async và await: chờ mà không đứng hình

Gọi mạng, đọc file lớn, truy vấn cơ sở dữ liệu… mất thời gian. Nếu đứng chờ, giao diện (và AutoCAD) **đứng hình**. `async`/`await` cho phép **bắt đầu** một việc, làm việc khác, rồi quay lại khi có kết quả:

```csharp
using System.Diagnostics;

var sw = Stopwatch.StartNew();
Task<string> download = DownloadAsync();          // BẮT ĐẦU tải, không đứng chờ
Console.WriteLine("Trong lúc tải, chương trình vẫn chạy tiếp…");
string data = await download;                     // chờ ở đây — nhưng không "đóng băng" thread
Console.WriteLine($"{data} sau khoảng {sw.ElapsedMilliseconds / 1000} giây");

async Task<string> DownloadAsync()
{
    await Task.Delay(2000);                        // giả lập gọi mạng mất 2 giây
    return "Đã tải xong";
}
```

```text
Trong lúc tải, chương trình vẫn chạy tiếp…
Đã tải xong sau khoảng 2 giây
```

| Mảnh | Nghĩa |
| :--- | :--- |
| `Task` / `Task<T>` | một việc **đang chạy hoặc sẽ xong**; `Task<string>` là việc sẽ cho ra một `string` |
| `async` | đánh dấu hàm có dùng `await` bên trong |
| `await x` | "chờ `x` xong rồi chạy tiếp phần dưới" — trong lúc chờ, thread được trả lại để làm việc khác |
| đuôi `…Async` | quy ước đặt tên hàm trả về `Task`: `ExecuteAsync`, `RunSafeAsync`, `ExecuteWithScopeAsync` |

### Sau await, bạn đang ở thread nào?

Mỗi chương trình có một hoặc nhiều **thread** (luồng chạy). Chạy thử:

```csharp
Console.WriteLine($"Trước await: thread {Environment.CurrentManagedThreadId}");
await Task.Delay(100);
Console.WriteLine($"Sau await:   thread {Environment.CurrentManagedThreadId}");   // thường là số KHÁC!

int total = await Task.Run(() => Enumerable.Range(1, 1_000_000).Sum(x => x % 7));   // việc nặng → thread nền
Console.WriteLine(total);                                                          // 2999998
```

```text
Trước await: thread 1
Sau await:   thread 5
2999998
```

Số thread trên máy bạn có thể khác (trên .NET Framework 4.8 là `1` rồi `4`), nhưng **trước và sau khác nhau**. Lý do: chương trình console không có **`SynchronizationContext`** — thứ "nhớ đường về" thread ban đầu — nên phần sau `await` chạy trên một thread bất kỳ trong ThreadPool (*bể thread nền*).

> [!WARNING]
> **Vì sao điều này quan trọng với AutoCAD:** AutoCAD API **chỉ an toàn trên main thread**. Trong một lệnh hay một nút bấm của palette, main thread của AutoCAD **có** `SynchronizationContext`, nên `await` quay về đúng main thread. Nhưng `Task.Run(…)`, `ConfigureAwait(false)` hay một thread nền đều đưa bạn ra khỏi main thread — chạm bản vẽ lúc đó thì "lúc chạy được, lúc hỏng dữ liệu, lúc crash" (comment của [`CadThread`](../../Core/CadThread.cs)). Framework chặn điều này bằng `CadThread.EnsureMainThread` và thiết kế `ExecuteAsync` hai giai đoạn ([T02 §4.4](T02_ARCHITECTURE_DEEP_DIVE.md#44-executeasync-hai-giai-đoạn), [T01 §8.2](T01_AUTOCAD_NET_FOUNDATIONS.md#82-asyncawait-và-synchronizationcontext)).

💡 **20/80 — năm quy tắc async của dự án:**
1. **Thread nền chỉ để tính toán và I/O** (gọi mạng, đọc file) — **không bao giờ** chạm AutoCAD API. [`CadAsyncHelper`](../../Utils/CadAsyncHelper.cs) ghi rõ: "TUYỆT ĐỐI không gọi AutoCAD API ở đây".
2. **Không** chặn chờ bằng `.Result` hay `.Wait()` trên main thread: dễ **deadlock** (treo cứng) khi task đó cần quay về main thread.
3. **Không** dùng `ConfigureAwait(false)` ở chỗ mà phía sau còn gọi AutoCAD. Dự án chỉ dùng nó trong đoạn chắc chắn không chạm AutoCAD, ví dụ bên trong `Task.Run`.
4. **`async void` chỉ ở biên** (event handler). Lệnh AutoCAD gọi code async qua `FireAndForget` để lỗi không bị nuốt: `RunAsyncAwaitDemo().FireAndForget(ex => Logger?.Error("DEMO_ASYNC_AWAIT thất bại.", ex));` ([`DemoServicesCommands`](../../Demos/DemoServicesCommands.cs)).
5. **Sau `await`, thế giới có thể đã đổi**: người dùng đã chuyển sang bản vẽ khác. Lệnh `DEMO_ASYNC_AWAIT` từng không vẽ được đúng vì lý do này — sự cố thật ở [T03 B31](T03_PITFALLS_CASEBOOK.md#b31-gọi-editor-của-bản-vẽ-không-active).

---

## 2. Unit test với xUnit

**Unit test** là một đoạn code **kiểm** một đoạn code khác: gọi hàm với dữ liệu biết trước rồi so kết quả với kỳ vọng. Viết một lần, chạy lại mãi mãi — mỗi lần sửa code, máy kiểm lại mọi thứ trong vài giây. Dự án dùng **xUnit** (bộ chạy test) và **FluentAssertions** (viết kiểm tra như câu tiếng Anh).

Tạo dự án test để luyện:

```powershell
cd $HOME\hoc-csharp
dotnet new xunit -n HocTest
cd HocTest
dotnet add package FluentAssertions --version 6.12.0
```

> ⚠️ **Đúng phiên bản 6.12.0** như dự án ([`CadCoreFramework.Tests.csproj`](../../CadCoreFramework.Tests/CadCoreFramework.Tests.csproj)). Từ bản 8, FluentAssertions đổi giấy phép: dùng cho mục đích thương mại phải mua license. Đừng tự nâng cấp lên bản mới nhất — việc đổi phiên bản là quyết định của nhóm.

Thay nội dung `UnitTest1.cs` bằng:

```csharp
using FluentAssertions;
using Xunit;

namespace HocTest;

public static class PanelMath
{
    public static double TotalWidth(int count, double width, double gap)
    {
        if (count < 1) throw new ArgumentOutOfRangeException(nameof(count), "Phải có ít nhất 1 tủ.");
        return count * width + (count - 1) * gap;
    }
}

public class PanelMathTests
{
    [Fact]                                                          // [Fact]: một bài test
    public void TotalWidth_ThreePanels_IncludesTwoGaps()
    {
        // Arrange — chuẩn bị dữ liệu
        int count = 3;
        double width = 600, gap = 50;

        // Act — gọi đúng MỘT hành động cần kiểm
        double total = PanelMath.TotalWidth(count, width, gap);

        // Assert — kiểm kết quả
        total.Should().Be(1900);
    }

    [Theory]                                                        // [Theory]: một bài test, NHIỀU bộ dữ liệu
    [InlineData(1, 600, 50, 600)]
    [InlineData(2, 600, 50, 1250)]
    [InlineData(4, 800, 0, 3200)]
    public void TotalWidth_Cases(int count, double width, double gap, double expected)
    {
        PanelMath.TotalWidth(count, width, gap).Should().Be(expected);
    }

    [Fact]
    public void TotalWidth_ZeroPanels_Throws()
    {
        Action act = () => PanelMath.TotalWidth(0, 600, 50);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
```

(Để gọn, ví dụ đặt code và test chung một file. Thực tế code nằm ở project riêng, test ở project test — như [§5](#5-đồ-án-nhập-môn-planner-dãy-tủ-điện).)

```powershell
dotnet test
```

```text
Passed!  - Failed:     0, Passed:     5, Skipped:     0, Total:     5, …
```

Ba test method nhưng **5** test case: `[Theory]` có ba bộ `[InlineData]`, mỗi bộ là một lần chạy.

### Đọc một test fail

🧪 Sửa `Be(1900)` thành `Be(1950)` rồi chạy lại:

```text
  Failed HocTest.PanelMathTests.TotalWidth_ThreePanels_IncludesTwoGaps [470 ms]
   Expected total to be 1950.0, but found 1900.0 (difference of -50).
   at HocTest.PanelMathTests.TotalWidth_ThreePanels_IncludesTwoGaps() in …\UnitTest1.cs:line 28
Failed!  - Failed:     1, Passed:     4, Skipped:     0, Total:     5, …
```

Đọc theo thứ tự: **test nào** fail → **kỳ vọng** gì, **thực tế** ra gì → **dòng nào**. Rồi tự hỏi: code sai hay *test* sai? (Ở đây test sai: 3 tủ chỉ có 2 khe hở.) Sửa lại `1900` trước khi đi tiếp.

### Các kiểm tra hay dùng

| FluentAssertions | Kiểm gì | Gặp trong |
| :--- | :--- | :--- |
| `x.Should().Be(5)` | bằng | mọi nơi |
| `list.Should().Equal("A1", "A2")` | đúng các phần tử, đúng thứ tự | [`GridPlannerTests`](../../CadCoreFramework.Tests/Samples/GridMarker/GridPlannerTests.cs) |
| `list.Should().ContainSingle(e => …)` | đúng **một** phần tử thỏa điều kiện | §5 |
| `list.Should().OnlyHaveUniqueItems()` | không có phần tử trùng | `GridPlannerTests` |
| `text.Should().Contain("…")` | chuỗi có chứa | [`SymbolNameRulesTests`](../../CadCoreFramework.Tests/SymbolNameRulesTests.cs) |
| `act.Should().Throw<ArgumentException>()` | `act` ném đúng loại lỗi | `GridPlannerTests` |

Chạy **riêng** một nhóm test bằng `--filter` (tên test có chứa chuỗi đã cho):

```powershell
dotnet test --filter "FullyQualifiedName~ZeroPanels"
```

```text
Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, …
```

💡 **20/80 — quy ước test của dự án:**
- **Tên test** dạng `Method_TìnhHuống_KếtQuả`: `Plan_InvalidSpec_ThrowsWithValidationMessage`, `Plan_SingleCell_Works`. Đọc tên là biết test kiểm gì.
- **Arrange – Act – Assert**: mỗi test kiểm **một** điều.
- **Test biên**: giá trị ngay sát giới hạn (`0`, `1`, `100`, `101` với `MaxDimension = 100`). Lỗi hay nấp ở biên.
- Đọc **test trước khi đọc code**: test là tài liệu *chạy được* về việc code phải làm.

Chi tiết chiến lược test của cả dự án: [T05](T05_TESTING_STRATEGY.md).

---

## 3. Đọc solution của dự án

Đến lúc mở dự án thật. **Chưa cần AutoCAD**: build và unit test chạy trên mọi máy Windows, giống máy CI của GitHub.

```powershell
git clone https://github.com/Tmphi/CadCoreFramework.git
cd CadCoreFramework
dotnet build CadCoreFramework.sln -c Debug
dotnet test CadCoreFramework.Tests/CadCoreFramework.Tests.csproj
```

Build cần bộ **.NET Framework 4.8** (đích của plugin cho AutoCAD 2024). Cài Visual Studio 2022 với workload ".NET desktop development" là có sẵn; dùng VS Code thì cài thêm *.NET Framework 4.8 Developer Pack*. Kết quả `dotnet test`:

```text
Passed!  - Failed:     0, Passed:   275, Skipped:     0, Total:   275, Duration: 3 s - CadCoreFramework.Tests.dll (net8.0)
Passed!  - Failed:     0, Passed:   275, Skipped:     0, Total:   275, Duration: 5 s - CadCoreFramework.Tests.dll (net48)
```

(Con số 275 là tại thời điểm viết, ngày 2026-10-14; test sẽ nhiều thêm theo thời gian.) Test chạy **hai lần**, một lần cho mỗi đích (`net48` và `net8.0-windows`), và cả bộ chạy trong vài giây. Chạy riêng phần Golden Sample:

```powershell
dotnet test CadCoreFramework.Tests/CadCoreFramework.Tests.csproj --filter "FullyQualifiedName~GridPlanner"
```

```text
Passed!  - Failed:     0, Passed:    18, Skipped:     0, Total:    18, … CadCoreFramework.Tests.dll (net8.0)
Passed!  - Failed:     0, Passed:    18, Skipped:     0, Total:    18, … CadCoreFramework.Tests.dll (net48)
```

### Solution gồm những project nào

| Project | Là gì | Bạn cần khi |
| :--- | :--- | :--- |
| `CadCoreFramework` (gốc repo) | **Plugin** — DLL mà AutoCAD nạp. Build ra `net48` (AutoCAD 2021–2024) và `net8.0-windows` (2025+, chưa kiểm chứng) | luôn |
| `CadCoreFramework.Tests` | Unit test xUnit + FluentAssertions | luôn ([§2](#2-unit-test-với-xunit)) |
| `CadCoreFramework.Testing` | Bản giả `FakeCadContext`, `FakeCadScope` | viết test cho service ([C03 §6](C03_OBJECT_ORIENTED.md#6-interface-hợp-đồng-và-bản-giả)) |
| `CadCoreFramework.Analyzers` | Analyzer CAD001–CAD003: chặn lỗi kiến trúc ngay lúc build | [T02 §11](T02_ARCHITECTURE_DEEP_DIVE.md#11-analyzer-cad001cad003) |
| `CadCoreFramework.Benchmarks` | Đo hiệu năng | [T07](T07_PERFORMANCE_AND_SCALE.md) |
| `tests/integration/HeadlessLab` | Kiểm thử trên AutoCAD thật, không giao diện | [T05 §9](T05_TESTING_STRATEGY.md#9-integration-test-headless-hiện-trạng-và-hướng-đi) |

Bên trong plugin, mỗi thư mục có một vai trò, và **phụ thuộc chỉ đi một chiều** — bảng đầy đủ ở [T02 §1](T02_ARCHITECTURE_DEEP_DIVE.md#1-bản-đồ-dự-án-và-quy-tắc-phụ-thuộc). Bản 20/80: `Commands/` (lệnh) và `Views/` (palette) là **điểm vào**; `ViewModels/` và `Services/` là **nghiệp vụ**; `Core/`, `Extensions/`, `Utils/` là **hạ tầng** chạm AutoCAD; `Samples/GridMarker/` là **Golden Sample** — tính năng mẫu mà mọi tính năng mới làm theo.

### Thứ tự đọc Golden Sample

| # | File | Tầng | Bạn đã có đủ kiến thức? |
| :---: | :--- | :--- | :--- |
| 1 | [`GridSpec.cs`](../../Samples/GridMarker/GridSpec.cs) | Model | ✅ đã đọc ở [C03 §9](C03_OBJECT_ORIENTED.md#9-đọc-có-hướng-dẫn-gridspeccs) |
| 2 | [`GridPlanner.cs`](../../Samples/GridMarker/GridPlanner.cs) | Logic thuần | ✅ C01–C04 |
| 3 | [`GridPlannerTests.cs`](../../CadCoreFramework.Tests/Samples/GridMarker/GridPlannerTests.cs) | Test | ✅ §2 |
| 4 | [`IGridMarkerService.cs`](../../Samples/GridMarker/IGridMarkerService.cs), [`GridMarkerService.cs`](../../Samples/GridMarker/GridMarkerService.cs) | Service | ✅ phần C#; phần AutoCAD (`ICadScope`, XData) học ở T01–T02 |
| 5 | [`GridMarkerCommands.cs`](../../Samples/GridMarker/GridMarkerCommands.cs) | Lệnh `SAMPLE_GRID` | ✅ phần C#; prompt học ở [T01 §7](T01_AUTOCAD_NET_FOUNDATIONS.md#7-editor-prompt-hệ-tọa-độ-selection) |
| 6 | [`GridMarkerViewModel.cs`](../../Samples/GridMarker/GridMarkerViewModel.cs), [`GridMarkerPalette.xaml`](../../Samples/GridMarker/GridMarkerPalette.xaml) | Palette (WPF/MVVM) | một phần — MVVM ở [Phần 5](../05_WPF_UI_AND_MVVM.md) |

🧪 **Thử ngay — test bảo vệ code thế nào:** trong `GridSpec.cs`, đổi chữ `"Số hàng phải trong khoảng…"` thành `"Số dòng phải trong khoảng…"`, rồi chạy:

```powershell
dotnet test CadCoreFramework.Tests/CadCoreFramework.Tests.csproj --filter "FullyQualifiedName~GridSpec|FullyQualifiedName~GridPlanner" -f net8.0-windows
```

```text
  Failed CadCoreFramework.Tests.Samples.GridMarker.GridPlannerTests.Plan_InvalidSpec_ThrowsWithValidationMessage [331 ms]
   Expected exception message to match the equivalent of "*Số hàng*", but "Số dòng phải trong khoảng 1–100. (Parameter 'spec')" does not.
Failed!  - Failed:     1, Passed:    35, Skipped:     0, Total:    36, …
```

Một chữ trong thông báo cho người dùng bị đổi là có test báo ngay. (`|` trong `--filter` nghĩa là "hoặc"; `-f net8.0-windows` chỉ chạy một đích cho nhanh.) Ngược lại, đổi `MaxDimension = 100` thành `99` thì **không** test nào fail: `GridSpecTests` viết `GridSpec.MaxDimension + 1` thay vì số cứng `101`, nên luôn kiểm đúng biên mới. Xong thì **hoàn tác**: `git checkout -- Samples/GridMarker/GridSpec.cs`.

---

## 4. Git và Pull Request

**Git** lưu toàn bộ lịch sử thay đổi của dự án. **GitHub** là nơi đặt bản chung để cả nhóm cùng làm.

| Khái niệm | Nghĩa |
| :--- | :--- |
| **repository** (repo) | thư mục dự án kèm toàn bộ lịch sử |
| **commit** | một "ảnh chụp" thay đổi, có lời mô tả |
| **branch** | một nhánh làm việc riêng; `main` là nhánh chính |
| **push** | đẩy commit từ máy bạn lên GitHub |
| **Pull Request** (PR) | đề nghị gộp branch của bạn vào `main`; người khác **review** trước khi **merge** |

Vòng làm việc hằng ngày:

```powershell
git switch main
git pull                                  # lấy bản mới nhất
git switch -c feature/panel-row           # tạo branch mới cho MỘT việc
# … sửa code, build, test …
git status                                # xem file nào đã đổi
git diff                                  # xem đổi những gì — LUÔN đọc trước khi commit
git add PanelRow.Core/PanelRowSpec.cs     # chọn file đưa vào commit
git commit -m "feat: add PanelRowSpec with validation"
git push -u origin feature/panel-row      # đẩy branch lên GitHub
```

Sau `push`, GitHub hiện nút **Compare & pull request**. Mô tả PR gồm ba phần: **làm gì**, **vì sao**, **đã kiểm thế nào** (lệnh test đã chạy và kết quả).

💡 **20/80 — quy ước của dự án:**
- Lời commit theo kiểu *Conventional Commits*, giống lịch sử của repo: `fix: …` (sửa lỗi), `feat: …` (tính năng), `docs(T09): …` (tài liệu, kèm phạm vi), `test: …`.
- **Một branch, một việc, PR nhỏ.** PR 200 dòng được review kỹ; PR 2000 dòng chỉ được "lướt".
- **Không commit** `bin/`, `obj/` (đã có `.gitignore`), mật khẩu, hay file của cá nhân.
- Không có quyền ghi vào repo: bấm **Fork** trên GitHub, làm trên bản fork của bạn rồi mở PR từ đó.

⚠️ **Bẫy:** `git add .` gom **mọi** thứ đang thay đổi, kể cả file bạn không định đưa vào. Luôn chạy `git status` và `git diff` trước khi commit.

---

## 5. Đồ án nhập môn: Planner dãy tủ điện

Đồ án là **nửa đầu** của lab [T10 L07 — Dãy tủ điện](T10_LABS_AND_ASSESSMENT.md#l07--nhân-bản-golden-sample): phần thuần C#, theo đúng khuôn `GridSpec` + `GridPlanner`. Ở L07 bạn sẽ chép hai file này vào dự án và viết tiếp phần chạm AutoCAD (service, lệnh `PANEL_ROW`, palette).

### Chuẩn bị

Làm trong một repo GitHub **của riêng bạn** (ví dụ `hoc-csharp`), có mentor làm reviewer:

```powershell
mkdir $HOME\hoc-csharp\PanelRow
cd $HOME\hoc-csharp\PanelRow
dotnet new sln -n PanelRow
dotnet new classlib -n PanelRow.Core -f net8.0
dotnet new xunit -n PanelRow.Tests -f net8.0
dotnet sln add PanelRow.Core/PanelRow.Core.csproj PanelRow.Tests/PanelRow.Tests.csproj
dotnet add PanelRow.Tests/PanelRow.Tests.csproj reference PanelRow.Core/PanelRow.Core.csproj
dotnet add PanelRow.Tests/PanelRow.Tests.csproj package FluentAssertions --version 6.12.0
```

Xóa `Class1.cs` và `UnitTest1.cs`. Thêm `<LangVersion>10.0</LangVersion>` vào `PanelRow.Core.csproj` để code chép sang dự án ở L07 mà không phải sửa ([C04](C04_CSHARP_IN_THIS_PROJECT.md)). (SDK .NET 10 trở lên tạo `PanelRow.slnx` thay cho `PanelRow.sln` — dùng như nhau.)

### Đề bài

**`PanelRowSpec`** — thông số một dãy tủ, **bất biến**, đặt trong namespace `PanelRow.Core`:

| Thành viên | Yêu cầu |
| :--- | :--- |
| `PanelRowSpec(int count, double width, double height, double gap = 0, string namePrefix = "P")` | constructor; `namePrefix` là null thì lưu chuỗi rỗng |
| `public const int MaxPanels = 30;` · `public const double MinWidth = 300;` | hằng số |
| `Count`, `Width`, `Height`, `Gap`, `NamePrefix` | property chỉ đọc |
| `TotalWidth` | chiều dài cả dãy: `Count × Width + (Count − 1) × Gap` |
| `IReadOnlyList<string> Validate()` | trả về **mọi** lỗi, đúng **nguyên văn** bảng dưới |
| `IsValid` | `true` khi `Validate()` không có lỗi |

| Luật | Thông điệp lỗi |
| :--- | :--- |
| `Count` < 1 hoặc > `MaxPanels` | `Số tủ phải trong khoảng 1–30.` |
| `Width` < `MinWidth` | `Chiều rộng tủ phải ≥ 300.` |
| `Height` ≤ 0 | `Chiều cao phải > 0.` |
| `Gap` < 0 | `Khe hở không được âm.` |
| `NamePrefix` rỗng hoặc toàn khoảng trắng | `Tiền tố tên không được để trống.` |

(Viết số trong thông điệp bằng nội suy `{MaxPanels}`, `{MinWidth}` như `GridSpec`, đừng gõ cứng.)

**`PanelCell`** — một tủ đã tính vị trí, **bất biến**: `Index` (thứ tự từ 0), `MinX`, `MinY`, `MaxX`, `MaxY`, `Name`.

**`PanelRowPlanner`** — `static class`, một hàm `IReadOnlyList<PanelCell> Plan(PanelRowSpec spec, double originX, double originY)`:
- `spec` là null → ném `ArgumentNullException`.
- `spec` không hợp lệ → ném `ArgumentException`, thông điệp là **mọi** lỗi nối bằng dấu cách (như `GridPlanner.Plan`).
- Tủ thứ `i` (từ 0): `MinX = originX + i × (Width + Gap)`, `MaxX = MinX + Width`, `MinY = originY`, `MaxY = originY + Height`.
- `Name` = tiền tố + số thứ tự **hai chữ số**, bắt đầu từ 1: `P01`, `P02`, …, `P10`. Gợi ý: `$"{7:D2}"` cho ra `"07"`.

### Test mẫu (phải chạy xanh)

Tạo `PanelRow.Tests/PanelRowPlannerTests.cs`:

```csharp
using FluentAssertions;
using PanelRow.Core;
using Xunit;

namespace PanelRow.Tests;

public class PanelRowPlannerTests
{
    [Fact]
    public void Plan_ThreePanels_NamesAreP01ToP03()
    {
        var cells = PanelRowPlanner.Plan(new PanelRowSpec(3, 600, 2000, gap: 50), 0, 0);

        cells.Select(c => c.Name).Should().Equal("P01", "P02", "P03");
    }

    [Fact]
    public void Plan_SecondPanelStartsAfterWidthPlusGap()
    {
        var second = PanelRowPlanner.Plan(new PanelRowSpec(3, 600, 2000, gap: 50), 100, 0)[1];

        second.MinX.Should().Be(750);    // 100 + 600 + 50
        second.MaxX.Should().Be(1350);   // 750 + 600
    }

    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    public void Validate_CountOutOfRange_ReportsError(int count)
    {
        new PanelRowSpec(count, 600, 2000).Validate().Should().ContainSingle(e => e.Contains("Số tủ"));
    }

    [Fact]
    public void Plan_InvalidSpec_Throws()
    {
        Action act = () => PanelRowPlanner.Plan(new PanelRowSpec(0, 600, 2000), 0, 0);

        act.Should().Throw<ArgumentException>();
    }
}
```

Đây là 4 test method, 5 test case. **Tự viết thêm ít nhất 6 test** (tổng ≥ 10 test case). Gợi ý những thứ đáng kiểm: `TotalWidth` có và không có khe hở; biên `MaxPanels` (30 hợp lệ, 31 không); biên `MinWidth` (300 hợp lệ, 299.9 không); khe hở âm; tiền tố rỗng; tên thứ 10 là `P10`; `MinY`/`MaxY` khi `originY` khác 0; spec có **ba** lỗi thì `Validate()` trả đủ ba; `spec` null.

### Definition of Done

- [ ] `dotnet build` **0 warning**, `dotnet test` xanh toàn bộ.
- [ ] `PanelRowSpec` và `PanelCell` không có `set`; `PanelRowPlanner` là `static class`.
- [ ] `PanelRow.Core` không có `Console.WriteLine` và không có kiểu nào của AutoCAD.
- [ ] Thông điệp lỗi đúng **nguyên văn** bảng (mentor kiểm bằng test riêng).
- [ ] ≥ 10 test case, tên theo kiểu `Method_TìnhHuống_KếtQuả`.
- [ ] Làm trên branch, mở **PR** với mô tả đủ ba phần ([§4](#4-git-và-pull-request)); mentor review và bạn sửa theo góp ý ít nhất một vòng.

> Tài liệu **không có lời giải** đồ án — đây là bài đánh giá. Mentor chấm thêm bằng một bộ **test ẩn** (không công bố), nên hãy test kỹ các giá trị biên. Kẹt: đọc lại `GridSpec.cs` và `GridPlanner.cs` — đồ án có cùng hình dạng.

---

## 6. Đánh giá nhập môn → Fresher

**Điều kiện lên 🌱 Fresher** (cũng ghi trong [T10 §4](T10_LABS_AND_ASSESSMENT.md#4-đề-đánh-giá-lên-cấp)):
1. Làm xong bài tập C01–C05 (tự so đáp án).
2. Đồ án §5 đạt DoD, PR đã qua review.
3. **Vấn đáp** với mentor: trả lời đạt **≥ 7/10** câu dưới đây, **không xem tài liệu**, giải thích bằng lời của mình.
4. **Thực hành tại chỗ** (20 phút): mentor sửa một luật trong đồ án của bạn khiến test fail; bạn đọc thông báo, tìm nguyên nhân, sửa cho xanh lại.

| # | Câu hỏi | Ôn ở |
| :---: | :--- | :--- |
| 1 | Giải thích từng mảnh của `public int CellCount => Rows * Columns;`. | C01 §8, C03 §2 |
| 2 | Vì sao một hàm nhận `Point3d` không làm đổi được điểm của bạn, còn nhận `Line` thì được? | C02 §1 |
| 3 | `GridSpec.Validate()` trả về `IReadOnlyList<string>`. Vì sao không phải `List<string>`, và vì sao không ném lỗi ngay ở lỗi đầu tiên? | C02 §5, C03 §9 |
| 4 | Đọc to và giải thích: `_cadContext = cadContext ?? throw new ArgumentNullException(nameof(cadContext));` | C02 §6, C04 §7 |
| 5 | Vì sao `GridMarkerServiceTests` chạy được trên máy không có AutoCAD? | C03 §6, §8 |
| 6 | Trong `TryExecute(scope => DrawCells(scope, spec, cells), …)`, lambda chạy **lúc nào**? Nó "bắt" những biến nào? | C04 §1, §2, §12 |
| 7 | Vì sao `RibbonService` phải gọi `.ToList()` trước khi xóa tab? | C04 §4 |
| 8 | Trong `using (Transaction tr = …) { … tr.Commit(); }`, chuyện gì xảy ra nếu code ném lỗi trước `Commit()`? | C04 §8 |
| 9 | Vì sao `-=` với một lambda viết lại không gỡ được đăng ký sự kiện? | C04 §9 |
| 10 | Sau `await` trong một lệnh AutoCAD, hai điều gì có thể đã khác so với trước `await`? | C05 §1 |

<details><summary>Gợi ý chấm (dành cho mentor)</summary>

1. `public` + kiểu `int` + tên + `=>`: property **tính toán**, tính lại mỗi lần đọc, không lưu.
2. `Point3d` là struct: hàm nhận bản sao. `Line` là class: hàm nhận "địa chỉ" của cùng một đối tượng.
3. Chỉ đọc để người gọi không sửa được kết quả; trả danh sách để palette hiện **mọi** lỗi cùng lúc.
4. Gán field; nếu tham số null thì ném ngay, kèm tên tham số qua `nameof` — lỗi cấu hình DI lộ ra ngay lúc khởi tạo.
5. Service chỉ biết interface `ICadContext`; test đưa vào `FakeCadContext`.
6. Bên trong `TryExecute`, sau khi đã khóa bản vẽ và mở transaction, trước `Commit`; bắt `spec` và `cells`.
7. `Where` thực thi trì hoãn: không chụp trước thì vòng lặp đọc `Tabs` trong lúc xóa khỏi chính nó → `InvalidOperationException`.
8. `Commit` không chạy; transaction bị `Dispose` khi chưa commit → mọi thay đổi bị hủy; khóa bản vẽ vẫn được trả.
9. Mỗi lambda viết ra là một delegate mới; `-=` chỉ gỡ đúng delegate đã `+=` — phải cất vào biến.
10. (a) Thread — nếu có `ConfigureAwait(false)`, `Task.Run` hoặc không có `SynchronizationContext`; (b) bản vẽ active — người dùng có thể đã chuyển bản vẽ (T03 B31).

**Đạt** khi trả lời đúng ý chính, không cần đúng từng chữ. Câu nào chưa đạt: giao lại đúng mục "Ôn ở", hẹn vấn đáp lại câu đó sau một tuần.
</details>

**Đạt:** chúc mừng — bạn là 🌱 **Fresher**. Tiếp theo: [T01 — Nền tảng AutoCAD .NET](T01_AUTOCAD_NET_FOUNDATIONS.md) và [lộ trình Giai đoạn 1–3](README.md#3-lộ-trình-đào-tạo). Từ T01 bạn cần AutoCAD 2024.

---

## ✅ Tự kiểm tra C05

1. Sau `await Task.Delay(100);` trong một chương trình console, code chạy tiếp trên thread nào? Trong một lệnh AutoCAD thì sao?
2. `[Fact]` và `[Theory]` khác nhau thế nào?
3. Một test fail báo `Expected total to be 1950.0, but found 1900.0`. Bạn kết luận ngay là code sai được không?
4. Vì sao phải đọc `git diff` trước khi commit?

<details><summary>Gợi ý</summary>

1. Console không có `SynchronizationContext` → một thread bất kỳ của ThreadPool. Lệnh AutoCAD (main thread có context) → quay về main thread, *trừ khi* có `ConfigureAwait(false)` hoặc đang ở trong `Task.Run`.
2. `[Fact]` là một test cố định; `[Theory]` chạy cùng một test với nhiều bộ dữ liệu `[InlineData]`.
3. Không. Có thể **test** sai (như ví dụ ở §2). Kiểm lại kỳ vọng bằng tay trước.
4. Để chắc chỉ đưa vào commit đúng những thay đổi bạn định — không lẫn file thử, file cá nhân, hay dòng debug.
</details>

---

**Tiếp theo:** [T01 — Nền tảng AutoCAD .NET →](T01_AUTOCAD_NET_FOUNDATIONS.md) · [Về lộ trình](README.md)
