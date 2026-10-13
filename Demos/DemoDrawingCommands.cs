using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using CadCoreFramework.Commands;
using CadCoreFramework.Core;
using CadCoreFramework.Extensions;
using CadCoreFramework.Utils;

namespace CadCoreFramework.Demos
{
    /// <summary>
    /// Các lệnh minh họa vẽ đối tượng hình học, chữ viết, bảng biểu và đường chỉ dẫn.
    /// Sử dụng bộ thư viện mở rộng CadScopeDrawingExtensions và CadTableBuilder.
    /// </summary>
    public class DemoDrawingCommands : CadCommandBase
    {
        private INotificationService Notify
            => GetRequiredService<INotificationService>();

        /// <summary>
        /// DEMO_DRAW_SHAPES — Vẽ tổ hợp các hình học cơ bản và nâng cao: Line, Circle, Arc,
        /// Polyline, Rectangle, FilletRectangle, RegularPolygon, Slot, Ellipse, Hatch, AlignedDimension.
        /// Minh họa cơ chế Action callback configure và các phép biến đổi hình học.
        /// </summary>
        [CommandMethod("DEMO_DRAW_SHAPES")]
        public void DemoDrawShapes()
        {
            ExecuteWithScope(scope =>
            {
                Notify.Section("DEMO: VẼ HÌNH HỌC CƠ BẢN VÀ NÂNG CAO");

                const string layer = "DEMO_SHAPES";

                // 1. Tạo Layer chuyên dụng cho demo
                scope.EnsureLayer(layer, colorIndex: 3);
                Notify?.Info("1. Đã tạo Layer 'DEMO_SHAPES' (màu xanh lá).");

                // ── Line: Đoạn thẳng ─────────────────────────────────────────────
                var lineStart = new Point3d(0, 0, 0);
                var lineEnd = new Point3d(100, 0, 0);
                ObjectId lineId = scope.DrawLine(lineStart, lineEnd, layer: layer);
                Notify?.Info($"2. DrawLine: Đoạn thẳng (0,0) → (100,0) | ID: {lineId}");

                // ── Circle: Đường tròn có configure lambda ───────────────────────
                var circleCenter = new Point3d(50, 50, 0);
                ObjectId circleId = scope.DrawCircle(circleCenter, radius: 20, layer: layer, configure: c =>
                {
                    c.LineWeight = LineWeight.LineWeight030; // Chỉnh nét 0.3mm qua Action configure
                });
                Notify?.Info($"3. DrawCircle: Tâm (50,50), R=20 (nét 0.30mm) | ID: {circleId}");

                // ── Arc: Cung tròn qua Tâm, Bán kính, Góc bắt đầu & kết thúc ──────
                ObjectId arcId = scope.DrawArc(
                    center: new Point3d(100, 50, 0),
                    radius: 20,
                    startAngleRad: 0,
                    endAngleRad: Math.PI, // Nửa đường tròn 180°
                    layer: layer
                );
                Notify?.Info($"4. DrawArc: Tâm (100,50), R=20, 0° → 180° | ID: {arcId}");

                // ── Polyline: Đường gấp khúc nhiều đỉnh ───────────────────────────
                var vertices = new[]
                {
                    new Point2d(0, 80),
                    new Point2d(30, 110),
                    new Point2d(60, 80),
                    new Point2d(90, 110),
                    new Point2d(120, 80)
                };
                ObjectId polyId = scope.DrawPolyline(vertices, isClosed: false, constantWidth: 1.0, layer: layer);
                Notify?.Info($"5. DrawPolyline: 5 đỉnh gợn sóng (constantWidth=1.0) | ID: {polyId}");

                // ── Polyline có đoạn cong (Bulge): Hình chữ U bo đáy ─────────────
                var curvedVertices = new[]
                {
                    new PolylineVertex(new Point2d(140, 120)),
                    new PolylineVertex(new Point2d(140, 90), bulge: 1.0), // Bulge = 1.0 tạo nửa đường tròn
                    new PolylineVertex(new Point2d(170, 90)),
                    new PolylineVertex(new Point2d(170, 120))
                };
                ObjectId curvedPolyId = scope.DrawPolyline(curvedVertices, isClosed: false, layer: layer);
                Notify?.Info($"6. DrawPolyline (Curved): Bulge=1.0 tạo cung bo cong | ID: {curvedPolyId}");

                // ── Rectangle: Hình chữ nhật từ 2 điểm góc đối diện ───────────────
                ObjectId rectId = scope.DrawRectangle(
                    new Point3d(0, 130, 0),
                    new Point3d(50, 160, 0),
                    layer: layer,
                    lineWidth: 0.5
                );
                Notify?.Info($"7. DrawRectangle: (0,130) → (50,160) | ID: {rectId}");

                // ── FilletRectangle: Hình chữ nhật bo tròn 4 góc ─────────────────
                ObjectId filletRectId = scope.DrawFilletRectangle(
                    new Point3d(60, 130, 0),
                    new Point3d(120, 160, 0),
                    filletRadius: 5.0,
                    layer: layer
                );
                Notify?.Info($"8. DrawFilletRectangle: R=60x30, bo góc r=5 | ID: {filletRectId}");

                // ── RegularPolygon: Đa giác đều nội tiếp (ví dụ lục giác 6 cạnh) ──
                ObjectId hexId = scope.DrawRegularPolygon(
                    center: new Point3d(150, 145, 0),
                    radius: 15,
                    sides: 6,
                    layer: layer
                );
                Notify?.Info($"9. DrawRegularPolygon: Lục giác đều (6 cạnh) R=15 | ID: {hexId}");

                // ── Slot: Lỗ rãnh then/cơ khí (2 đầu bán nguyệt) ─────────────────
                ObjectId slotId = scope.DrawSlot(
                    center: new Point3d(200, 145, 0),
                    length: 40,
                    width: 16,
                    rotationAngleRad: 0,
                    layer: layer
                );
                Notify?.Info($"10. DrawSlot: Rãnh then 40x16mm | ID: {slotId}");

                // ── Ellipse: Hình Elip ───────────────────────────────────────────
                ObjectId ellipseId = scope.DrawEllipse(
                    center: new Point3d(50, 200, 0),
                    majorAxis: new Vector3d(30, 0, 0), // Trục lớn dài 30 theo trục X
                    minorToMajorRatio: 0.5,             // Trục nhỏ = 30 * 0.5 = 15
                    layer: layer
                );
                Notify?.Info($"11. DrawEllipse: Trục lớn 30, tỷ lệ trục 0.5 | ID: {ellipseId}");

                // ── Hatch: Mặt cắt/vật liệu tô vào hình chữ nhật ─────────────────
                ObjectId hatchId = scope.DrawHatch(
                    boundaryId: rectId,
                    patternName: "ANSI31",
                    scale: 1.0,
                    layer: layer
                );
                Notify?.Info($"12. DrawHatch: Mẫu 'ANSI31' tô vào hình chữ nhật | ID: {hatchId}");

                // ── AlignedDimension: Đo kích thước đoạn thẳng ────────────────────
                ObjectId dimId = scope.DrawAlignedDimension(
                    lineStart,
                    lineEnd,
                    dimLinePoint: new Point3d(50, -15, 0),
                    layer: layer
                );
                Notify?.Info($"13. DrawAlignedDimension: Đo chiều dài Line 100mm | ID: {dimId}");

                // ── Phép biến đổi: Mirror (Lấy đối xứng qua trục) ────────────────
                var lineToMirror = scope.Transaction.GetObject(lineId, OpenMode.ForWrite) as Entity;
                if (lineToMirror != null)
                {
                    // Lấy đối xứng hình chữ nhật qua trục Y (X=0)
                    var rectToMirror = scope.Transaction.GetObject(rectId, OpenMode.ForWrite) as Entity;
                    rectToMirror?.Mirror(new Point3d(0, 0, 0), new Point3d(0, 200, 0));
                    Notify?.Info("14. EntityExtensions.Mirror: Đã lấy đối xứng hình chữ nhật qua trục thẳng đứng.");
                }

                Notify.Section("HOÀN TẤT: Đã vẽ toàn bộ hình học cơ bản & nâng cao trên Layer DEMO_SHAPES");
            });
        }

