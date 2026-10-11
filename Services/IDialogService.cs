namespace CadCoreFramework.Services
{
    /// <summary>
    /// Giao diện dịch vụ hiển thị các hộp thoại chuẩn của hệ thống (mở tệp, lưu tệp, chọn thư mục).
    /// </summary>
    public interface IDialogService
    {
        /// <summary>
        /// Hiển thị hộp thoại chọn tệp để mở (Open File Dialog).
        /// </summary>
        /// <param name="filter">Bộ lọc định dạng tệp (ví dụ: "DWG Files (*.dwg)|*.dwg").</param>
        /// <param name="title">Tiêu đề của hộp thoại.</param>
        /// <returns>Đường dẫn tệp được chọn hoặc null nếu người dùng hủy.</returns>
        string ShowOpenFileDialog(string filter, string title = "Chọn tệp");

        /// <summary>
        /// Hiển thị hộp thoại chọn tệp để lưu (Save File Dialog).
        /// </summary>
        /// <param name="filter">Bộ lọc định dạng tệp.</param>
        /// <param name="defaultExt">Phần mở rộng mặc định của tệp.</param>
        /// <param name="title">Tiêu đề của hộp thoại.</param>
        /// <returns>Đường dẫn tệp được lưu hoặc null nếu người dùng hủy.</returns>
        string ShowSaveFileDialog(string filter, string defaultExt = "", string title = "Lưu tệp");

        /// <summary>
        /// Hiển thị hộp thoại duyệt và chọn thư mục (Folder Browser Dialog).
        /// </summary>
        /// <param name="description">Mô tả hoặc hướng dẫn hiển thị trên hộp thoại.</param>
        /// <returns>Đường dẫn thư mục được chọn hoặc null nếu người dùng hủy.</returns>
        string ShowFolderBrowserDialog(string description = "Chọn thư mục");
    }
}
