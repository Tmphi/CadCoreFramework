using System;

namespace CadCoreFramework.Core
{
    /// <summary>
    /// Phát hiện và ngăn chặn lời gọi lồng nhau (nested) vào CadContext.Execute / ExecuteRead
    /// trên cùng một thread.
    ///
    /// TẠI SAO CẦN:
    /// Khi một lambda đang chạy bên trong Execute() (document đã được LockDocument và transaction
    /// đã được mở), nếu lambda đó gọi một Service khác mà Service đó cũng gọi Execute(),
    /// AutoCAD sẽ ném eLockViolation hoặc gây deadlock. Lỗi này rất khó debug vì không có
    /// stack trace rõ ràng. Guard này biến lỗi ngầm thành exception có message mô tả rõ ràng.
    ///
    /// CÁCH DÙNG (CadContext tự gọi, không cần gọi thủ công):
    /// <code>
    ///   CadContextGuard.Enter();
    ///   try { /* logic */ }
    ///   finally { CadContextGuard.Exit(); }
    /// </code>
    /// </summary>
    internal static class CadContextGuard
    {
        [ThreadStatic]
        private static bool _active;

        /// <summary>
        /// Đánh dấu thread hiện tại đang ở trong một Execute/ExecuteRead scope.
        /// Ném InvalidOperationException nếu scope đã được mở (lồng nhau không được phép).
        /// </summary>
        public static void Enter()
        {
            if (_active)
                throw new InvalidOperationException(
                    "Nested CadContext.Execute detected trên cùng một thread.\n" +
                    "Nguyên nhân thường gặp: một Service bên trong lambda Execute() đang gọi thêm một Execute() khác.\n" +
                    "Giải pháp: gom toàn bộ logic vẽ vào một lambda duy nhất, hoặc tách phần đọc dữ liệu ra ngoài transaction.");
            _active = true;
        }

        /// <summary>Đánh dấu thread hiện tại đã thoát khỏi scope. Luôn gọi trong khối finally.</summary>
        public static void Exit() => _active = false;

        /// <summary>Trạng thái hiện tại của guard trên thread này (dùng cho testing).</summary>
        internal static bool IsActive => _active;
    }
}
