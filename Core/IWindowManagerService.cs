using System.Windows;

namespace CadCoreFramework.Core
{
    /// <summary>
    /// Giao diện dịch vụ quản lý hiển thị cửa sổ (Window) WPF trong AutoCAD.
    /// Giúp hiển thị cửa sổ Modal hoặc Modeless với owner chính xác là cửa sổ AutoCAD.
    /// </summary>
    public interface IWindowManagerService
    {
        /// <summary>Hiển thị cửa sổ WPF dưới dạng Modeless (không chiếm quyền điều khiển AutoCAD).</summary>
        void ShowModelessWindow(Window window);

        /// <summary>Hiển thị cửa sổ WPF dưới dạng Modal Dialog (chặn tương tác với AutoCAD cho đến khi đóng).</summary>
        bool? ShowModalDialog(Window window);

        /// <summary>Hiển thị hộp thoại thông báo đơn giản (MessageBox).</summary>
        void ShowMessageBox(string message, string title);
    }
}
