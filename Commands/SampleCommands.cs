using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using CadCoreFramework.Core;
using CadCoreFramework.Extensions;

// [assembly: CommandClass] được khai báo tập trung trong Properties/AssemblyInfo.cs

namespace CadCoreFramework.Commands
{
    /// <summary>
    /// Lớp lệnh mẫu thực thi các tính năng cốt lõi của framework.
    /// Kế thừa CadCommandBase để tự động bắt lỗi và truy cập an toàn
    /// vào Editor và Document mà không cần kiểm tra null lặp lại.
    /// </summary>
    public class SampleCommands : CadCommandBase
    {
        /// <summary>Dịch vụ quản lý Palette của AutoCAD.</summary>
        private IPaletteService PaletteService
            => GetRequiredService<IPaletteService>();

        // ─── Danh Sách Lệnh ───────────────────────────────────────────────────

        /// <summary>
        /// CORE_INFO — Kiểm tra plugin đã được tải và DI container hoạt động bình thường.
        /// Gõ "CORE_INFO" trên dòng lệnh AutoCAD.
        /// </summary>
        [CommandMethod("CORE_INFO")]
        public void CoreInfoCommand()
        {
            ExecuteCommand(() =>
            {
                Editor?.WriteMessage("\n[CadCoreFramework] Hạ tầng plugin đã sẵn sàng.");
            });
        }

        /// <summary>
        /// SHOW_PALETTE — Bật/Tắt palette chính (Golden Sample: Grid Marker).
        /// Gõ "SHOW_PALETTE" trên dòng lệnh AutoCAD.
        /// </summary>
        [CommandMethod("SHOW_PALETTE")]
        public void ShowPaletteCommand()
        {
            ExecuteCommand(() =>
            {
                PaletteService.TogglePalette(
                    "CadCoreFrameworkPalette",
                    () => new Samples.GridMarker.GridMarkerPalette(),
                    tabTitle: "Grid Marker"
                );
            });
        }

        /// <summary>
        /// SHOW_UI_TEMPLATE — Bật/Tắt thanh công cụ trưng bày các control WPF.
        /// Gõ "SHOW_UI_TEMPLATE" trên dòng lệnh AutoCAD.
        /// </summary>
        [CommandMethod("SHOW_UI_TEMPLATE")]
        public void ShowUiTemplate()
        {
            ExecuteCommand(() =>
            {
                PaletteService.TogglePalette(
                    "UiTemplateId",
                    () => new Views.CommonControlsPalette(),
                    tabTitle: "UI Template (WPF)"
                );
            });
        }

        /// <summary>
        /// CORE_DEBUG — Mở Debug Inspector Palette (xem Command History,
        /// DI Services, Event Subscribers live). Gõ "CORE_DEBUG" trong AutoCAD.
        /// </summary>
        [CommandMethod("CORE_DEBUG")]
        public void ShowDebugInspector()
        {
            ExecuteCommand(() =>
            {
                PaletteService.TogglePalette(
                    "CadCoreFrameworkDebugInspector",
                    () => new Views.DebugInspectorPaletteControl(),
                    tabTitle: "Debug Inspector"
                );
            });
        }

        /// <summary>
        /// CORE_TEST_ALL — Kiểm tra toàn bộ API vẽ hình, layer, XData và thông báo.
        /// Gõ "CORE_TEST_ALL" trên dòng lệnh AutoCAD.
        /// </summary>
        [CommandMethod("CORE_TEST_ALL")]
        public void CoreTestAllCommand()
        {
            ExecuteWithScope(scope =>
            {
                var notify = GetRequiredService<INotificationService>();
                notify.Section("KIỂM TRA HẠ TẦNG CADCOREFRAMEWORK");

                // 1. Tự động tạo Layer
                ObjectId layerId = scope.EnsureLayer("CORE_TEST", colorIndex: 1);
                notify?.Info($"Đã tạo Layer 'CORE_TEST': {layerId}");

                // 2. Vẽ hình chữ nhật
                var p1 = new Point3d(0, 0, 0);
                var p2 = new Point3d(200, 100, 0);
                ObjectId rectId = scope.DrawRectangle(p1, p2, layer: "CORE_TEST", lineWidth: 2.0);
                notify?.Info($"Đã vẽ hình chữ nhật (ID: {rectId})");

                // 3. Vẽ MText (chữ nhiều dòng)
                ObjectId textId = scope.DrawMText(
                    "CadCoreFramework — Infrastructure OK",
                    new Point3d(10, 50, 0),
                    textHeight: 8.0,
                    layer: "CORE_TEST"
                );
                notify?.Info($"Đã vẽ MText (ID: {textId})");

                // 4. Vẽ DBText (chữ một dòng)
                ObjectId dbtextId = scope.DrawText(
                    "DBText OK",
                    new Point3d(10, 30, 0),
                    height: 5.0,
                    layer: "CORE_TEST"
                );
                notify?.Info($"Đã vẽ DBText (ID: {dbtextId})");

                // 5. Ghi và đọc lại metadata JSON trong XData
                var entity = scope.Transaction.GetObject(rectId, OpenMode.ForWrite) as Entity;
                if (entity != null)
                {
                    var metadata = new { Project = "CadCoreFramework", Version = "2.0", Author = "Senior CAD Engineer" };
                    entity.SetMetadata(scope.Transaction, "CAD_CORE_METADATA", metadata);
                    notify?.Info("Đã ghi metadata JSON vào XData.");

                    var readBack = entity.GetMetadata<dynamic>("CAD_CORE_METADATA");
                    if (readBack != null)
                        notify?.Info($"Đọc lại XData thành công: Version={readBack.Version}, Project={readBack.Project}");
                }

                // Chỉ in khi mọi bước trên không ném exception (smoke.scr dựa vào log này).
                notify.Section("TẤT CẢ BÀI TEST HẠ TẦNG ĐÃ VƯỢT QUA");
            });
        }

        /// <summary>
        /// DRAW_CIRCLE_SAMPLE — Ví dụ tối giản: vẽ một đường tròn tại gốc tọa độ.
        /// Gõ "DRAW_CIRCLE_SAMPLE" trên dòng lệnh AutoCAD.
        /// </summary>
        [CommandMethod("DRAW_CIRCLE_SAMPLE")]
        public void DrawCircleSample()
        {
            ExecuteWithScope(scope =>
            {
                ObjectId circleId = scope.DrawCircle(new Point3d(0, 0, 0), radius: 10);
                GetRequiredService<INotificationService>().Info($"Đã vẽ đường tròn (ID: {circleId})");
            });
        }
    }
}
