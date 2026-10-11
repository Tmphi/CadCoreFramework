using Autodesk.AutoCAD.Runtime;
using CadCoreFramework.Core;
using CadCoreFramework.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CadCoreFramework.Commands
{
    /// <summary>
    /// Các lệnh AutoCAD liên quan đến quản lý Layer.
    /// Kế thừa từ CadCommandBase để tự động xử lý ngoại lệ và bắt lỗi.
    /// </summary>
    public class LayerCommands : CadCommandBase
    {
        /// <summary>Dịch vụ quản lý thanh công cụ neo (Palette).</summary>
        private IPaletteService PaletteService => AppBootstrapper.ServiceProvider?.GetService<IPaletteService>();

        /// <summary>
        /// Lệnh CADCORE_LAYER_MANAGER: Bật/Tắt thanh công cụ Quản Lý Layer (WPF Palette).
        /// Gõ "CADCORE_LAYER_MANAGER" trên dòng lệnh AutoCAD.
        /// </summary>
        [CommandMethod("CADCORE_LAYER_MANAGER")]
        public void ShowLayerManagerCommand()
        {
            ExecuteCommand(() =>
            {
                PaletteService?.TogglePalette(
                    "LayerManagerPaletteId",
                    () => new Views.LayerManagerControl(),
                    tabTitle: "Quản Lý Layer"
                );
            });
        }

        /// <summary>
        /// Lệnh CADCORE_COUNT_LAYERS: Đếm tổng số Layer và số Layer đang bị tắt trong bản vẽ hiện hành.
        /// Gõ "CADCORE_COUNT_LAYERS" trên dòng lệnh AutoCAD.
        /// </summary>
        [CommandMethod("CADCORE_COUNT_LAYERS")]
        public void CountLayersCommand()
        {
            ExecuteCommand(() =>
            {
                var notify = AppBootstrapper.ServiceProvider?.GetService<INotificationService>();
                var layerService = AppBootstrapper.ServiceProvider?.GetService<ILayerService>();

                if (layerService == null || notify == null) return;

                // Gọi trực tiếp Service
                var layers = layerService.GetAllLayers();
                
                notify.Info($"Hiện tại bản vẽ có {layers.Count} layers.");
                
                int offCount = 0;
                foreach (var l in layers)
                {
                    if (!l.IsOn) offCount++;
                }
                notify.Info($"Có {offCount} layers đang bị tắt (Off).");
            });
        }
    }
}
