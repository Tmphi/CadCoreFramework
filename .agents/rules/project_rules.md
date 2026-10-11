---
name: Dự án CadCoreFramework (AutoCAD Plugin)
description: Hướng dẫn cốt lõi khi AI làm việc với dự án CadCoreFramework. Áp dụng cho mọi câu hỏi và mọi đoạn code sinh ra.
---

# CadCoreFramework AI Guidelines

Tài liệu này định nghĩa cấu trúc, nguyên tắc và quy chuẩn kỹ thuật của dự án AutoCAD Plugin **CadCoreFramework**. Bạn (AI) PHẢI tuân thủ các quy tắc này trong mọi trường hợp.

## 1. Kiến Trúc Cốt Lõi (Core Architecture)

Dự án này sử dụng kiến trúc đóng gói các API thô của AutoCAD để đảm bảo an toàn Thread, quản lý Giao dịch (Transaction) và Quản lý tài nguyên.

- **KHÔNG BAO GIỜ** sử dụng `Document.LockDocument()` hoặc `TransactionManager.StartTransaction()` thủ công.
- **LUÔN LUÔN** tương tác với AutoCAD thông qua interface `ICadContext`.
- Mọi hàm đọc/ghi vào Database đều phải thực hiện trong một khối lambda nhận vào đối tượng `ICadScope`.
  - Để thay đổi bản vẽ: Sử dụng `_cadContext.Execute(scope => { ... }, autoRegen, undoMarkName)`
  - Để đọc dữ liệu (Read-Only): Sử dụng `_cadContext.ExecuteRead(scope => { ... })`
  - Để tương tác bất đồng bộ: Sử dụng `_cadContext.ExecuteAsync(...)`
- Không sử dụng `Application.DocumentManager.MdiActiveDocument` bên trong các hàm xử lý dữ liệu. Luôn dùng `scope.Document`, `scope.Database`, `scope.Editor` được cung cấp từ `ICadScope`.

## 2. Tiện Ích Vẽ (Drawing Extensions)

Để tạo mới hoặc chỉnh sửa Entity, **bắt buộc** dùng các Extension Methods có sẵn của `ICadScope` thay vì khởi tạo Entity và gọi `AppendEntity` thủ công. Các class này nằm trong `CadCoreFramework.Extensions`:
- Layer/Style: `scope.EnsureLayer()`, `scope.EnsureTextStyle()`, `scope.EnsureDimStyle()`.
- Line/Shape: `scope.DrawLine()`, `scope.DrawPolyline()`, `scope.DrawRectangle()`, `scope.DrawCircle()`, v.v.
- Text/Dim: `scope.DrawMText()`, `scope.DrawText()`, `scope.DrawAlignedDimension()`, v.v.
- Blocks/Hatch: `scope.InsertBlock()`, `scope.DrawHatch()`, `scope.DrawTable()`.
- User Input: `scope.AskPoint()`, `scope.AskString()`, `scope.AskEntity()`.

## 3. Tạo Lệnh AutoCAD (AutoCAD Commands)

- Mọi class định nghĩa lệnh AutoCAD (`[CommandMethod]`) phải được đặt trong thư mục `Commands/`.
- Các class lệnh **bắt buộc kế thừa** `CadCommandBase`.
- Bên trong hàm lệnh, hãy dùng `ExecuteWithScope(scope => { ... })` (được thừa kế từ Base) để chạy logic thay vì gọi try/catch thủ công.
- Hỗ trợ Constructor Injection (inject `ILoggerService`, `ICadContext`,...). Không khởi tạo tay các Service.

## 4. MVVM & Giao Diện (WPF UI)

- Dự án sử dụng bộ công cụ `CommunityToolkit.Mvvm`.
- Tất cả các ViewModels phải kế thừa từ `ViewModelBase` (chứa sẵn `IsBusy`, `ErrorMessage`, `Title`).
- Không bao giờ gọi AutoCAD API trực tiếp từ ViewModel. Inject `ICadContext` vào ViewModel và thao tác qua nó.
- Nếu có thao tác từ UI (Command/Button Click) chạy Async, bắt buộc dùng hàm `RunSafeAsync(async () => { ... })` để tự động kích hoạt trạng thái loading spinner (`IsBusy`).
- Giao tiếp giữa các ViewModel (hoặc giữa Palette và Document) bằng `IEventAggregator` (không refer chéo nhau).

## 5. Quy Chuẩn Phát Triển & Testing

- **Xử lý Lỗi**: Trả về object `CadResult<T>` hoặc `CadResult` thay vì ném ra Exceptions cho các thao tác logic nghiệp vụ. Sử dụng lớp `Guard` (`Guard.NotNull`, `Guard.NotEmpty`) để validate tham số đầu vào.
- **Dependency Injection**: Mọi Service/ViewModel mới phải được đăng ký trong `AppBootstrapper.cs`.
- **Build**: Tuyệt đối không gợi ý người dùng chạy `dotnet build`. Khuyên người dùng dùng PowerShell script: `.\build.ps1` (hoặc `.\build.ps1 -RunTests`) vì file DLL có thể bị AutoCAD lock.
- **Cấu trúc thư mục**: 
  - `Commands/`: Chứa các lệnh Cad.
  - `Core/`: Logic nền tảng (CadContext, EventAggregator).
  - `Extensions/`: Các class tiện ích mở rộng cho ICadScope.
  - `Services/`: Các lớp nghiệp vụ.
  - `ViewModels/` và `Views/`: Mã nguồn UI WPF.
  - `Utils/`: Lớp Helpers, Guards.
- Tất cả mã nguồn C# đều phải dùng format 4 khoảng trắng, CRLF. Field private sử dụng format `_camelCase`.
