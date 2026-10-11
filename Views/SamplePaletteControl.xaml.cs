using System.Windows;
using System.Windows.Controls;
using CadCoreFramework.Core;
using CadCoreFramework.Extensions;
using CadCoreFramework.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CadCoreFramework.Views
{
    /// <summary>
    /// Giao diện WPF mẫu hiển thị trong Dockable Palette của AutoCAD.
    /// BẠN CÓ THỂ COPY FILE NÀY (bao gồm cả .xaml và .xaml.cs) và đổi tên để tạo giao diện công cụ mới.
    /// Nếu dùng kiến trúc MVVM, hãy thay DataContext bằng ViewModel của riêng bạn.
    /// </summary>
    public partial class SamplePaletteControl : UserControl
    {
        /// <summary>
        /// Khởi tạo các thành phần giao diện của SamplePaletteControl và gán DataContext từ DI container.
        /// </summary>
        public SamplePaletteControl()
        {
            InitializeComponent();
            this.DataContext = AppBootstrapper.ServiceProvider?.GetService<SampleViewModel>();
        }
    }
}
