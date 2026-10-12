using CadCoreFramework.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CadCoreFramework.ViewModels
{
    /// <summary>
    /// Đóng vai trò làm cầu nối (Bridge) giữa giao diện XAML và hệ thống tiêm phụ thuộc (Dependency Injection).
    /// Cho phép các file XAML lấy trực tiếp ViewModel từ DI container.
    /// 
    /// Cách dùng trong file XAML:
    /// <code>
    /// &lt;UserControl.Resources&gt;
    ///     &lt;viewmodels:ViewModelLocator x:Key="Locator" /&gt;
    /// &lt;/UserControl.Resources&gt;
    /// &lt;UserControl DataContext="{Binding Source={StaticResource Locator}, Path=MyViewModel}"&gt;
    /// </code>
    /// </summary>
    public class ViewModelLocator
    {
        // Thêm các property vào đây cho mỗi ViewModel mà bạn muốn XAML có thể gọi được

        /// <summary>
        /// Lấy phiên bản CommonControlsViewModel từ DI ServiceProvider.
        /// </summary>
        public CommonControlsViewModel CommonControls => AppBootstrapper.ServiceProvider?.GetService<CommonControlsViewModel>();
    }
}
