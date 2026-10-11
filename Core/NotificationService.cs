using System.Windows;
using Autodesk.AutoCAD.ApplicationServices;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace CadCoreFramework.Core
{
    /// <summary>
    /// Dịch vụ hiển thị thông báo.
    /// Giúp định dạng màu sắc và icon cho các log in ra trên cửa sổ dòng lệnh (Command Line) của AutoCAD
    /// tương tự như Toast/Snackbar notification.
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly ILoggerService _logger;

        public NotificationService(ILoggerService logger)
        {
            _logger = logger;
        }

        private static string Editor_Write(string msg)
        {
            Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(msg);
            return msg;
        }

        /// <summary>Hiển thị thông báo thành công / thông tin (Icon dấu tick ✔).</summary>
        public void Info(string message)
        {
            Editor_Write($"\n[✔] {message}");
            _logger?.Info(message);
        }

        /// <summary>Hiển thị cảnh báo (Icon dấu chấm than ⚠).</summary>
        public void Warn(string message)
        {
            Editor_Write($"\n[⚠] {message}");
            _logger?.Warning(message);
        }

        /// <summary>Hiển thị báo lỗi (Icon dấu X ✖).</summary>
        public void Error(string message, System.Exception ex = null)
        {
            Editor_Write($"\n[✖] {message}");
            _logger?.Error(message, ex);
        }

        /// <summary>
        /// Hiển thị hộp thoại (Dialog) xác nhận dạng Yes/No.
        /// Thường dùng trước khi thực hiện hành động xóa hoặc ghi đè nguy hiểm.
        /// </summary>
        public bool Confirm(string message, string title = "Xác nhận")
        {
            var result = MessageBox.Show(
                message, title,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }

        /// <summary>In ra một dải phân cách trang trí để làm nổi bật phần tiếp theo.</summary>
        public void Section(string title)
        {
            string border = new string('─', 50);
            Editor_Write($"\n{border}");
            Editor_Write($"\n  {title.ToUpperInvariant()}");
            Editor_Write($"\n{border}");
        }
    }
}
