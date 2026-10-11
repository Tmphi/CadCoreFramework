# Phần 6: DevOps, Unit Test & Tiêu Chuẩn (DevOps & Standards)

Phần cuối cùng này hướng dẫn các kỹ thuật bảo trì cấp độ cao (DevOps), cách bắt lỗi và quy chuẩn viết code (Coding Conventions) khắt khe để dự án đạt độ tin cậy Enterprise.

---

## 6.1 Tuyệt Chiêu Hot-Reload WPF ("Lời Nguyền BAML Cache")

WPF dịch các file XAML sang dạng nhị phân (BAML) và load thẳng vào RAM của tiến trình AutoCAD thông qua `PresentationFramework.dll`.
- **Vấn đề:** AutoCAD sử dụng cơ chế Reflection để load các DLL. Khi DLL tên là `MyPlugin.dll` được load vào, BAML của nó bị Cache lại. Nếu bạn thoát Visual Studio, sửa một cái Nút Bấm, Build lại, và đè file mới (cùng tên) vào thư mục CAD, sau đó load lại -> CAD thấy tên Assembly vẫn vậy, nó sẽ lôi cái giao diện cũ rích trong Cache ra để hiện -> Type Mismatch -> Crash.
- **Cách thợ vườn:** Tắt hẳn AutoCAD, build lại, mở AutoCAD (Mất 3 phút cho 1 lần sửa màu chữ).
- **Cách của Framework (Trình bày trong `build.ps1` & `.csproj`):**
  1. Cấu hình MSBuild `<AssemblyName Condition="'$(Configuration)' == 'Debug'">CadCoreFramework_$([System.DateTime]::Now.ToString("yyyyMMdd_HHmmss"))</AssemblyName>`.
  2. Mỗi lần bạn nhấn Build, tên file Assembly thực sự sinh ra trong ruột DLL sẽ khác nhau (dựa trên giờ).
  3. Khi NETLOAD, WPF nhận thấy tên Assembly đã khác, nó sẽ nạp bản BAML mới tinh. Bạn tha hồ chỉnh sửa giao diện XAML liên tục mà không cần tắt AutoCAD.

---

## 6.2 Hướng Dẫn Viết Unit Test (Moq & xUnit)

Không thể gọi trực tiếp `Application.DocumentManager` bên trong một Project Unit Test (vì nó chạy ngoài môi trường AutoCAD, thiếu file C++ lõi).

> [!TIP]
> **Chiến lược Test trong CAD:** Trích xuất toàn bộ Logic toán học, chuỗi (String), và các lệnh điều hướng vào một Service (Kế thừa Interface). Chừa lại phần móc nối vào CAD (`Execute`) ở lớp mỏng nhất. Dùng thư viện `Moq` để làm giả các Service.

**Ví dụ Test một chức năng (Trong project `CadCoreFramework.Tests`):**

```csharp
using Moq;
using Xunit;

public class LayerServiceTests
{
    [Fact]
    public void GetLayers_KiemTraTraVeDungSoLuong()
    {
        // 1. Arrange: Làm giả (Mock) ICadContext để nó không gọi thật xuống AutoCAD
        var mockContext = new Mock<ICadContext>();
        
        // Mô phỏng hàm ExecuteRead chỉ gọi hàm callback giả lập
        mockContext.Setup(c => c.ExecuteRead(It.IsAny<Action<ICadScope>>()))
                   .Callback<Action<ICadScope>>(action => {
                       // Bỏ qua tương tác CAD thật
                   });

        var service = new LayerService(mockContext.Object);

        // 2. Act
        var layers = service.GetLayers();

        // 3. Assert (Luôn thành công dù không có AutoCAD chạy)
        Assert.NotNull(layers);
    }
}
```

---

## 6.3 Quy Chuẩn Viết Code (Coding Conventions)

Là một Senior, mã nguồn không chỉ chạy được mà phải đọc được. Dưới đây là Luật Thép của dự án:

### 1. Quy tắc đặt tên (Naming)

| Loại (Type) | Kiểu Đặt Tên | Ký hiệu (Prefix) | Ví dụ |
| :--- | :--- | :--- | :--- |
| **Class, Method, Property** | `PascalCase` | Không | `CadContext`, `DrawRectangle`, `IsBusy` |
| **Interface** | `PascalCase` | Chữ `I` in hoa | `ICadScope`, `ILoggerService` |
| **Tham số (Parameter) & Biến cục bộ** | `camelCase` | Không | `layerName`, `radius`, `index` |
| **Biến nội bộ (Private Field)** | `camelCase` | Dấu gạch dưới `_` | `_cadContext`, `_logger` |
| **Hàm Bất Đồng Bộ (Async)** | `PascalCase` | Hậu tố `Async` | `ExecuteAsync`, `LoadDataAsync` |

### 2. Cấu Trúc Khung của Một File C# (`.cs`)
Thứ tự khai báo trong một Class bắt buộc phải tuân theo thứ tự sau (từ trên xuống dưới):
1. **Fields**: Các biến nội bộ `private readonly ...`.
2. **Constructors**: Các hàm khởi tạo (Tiêm DI vào đây).
3. **Properties**: Các thuộc tính public.
4. **Public Methods**: Các hàm giao tiếp bên ngoài (API bề mặt).
5. **Private Methods**: Các hàm hỗ trợ ẩn ở dưới cùng.

### 3. Quy Tắc Kiến Trúc
- **Không nhét logic CAD vào ViewModels**: ViewModels (kể cả hàm Command) chỉ lo chuẩn bị dữ liệu và gọi Service. Việc lấy tọa độ (Prompt) và mở Transaction phải đẩy xuống Service (hoặc lớp Command Base).
- **Không dùng `System.Exception` chung chung**: Hãy bắt các loại Exception cụ thể (như `ArgumentNullException`, `InvalidOperationException`). Framework đã bắt riêng `UserBreakException` (khi nhấn ESC) để tránh in lỗi rác ra màn hình.
