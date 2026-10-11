using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using CadCoreFramework.Core;
using CadCoreFramework.Models;

namespace CadCoreFramework.Services
{
    /// <summary>
    /// Triển khai dịch vụ thao tác Layer.
    /// Gọi các API AutoCAD an toàn thông qua ICadContext (tránh crash khi sai luồng).
    /// </summary>
    public class LayerService : ILayerService
    {
        private readonly ICadContext _cadContext;
        private readonly INotificationService _notify;

        /// <summary>
        /// Khởi tạo một phiên bản của LayerService với các phụ thuộc CadContext và NotificationService.
        /// </summary>
        public LayerService(ICadContext cadContext, INotificationService notify)
        {
            _cadContext = cadContext;
            _notify = notify;
        }

        /// <summary>
        /// Lấy danh sách toàn bộ Layer trong bản vẽ hiện hành.
        /// </summary>
        public List<LayerModel> GetAllLayers()
        {
            var layers = new List<LayerModel>();

            // Thực thi đoạn code lấy dữ liệu trong CadScope (mở sẵn Transaction an toàn)
            _cadContext.ExecuteRead(scope =>
            {
                var layerTable = (LayerTable)scope.Transaction.GetObject(scope.Database.LayerTableId, OpenMode.ForRead);

                foreach (ObjectId layerId in layerTable)
                {
                    var layerRecord = (LayerTableRecord)scope.Transaction.GetObject(layerId, OpenMode.ForRead);
                    layers.Add(new LayerModel(
                        name: layerRecord.Name,
                        isOn: !layerRecord.IsOff, // IsOff = true nghĩa là đang Tắt
                        colorIndex: layerRecord.Color.ColorIndex
                    ));
                }
            });

            return layers;
        }

        /// <summary>
        /// Bật hoặc tắt trạng thái hiển thị của một Layer một cách bất đồng bộ.
        /// </summary>
        public async Task<bool> ToggleLayerVisibilityAsync(string layerName, bool isOn)
        {
            bool success = false;

            await _cadContext.ExecuteAsync(() =>
            {
                Action<ICadScope> action = scope =>
                {
                    var layerTable = (LayerTable)scope.Transaction.GetObject(scope.Database.LayerTableId, OpenMode.ForRead);

                    if (layerTable.Has(layerName))
                    {
                        var layerRecord = (LayerTableRecord)scope.Transaction.GetObject(layerTable[layerName], OpenMode.ForWrite);

                        // Lưu ý: Layer hiện hành (Current Layer) không thể tắt được (AutoCAD rule)
                        if (scope.Database.Clayer == layerRecord.ObjectId && !isOn)
                        {
                            _notify.Warn($"Không thể tắt Layer hiện hành: {layerName}");
                            success = false;
                        }
                        else
                        {
                            layerRecord.IsOff = !isOn;
                            success = true;
                        }
                    }
                };

                return Task.FromResult(action);
            });

            return success;
        }
    }
}
