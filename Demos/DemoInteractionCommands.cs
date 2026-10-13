using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using CadCoreFramework.Commands;
using CadCoreFramework.Core;
using CadCoreFramework.Extensions;
using CadCoreFramework.Utils;
using CadCoreFramework.Utils.Jigs;

namespace CadCoreFramework.Demos
{
    /// <summary>
    /// Các lệnh minh họa tương tác với người dùng qua Command Line và Editor:
    /// Nhập liệu, lọc chọn đối tượng (SelectionFilter), và hiệu ứng kéo thả động (Jig).
    /// </summary>
    public class DemoInteractionCommands : CadCommandBase
    {
        private INotificationService Notify
            => GetRequiredService<INotificationService>();

        /// <summary>
        /// DEMO_PROMPT — Minh họa toàn bộ các hàm yêu cầu người dùng nhập liệu:
        /// GetPoint, GetString, GetInteger, GetDouble, GetKeywords, GetDistance, GetEntity.
        /// </summary>
        [CommandMethod("DEMO_PROMPT")]
        public void DemoPrompt()
        {
            ExecuteCommand(() =>
            {
                Notify.Section("DEMO: CADPROMPTHELPER — NHẬP LIỆU TỪ NGƯỜI DÙNG");

                // 1. GetPoint — Chọn điểm trên màn hình
                var pt = CadPromptHelper.GetPoint("Chọn một điểm bất kỳ trên bản vẽ:");
                if (pt == null) { Notify?.Warn("Đã hủy tại GetPoint."); return; }
                Notify?.Info($"1. GetPoint: ({pt.Value.X:F2}, {pt.Value.Y:F2}, {pt.Value.Z:F2})");

                // 2. GetString — Nhập chuỗi ký tự
                var text = CadPromptHelper.GetString("Nhập tên thiết bị:", defaultVal: "MCB 1P 16A");
                if (text == null) { Notify?.Warn("Đã hủy tại GetString."); return; }
                Notify?.Info($"2. GetString: \"{text}\"");

                // 3. GetInteger — Nhập số nguyên (có giới hạn)
                var num = CadPromptHelper.GetInteger("Nhập số lượng (1-100):", defaultValue: 10, lowerLimit: 1, upperLimit: 100);
                if (num == null) { Notify?.Warn("Đã hủy tại GetInteger."); return; }
                Notify?.Info($"3. GetInteger: {num.Value}");

                // 4. GetDouble — Nhập số thực
                var val = CadPromptHelper.GetDouble("Nhập công suất (kW):", defaultValue: 2.5);
                if (val == null) { Notify?.Warn("Đã hủy tại GetDouble."); return; }
                Notify?.Info($"4. GetDouble: {val.Value:F2} kW");

                // 5. GetKeywords — Chọn từ khóa
                var kw = CadPromptHelper.GetKeywords(
                    "Chọn kiểu lắp đặt",
                    new[] { "Horizontal", "Vertical", "Diagonal" },
                    defaultKeyword: "Horizontal"
                );
                if (kw == null) { Notify?.Warn("Đã hủy tại GetKeywords."); return; }
                Notify?.Info($"5. GetKeywords: \"{kw}\"");

                // 6. GetDistance — Đo khoảng cách giữa 2 điểm
                var dist = CadPromptHelper.GetDistance("Đo khoảng cách (click 2 điểm):", pt);
                if (dist == null) { Notify?.Warn("Đã hủy tại GetDistance."); return; }
                Notify?.Info($"6. GetDistance: {dist.Value:F2} đơn vị");

                // 7. GetEntity — Chọn một đối tượng
                var entId = CadPromptHelper.GetEntity("Chọn một đối tượng trên bản vẽ:");
                if (entId == ObjectId.Null) { Notify?.Warn("Đã hủy tại GetEntity."); return; }
                Notify?.Info($"7. GetEntity: ObjectId = {entId}, ObjectClass = {entId.ObjectClass.Name}");

                Notify.Section("HOÀN TẤT: ĐÃ THỬ TOÀN BỘ CADPROMPTHELPER");
            });
        }

