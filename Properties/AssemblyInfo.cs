using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// Cho phép dự án kiểm thử truy cập các thành viên internal (ví dụ: SetServiceProviderForTesting)
// mà không cần phải đặt quyền public làm ảnh hưởng API sản phẩm.
[assembly: InternalsVisibleTo("CadCoreFramework.Tests")]

// Thông tin chung về assembly được quản lý thông qua tập hợp các thuộc tính sau.
// Thay đổi giá trị các thuộc tính này để cập nhật thông tin tương ứng.
[assembly: AssemblyTitle("CadCoreFramework")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("CadCoreFramework")]
[assembly: AssemblyCopyright("Copyright ©  2026")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Đặt ComVisible thành false làm các kiểu dữ liệu trong assembly này không hiển thị đối với các thành phần COM.
// Nếu cần truy xuất một kiểu dữ liệu từ COM, hãy đặt thuộc tính ComVisible thành true trên kiểu đó.
[assembly: ComVisible(false)]

// GUID sau đây đại diện cho ID của typelib nếu dự án này được hiển thị cho COM
[assembly: Guid("32077c6a-6136-4ad8-bf65-d8235959acf1")]

// Thông tin phiên bản của assembly bao gồm 4 giá trị:
//
//      Phiên bản chính (Major Version)
//      Phiên bản phụ (Minor Version)
//      Số hiệu bản dựng (Build Number)
//      Bản sửa đổi (Revision)
//
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

// CA1416: Suppress Windows-platform warnings on .NET 8 — this plugin only targets AutoCAD on Windows.
// The two marker lines keep this conditional block intact in projects created with `dotnet new` (the
// template engine otherwise treats preprocessor directives as template conditions — unknown symbol =
// false — and drops the block). Never write a hash sign directly followed by "if" inside a .cs comment:
// the engine scans comments too and fails with "Index was out of range".
//-:cnd:noEmit
#if NET
[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
//+:cnd:noEmit

// ═══════════════════════════════════════════════════════════════════════════════
//  ĐĂNG KÝ PLUGIN AUTOCAD (AUTOCAD PLUGIN REGISTRATION)
//
//  ExtensionApplication: Khai báo lớp điểm vào (entry-point) của plugin.
//  AutoCAD sẽ gọi IExtensionApplication.Initialize() khi plugin được nạp qua lệnh NETLOAD.
//
//  CommandClass: Khai báo MỌI class có chứa phương thức xử lý [CommandMethod].
//  Nếu thiếu khai báo này, AutoCAD sẽ không tự tìm thấy lệnh khi người dùng gõ trên Command Line.
//
//  QUAN TRỌNG: Mọi class mới chứa [CommandMethod] BẮT BUỘC phải được đăng ký ở đây.
//  Thêm một dòng [assembly: CommandClass(typeof(...))] cho mỗi class lệnh mới.
// ═══════════════════════════════════════════════════════════════════════════════

// Lưu ý: GenerateAssemblyInfo=false trong file .csproj có nghĩa tệp này là nguồn thông tin chính.
// Khuyến nghị tập trung các thuộc tính ExtensionApplication và CommandClass vào một tệp duy nhất để dễ quản lý.
[assembly: Autodesk.AutoCAD.Runtime.ExtensionApplication(
    typeof(CadCoreFramework.Core.AppBootstrapper))]

[assembly: Autodesk.AutoCAD.Runtime.CommandClass(
    typeof(CadCoreFramework.Commands.SampleCommands))]

// ─── Thêm một mục CommandClass cho mỗi mô-đun lệnh mới tại đây ──────────────
[assembly: Autodesk.AutoCAD.Runtime.CommandClass(
    typeof(CadCoreFramework.Commands.LayerCommands))]

// Bộ lệnh demo (DEMO_*) tự đăng ký CommandClass trong Demos/DemoAssemblyInfo.cs —
// build với /p:IncludeDemos=false là toàn bộ demo biến mất khỏi DLL (xem CadCoreFramework.csproj).

// Golden Sample — xem docs/reference/R05_GOLDEN_SAMPLE.md
[assembly: Autodesk.AutoCAD.Runtime.CommandClass(
    typeof(CadCoreFramework.Samples.GridMarker.GridMarkerCommands))]
