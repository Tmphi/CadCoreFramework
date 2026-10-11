using System;
using System.Windows;
using System.Windows.Interop;
using Autodesk.AutoCAD.ApplicationServices;

namespace CadCoreFramework.Utils
{
    /// <summary>
    /// Các phương thức tiện ích hỗ trợ tích hợp và hiển thị cửa sổ WPF bên trong môi trường AutoCAD.
    /// </summary>
    public static class WpfCadHelper
    {
        /// <summary>
        /// Hiển thị một cửa sổ WPF dưới dạng hộp thoại Modal.
        /// Tự động thiết lập Parent của cửa sổ là AutoCAD để tránh việc cửa sổ bị ẩn ra sau lưng AutoCAD.
        /// </summary>
        /// <param name="window">Cửa sổ WPF cần hiển thị.</param>
        /// <returns>True/False tùy theo kết quả đóng cửa sổ.</returns>
        public static bool? ShowDialogUnderCad(Window window)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));

            // Handle của cửa sổ chính AutoCAD Application
            var cadHandle = Autodesk.AutoCAD.ApplicationServices.Application.MainWindow.Handle;

            // Gán owner handle cho cửa sổ WPF
            var helper = new WindowInteropHelper(window)
            {
                Owner = cadHandle
            };

            return window.ShowDialog();
        }

        /// <summary>
        /// Hiển thị một cửa sổ WPF dưới dạng Modeless (Không chiếm quyền điều khiển, vẫn có thể click vào màn hình CAD).
        /// Tự động thiết lập Parent của cửa sổ là AutoCAD.
        /// </summary>
        /// <param name="window">Cửa sổ WPF cần hiển thị.</param>
        public static void ShowUnderCad(Window window)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));

            var cadHandle = Autodesk.AutoCAD.ApplicationServices.Application.MainWindow.Handle;
            var helper = new WindowInteropHelper(window)
            {
                Owner = cadHandle
            };

            window.Show();
        }
    }
}
