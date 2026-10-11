namespace CadCoreFramework.Services
{
    /// <summary>
    /// Giao diện cung cấp đường dẫn đến các thư mục quan trọng của plugin (thư mục cài đặt, AppData, thư mục Templates).
    /// </summary>
    public interface IPathProvider
    {
        /// <summary>
        /// Lấy đường dẫn thư mục chứa assembly của plugin.
        /// </summary>
        string GetPluginDirectory();

        /// <summary>
        /// Lấy đường dẫn thư mục cấu hình và dữ liệu ứng dụng trong %AppData%.
        /// </summary>
        string GetAppDataDirectory();

        /// <summary>
        /// Lấy đường dẫn thư mục chứa các tệp biểu mẫu (Templates).
        /// </summary>
        string GetTemplatesDirectory();
    }
}
