using System;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
using CadCoreFramework.Core;
using CadCoreFramework.Extensions;
using CadCoreFramework.Utils;

namespace CadCoreFramework.Services
{
    /// <summary>
    /// Triển khai dịch vụ vẽ thiết bị mẫu.
    /// Tách rời mã thao tác AutoCAD khỏi ViewModel để đảm bảo nguyên tắc Encapsulation.
    /// </summary>
    public class SampleDrawingService : ISampleDrawingService
    {
        private readonly ICadContext _cadContext;

        /// <summary>
        /// Khởi tạo một phiên bản SampleDrawingService với CadContext được tiêm vào.
        /// </summary>
        public SampleDrawingService(ICadContext cadContext)
        {
            _cadContext = cadContext;
        }

        /// <summary>
        /// Thu thập điểm tương tác từ người dùng và vẽ các hình khối thiết bị mẫu vào bản vẽ.
        /// </summary>
        public async Task<bool> DrawDeviceAsync(string deviceName, string layerName)
        {
            // TRIẾT LÝ AUTODESK: 
            // Tuyệt đối KHÔNG ĐƯỢC hỏi người dùng (Prompt) KHI ĐANG LOCK DOCUMENT HOẶC MỞ TRANSACTION.
            // Việc này sẽ giữ Transaction và Document Lock vô thời hạn, gây crash hoặc treo ứng dụng.
            // Luôn thu thập toàn bộ điểm và dữ liệu người dùng TRƯỚC KHI gọi ExecuteAsync/Execute.

            // BƯỚC 1: Lấy các thông số từ người dùng (sử dụng CadPromptHelper)
            var pt = CadPromptHelper.GetPoint("Chọn điểm đặt thiết bị:");
            if (pt == null) return false;

            var p2 = pt.Value + new Vector3d(60, 30, 0);
            
            var p3 = CadPromptHelper.GetPoint("Chọn điểm đặt mũi tên", pt.Value);
            if (p3 == null) return false;
            
            var p4 = CadPromptHelper.GetPoint("Chọn điểm đặt nội dung", p3.Value);
            if (p4 == null) return false;

            // BƯỚC 2: Mở Transaction và thực thi bản vẽ (nhanh gọn, không chờ đợi)
            await _cadContext.ExecuteAsync(() =>
            {
                Action<ICadScope> action = scope =>
                {
                    scope.DrawMLeader("trần minh phở", p3.Value, p4.Value);
                    scope.DrawRectangle(pt.Value, p2, layer: layerName);
                    scope.DrawText(deviceName, pt.Value + new Vector3d(5, 15, 0),
                                   height: 5, layer: layerName);
                };
                return Task.FromResult(action);
            });

            return true;
        }
    }
}