        /// <summary>
        /// DEMO_DRAW_TEXTS — Vẽ các loại chữ: DBText, MText, MLeader, Table.
        /// Minh họa cách xuất thông tin lên bản vẽ ở nhiều định dạng khác nhau.
        /// </summary>
        [CommandMethod("DEMO_DRAW_TEXTS")]
        public void DemoDrawTexts()
        {
            ExecuteWithScope(scope =>
            {
                Notify.Section("DEMO: VẼ CHỮ VÀ BẢNG");

                const string layer = "DEMO_TEXTS";
                scope.EnsureLayer(layer, colorIndex: 2); // Vàng

                // ── DBText: Chữ đơn dòng ────────────────────────────────────────
                ObjectId textId = scope.DrawText(
                    "DBText — Chữ đơn dòng (height=5)",
                    new Point3d(0, 0, 0),
                    height: 5.0,
                    layer: layer
                );
                Notify?.Info($"1. DrawText (DBText): ID: {textId}");

                // ── DBText: Căn giữa (TextCenter) ───────────────────────────────
                ObjectId textCenterId = scope.DrawText(
                    "DBText — Căn giữa",
                    new Point3d(100, 0, 0),
                    height: 4.0,
                    layer: layer,
                    justification: TextHorizontalMode.TextCenter
                );
                Notify?.Info($"2. DrawText (TextCenter): ID: {textCenterId}");

                // ── MText: Chữ nhiều dòng ────────────────────────────────────────
                ObjectId mtextId = scope.DrawMText(
                    "MText — Chữ nhiều dòng\\P"
                    + "Dòng thứ 2: Hỗ trợ \\Frial|b1;Bold\\Frial|b0; và {\\C1;Màu đỏ}\\P"
                    + "Dòng thứ 3: width = 80",
                    new Point3d(0, -15, 0),
                    textHeight: 3.5,
                    width: 80,
                    layer: layer
                );
                Notify?.Info($"3. DrawMText: ID: {mtextId}");

                // ── MLeader: Mũi tên chỉ dẫn kèm chữ ──────────────────────────
                ObjectId mleaderId = scope.DrawMLeader(
                    "MLeader — Mũi tên chỉ dẫn",
                    arrowPoint: new Point3d(50, 30, 0),  // Đầu mũi tên
                    textPoint: new Point3d(100, 45, 0),  // Vị trí chữ
                    layer: layer
                );
                Notify?.Info($"4. DrawMLeader: ID: {mleaderId}");

                // ── Table: Bảng thống kê (BOM) ──────────────────────────────────
                var tableData = new string[3, 3]
                {
                    { "STT", "Tên thiết bị", "Số lượng" },
                    { "1",   "MCB 1P 16A",   "10" },
                    { "2",   "RCCB 2P 40A",  "5" }
                };
                ObjectId tableId = scope.DrawTable(
                    new Point3d(0, -60, 0),
                    rows: 4, columns: 3,
                    rowHeight: 8.0, colWidth: 40.0,
                    title: "BẢNG VẬT TƯ MẪU",
                    data: tableData,
                    layer: layer
                );
                Notify?.Info($"5. DrawTable: Bảng 4x3 | ID: {tableId}");

                Notify.Section("HOÀN TẤT: Đã vẽ 5 loại chữ/bảng trên Layer DEMO_TEXTS");
            });
        }

