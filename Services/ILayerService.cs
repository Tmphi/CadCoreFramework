using System.Collections.Generic;
using CadCoreFramework.Core;
using CadCoreFramework.Models;

namespace CadCoreFramework.Services
{
    /// <summary>
    /// Interface định nghĩa các phương thức thao tác với Layer của bản vẽ.
    /// Đóng vai trò Data Access Layer.
    /// </summary>
    public interface ILayerService
    {
        /// <summary>
        /// Lấy danh sách tất cả các Layer trong bản vẽ hiện tại.
        /// </summary>
        List<LayerModel> GetAllLayers();

        /// <summary>
        /// Bật hoặc tắt trạng thái hiển thị của một Layer (một bước Undo).
        /// </summary>
        /// <param name="layerName">Tên layer cần thay đổi</param>
        /// <param name="isOn">Trạng thái (true = bật, false = tắt)</param>
        /// <returns>
        /// Thành công với Value = true nếu đã đổi, false nếu không có layer tên đó;
        /// thất bại (không throw) nếu thao tác với bản vẽ lỗi.
        /// </returns>
        CadResult<bool> ToggleLayerVisibility(string layerName, bool isOn);
    }
}
