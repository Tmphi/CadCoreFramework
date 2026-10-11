using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CadCoreFramework.Core;
using CadCoreFramework.Models;
using CadCoreFramework.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CadCoreFramework.ViewModels
{
    /// <summary>
    /// ViewModel quản lý dữ liệu Layer và logic tương tác của UI.
    /// Hoàn toàn độc lập với AutoCAD API.
    /// </summary>
    public partial class LayerManagerViewModel : ViewModelBase
    {
        private readonly ILayerService _layerService;
        private readonly INotificationService _notify;

        /// <summary>
        /// Danh sách các Layer được hiển thị trên giao diện.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<LayerModel> _layers = new ObservableCollection<LayerModel>();

        /// <summary>
        /// Cờ chỉ báo trạng thái đang tải dữ liệu.
        /// </summary>
        [ObservableProperty]
        private bool _isLoading;

        /// <summary>
        /// Khởi tạo một phiên bản LayerManagerViewModel với các phụ thuộc ILayerService và INotificationService.
        /// </summary>
        public LayerManagerViewModel(ILayerService layerService, INotificationService notify)
        {
            _layerService = layerService;
            _notify = notify;
            Title = "Layer Manager";
        }

        /// <summary>
        /// Lệnh tải danh sách Layer từ bản vẽ.
        /// Thường được gọi khi View vừa khởi tạo (Loaded event).
        /// </summary>
        [RelayCommand]
        private void LoadLayers()
        {
            IsLoading = true;
            try
            {
                var layerList = _layerService.GetAllLayers();

                Layers.Clear();
                foreach (var layer in layerList)
                    Layers.Add(layer);

                _notify.Info($"Đã tải thành công {Layers.Count} layers.");
            }
            catch (Exception ex)
            {
                _notify.Error("Lỗi khi tải danh sách Layer", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Lệnh thay đổi trạng thái On/Off của một Layer.
        /// </summary>
        [RelayCommand]
        private async Task ToggleLayerAsync(LayerModel layer)
        {
            if (layer == null) return;

            bool newState = !layer.IsOn;
            try
            {
                bool success = await _layerService.ToggleLayerVisibilityAsync(layer.Name, newState);
                if (success)
                {
                    layer.IsOn = newState;
                    _notify.Info($"Đã {(newState ? "BẬT" : "TẮT")} layer: {layer.Name}");
                }
            }
            catch (Exception ex)
            {
                _notify.Error($"Lỗi khi đổi trạng thái layer: {layer.Name}", ex);
            }
        }
    }
}