        /// <summary>
        /// DEMO_DRAW_TABLE — Minh họa nâng cao tạo bảng vật tư thiết bị (BOM) bằng CadTableBuilder.
        /// Tự động căn lề, định dạng độ rộng cột, tính toán chiều cao và đưa vào bản vẽ.
        /// </summary>
        [CommandMethod("DEMO_DRAW_TABLE")]
        public void DemoDrawTable()
        {
            ExecuteWithScope(scope =>
            {
                Notify.Section("DEMO: CADTABLEBUILDER — TẠO BẢNG DỮ LIỆU KỸ THUẬT");

                const string layer = "DEMO_TABLES";
                scope.EnsureLayer(layer, colorIndex: 4); // Cyan

                // Sử dụng CadTableBuilder theo chuẩn Fluent API
                var table = CadTableBuilder.Create()
                    .SetTitle("BẢNG THỐNG KÊ THIẾT BỊ TỦ ĐIỆN PHÂN PHỐI DB-01")
                    .SetPosition(new Point3d(0, 0, 0))
                    .SetRowHeight(7.0)
                    .SetTextHeight(2.5)
                    .AddColumn("STT", 15)
                    .AddColumn("MÃ VẬT TƯ", 35)
                    .AddColumn("DIỄN GIẢI THIẾT BỊ", 75)
                    .AddColumn("THÔNG SỐ", 30)
                    .AddColumn("ĐVT", 15)
                    .AddColumn("SỐ LƯỢNG", 20)
                    .AddRow("1", "EZ9F34116", "Aptomat tép MCB 1 cực Acti9", "1P 16A 4.5kA", "Cái", "12")
                    .AddRow("2", "EZ9F34120", "Aptomat tép MCB 1 cực Acti9", "1P 20A 4.5kA", "Cái", "8")
                    .AddRow("3", "EZ9F34232", "Aptomat tép MCB 2 cực Acti9", "2P 32A 4.5kA", "Cái", "3")
                    .AddRow("4", "LV429630",  "Aptomat khối MCCB 3 cực Compact NSX", "3P 100A 36kA", "Cái", "1")
                    .AddRow("5", "A9R71240",  "Chống dòng rò RCCB 2 cực Acti9", "2P 40A 30mA", "Cái", "3")
                    .Build();

                table.Layer = layer;
                ObjectId tableId = scope.AppendEntityToCurrentSpace(table);

                Notify?.Info($"✔ Đã tạo bảng thành công với CadTableBuilder | ID: {tableId}");
                Notify?.Info("  → Bảng gồm 5 cột, 5 dòng dữ liệu, định dạng tiêu đề và Header tự động.");

                Notify.Section("HOÀN TẤT: DEMO TẠO BẢNG NÂNG CAO");
            });
        }

