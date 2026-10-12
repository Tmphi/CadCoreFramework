using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using CadCoreFramework.Core;
using CadCoreFramework.Models;

namespace CadCoreFramework.Services
{
    /// <summary>
    /// Triển khai dịch vụ thao tác Layer.
    /// Gọi các API AutoCAD an toàn thông qua ICadContext (tránh crash khi sai luồng).
    /// Theo khuôn Golden Sample (docs/09): không prompt, đọc bằng ExecuteRead,
    /// ghi bằng TryExecute + undo mark để dùng được cả từ Command lẫn Palette.
    /// </summary>
    public class LayerService : ILayerService
    {
        /// <summary>Tên bước Undo khi bật/tắt layer từ Palette.</summary>
        public const string ToggleUndoMarkName = "Layer On/Off";

        private readonly ICadContext _cadContext;

        /// <summary>
        /// Khởi tạo một phiên bản của LayerService với ICadContext được tiêm vào.
        /// </summary>
        public LayerService(ICadContext cadContext)
        {
            _cadContext = cadContext ?? throw new ArgumentNullException(nameof(cadContext));
        }

        /// <summary>
        /// Lấy danh sách toàn bộ Layer trong bản vẽ hiện hành.
        /// </summary>
        public List<LayerModel> GetAllLayers()
        {
            var layers = new List<LayerModel>();

            // ExecuteRead: transaction chỉ đọc, tự lock khi được gọi từ Palette.
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

        /// <inheritdoc/>
        public CadResult<bool> ToggleLayerVisibility(string layerName, bool isOn)
        {
            if (string.IsNullOrWhiteSpace(layerName)) throw new ArgumentException("Tên layer không được trống.", nameof(layerName));

            // Thao tác ngắn, đồng bộ → TryExecute (không cần ExecuteAsync: không có gì để chờ).
            // autoRegen: Palette là application context, màn hình không tự cập nhật trạng thái Off.
            return _cadContext.TryExecute(scope =>
            {
                var layerTable = (LayerTable)scope.Transaction.GetObject(scope.Database.LayerTableId, OpenMode.ForRead);
                if (!layerTable.Has(layerName)) return false;

                var layerRecord = (LayerTableRecord)scope.Transaction.GetObject(layerTable[layerName], OpenMode.ForWrite);

                // Off/On là hợp lệ kể cả với Current Layer.
                // Điều không thể là FREEZE Current Layer (IsFrozen) — không phải IsOff.
                layerRecord.IsOff = !isOn;
                return true;
            }, autoRegen: true, undoMarkName: ToggleUndoMarkName);
        }
    }
}
