using System;

namespace CadCoreFramework.Core
{
    /// <summary>
    /// Đối tượng kết quả thống nhất cho toàn bộ framework.
    /// Giúp tránh việc sử dụng ngoại lệ cho luồng điều khiển và cung cấp nhiều ngữ cảnh hơn (thông báo lỗi, ngoại lệ)
    /// thay vì chỉ trả về boolean.
    /// </summary>
    /// <typeparam name="T">Kiểu dữ liệu của kết quả trả về khi thành công.</typeparam>
    public class CadResult<T>
    {
        /// <summary>Cờ đánh dấu thành công.</summary>
        public bool IsSuccess { get; }

        /// <summary>Giá trị kết quả trả về (chỉ hợp lệ khi IsSuccess = true).</summary>
        public T Value { get; }

        /// <summary>Thông báo lỗi (khi IsSuccess = false).</summary>
        public string ErrorMessage { get; }

        /// <summary>Đối tượng ngoại lệ (nếu có lỗi xảy ra).</summary>
        public Exception Exception { get; }

        protected CadResult(bool isSuccess, T value, string errorMessage, Exception exception)
        {
            IsSuccess = isSuccess;
            Value = value;
            ErrorMessage = errorMessage;
            Exception = exception;
        }

        /// <summary>Khởi tạo kết quả thành công với dữ liệu trả về.</summary>
        public static CadResult<T> Success(T value) => new CadResult<T>(true, value, null, null);

        /// <summary>Khởi tạo kết quả thất bại với thông điệp lỗi và ngoại lệ tùy chọn.</summary>
        public static CadResult<T> Fail(string message, Exception ex = null) => new CadResult<T>(false, default, message, ex);
    }

    /// <summary>
    /// Đối tượng kết quả không có giá trị trả về (Void).
    /// </summary>
    public class CadResult : CadResult<object>
    {
        protected CadResult(bool isSuccess, string errorMessage, Exception exception) 
            : base(isSuccess, null, errorMessage, exception)
        {
        }

        /// <summary>Khởi tạo kết quả thành công rỗng.</summary>
        public static CadResult Ok() => new CadResult(true, null, null);

        /// <summary>Khởi tạo kết quả thất bại với thông điệp lỗi và ngoại lệ tùy chọn.</summary>
        public static new CadResult Fail(string message, Exception ex = null) => new CadResult(false, message, ex);
    }
}
