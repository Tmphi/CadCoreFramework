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
        public string GetPluginDirectory()
        {
            // Assembly.Location is the recommended replacement for the obsolete CodeBase
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
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
