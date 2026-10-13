using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace CadCoreFramework.Utils
{
    /// <summary>
    /// Centralized accessor cho AutoCAD Application-level APIs.
    /// Tập trung tất cả Autodesk.AutoCAD.ApplicationServices.Core.Application.*
    /// vào một nơi duy nhất để tránh lặp code và dễ mock khi unit test.
    ///
    /// TRIẾT LÝ AUTODESK:
    /// - System Variables kiểm soát hành vi AutoCAD (DARKMODE, UNITS, MEASUREMENT...).
    /// - Luôn dùng GetSystemVariable/SetSystemVariable thay vì thao tác trực tiếp.
    /// - DARKMODE: 0 = Light, 1 = Dark. Thay đổi theo user settings.
    /// - Tất cả accessor phải check null trước khi trả về để tránh crash khi
    ///   gọi trước khi AutoCAD khởi động hoàn tất.
    /// </summary>
    public static class CadApplicationHelper
    {
        // ─── Document ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Bản vẽ đang được active. Có thể null nếu không có bản vẽ nào mở.
        /// LUÔN kiểm tra null trước khi dùng.
        /// </summary>
        public static Document ActiveDocument
            => Application.DocumentManager.MdiActiveDocument;

        /// <summary>
        /// Database của bản vẽ đang active. Shortcut an toàn.
        /// </summary>
        public static Database ActiveDatabase
            => Application.DocumentManager.MdiActiveDocument?.Database;

        /// <summary>
        /// DocumentManager — quản lý tất cả bản vẽ đang mở.
        /// </summary>
        public static DocumentCollection Documents
            => Application.DocumentManager;

        // ─── System Variables ─────────────────────────────────────────────────────

        /// <summary>
        /// Lấy giá trị của một System Variable theo tên.
        /// Trả về null nếu tên sysvar không hợp lệ hoặc có lỗi.
        /// </summary>
        public static object GetSystemVariable(string sysVarName)
        {
            if (string.IsNullOrWhiteSpace(sysVarName)) return null;
            try { return Application.GetSystemVariable(sysVarName); }
            catch (Exception) { return null; }
        }

        /// <summary>
        /// Lấy giá trị System Variable dưới dạng kiểu T.
        /// Ví dụ: GetSystemVariable&lt;int&gt;("COLORTHEME") → 0 (tối) hoặc 1 (sáng).
        /// </summary>
        public static T GetSystemVariable<T>(string sysVarName, T defaultValue = default)
        {
            var value = GetSystemVariable(sysVarName);
            if (value == null) return defaultValue;
            try { return (T)Convert.ChangeType(value, typeof(T)); }
            catch (Exception) { return defaultValue; }
        }

        /// <summary>
        /// Đặt giá trị cho một System Variable.
        /// Trả về true nếu thành công, false nếu có lỗi (ví dụ: SYSVAR read-only).
        /// </summary>
        public static bool SetSystemVariable(string sysVarName, object value)
        {
            if (string.IsNullOrWhiteSpace(sysVarName)) return false;
            try
            {
                Application.SetSystemVariable(sysVarName, value);
                return true;
            }
            catch (Exception) { return false; }
        }

        // ─── Dark Mode ────────────────────────────────────────────────────────────

        /// <summary>Tên sysvar quyết định theme giao diện AutoCAD: 0 = tối (mặc định), 1 = sáng.</summary>
        public const string ColorThemeVariable = "COLORTHEME";

        /// <summary>
        /// Kiểm tra giao diện AutoCAD đang ở theme TỐI hay không — đọc sysvar <c>COLORTHEME</c>.
        /// (AutoCAD KHÔNG có sysvar "DARKMODE": đọc nó trả về null — đã kiểm chứng trên AutoCAD 2024,
        /// docs/training/T03 A13.)
        /// </summary>
        public static bool IsDarkMode
            => IsDarkColorTheme(GetSystemVariable(ColorThemeVariable));

        /// <summary>
        /// Quy đổi giá trị <c>COLORTHEME</c>: 1 = sáng; 0 = tối. Thiếu hoặc lạ → tối (mặc định của AutoCAD).
        /// Hàm thuần — unit test được.
        /// </summary>
        public static bool IsDarkColorTheme(object colorTheme)
        {
            if (colorTheme == null) return true;
            try { return Convert.ToInt32(colorTheme, System.Globalization.CultureInfo.InvariantCulture) != 1; }
            catch (Exception) { return true; }
        }

        // ─── Units & Measurement ──────────────────────────────────────────────────

        /// <summary>
        /// Lấy đơn vị đo lường hiện tại (UNITS sysvar).
        /// 1 = Scientific, 2 = Decimal, 3 = Engineering, 4 = Architectural, 5 = Fractional.
        /// </summary>
        public static int CurrentUnits
            => GetSystemVariable<int>("LUNITS", 2);

        /// <summary>
        /// Kiểm tra đơn vị đo lường: 0 = Imperial (feet/inches), 1 = Metric.
        /// </summary>
        public static bool IsMetric
            => GetSystemVariable<int>("MEASUREMENT", 1) == 1;

        // ─── Application Window ───────────────────────────────────────────────────

        /// <summary>
        /// Handle của cửa sổ chính AutoCAD. Dùng để set owner cho WPF dialog.
        /// </summary>
        public static IntPtr MainWindowHandle
            => Application.MainWindow?.Handle ?? IntPtr.Zero;

        // ─── Utility Methods ──────────────────────────────────────────────────────

        /// <summary>
        /// Ghi thông báo vào Command Line của AutoCAD.
        /// An toàn — không crash nếu không có document active.
        /// Luôn bắt đầu bằng ký tự newline nếu chưa có.
        /// </summary>
        public static void WriteToCommandLine(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            var msg = message.StartsWith("\n") ? message : "\n" + message;
            ActiveDocument?.Editor.WriteMessage(msg);
        }

        /// <summary>
        /// Chạy một lệnh AutoCAD theo chương trình, tương tự gõ tay vào Command Line.
        /// Chỉ dùng từ command context — KHÔNG dùng từ event handler.
        /// </summary>
        /// <param name="command">Tên lệnh (ví dụ: "ZOOM", "REGEN", "ZOOM E ").</param>
        public static void SendCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command)) return;
            string cmd = command.EndsWith(" ") || command.EndsWith("\n")
                ? command
                : command + " ";
            ActiveDocument?.SendStringToExecute(cmd, true, false, false);
        }
    }
}
