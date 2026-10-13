using System.Windows;
using System.Windows.Controls;
using Autodesk.AutoCAD.Runtime;
using CadCoreFramework.Commands;
using CadCoreFramework.Core;
using CadCoreFramework.Views;

namespace CadCoreFramework.Demos
{
    /// <summary>
    /// Các lệnh minh họa tích hợp giao diện người dùng (WPF UI):
    /// Thanh công cụ neo Dockable Palette, Cửa sổ Modal Dialog và Modeless Window.
    /// </summary>
    public class DemoUiCommands : CadCommandBase
    {
        private INotificationService Notify
            => GetRequiredService<INotificationService>();

        private IPaletteService PaletteService
            => GetRequiredService<IPaletteService>();

        private IWindowManagerService WindowManager
            => GetRequiredService<IWindowManagerService>();

        /// <summary>
        /// DEMO_SHOW_LAYER_MANAGER — Mở hoặc tắt thanh Palette Quản lý Layer chuẩn MVVM.
        /// </summary>
        [CommandMethod("DEMO_SHOW_LAYER_MANAGER")]
        public void DemoShowLayerManager()
        {
            ExecuteCommand(() =>
            {
                PaletteService.TogglePalette(
                    "LayerManagerPaletteId",
                    () => new LayerManagerControl(),
                    tabTitle: "Quản Lý Layer"
                );
            });
        }

        /// <summary>
        /// DEMO_SHOW_WINDOW_DIALOG — Minh họa mở cửa sổ WPF Modal Dialog với Owner chuẩn xác là AutoCAD Window.
        /// Ngăn chặn việc cửa sổ bị chìm ra sau lưng AutoCAD (WindowInteropHelper).
        /// </summary>
        [CommandMethod("DEMO_SHOW_WINDOW_DIALOG")]
        public void DemoShowWindowDialog()
        {
            ExecuteCommand(() =>
            {
                // Tạo cửa sổ WPF đơn giản trực tiếp bằng C# code
                var window = new Window
                {
                    Title = "CadCoreFramework — Modal Dialog Demo",
                    Width = 420,
                    Height = 240,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.NoResize
                };

                var stack = new StackPanel { Margin = new Thickness(20) };
                stack.Children.Add(new TextBlock
                {
                    Text = "Đây là cửa sổ WPF Modal Dialog!",
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 10)
                });
                stack.Children.Add(new TextBlock
                {
                    Text = "Cửa sổ này được quản lý bởi IWindowManagerService và tự động gán Owner là AutoCAD Main Window.\nAutoCAD sẽ bị khóa tương tác cho tới khi bạn đóng hộp thoại này.",
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 20)
                });

                var btnClose = new Button
                {
                    Content = "Đồng ý & Đóng",
                    Width = 140,
                    Height = 35,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                btnClose.Click += (s, e) => window.DialogResult = true;
                stack.Children.Add(btnClose);

                window.Content = stack;

                // Hiển thị modal dialog
                bool? result = WindowManager.ShowModalDialog(window);
                Notify.Info($"Hộp thoại đã đóng với kết quả: {result}");
            });
        }
    }
}
