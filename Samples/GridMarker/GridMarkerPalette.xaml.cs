using System;
using System.Reflection;
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
            LoadStyles();

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

        private void LoadStyles()
        {
            // Tên assembly có hậu tố timestamp ở bản Debug (hot-reload) → dựng pack URI lúc runtime.
            string assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri($"/{assemblyName};component/Themes/Styles.xaml", UriKind.Relative)
            });
            ThemeManager.ApplyThemeTo(Resources);
        }
    }
}
