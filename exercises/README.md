# 🧪 Bài tập chấm tự động

Bài tập của [C01–C04](../docs/training/C00_START_HERE.md) và phần ViewModel của [U01-E1](../docs/training/U01_WPF_MVVM_BASICS.md#12-bài-tập), có sẵn test chấm. Làm xong một bài, chạy một lệnh là biết đúng hay sai, và sai ở đâu. Không cần AutoCAD; chỉ cần .NET SDK 8 trở lên ([C00 §3](../docs/training/C00_START_HERE.md#3-chuẩn-bị-môi-trường)).

## Cách làm

1. Đọc đề trong tài liệu (mục **Bài tập** cuối mỗi module). Đề tóm tắt cũng nằm ở đầu file bài làm.
2. Mở file bài làm, ví dụ [`Exercises/C01/E3.cs`](Exercises/C01/E3.cs), thay dòng `throw new NotImplementedException();` bằng bài làm của bạn.
3. Chạy test **của riêng bài đó** (từ thư mục gốc của repo):

   ```powershell
   dotnet test exercises/Exercises.Tests --filter FullyQualifiedName~C01.E3
   ```

4. Đỏ thì đọc thông báo: FluentAssertions ghi rõ mong gì, nhận gì, và nhiều test có ghi thêm lý do (`because …`). Sửa rồi chạy lại cho tới khi xanh.

Chạy mọi bài của một module: `--filter FullyQualifiedName~C02`. Chạy tất cả: bỏ `--filter`. Trong Visual Studio: mở `exercises/Exercises.sln`, dùng **Test Explorer**.

> [!TIP]
> Test đỏ với `NotImplementedException` nghĩa là bạn **chưa làm** bài đó, không phải làm sai. Lúc mới bắt đầu, gần như mọi test đều đỏ; đó là bình thường.

## Danh sách bài

| Bài | File bài làm | Test kiểm gì |
| :--- | :--- | :--- |
| C01-E1 | `C01/E1.cs` | Output đúng hai dòng, 2 chữ số thập phân |
| C01-E2 | `C01/E2.cs` | Bảng nhãn 4 × 5 |
| C01-E3 | `C01/E3.cs` | `ToRowLetters`: 10 trường hợp, rồi so với `GridPlanner.ToRowLetters` **thật** trên 20.000 giá trị |
| C01-E4 | `C01/E4.cs` | Thông báo lỗi giống hệt `GridSpec.Validate` **thật** trên 169 cặp (hàng, cột) |
| C02-E1 | `C02/E1.cs` | Lọc tên: từng ký tự cấm, tên rỗng/chỉ dấu cách, giữ thứ tự |
| C02-E2 | `C02/E2.cs` | Đếm theo layer bằng `Dictionary` |
| C02-E3 | `C02/E3.cs` | `Validate` trả danh sách lỗi, giống `GridSpec.Validate` thật, **kể cả số âm** |
| C03-E1 | `C03/E1.cs` | `MaxHeight`, `IsTall`, luật chiều cao; ba luật cũ giữ nguyên |
| C03-E2 | `C03/E2.cs` | `Triangle : Shape`: diện tích, tên, `Describe()`, dòng in thêm |
| C03-E3 | `C03/E3.cs` | Repository, `Summary()`, và `PanelReport` dùng đúng repository **giả** mà test truyền vào |
| C04-E1 | `C04/E1.cs` | Bốn câu LINQ, với danh sách mẫu và danh sách khác |
| C04-E2 | `C04/E2.cs` | `IsValidSymbolName`, so với `SymbolNameRules.IsValid` thật (độ dài 255/256, từng ký tự ASCII) |
| C04-E3 | `C04/E3.cs` | Event: phần trăm, `sender`, gỡ đăng ký, không ai đăng ký |
| U01-E1 | `U01/E1.cs` | ViewModel form tủ điện: `2,5`, ô trống, biên 1/30, mỗi lỗi một dòng, cập nhật theo từng phím |

Phần XAML của U01-E1, cùng U01-E2/E3, vẫn làm và chấm bằng mắt trên project `HocWpf` như tài liệu. Đồ án nhập môn ([C05 §5](../docs/training/C05_ASYNC_TESTING_GIT.md#5-đồ-án-nhập-môn-planner-dãy-tủ-điện)) giữ cách cũ: project riêng, mentor chấm bằng test ẩn.

## Quy tắc

- **Đừng sửa test** để cho xanh. Test là đề bài viết bằng code.
- File bài làm có sẵn chữ ký hàm, class (để test biên dịch được). Đừng đổi tên hay tham số. Muốn thêm hàm phụ, field, `using` thì cứ thêm.
- Đáp án nằm trong tài liệu (khối thu gọn) và trong [`LoiGiai/`](LoiGiai/). **Làm trước, xem sau.**
- Máy định dạng Việt Nam in `1,5` thay cho `1.5`. Test tạm đặt định dạng bất biến khi chạy bài của bạn, nên bạn không bị chấm sai vì máy ([C01 §5](../docs/training/C01_FIRST_PROGRAM.md#5-chuỗi-và-định-dạng-số--bẫy-định-dạng-việt-nam)).

## Dành cho người bảo trì

- **Đổi đề hay đáp án trong tài liệu:** sửa luôn file bài làm, `LoiGiai/` và test tương ứng.
- **CI kiểm chính bộ bài tập ở mọi PR** ([`tools/docs/check-exercises.mjs`](../tools/docs/check-exercises.mjs), job `docs`):
  - với lời giải mẫu, mọi test phải xanh;
  - với code khởi đầu, **mỗi** bài phải có ít nhất một test đỏ, nếu không thì test của bài đó không kiểm được gì;
  - mỗi file bài làm phải có đúng một lớp test.
- **Chạy tay:**
  - với lời giải mẫu: `dotnet test exercises/Exercises.sln -p:UseSolutions=true`;
  - cả ba bước kiểm như CI: `node tools/docs/check-exercises.mjs`.
- **Version package** đi cùng repo chính: xUnit, FluentAssertions 7.x và CommunityToolkit.Mvvm giống `CadCoreFramework.Tests` và plugin. Dependabot quét cả thư mục này.
- **Ba file "bản thật"** được link vào project test: `GridSpec.cs`, `GridPlanner.cs`, `SymbolNameRules.cs`. Đổi câu chữ hay luật trong đó thì test bài tập đổi theo, và lời giải mẫu phải sửa cho khớp.
