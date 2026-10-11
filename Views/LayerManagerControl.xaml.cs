using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using CadCoreFramework.Core;
using CadCoreFramework.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CadCoreFramework.Views
{
    /// <summary>
    /// Code-behind cho giao diện LayerManagerControl.xaml (Bảng quản lý Layer).
    /// </summary>
    public partial class LayerManagerControl : UserControl
    {
        /// <summary>
        /// Khởi tạo UserControl và gán DataContext từ DI container.
        /// </summary>
        public LayerManagerControl()
        {
            InitializeComponent();
            this.DataContext = AppBootstrapper.ServiceProvider?.GetService<LayerManagerViewModel>();
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (this.DataContext is LayerManagerViewModel vm && vm.LoadLayersCommand.CanExecute(null))
            {
                vm.LoadLayersCommand.Execute(null);
            }
        }
    }

    /// <summary>
    /// Bộ chuyển đổi giá trị bool thành văn bản "Tắt"/"Bật" cho nút thao tác trên giao diện.
    /// </summary>
    public class BooleanToOnOffConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b) return b ? "Tắt" : "Bật"; // Nếu đang bật (true), hiện nút để "Tắt" và ngược lại
            return "Chuyển";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    /// <summary>
    /// Bộ chuyển đổi đảo ngược giá trị Boolean (true thành false và ngược lại).
    /// </summary>
    public class InvertBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b) return !b;
            return value;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
