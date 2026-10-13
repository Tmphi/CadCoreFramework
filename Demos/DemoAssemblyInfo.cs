// ═══════════════════════════════════════════════════════════════════════════════
//  ĐĂNG KÝ LỆNH DEMO (DEMO_*)
//
//  Tách khỏi Properties/AssemblyInfo.cs để cả thư mục Demos/ tự chứa: khi build với
//  /p:IncludeDemos=false, csproj loại bỏ Demos/** → các lệnh demo và các dòng đăng ký
//  dưới đây cùng biến mất, không để lại typeof() trỏ tới class không tồn tại.
//
//  Dự án thật dựng từ template: đặt <IncludeDemos>false</IncludeDemos> trong csproj
//  (hoặc xóa hẳn thư mục Demos/) — không cần sửa file nào khác.
// ═══════════════════════════════════════════════════════════════════════════════

[assembly: Autodesk.AutoCAD.Runtime.CommandClass(
    typeof(CadCoreFramework.Demos.DemoCommands))]

[assembly: Autodesk.AutoCAD.Runtime.CommandClass(
    typeof(CadCoreFramework.Demos.DemoDrawingCommands))]

[assembly: Autodesk.AutoCAD.Runtime.CommandClass(
    typeof(CadCoreFramework.Demos.DemoInteractionCommands))]

[assembly: Autodesk.AutoCAD.Runtime.CommandClass(
    typeof(CadCoreFramework.Demos.DemoDataCommands))]

[assembly: Autodesk.AutoCAD.Runtime.CommandClass(
    typeof(CadCoreFramework.Demos.DemoServicesCommands))]

[assembly: Autodesk.AutoCAD.Runtime.CommandClass(
    typeof(CadCoreFramework.Demos.DemoEventCommands))]

[assembly: Autodesk.AutoCAD.Runtime.CommandClass(
    typeof(CadCoreFramework.Demos.DemoOverruleCommands))]

[assembly: Autodesk.AutoCAD.Runtime.CommandClass(
    typeof(CadCoreFramework.Demos.DemoUiCommands))]
