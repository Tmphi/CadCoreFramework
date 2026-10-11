namespace CadCoreFramework.Core
{
    /// <summary>
    /// Giao diện dịch vụ hiển thị thông báo tới người dùng.
    /// Hỗ trợ in tin nhắn định dạng màu sắc lên dòng lệnh AutoCAD hoặc hiển thị hộp thoại xác nhận.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Hiển thị thông báo ngắn gọn lên dòng lệnh (Command Line) của AutoCAD.
        /// </summary>
        void Info(string message);

        /// <summary>
        /// Hiển thị thông báo cảnh báo lên dòng lệnh AutoCAD.
        /// </summary>
        void Warn(string message);

        /// <summary>
        /// Hiển thị thông báo lỗi lên dòng lệnh AutoCAD và đồng thời ghi vào hệ thống log.
        /// </summary>
        void Error(string message, System.Exception ex = null);

        /// <summary>
        /// Hiển thị hộp thoại xác nhận dạng MessageBox (dùng cho các xác nhận quan trọng hoặc cảnh báo nguy hiểm).
        /// </summary>
        bool Confirm(string message, string title = "Xác nhận");

        /// <summary>
        /// In ra một tiêu đề phân đoạn được định dạng đẹp mắt trên dòng lệnh AutoCAD.
        /// Hữu ích để phân tách rõ ràng kết quả của các công việc khác nhau.
        /// </summary>
        void Section(string title);
    }
}
