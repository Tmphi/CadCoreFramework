using System;

namespace CadCoreFramework.Core
{
    /// <summary>
    /// Giao diện dịch vụ ghi log (Logging Service) của plugin.
    /// Giúp theo dõi thông tin hoạt động, cảnh báo và lỗi phát sinh trong quá trình chạy.
    /// </summary>
    public interface ILoggerService
    {
        /// <summary>Ghi log thông tin debug.</summary>
        void Debug(string message);

        /// <summary>Ghi log thông tin (Information).</summary>
        void Info(string message);

        /// <summary>Ghi log cảnh báo (Warning).</summary>
        void Warning(string message);

        /// <summary>Ghi log lỗi (Error) kèm theo ngoại lệ Exception nếu có.</summary>
        void Error(string message, Exception ex = null);

        /// <summary>Ghi log đo lường hiệu năng thực thi.</summary>
        void Performance(string operationName, TimeSpan elapsed);
    }
}
