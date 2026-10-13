using System;
using System.Windows.Controls;
using CadCoreFramework.Core;
using CadCoreFramework.Themes;
using CadCoreFramework.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CadCoreFramework.Views
{
    /// <summary>
    /// Code-behind cho giao diện CommonControlsPalette.xaml (bảng minh họa các điều khiển giao diện chuẩn).
    /// </summary>
    public partial class CommonControlsPalette : UserControl
    {
        /// <summary>
        /// Khởi tạo các thành phần giao diện của CommonControlsPalette.
        /// </summary>
        public CommonControlsPalette()
        {
            InitializeComponent();

            // Styles + theme hiện hành, tự áp lại khi người dùng đổi COLORTHEME.
            ThemeManager.Attach(this);

            // DataContext gán cho CHÍNH UserControl (không phải một Grid con) để PaletteService
            // Dispose được ViewModel khi thay content. Fail loud: thiếu đăng ký DI là lỗi cấu hình.
            DataContext = AppBootstrapper.ServiceProvider?.GetService<CommonControlsViewModel>()
                ?? throw new InvalidOperationException(
                    "CommonControlsViewModel chưa được đăng ký trong AppBootstrapper.ConfigureServices.");
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }
    }
}
