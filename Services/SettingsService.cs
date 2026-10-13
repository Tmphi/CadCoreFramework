using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using CadCoreFramework.Core;
using Newtonsoft.Json;

namespace CadCoreFramework.Services
{
    /// <summary>
    /// Lớp triển khai ISettingsService quản lý lưu trữ và truy xuất cài đặt ứng dụng dưới dạng tệp JSON.
    /// Thread-safe với cơ chế đồng bộ hóa luồng.
    ///
    /// NHIỀU PHIÊN AUTOCAD CÙNG LÚC: mỗi phiên giữ bản sao trong bộ nhớ. <see cref="Save"/> không ghi đè
    /// mù quáng — nó đọc lại tệp, chỉ áp các khóa phiên này đã đổi (<see cref="SetSetting{T}"/>), giữ nguyên
    /// khóa phiên khác đã lưu, rồi ghi atomic. Một named mutex tuần tự hóa các lần Save giữa các tiến trình.
    /// </summary>
    public class SettingsService : ISettingsService
    {
        private readonly string _settingsFilePath;
        private readonly ILoggerService _logger;
        private readonly IFileSystemService _fileSystem;
        private Dictionary<string, object> _settings;

        // Các khóa phiên này đã SetSetting kể từ lần Save thành công gần nhất.
        private readonly HashSet<string> _dirtyKeys = new HashSet<string>();

        // Lock object để bảo vệ _settings khỏi race condition khi đọc/ghi từ nhiều thread
        private readonly object _lock = new object();

        private static readonly TimeSpan CrossProcessLockTimeout = TimeSpan.FromSeconds(5);

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
                _settings = ReadFromDisk() ?? new Dictionary<string, object>();
            }
        }

        /// <summary>Đọc tệp cài đặt; null nếu chưa có tệp hoặc tệp hỏng (đã log).</summary>
        private Dictionary<string, object>? ReadFromDisk()
        {
            try
            {
                if (!_fileSystem.FileExists(_settingsFilePath)) return null;
                string json = _fileSystem.ReadAllText(_settingsFilePath);
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
            }
            catch (Exception ex)
            {
                _logger?.Error($"Lỗi khi tải cài đặt từ {_settingsFilePath}", ex);
                return null;
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
                _dirtyKeys.Add(key);
            }
        }

        /// <summary>
        /// Lưu cài đặt xuống tệp JSON trên đĩa: đọc lại tệp, áp các khóa phiên này đã đổi lên trên
        /// (khóa phiên khác đã lưu được giữ nguyên), ghi atomic. Lỗi ghi → log, các khóa đã đổi được giữ
        /// lại để lần Save sau ghi tiếp.
        /// </summary>
        public void Save()
        {
            try
            {
                using (var mutex = new Mutex(false, MutexNameFor(_settingsFilePath)))
                {
                    bool owned = false;
                    try
                    {
                        try { owned = mutex.WaitOne(CrossProcessLockTimeout); }
                        catch (AbandonedMutexException) { owned = true; } // phiên giữ khóa đã crash → khóa thuộc về ta

                        if (!owned)
                            _logger?.Warning($"Không lấy được khóa lưu cài đặt sau {CrossProcessLockTimeout.TotalSeconds:0}s — vẫn lưu.");

                        SaveMerged();
                    }
                    finally
                    {
                        if (owned) mutex.ReleaseMutex();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"Lỗi khi lưu cài đặt vào {_settingsFilePath}", ex);
            }
        }

        private void SaveMerged()
        {
            // Giữ _lock suốt lần ghi (tệp nhỏ, Save hiếm) để trạng thái bộ nhớ chỉ đổi khi ghi THÀNH CÔNG.
            lock (_lock)
            {
                // Tệp chưa có / hỏng → lấy toàn bộ những gì phiên này biết làm nền (không làm mất dữ liệu).
                Dictionary<string, object> merged = ReadFromDisk() ?? new Dictionary<string, object>(_settings);
                foreach (string key in _dirtyKeys)
                    merged[key] = _settings[key];

                _fileSystem.WriteAllText(_settingsFilePath, JsonConvert.SerializeObject(merged, Formatting.Indented));

                _settings = merged;   // phiên này giờ thấy cả khóa do phiên khác lưu
                _dirtyKeys.Clear();
            }
        }

        /// <summary>Tên named mutex theo đường dẫn tệp (tên mutex không được chứa '\' ngoài tiền tố).</summary>
        private static string MutexNameFor(string path)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(Path.GetFullPath(path).ToUpperInvariant());
            byte[] hash;
            using (var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))   // net48 + net8, không cần biên dịch có điều kiện
            {
                sha.AppendData(bytes);
                hash = sha.GetHashAndReset();
            }
            var sb = new StringBuilder(@"Local\CadCoreFramework.Settings.", 64);
            for (int i = 0; i < 12; i++) sb.Append(hash[i].ToString("x2"));
            return sb.ToString();
        }
    }
}
