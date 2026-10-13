using System;
using System.Windows;
using System.Windows.Controls;
using CadCoreFramework.Core;
using CadCoreFramework.Themes;
using Microsoft.Extensions.DependencyInjection;

namespace CadCoreFramework.Samples.GridMarker
{
    /// <summary>
    /// GOLDEN SAMPLE — CODE-BEHIND.
    /// Chỉ làm 3 việc hạ tầng: lấy ViewModel từ DI, nạp Styles/Theme, kích hoạt lần tải đầu.
    /// Mọi logic nằm trong <see cref="GridMarkerViewModel"/>.
    /// </summary>
    public partial class GridMarkerPalette : UserControl
    {
        public GridMarkerPalette()
        {
            InitializeComponent();

            // Styles + theme hiện hành; tự áp lại khi người dùng đổi COLORTHEME lúc palette đang mở.
            ThemeManager.Attach(this);

            // Fail loud: palette không có ViewModel là lỗi cấu hình DI, không được hiện UI "chết".
            DataContext = AppBootstrapper.ServiceProvider?.GetService<GridMarkerViewModel>()
                ?? throw new InvalidOperationException(
                    "GridMarkerViewModel chưa được đăng ký trong AppBootstrapper.ConfigureServices.");

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is GridMarkerViewModel vm && vm.RefreshCountCommand.CanExecute(null))
                vm.RefreshCountCommand.Execute(null);
        }
    }
}
