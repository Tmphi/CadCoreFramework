using System;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using CadCoreFramework.Core;
using CadCoreFramework.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CadCoreFramework.ViewModels
{
    /// <summary>
    /// ViewModel mẫu (Sample) dùng cho SamplePaletteControl.
    /// Minh họa cách áp dụng DataBinding, RelayCommand và xử lý sự kiện trong MVVM.
    /// </summary>
    public partial class SampleViewModel : ViewModelBase
    {
        private readonly ISampleDrawingService _drawingService;
        private readonly INotificationService _notify;

        /// <summary>
        /// Tên của thiết bị cần vẽ.
        /// </summary>
        [ObservableProperty]
        private string _deviceName = string.Empty;

        /// <summary>
        /// Tên Layer mà thiết bị sẽ được vẽ lên.
        /// </summary>
        [ObservableProperty]
        private string _layerName = "EQUIPMENT";

        /// <summary>
        /// Chuỗi văn bản hiển thị trạng thái của thao tác.
        /// </summary>
        [ObservableProperty]
        private string _statusText = "Sẵn sàng";

        /// <summary>
        /// Màu sắc của nhãn trạng thái (Brush).
        /// </summary>
        [ObservableProperty]
        private Brush _statusColor = Brushes.LimeGreen;

        /// <summary>
        /// Khởi tạo một phiên bản SampleViewModel với các phụ thuộc drawingService và notify.
        /// </summary>
        public SampleViewModel(ISampleDrawingService drawingService, INotificationService notify)
        {
            _drawingService = drawingService;
            _notify = notify;
            Title = "Sample Palette";
        }

        [RelayCommand]
        private async Task DrawSampleAsync()
        {
            if (string.IsNullOrWhiteSpace(DeviceName))
            {
                StatusText = "⚠ Vui lòng nhập tên thiết bị.";
                StatusColor = Brushes.Orange;
                return;
            }

            // Dùng RunSafeAsync() từ ViewModelBase để tự động:
            // - Bật/tắt IsBusy (hiện/ẩn Spinner)
            // - Bắt lỗi và hiển thị ErrorMessage trên UI
            await RunSafeAsync(async () =>
            {
                bool success = await _drawingService.DrawDeviceAsync(DeviceName, LayerName);

                if (success)
                {
                    StatusText = $"✔ Đã vẽ: {DeviceName}";
                    StatusColor = Brushes.LimeGreen;
                }
                else
                {
                    StatusText = "⚠ Đã hủy tương tác.";
                    StatusColor = Brushes.Orange;
                }
            }, "Lỗi vẽ thiết bị");
        }

        [RelayCommand]
        private void Clear()
        {
            DeviceName = string.Empty;
            StatusText = "Sẵn sàng";
            StatusColor = Brushes.LimeGreen;
        }
    }
}
