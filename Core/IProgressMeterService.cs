using System;

namespace CadCoreFramework.Core
{
    /// <summary>
    /// Service quản lý giao diện thanh tiến trình (Progress Meter) của AutoCAD.
    /// Giúp báo cáo tiến độ khi chạy các tác vụ nặng (VD: Duyệt hàng ngàn Block, query Database lớn)
    /// để tránh việc AutoCAD trông như bị "treo".
    /// </summary>
    public interface IProgressMeterService
    {
        /// <summary>
        /// Bắt đầu một tiến trình mới với thông báo và tổng số bước (tối đa).
        /// </summary>
        void Start(string title, int totalLimit);

        /// <summary>
        /// Cập nhật lại tổng số bước nếu có thay đổi động trong quá trình chạy.
        /// </summary>
        void SetLimit(int max);

        /// <summary>
        /// Tăng thanh tiến trình lên 1 bước.
        /// </summary>
        void MeterProgress();

        /// <summary>
        /// Dừng và ẩn thanh tiến trình. Bắt buộc gọi ở cuối quá trình (trong finally block).
        /// </summary>
        void Stop();
    }
}
