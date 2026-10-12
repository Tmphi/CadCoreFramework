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
    /// </summary>
    public class XxxCommand : CadCommandBase
    {
        /// <summary>
        /// MY_COMMAND — mô tả ngắn gọn công dụng của lệnh (hiện ở Command Line).
        /// </summary>
        [CommandMethod("MY_COMMAND")]
        public void Run()
        {
            ExecuteWithScope(scope =>
            {
                // TODO: logic vẽ ở đây — scope cung cấp Transaction, Database, Editor,
                //       CurrentSpace, cùng với các extension methods DrawLine/DrawRectangle/
                //       EnsureLayer/DrawMText trong CadCoreFramework.Extensions.
                scope.Editor.WriteMessage("\n[XxxCommand] Chạy thành công.");
            }, undoMarkName: "__UNDO_MARK__");
        }
    }
}
