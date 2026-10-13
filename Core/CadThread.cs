using System;

namespace CadCoreFramework.Core
{
    /// <summary>
    /// Ghi nhớ main thread của AutoCAD và chặn việc chạm vào bản vẽ từ thread khác.
    ///
    /// TẠI SAO CẦN:
    /// AutoCAD API chỉ an toàn trên main thread. Gọi từ ThreadPool (Task.Run, System.Timers.Timer,
    /// phần tiếp theo của một <c>await</c> không có SynchronizationContext, <c>ConfigureAwait(false)</c>…)
    /// cho hành vi không xác định: lúc chạy được, lúc hỏng dữ liệu, lúc crash — rất khó tái hiện.
    /// Guard này biến lỗi đó thành một exception rõ ràng NGAY tại điểm vào ICadContext.
    ///
    /// <see cref="AppBootstrapper"/> gọi <see cref="CaptureMainThread"/> trong Initialize() — AutoCAD luôn
    /// gọi Initialize trên main thread. Chưa capture (unit test, host khác) → guard không làm gì.
    /// </summary>
    internal static class CadThread
    {
        private static int _mainThreadId;

        /// <summary>Ghi nhận thread hiện tại là main thread của AutoCAD.</summary>
        internal static void CaptureMainThread() => _mainThreadId = Environment.CurrentManagedThreadId;

        /// <summary>Id main thread đã ghi nhận (0 = chưa ghi nhận).</summary>
        internal static int MainThreadId => _mainThreadId;

        /// <summary>
        /// Ném <see cref="InvalidOperationException"/> nếu đang KHÔNG ở main thread đã ghi nhận.
        /// </summary>
        /// <param name="operation">Tên thao tác, đưa vào thông điệp lỗi (ví dụ "CadContext.Execute").</param>
        internal static void EnsureMainThread(string operation)
        {
            int main = _mainThreadId;
            int current = Environment.CurrentManagedThreadId;
            if (main != 0 && current != main)
                throw new InvalidOperationException(
                    $"{operation} được gọi từ thread {current}, không phải main thread của AutoCAD ({main}).\n" +
                    "AutoCAD API chỉ an toàn trên main thread. Nguyên nhân thường gặp: Task.Run / System.Timers.Timer, " +
                    "ConfigureAwait(false), hoặc phần tiếp theo của await không quay về main thread.\n" +
                    "Giải pháp: chỉ tính toán / I-O ở thread nền rồi quay về main thread (CadAsyncHelper.ExecuteSafeAsync).");
        }

        /// <summary>CHỈ DÙNG CHO KIỂM THỬ: đặt lại trạng thái.</summary>
        internal static void ResetForTesting() => _mainThreadId = 0;
    }
}
