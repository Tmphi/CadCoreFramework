using System;
using System.IO;
using Serilog;

namespace CadCoreFramework.Core
{
    /// <summary>
    /// Triển khai dịch vụ ILoggerService sử dụng thư viện Serilog.
    /// Tự động tạo thư mục và ghi log ra file theo ngày tại %AppData%/CadCoreFramework/Logs.
    /// </summary>
    public class LoggerService : ILoggerService, IDisposable
    {
        private readonly Serilog.Core.Logger _logger;
        private bool _disposed;

        /// <summary>
        /// Khởi tạo LoggerService và cấu hình đường dẫn file log luân phiên theo ngày.
        /// </summary>
        public LoggerService()
        {
            string logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CadCoreFramework", "Logs");
            Directory.CreateDirectory(logDir);

            var level =
#if DEBUG
                Serilog.Events.LogEventLevel.Debug;
#else
                Serilog.Events.LogEventLevel.Information;
#endif
            _logger = new LoggerConfiguration()
                .MinimumLevel.Is(level)
                .WriteTo.File(Path.Combine(logDir, "log-.txt"), rollingInterval: RollingInterval.Day, shared: true)
                .CreateLogger();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _logger.Dispose();
            _disposed = true;
        }

        /// <inheritdoc/>
        public void Debug(string message)
        {
            _logger.Debug(message);
        }

        /// <inheritdoc/>
        public void Info(string message)
        {
            _logger.Information(message);
        }

        /// <inheritdoc/>
        public void Warning(string message)
        {
            _logger.Warning(message);
        }

        /// <inheritdoc/>
        public void Error(string message, Exception ex = null)
        {
            if (ex != null)
                _logger.Error(ex, message);
            else
                _logger.Error(message);
        }

        /// <inheritdoc/>
        public void Performance(string operationName, TimeSpan elapsed)
        {
            _logger.Information("[PERF] {OperationName} took {ElapsedMilliseconds}ms", operationName, elapsed.TotalMilliseconds);
        }
    }
}
