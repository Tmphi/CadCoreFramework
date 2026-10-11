using System.Threading.Tasks;

namespace CadCoreFramework.Services
{
    /// <summary>
    /// Service đóng gói toàn bộ logic tương tác AutoCAD (vẽ thiết bị mẫu).
    /// Giúp ViewModel (UI) không bị phụ thuộc vào AutoCAD API.
    /// </summary>
    public interface ISampleDrawingService
    {
        /// <summary>
        /// Thực thi quy trình thu thập điểm (Prompt) và vẽ thiết bị mẫu xuống bản vẽ.
        /// Trả về true nếu thành công, false nếu người dùng hủy (ESC).
        /// </summary>
        Task<bool> DrawDeviceAsync(string deviceName, string layerName);
    }
}