        /// <summary>
        /// DEMO_DRAW_LEADER — Minh họa vẽ các đường dẫn chú thích (MLeader) chỉ vào các vị trí thiết bị.
        /// </summary>
        [CommandMethod("DEMO_DRAW_LEADER")]
        public void DemoDrawLeader()
        {
            ExecuteWithScope(scope =>
            {
                Notify.Section("DEMO: VẼ ĐƯỜNG DẪN CHÚ THÍCH (MLEADER)");

                const string layer = "DEMO_LEADER";
                scope.EnsureLayer(layer, colorIndex: 1); // Đỏ

                // 1. Chú thích nguồn điện chính
                var id1 = scope.DrawMLeader(
                    "Cáp nguồn tổng 3x50mm² + 1x25mm² Cu/XLPE/PVC",
                    arrowPoint: new Point3d(0, 0, 0),
                    textPoint: new Point3d(40, 25, 0),
                    layer: layer
                );

                // 2. Chú thích vị trí tiếp địa
                var id2 = scope.DrawMLeader(
                    "Cọc tiếp địa mạ đồng D16 x 2.4m, Rđ < 4Ω",
                    arrowPoint: new Point3d(100, 0, 0),
                    textPoint: new Point3d(140, 25, 0),
                    layer: layer
                );

                Notify?.Info($"✔ Đã tạo 2 MLeader chỉ dẫn: {id1}, {id2}");

                Notify.Section("HOÀN TẤT: DEMO MLEADER");
            });
        }
    }
}
