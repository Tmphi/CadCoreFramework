using System.Windows;
using Autodesk.AutoCAD.ApplicationServices;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace CadCoreFramework.Core
{
    /// <summary>
    /// Triển khai dịch vụ IWindowManagerService.
    /// Sử dụng API Autodesk.AutoCAD.ApplicationServices.Application để hiển thị các cửa sổ WPF chuẩn xác theo AutoCAD.
    /// </summary>
    public class WindowManagerService : IWindowManagerService
    {
        /// <inheritdoc/>
        public void ShowModelessWindow(Window window)
        {
            if (window == null) return;
            Application.ShowModelessWindow(window);
        }

        /// <inheritdoc/>
        public bool? ShowModalDialog(Window window)
        {
            if (window == null) return null;
            return Application.ShowModalWindow(window);
        }

        /// <inheritdoc/>
        public void ShowMessageBox(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
