namespace CadCoreFramework.Services
{
    /// <summary>
    /// Giao diện quản lý cấu hình và cài đặt của plugin (lưu trữ dạng key-value dưới tệp JSON).
    /// </summary>
    public interface ISettingsService
    {
        /// <summary>
        /// Lấy giá trị cài đặt theo khóa chỉ định, ép kiểu về T.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu của giá trị cần lấy.</typeparam>
        /// <param name="key">Khóa cấu hình.</param>
        /// <param name="defaultValue">Giá trị mặc định nếu khóa chưa tồn tại.</param>
        T GetSetting<T>(string key, T defaultValue = default);

        /// <summary>
        /// Gán hoặc cập nhật giá trị cấu hình theo khóa.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu của giá trị cần lưu.</typeparam>
        /// <param name="key">Khóa cấu hình.</param>
        /// <param name="value">Giá trị cần lưu.</param>
        void SetSetting<T>(string key, T value);

        /// <summary>
        /// Lưu toàn bộ dữ liệu cấu hình xuống tệp lưu trữ trên đĩa.
        /// </summary>
        void Save();
    }
}
