using System;
using System.Threading.Tasks;
using System.Windows;
using Autodesk.AutoCAD.Windows;
using CadCoreFramework.ViewModels;

namespace CadCoreFramework.Core
{
    /// <summary>
    /// Service quản lý các thanh công cụ neo (Dockable Palette) của AutoCAD.
    /// </summary>
    public interface IPaletteService
    {
        /// <summary>
        /// Lấy hoặc tạo một PaletteSet. Dùng Guid ổn định để AutoCAD lưu vị trí qua các lần load.
        /// </summary>
        PaletteSet GetOrCreatePaletteSet(string name, Guid? guid = null,
            DockSides defaultDock = DockSides.Right);

        /// <summary>Hiển thị một Palette với content WPF.</summary>
        void ShowPalette(string paletteName, FrameworkElement content,
            string tabTitle = null, DockSides defaultDock = DockSides.Right);

        /// <summary>
        /// Hiển thị Palette với content WPF và gắn IBusyIndicator để RunWithBusyAsync quản lý.
        /// </summary>
        void ShowPalette(string paletteName, FrameworkElement content,
            IBusyIndicator busyIndicator, string tabTitle = null,
            DockSides defaultDock = DockSides.Right);

        /// <summary>Bật/Tắt Palette với Lazy initialization (chỉ tạo View khi mở).</summary>
        void TogglePalette(string paletteName, Func<FrameworkElement> contentFactory,
            string tabTitle = null, DockSides defaultDock = DockSides.Right);

        /// <summary>Ẩn Palette.</summary>
        void HidePalette(string paletteName);

        /// <summary>Kiểm tra Palette có đang hiển thị không.</summary>
        bool IsPaletteVisible(string paletteName);

        /// <summary>
        /// Chạy một task async, tự động bật IsBusy trước và tắt sau khi hoàn thành.
        /// Yêu cầu Palette đã được hiển thị với IBusyIndicator qua ShowPalette overload phù hợp.
        /// </summary>
        Task RunWithBusyAsync(string paletteName, Func<Task> asyncTask);
    }
}
