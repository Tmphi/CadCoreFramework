using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace CadCoreFramework.Core
{
    /// <summary>Mức độ của một dòng log.</summary>
    public enum LogSeverity
    {
        Debug = 0,
        Information = 1,
        Warning = 2,
        Error = 3
    }

    /// <summary>
    /// Triển khai ILoggerService ghi log ra file theo ngày tại %AppData%/CadCoreFramework/Logs/log-yyyyMMdd.txt.
    ///
    /// VÌ SAO KHÔNG DÙNG SERILOG?
    /// AutoCAD 2024 tự mang theo Serilog 2.9 + Serilog.Sinks.File 4.1 và nạp chúng TRƯỚC plugin.
    /// Serilog giữ nguyên AssemblyVersion 2.0.0.0 qua mọi bản 2.x/3.x nên plugin bị "dính" vào bản
    /// của AutoCAD → MissingMethodException ngay lúc khởi tạo. Plugin AutoCAD dùng chung một
    /// AppDomain với host và mọi plugin khác, nên logger không phụ thuộc thư viện ngoài là lựa chọn an toàn.
    ///
    /// ĐẶC ĐIỂM:
    /// - Mở/đóng file mỗi lần ghi với FileShare.ReadWrite → nhiều phiên AutoCAD ghi chung một file được.
    /// - Giữ <see cref="RetainedDays"/> ngày gần nhất, file cũ hơn bị xóa khi khởi tạo.
    /// - KHÔNG BAO GIỜ throw: lỗi ghi log không được làm hỏng lệnh hay làm sập AutoCAD.
    /// </summary>
    public sealed class LoggerService : ILoggerService, IDisposable
    {
        /// <summary>Số ngày log được giữ lại.</summary>
        public const int RetainedDays = 31;

        private const string FilePrefix = "log-";
        private const string FileExtension = ".txt";

        private readonly object _lock = new object();
        private readonly string _logDirectory;
        private readonly LogSeverity _minimumLevel;
        private readonly Func<DateTimeOffset> _clock;
        private bool _disposed;

        /// <summary>
        /// Khởi tạo logger ghi vào %AppData%/CadCoreFramework/Logs.
        /// Mức tối thiểu: Debug ở bản Debug, Information ở bản Release.
        /// </summary>
        public LoggerService()
            : this(DefaultLogDirectory(),
                   // Hai dòng marker: template engine của `dotnet new` coi chỉ thị tiền xử lý là điều kiện
                   // TEMPLATE (ký hiệu lạ = false) và sẽ cắt mất nhánh DEBUG khỏi dự án sinh ra.
//-:cnd:noEmit
#if DEBUG
                   LogSeverity.Debug,
#else
                   LogSeverity.Information,
#endif
//+:cnd:noEmit
                   () => DateTimeOffset.Now)
        {
        }

        /// <summary>Constructor đầy đủ — dùng cho unit test (thư mục tạm, đồng hồ giả).</summary>
        internal LoggerService(string logDirectory, LogSeverity minimumLevel, Func<DateTimeOffset> clock)
        {
            _logDirectory = logDirectory ?? throw new ArgumentNullException(nameof(logDirectory));
            _minimumLevel = minimumLevel;
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));

            try
            {
                Directory.CreateDirectory(_logDirectory);
                DeleteExpiredFiles();
            }
            catch
            {
                // Không tạo được thư mục log (quyền, ổ đầy…) → các lần ghi sau sẽ bị bỏ qua êm thấm.
            }
        }

        /// <summary>Thư mục chứa file log.</summary>
        public string LogDirectory => _logDirectory;

        /// <inheritdoc/>
        public void Debug(string message) => Write(LogSeverity.Debug, message, null);

        /// <inheritdoc/>
        public void Info(string message) => Write(LogSeverity.Information, message, null);

        /// <inheritdoc/>
        public void Warning(string message) => Write(LogSeverity.Warning, message, null);

        /// <inheritdoc/>
        public void Error(string message, Exception ex = null) => Write(LogSeverity.Error, message, ex);

        /// <inheritdoc/>
        public void Performance(string operationName, TimeSpan elapsed)
            => Write(LogSeverity.Information,
                     $"[PERF] {operationName} took {elapsed.TotalMilliseconds.ToString("0.###", CultureInfo.InvariantCulture)}ms",
                     null);

        /// <summary>Sau Dispose, mọi lần ghi bị bỏ qua.</summary>
        public void Dispose()
        {
            lock (_lock) _disposed = true;
        }

        /// <summary>Đường dẫn file log của một ngày.</summary>
        internal string GetLogFilePath(DateTimeOffset timestamp)
            => Path.Combine(_logDirectory, FilePrefix + timestamp.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + FileExtension);

        // ─── Private ──────────────────────────────────────────────────────────────

        private void Write(LogSeverity level, string message, Exception ex)
        {
            if (level < _minimumLevel) return;

            DateTimeOffset now = _clock();
            string entry = Format(now, level, message, ex);

            lock (_lock)
            {
                if (_disposed) return;
                try
                {
                    using (var stream = new FileStream(GetLogFilePath(now), FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                    using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                    {
                        writer.Write(entry);
                    }
                }
                catch
                {
                    // Ghi log thất bại (file bị khóa, ổ đầy…) — nuốt lỗi có chủ đích.
                }
            }
        }

        // Định dạng giống output mặc định của Serilog để log cũ và mới đọc liền mạch:
        // 2026-10-12 19:30:00.123 +07:00 [INF] message
        private static string Format(DateTimeOffset timestamp, LogSeverity level, string message, Exception ex)
        {
            var sb = new StringBuilder();
            sb.Append(timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture))
              .Append(" [").Append(LevelTag(level)).Append("] ")
              .Append(message ?? string.Empty)
              .Append(Environment.NewLine);

            if (ex != null)
                sb.Append(ex).Append(Environment.NewLine);

            return sb.ToString();
        }

        private static string LevelTag(LogSeverity level)
        {
            switch (level)
            {
                case LogSeverity.Debug: return "DBG";
                case LogSeverity.Warning: return "WRN";
                case LogSeverity.Error: return "ERR";
                default: return "INF";
            }
        }

        private void DeleteExpiredFiles()
        {
            // Xếp theo tên (yyyyMMdd) — giữ RetainedDays file mới nhất, giống retainedFileCountLimit của Serilog.
            var expired = Directory.GetFiles(_logDirectory, FilePrefix + "*" + FileExtension)
                .OrderByDescending(Path.GetFileName, StringComparer.Ordinal)
                .Skip(RetainedDays);

            foreach (string file in expired)
            {
                try { File.Delete(file); }
                catch { /* đang bị mở bởi phiên AutoCAD khác — để lần sau */ }
            }
        }

        private static string DefaultLogDirectory()
            => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CadCoreFramework", "Logs");
    }
}
