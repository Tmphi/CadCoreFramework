using System;
using System.IO;
using System.Reflection;

namespace CadCoreFramework.Services
{
    /// <summary>
    /// Lớp triển khai IPathProvider cung cấp thông tin đường dẫn thư mục cho ứng dụng.
    /// </summary>
    public class PathProvider : IPathProvider
    {
        private const string AppName = "CadCoreFramework";

        /// <summary>
        /// Lấy đường dẫn thư mục cài đặt của plugin dựa trên vị trí assembly hiện tại.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Plugin được nạp từ byte array (tool chống khóa DLL / hot-reload): không có thư mục trên đĩa.
        /// </exception>
        public string GetPluginDirectory() => DirectoryOf(Assembly.GetExecutingAssembly());

        /// <summary>
        /// Thư mục chứa file của một assembly. Assembly nạp từ byte array có <c>Location</c> RỖNG, và
        /// <c>Path.GetDirectoryName("")</c> ném <c>ArgumentException</c> khó hiểu trên .NET Framework — thay
        /// bằng thông điệp nói rõ nguyên nhân (docs/training/T03 B30).
        /// </summary>
        internal static string DirectoryOf(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            string location = assembly.IsDynamic ? string.Empty : assembly.Location;
            if (string.IsNullOrEmpty(location))
                throw new InvalidOperationException(
                    $"Assembly '{assembly.GetName().Name}' không có vị trí trên đĩa (được nạp từ byte array hoặc là dynamic assembly) " +
                    "— không xác định được thư mục plugin. Đặt dữ liệu cần đọc trong thư mục AppData (GetAppDataDirectory) " +
                    "hoặc nhúng làm embedded resource.");
            return Path.GetDirectoryName(location);
        }

        /// <summary>
        /// Lấy đường dẫn thư mục lưu trữ dữ liệu ứng dụng trong %AppData%/CadCoreFramework.
        /// </summary>
        public string GetAppDataDirectory()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appData, AppName);
            return appFolder;
        }

        /// <summary>
        /// Lấy đường dẫn thư mục chứa các tệp biểu mẫu (Templates) của plugin.
        /// </summary>
        public string GetTemplatesDirectory()
        {
            return Path.Combine(GetPluginDirectory(), "Templates");
        }
    }
}
