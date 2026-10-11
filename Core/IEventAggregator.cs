using System;

namespace CadCoreFramework.Core
{
    /// <summary>
    /// Mẫu thiết kế EventAggregator giúp giao tiếp lỏng lẻo (loosely coupled) giữa các ViewModel hoặc Service.
    /// Cho phép gửi và nhận message mà không cần tham chiếu trực tiếp đến nhau.
    /// </summary>
    public interface IEventAggregator
    {
        /// <summary>
        /// Đăng ký lắng nghe một kiểu thông điệp (Message Type) cụ thể.
        /// Trả về IDisposable để có thể tự động Unsubscribe bằng block using hoặc gọi Dispose().
        /// </summary>
        IDisposable? Subscribe<TMessage>(Action<TMessage>? action);

        /// <summary>
        /// Hủy đăng ký một hành động khỏi kiểu thông điệp cụ thể.
        /// </summary>
        void Unsubscribe<TMessage>(Action<TMessage>? action);

        /// <summary>
        /// Phát hành (Publish) một thông điệp tới tất cả những ai đã đăng ký kiểu thông điệp này.
        /// </summary>
        void Publish<TMessage>(TMessage message);
    }
}