        /// <summary>
        /// DEMO_SELECTION_FILTER — Minh họa cách dùng SelectionFilterBuilder (Fluent API)
        /// để lọc chọn đối tượng theo: Type, Layer, Color, XData.
        /// </summary>
        [CommandMethod("DEMO_SELECTION_FILTER")]
        public void DemoSelectionFilter()
        {
            ExecuteCommand(() =>
            {
                Notify.Section("DEMO: SELECTIONFILTERBUILDER — LỌC ĐỐI TƯỢNG");

                // ── Ví dụ 1: Lọc tất cả LINE và CIRCLE ─────────────────────────
                Notify?.Info("--- Ví dụ 1: Lọc tất cả LINE và CIRCLE ---");
                var filter1 = SelectionFilterBuilder.Create()
                    .OfTypes("LINE", "CIRCLE")
                    .Build();

                var ids1 = CadPromptHelper.GetSelection("Chọn các đối tượng (chỉ LINE và CIRCLE):", filter1);
                Notify?.Info($"  → Đã chọn {ids1.Length} đối tượng (LINE/CIRCLE).");

                // ── Ví dụ 2: Lọc Block INSERT trên layer cụ thể ─────────────────
                Notify?.Info("--- Ví dụ 2: Lọc INSERT trên layer 'DEMO_*' ---");
                var filter2 = SelectionFilterBuilder.Create()
                    .OfTypes("INSERT")
                    .OnLayers("DEMO_*")  // Wildcard — chọn mọi layer bắt đầu bằng DEMO_
                    .Build();

                var ids2 = CadPromptHelper.GetSelection("Chọn các Block trên layer DEMO_*:", filter2);
                Notify?.Info($"  → Đã chọn {ids2.Length} block(s) trên layer DEMO_*.");

                // ── Ví dụ 3: Lọc đối tượng theo màu ─────────────────────────────
                Notify?.Info("--- Ví dụ 3: Lọc đối tượng màu đỏ (color = 1) ---");
                var filter3 = SelectionFilterBuilder.Create()
                    .WithColor(1) // 1 = Đỏ
                    .Build();

                var ids3 = CadPromptHelper.GetSelection("Chọn các đối tượng màu đỏ:", filter3);
                Notify?.Info($"  → Đã chọn {ids3.Length} đối tượng màu đỏ.");

                // ── Ví dụ 4: Lọc đối tượng có XData ─────────────────────────────
                Notify?.Info("--- Ví dụ 4: Lọc đối tượng có XData 'DEMO_METADATA' ---");
                var filter4 = SelectionFilterBuilder.Create()
                    .WithXData("DEMO_METADATA")
                    .Build();

                var ids4 = CadPromptHelper.GetSelection("Chọn đối tượng có XData DEMO_METADATA:", filter4);
                Notify?.Info($"  → Đã chọn {ids4.Length} đối tượng có XData.");

                // ── Ví dụ 5: Kết hợp nhiều điều kiện (AND) ──────────────────────
                Notify?.Info("--- Ví dụ 5: Kết hợp LINE + layer DEMO_SHAPES ---");
                var filter5 = SelectionFilterBuilder.Create()
                    .OfTypes("LINE")
                    .OnLayers("DEMO_SHAPES")
                    .Build();

                var ids5 = CadPromptHelper.GetSelection("Chọn LINE trên layer DEMO_SHAPES:", filter5);
                Notify?.Info($"  → Đã chọn {ids5.Length} LINE(s) trên DEMO_SHAPES.");

                Notify.Section("HOÀN TẤT: ĐÃ MINH HỌA 5 CÁCH LỌC VỚI SELECTIONFILTERBUILDER");
            });
        }

        /// <summary>
        /// DEMO_JIG_RECTANGLE — Vẽ hình chữ nhật bằng RectangleJig (kéo thả trực quan).
        /// Người dùng click điểm đầu, sau đó kéo chuột để xác định kích thước.
        /// </summary>
        [CommandMethod("DEMO_JIG_RECTANGLE")]
        public void DemoJigRectangle()
        {
            ExecuteCommand(() =>
            {
                Notify.Section("DEMO: RECTANGLEJIG — VẼ HÌNH CHỮ NHẬT BẰNG TƯƠNG TÁC");

                // 1. Lấy điểm góc thứ nhất (TRƯỚC khi mở transaction)
                var corner1 = CadPromptHelper.GetPoint("Chọn góc thứ nhất của hình chữ nhật:");
                if (corner1 == null) { Notify?.Warn("Đã hủy."); return; }

                // 2. Tạo Polyline tạm để Jig vẽ lên
                var polyline = new Polyline();
                polyline.SetDatabaseDefaults();

                // 3. Tạo RectangleJig và chạy Editor.Drag()
                var jig = new RectangleJig(corner1.Value, polyline, "\nChọn góc đối diện: ");
                var dragResult = Editor.Drag(jig);

                if (dragResult.Status != PromptStatus.OK)
                {
                    polyline.Dispose();
                    Notify?.Warn("Đã hủy Jig.");
                    return;
                }

                // 4. Jig thành công → Mở transaction để thêm Polyline vào bản vẽ
                CadContext.Execute(scope =>
                {
                    scope.EnsureLayer("DEMO_JIGS", colorIndex: 3);
                    polyline.Layer = "DEMO_JIGS";
                    scope.AppendEntityToCurrentSpace(polyline);

                    Notify?.Info($"Đã vẽ hình chữ nhật:");
                    Notify?.Info($"  Góc 1: ({jig.Corner1.X:F2}, {jig.Corner1.Y:F2})");
                    Notify?.Info($"  Góc 2: ({jig.Corner2.X:F2}, {jig.Corner2.Y:F2})");
                    Notify?.Info($"  Kích thước: {jig.Width:F2} x {jig.Height:F2}");
                });

                Notify.Section("HOÀN TẤT: DEMO RECTANGLEJIG");
            });
        }

