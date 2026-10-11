using System.Collections.Generic;
using System.Threading.Tasks;
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
        /// Bật hoặc tắt trạng thái hiển thị của một Layer.
        /// </summary>
        /// <param name="layerName">Tên layer cần thay đổi</param>
        /// <param name="isOn">Trạng thái (true = bật, false = tắt)</param>
        Task<bool> ToggleLayerVisibilityAsync(string layerName, bool isOn);
    }
}
