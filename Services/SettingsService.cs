using System;
using System.Collections.Generic;
using System.IO;
using CadCoreFramework.Core;
using Newtonsoft.Json;

namespace CadCoreFramework.Services
{
    /// <summary>
    /// Lớp triển khai ISettingsService quản lý lưu trữ và truy xuất cài đặt ứng dụng dưới dạng tệp JSON.
    /// Thread-safe với cơ chế đồng bộ hóa luồng.
    /// </summary>
    public class SettingsService : ISettingsService
    {
        private readonly string _settingsFilePath;
        private readonly ILoggerService _logger;
        private readonly IFileSystemService _fileSystem;
        private Dictionary<string, object> _settings;

        // Lock object để bảo vệ _settings khỏi race condition khi đọc/ghi từ nhiều thread
        private readonly object _lock = new object();

        /// <summary>
        /// Khởi tạo một phiên bản SettingsService và nạp các giá trị cài đặt từ đĩa.
        /// </summary>
        public SettingsService(ILoggerService logger, IPathProvider pathProvider, IFileSystemService fileSystem)
        {
            _logger = logger;
            _fileSystem = fileSystem;

            string appFolder = pathProvider.GetAppDataDirectory();

            if (!_fileSystem.DirectoryExists(appFolder))
            {
                _fileSystem.CreateDirectory(appFolder);
            }

            _settingsFilePath = Path.Combine(appFolder, "settings.json");
            LoadSettings();
        }

        private void LoadSettings()
        {
            lock (_lock)
            {
                try
                {
                    if (_fileSystem.FileExists(_settingsFilePath))
                    {
                        string json = _fileSystem.ReadAllText(_settingsFilePath);
                        _settings = JsonConvert.DeserializeObject<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
                    }
                    else
                    {
                        _settings = new Dictionary<string, object>();
                    }
                }
                catch (Exception ex)
                {
                    _logger?.Error($"Lỗi khi tải cài đặt từ {_settingsFilePath}", ex);
                    _settings = new Dictionary<string, object>();
                }
            }
        }

        /// <summary>
        /// Lấy giá trị cài đặt theo khóa chỉ định, ép kiểu về T.
        /// </summary>
        public T GetSetting<T>(string key, T defaultValue = default)
        {
            lock (_lock)
            {
                if (_settings.TryGetValue(key, out object value))
                {
                    try
                    {
                        // Newtonsoft.Json có thể giải tuần tự hóa số thành Int64...
                        // Ép kiểu trực tiếp có thể thất bại, do đó ta tuần tự hóa rồi giải tuần tự hóa lại kiểu mục tiêu.
                        if (value is T typedValue)
                        {
                            return typedValue;
                        }
                        
                        string json = JsonConvert.SerializeObject(value);
                        return JsonConvert.DeserializeObject<T>(json);
                    }
                    catch (Exception ex)
                    {
                        _logger?.Error($"Lỗi khi chuyển đổi cài đặt {key} sang kiểu {typeof(T).Name}", ex);
                    }
                }
                return defaultValue;
            }
        }

        /// <summary>
        /// Gán hoặc cập nhật giá trị cấu hình theo khóa.
        /// </summary>
        public void SetSetting<T>(string key, T value)
        {
            lock (_lock)
            {
                _settings[key] = value;
            }
        }

        /// <summary>
        /// Lưu toàn bộ dữ liệu cấu hình xuống tệp JSON trên đĩa.
        /// </summary>
        public void Save()
        {
            string json;
            lock (_lock)
            {
                // Serialize trong lock để tránh snapshot bị thay đổi giữa chừng khi serialize
                json = JsonConvert.SerializeObject(_settings, Formatting.Indented);
            }

            try
            {
                // Ghi file ra ngoài lock để không block các thread khác trong khi I/O
                _fileSystem.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception ex)
            {
                _logger?.Error($"Lỗi khi lưu cài đặt vào {_settingsFilePath}", ex);
            }
        }
    }
}
