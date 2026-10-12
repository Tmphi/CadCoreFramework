using Autodesk.AutoCAD.Runtime;
using CadCoreFramework.Commands;
using CadCoreFramework.Core;

// ─────────────────────────────────────────────────────────────────────────────
// SAU KHI tạo file này, ĐỪNG QUÊN thêm dòng sau vào Properties/AssemblyInfo.cs:
//
//   [assembly: Autodesk.AutoCAD.Runtime.CommandClass(
//       typeof(CadCoreFramework.Commands.XxxCommand))]
//
// Thiếu CommandClass → AutoCAD không scan thấy lệnh khi NETLOAD.
// ─────────────────────────────────────────────────────────────────────────────

namespace CadCoreFramework.Commands
{
    /// <summary>
    /// XxxCommand — kế thừa CadCommandBase để tự động:
    ///   • bắt lỗi (chuyển exception thành dòng log + WriteMessage)
    ///   • phân biệt UserBreak (ESC) với exception thực
    ///   • ghi lại Stopwatch + success vào ICommandHistoryService
    ///
    /// Tính năng thật (có Service/ViewModel/test): copy khuôn Samples/GridMarker —
    /// xem docs/09_GOLDEN_SAMPLE.md.
    /// </summary>
    public class XxxCommand : CadCommandBase
    {
        /// <summary>
        /// MY_COMMAND — mô tả ngắn gọn công dụng của lệnh (hiện ở Command Line).
        /// </summary>
        [CommandMethod("MY_COMMAND")]
        public void Run()
        {
            ExecuteCommand(() =>
            {
                // 1. Lấy service (fail-loud nếu DI chưa sẵn sàng):
                //    var service = GetRequiredService<IMyService>();

                // 2. Prompt người dùng TRƯỚC khi mở transaction (CadPromptHelper.GetPoint…).
                //    Prompt trả về null = user nhấn ESC → return.

                // 3. Validate + tính toán bằng code thuần (không AutoCAD API).

                // 4. Ghi vào bản vẽ — qua service, hoặc trực tiếp cho lệnh nhỏ:
                CadContext.Execute(scope =>
                {
                    // scope cung cấp Transaction, Database, Editor, CurrentSpace và các
                    // extension DrawLine/DrawRectangle/EnsureLayer/DrawMText (CadCoreFramework.Extensions).
                    scope.Editor.WriteMessage("\n[XxxCommand] Chạy thành công.");
                }, undoMarkName: "__UNDO_MARK__");
            });
        }
    }
}
