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
  - Để thay đổi bản vẽ: `_cadContext.Execute(scope => { ... }, autoRegen, undoMarkName)` — NÉM exception khi lỗi.
  - Để thay đổi bản vẽ mà nhận lỗi dạng giá trị: `_cadContext.TryExecute(...)` — trả `CadResult`/`CadResult<T>`, không throw. (`Execute<T>` đã Obsolete.)
  - Để đọc dữ liệu (Read-Only): `_cadContext.ExecuteRead(scope => { ... })` — tự lock khi gọi từ Palette.
  - Để tương tác bất đồng bộ: `_cadContext.ExecuteAsync(...)` — CHỈ khi thật sự có việc async cần chờ (I/O, API); không bọc code đồng bộ trong `Task.FromResult`.
- **Khuôn mẫu chuẩn** cho tính năng mới: `Samples/GridMarker/` (đọc `docs/09_GOLDEN_SAMPLE.md`). Tách logic thuần (không kiểu AutoCAD) ra khỏi lambda của `ICadContext` để unit test được kết quả.
- Không sử dụng `Application.DocumentManager.MdiActiveDocument` bên trong các hàm xử lý dữ liệu. Luôn dùng `scope.Document`, `scope.Database`, `scope.Editor` được cung cấp từ `ICadScope`.

## 2. Tiện Ích Vẽ (Drawing Extensions)

Để tạo mới hoặc chỉnh sửa Entity, **bắt buộc** dùng các Extension Methods có sẵn của `ICadScope` thay vì khởi tạo Entity và gọi `AppendEntity` thủ công. Các class này nằm trong `CadCoreFramework.Extensions`:
- Layer/Style: `scope.EnsureLayer()`, `scope.EnsureTextStyle()`, `scope.EnsureDimStyle()`.
- Line/Shape: `scope.DrawLine()`, `scope.DrawPolyline()`, `scope.DrawRectangle()`, `scope.DrawCircle()`, v.v.
- Text/Dim: `scope.DrawMText()`, `scope.DrawText()`, `scope.DrawAlignedDimension()`, v.v.
- Blocks/Hatch: `scope.InsertBlock()`, `scope.DrawHatch()`, `scope.DrawTable()`.
- User Input: dùng `CadPromptHelper.GetPoint()/GetString()/GetEntity()/...` **TRƯỚC** khi gọi `ICadContext` — KHÔNG prompt bên trong lambda (giữ Transaction + Lock trong lúc chờ user). `scope.Ask*()` đã Obsolete.

## 3. Tạo Lệnh AutoCAD (AutoCAD Commands)

- Lệnh dùng chung đặt trong `Commands/`; lệnh của một tính năng đặt cùng thư mục tính năng (ví dụ `Samples/GridMarker/GridMarkerCommands.cs`).
- Các class lệnh **bắt buộc kế thừa** `CadCommandBase` và được đăng ký `[assembly: CommandClass]` trong `Properties/AssemblyInfo.cs`.
- Luồng chuẩn: `ExecuteCommand(() => { prompt → validate → tính toán → service/CadContext.Execute → báo kết quả })`. Lệnh nhỏ không prompt có thể dùng `ExecuteWithScope(scope => { ... })`.
- Lấy service bằng `GetRequiredService<T>()` (ném lỗi rõ ràng), KHÔNG dùng `AppBootstrapper.ServiceProvider?.GetService<T>()`. Gọi nó bên trong `ExecuteCommand` để lỗi được bắt và ghi log.

## 4. MVVM & Giao Diện (WPF UI)

- Dự án sử dụng bộ công cụ `CommunityToolkit.Mvvm`.
- Tất cả các ViewModels phải kế thừa từ `ViewModelBase` (chứa sẵn `IsBusy`, `ErrorMessage`, `Title`).
- Không bao giờ gọi AutoCAD API (hay `ICadContext`) trực tiếp từ ViewModel. Inject **Service interface** (ví dụ `IGridMarkerService`) và thao tác qua nó — ViewModel phải test được bằng Mock.
- ViewModel không dùng kiểu WPF (`Brush`, `Visibility`…): biểu diễn trạng thái bằng enum/bool, View tự map bằng DataTrigger/Converter.
- Không prompt từ ViewModel (Palette là modeless). Cần pick điểm → cung cấp một lệnh AutoCAD riêng.
- Bọc thao tác từ UI trong `RunSafe(...)` / `RunSafeAsync(...)` để tự quản lý `IsBusy` và `ErrorMessage`. `CanExecute` của lệnh gắn với kết quả validate.
- Subscription EventAggregator đăng ký qua `TrackSubscription(...)` để tự gỡ khi ViewModel bị Dispose.
- Giao tiếp giữa các ViewModel (hoặc giữa Palette và Document) bằng `IEventAggregator` (không refer chéo nhau).

## 5. Quy Chuẩn Phát Triển & Testing

- **Xử lý Lỗi**: Trả về object `CadResult<T>` hoặc `CadResult` thay vì ném ra Exceptions cho các thao tác logic nghiệp vụ. Sử dụng lớp `Guard` (`Guard.NotNull`, `Guard.NotEmpty`) để validate tham số đầu vào.
- **Dependency Injection**: Mọi Service/ViewModel mới phải được đăng ký trong `AppBootstrapper.cs`.
- **Build**: Dùng `dotnet build` để biên dịch. Không dùng `.\build.ps1` (xem `build_rule.md`). Nếu AutoCAD đang giữ lock file DLL, dùng công cụ hot-reload đã được cấu hình sẵn trong csproj (Debug timestamp suffix).
- **Cấu trúc thư mục**: 
  - `Commands/`: Chứa các lệnh Cad.
  - `Core/`: Logic nền tảng (CadContext, EventAggregator).
  - `Extensions/`: Các class tiện ích mở rộng cho ICadScope.
  - `Services/`: Các lớp nghiệp vụ.
  - `ViewModels/` và `Views/`: Mã nguồn UI WPF dùng chung.
  - `Samples/GridMarker/`: Golden Sample — khuôn mẫu một tính năng hoàn chỉnh (gom theo tính năng).
  - `Demos/`: Bộ lệnh minh họa `DEMO_*` (tự đăng ký CommandClass trong `Demos/DemoAssemblyInfo.cs`). Tắt bằng `<IncludeDemos>false</IncludeDemos>` — code lõi KHÔNG được tham chiếu tới `Demos/`.
  - `Utils/`: Lớp Helpers, Guards.
- Tất cả mã nguồn C# đều phải dùng format 4 khoảng trắng, CRLF. Field private sử dụng format `_camelCase`.
