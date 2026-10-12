using System;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using CadCoreFramework.Commands;
using CadCoreFramework.Core;
using CadCoreFramework.Utils;

// [assembly: CommandClass] được khai báo tập trung trong Properties/AssemblyInfo.cs

namespace CadCoreFramework.Samples.GridMarker
{
    /// <summary>
    /// GOLDEN SAMPLE — ĐIỂM VÀO COMMAND (command context).
    ///
    /// Luồng chuẩn của một lệnh AutoCAD:
    ///   1. Thu thập input bằng prompt — TRƯỚC khi mở transaction/lock
    ///      (prompt trong transaction sẽ giữ lock vô thời hạn khi user chần chừ).
    ///   2. Validate bằng GridSpec.Validate() — cùng luật với Palette.
    ///   3. Tính toán bằng GridPlanner (logic thuần).
    ///   4. Giao cho Service vẽ (một transaction, một bước Undo).
    ///   5. Báo kết quả.
    ///
    /// Bọc trong ExecuteCommand để mọi exception được in ra, ghi log và lưu CommandHistory;
    /// ESC (prompt trả về null) kết thúc lệnh êm thấm.
    /// </summary>
    public class GridMarkerCommands : CadCommandBase
    {
        /// <summary>
        /// SAMPLE_GRID — Vẽ lưới ô đánh nhãn tại điểm người dùng chọn.
        /// </summary>
        [CommandMethod("SAMPLE_GRID")]
        public void DrawGrid()
        {
            ExecuteCommand(() =>
            {
                // GetRequiredService ném exception (được ExecuteCommand bắt) nếu DI chưa sẵn sàng.
                var service = GetRequiredService<IGridMarkerService>();
                var notify = GetRequiredService<INotificationService>();

                // ── 1. Input (ngoài transaction) ─────────────────────────────────
                Point3d? origin = CadPromptHelper.GetPoint("Chọn tâm ô đầu tiên (A1):");
                if (origin == null) return;

                int? rows = CadPromptHelper.GetInteger("Số hàng", defaultValue: 3, lowerLimit: 1, upperLimit: GridSpec.MaxDimension);
                if (rows == null) return;

                int? columns = CadPromptHelper.GetInteger("Số cột", defaultValue: 4, lowerLimit: 1, upperLimit: GridSpec.MaxDimension);
                if (columns == null) return;

                double? spacing = CadPromptHelper.GetDouble("Khoảng cách tâm–tâm", defaultValue: 20);
                if (spacing == null) return;

                // ── 2. Validate ─────────────────────────────────────────────────
                var spec = new GridSpec(rows.Value, columns.Value, spacing.Value,
                    cellSize: Math.Min(15, spacing.Value), textHeight: Math.Min(4, spacing.Value));

                var errors = spec.Validate();
                if (errors.Count > 0)
                {
                    foreach (var error in errors) notify.Warn(error);
                    return;
                }

                // ── 3. Tính toán (logic thuần) ──────────────────────────────────
                var cells = GridPlanner.Plan(spec, origin.Value.X, origin.Value.Y);

                // ── 4. Vẽ ───────────────────────────────────────────────────────
                CadResult<int> result = service.Draw(spec, cells);

                // Ném lại để ExecuteCommand ghi nhận lệnh THẤT BẠI (log + history).
                if (!result.IsSuccess)
                    throw new InvalidOperationException($"Không vẽ được lưới: {result.ErrorMessage}", result.Exception);

                // ── 5. Báo kết quả ──────────────────────────────────────────────
                notify.Info($"Đã vẽ {result.Value} ô trên layer '{spec.LayerName}'. Ctrl+Z để hoàn tác toàn bộ.");
            });
        }
    }
}
