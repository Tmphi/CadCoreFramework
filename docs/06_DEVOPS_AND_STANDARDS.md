# Phần 6: DevOps, Unit Test & Tiêu Chuẩn (DevOps & Standards)

Phần cuối cùng này hướng dẫn các kỹ thuật bảo trì cấp độ cao (DevOps), cách bắt lỗi và quy chuẩn viết code (Coding Conventions) khắt khe để dự án đạt độ tin cậy Enterprise.

---

## 6.1 Tuyệt Chiêu Hot-Reload WPF ("Lời Nguyền BAML Cache")

WPF dịch các file XAML sang dạng nhị phân (BAML) và load thẳng vào RAM của tiến trình AutoCAD thông qua `PresentationFramework.dll`.
- **Vấn đề:** AutoCAD sử dụng cơ chế Reflection để load các DLL. Khi DLL tên là `MyPlugin.dll` được load vào, BAML của nó bị Cache lại. Nếu bạn thoát Visual Studio, sửa một cái Nút Bấm, Build lại, và đè file mới (cùng tên) vào thư mục CAD, sau đó load lại -> CAD thấy tên Assembly vẫn vậy, nó sẽ lôi cái giao diện cũ rích trong Cache ra để hiện -> Type Mismatch -> Crash.
- **Cách thợ vườn:** Tắt hẳn AutoCAD, build lại, mở AutoCAD (Mất 3 phút cho 1 lần sửa màu chữ).
- **Cách của Framework (cấu hình trong `.csproj`):**
  1. MSBuild tự sinh `AssemblyName` kèm Timestamp khi build Debug: `CadCoreFramework_20261010_120000.dll`.
  2. PostBuild event copy file đó về tên chuẩn `CadCoreFramework.dll` để NETLOAD script vẫn dùng được.
  3. Khi NETLOAD, WPF nhận thấy tên Assembly đã khác, nó sẽ nạp bản BAML mới tinh. Bạn tha hồ chỉnh sửa giao diện XAML liên tục mà không cần tắt AutoCAD.

Build bình thường bằng:
```powershell
dotnet build CadCoreFramework.sln -c Debug
```

---

## 6.2 Hướng Dẫn Viết Unit Test

Không thể gọi trực tiếp `Application.DocumentManager` bên trong một Project Unit Test (vì nó chạy ngoài môi trường AutoCAD, thiếu file C++ lõi).

> [!TIP]
> **Chiến lược Test trong CAD:** Trích xuất toàn bộ Logic toán học, chuỗi (String), và các lệnh điều hướng vào một Service (kế thừa Interface). Chừa lại phần móc nối vào CAD (`Execute`) ở lớp mỏng nhất. Dùng **`FakeCadContext`** (từ project `CadCoreFramework.Testing`) để kiểm soát và kiểm tra các lời gọi CAD.

### Cách 1: FakeCadContext (khuyến nghị cho Service tests)

`FakeCadContext` là một test-double đã có sẵn trong project `CadCoreFramework.Testing`. Nó ghi nhận mọi lời gọi mà không cần runtime AutoCAD.

```csharp
using CadCoreFramework.Testing.Fakes;
using FluentAssertions;
using Xunit;

public class PanelDrawServiceTests
{
    [Fact]
    public void DrawPanel_CallsExecuteOnce_WithCorrectUndoMarkName()
    {
        // Arrange
        var fake = new FakeCadContext();
        var service = new PanelDrawService(fake);

        // Act
        service.DrawPanel("MSB-01");

        // Assert
        fake.ExecuteCount.Should().Be(1);
        fake.LastUndoMarkName.Should().Be("DrawPanel");
    }

    [Fact]
    public void DrawPanel_WhenCadThrows_ServiceHandlesGracefully()
    {
        // Arrange — inject exception to simulate AutoCAD error
        var fake = new FakeCadContext { ExceptionToThrow = new InvalidOperationException("CAD error") };
        var service = new PanelDrawService(fake);

        // Act & Assert
        service.Invoking(s => s.DrawPanel("MSB-01")).Should().Throw<InvalidOperationException>();
    }
}
```

### Cách 2: Moq (cho interface-only mocking)

Dùng `Moq` khi bạn cần mock các interface khác (không phải `ICadContext`):

```csharp
using Moq;
using Xunit;

public class LayerServiceTests
{
    [Fact]
    public void GetLayers_ReturnsNonNull()
    {
        var mockContext = new Mock<ICadContext>();
        mockContext.Setup(c => c.ExecuteRead(It.IsAny<Action<ICadScope>>()));

        var service = new LayerService(mockContext.Object);
        var layers = service.GetLayers();

        Assert.NotNull(layers);
    }
}
```

### Chạy test và xem báo cáo coverage

```powershell
# Chạy test với coverage
dotnet test CadCoreFramework.Tests\CadCoreFramework.Tests.csproj --collect:"XPlat Code Coverage" --results-directory coverage

# Tạo báo cáo HTML
dotnet tool restore
dotnet reportgenerator -reports:"coverage/**/coverage.cobertura.xml" -targetdir:"coverage/report" -reporttypes:"Html"

# Mở báo cáo
Start-Process coverage\report\index.html
```

---

## 6.3 CI/CD — GitHub Actions

Dự án có hai workflow trong `.github/workflows/`:

| File | Kích hoạt | Mục đích |
| :--- | :--- | :--- |
| `build-and-test.yml` | Push/PR vào `main` | Build Release `-warnaserror` → chạy toàn bộ unit test trên net48 + net8 → thu thập coverage → upload báo cáo HTML |
| `release.yml` | Tag `v*` (ví dụ `v1.2.0`) | Build → Test → `package.ps1` → tạo GitHub Release với `.bundle` đính kèm |

**Badge CI** đã có sẵn ở đầu `README.md`. Dự án tạo từ template nhận đúng repo của bạn khi truyền `--GitHubRepo owner/name` (`dotnet new`) hoặc `-GitHubRepo owner/name` (`init-template.ps1`); bỏ trống thì badge là placeholder `YOUR_ORG/YOUR_REPO` — thay bằng tay sau.

---

## 6.4 Quy Chuẩn Viết Code (Coding Conventions)

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