        /// <summary>
        /// DEMO_JIG_BLOCK — Đặt Block bằng BlockPlacementJig (xem trước vị trí realtime).
        /// Tự tạo một Block đơn giản nếu chưa tồn tại trong bản vẽ,
        /// sau đó cho phép người dùng kéo thả để đặt Block.
        /// </summary>
        [CommandMethod("DEMO_JIG_BLOCK")]
        public void DemoJigBlock()
        {
            ExecuteCommand(() =>
            {
                Notify.Section("DEMO: BLOCKPLACEMENTJIG — ĐẶT BLOCK BẰNG TƯƠNG TÁC");

                const string blockName = "DEMO_SYMBOL";

                // 1. Đảm bảo block definition tồn tại (tạo mới nếu chưa có)
                CadContext.Execute(scope =>
                {
                    var bt = (BlockTable)scope.Transaction.GetObject(
                        scope.Database.BlockTableId, OpenMode.ForWrite);

                    if (!bt.Has(blockName))
                    {
                        // Tạo block đơn giản: hình tròn + dấu thập
                        var btr = new BlockTableRecord { Name = blockName };
                        bt.Add(btr);
                        scope.Transaction.AddNewlyCreatedDBObject(btr, true);

                        // Vẽ hình tròn R=5
                        var circle = new Circle(Point3d.Origin, Vector3d.ZAxis, 5);
                        btr.AppendEntity(circle);
                        scope.Transaction.AddNewlyCreatedDBObject(circle, true);

                        // Vẽ dấu thập (2 đường thẳng)
                        var line1 = new Line(new Point3d(-5, 0, 0), new Point3d(5, 0, 0));
                        btr.AppendEntity(line1);
                        scope.Transaction.AddNewlyCreatedDBObject(line1, true);

                        var line2 = new Line(new Point3d(0, -5, 0), new Point3d(0, 5, 0));
                        btr.AppendEntity(line2);
                        scope.Transaction.AddNewlyCreatedDBObject(line2, true);

                        Notify?.Info($"Đã tạo Block Definition '{blockName}' (hình tròn + dấu thập).");
                    }
                });

                // 2. Tạo BlockReference tạm (chưa thêm vào DB) cho Jig
                BlockReference blockRef = null;

                CadContext.Execute(scope =>
                {
                    var bt = (BlockTable)scope.Transaction.GetObject(
                        scope.Database.BlockTableId, OpenMode.ForRead);

                    if (!bt.Has(blockName))
                    {
                        Notify?.Error($"Block '{blockName}' không tồn tại.");
                        return;
                    }

                    blockRef = new BlockReference(Point3d.Origin, bt[blockName]);
                    blockRef.SetDatabaseDefaults(scope.Database);
                });

                if (blockRef == null)
                {
                    Notify?.Error("Không thể tạo BlockReference.");
                    return;
                }

                // 3. Chạy BlockPlacementJig (NGOÀI transaction)
                var jig = new BlockPlacementJig(blockRef, "\nChọn vị trí đặt Block: ");
                var result = Editor.Drag(jig);

                if (result.Status != PromptStatus.OK)
                {
                    blockRef.Dispose();
                    Notify?.Warn("Đã hủy đặt Block.");
                    return;
                }

                // 4. Thêm BlockReference vào bản vẽ
                CadContext.Execute(scope =>
                {
                    scope.EnsureLayer("DEMO_JIGS", colorIndex: 3);
                    blockRef.Layer = "DEMO_JIGS";
                    scope.AppendEntityToCurrentSpace(blockRef);

                    Notify?.Info($"Đã đặt Block '{blockName}' tại ({jig.Position.X:F2}, {jig.Position.Y:F2}).");
                });

                Notify.Section("HOÀN TẤT: DEMO BLOCKPLACEMENTJIG");
            });
        }
    }
}
