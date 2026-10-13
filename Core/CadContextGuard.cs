using System;

namespace CadCoreFramework.Core
{
    /// <summary>
    /// Phát hiện và ngăn chặn lời gọi lồng nhau (nested) vào CadContext.Execute / ExecuteRead
    /// trên cùng một thread.
    ///
    /// TẠI SAO CẦN (quy tắc THIẾT KẾ, không phải giới hạn của AutoCAD):
    /// AutoCAD cho phép lock document lồng nhau (đếm số lần) và transaction lồng nhau. Nhưng trong
    /// framework, MỘT lần gọi Execute là MỘT đơn vị công việc: một lock, một transaction, một bước Undo,
    /// một kết quả. Execute lồng nhau gần như luôn là dấu hiệu một Service gọi Service khác BÊN TRONG
    /// lambda, và làm mờ ranh giới commit/undo (transaction trong đã commit mà transaction ngoài
    /// rollback thì sao? undo mark lồng nhau tính thế nào?). Guard biến lỗi thiết kế đó thành một
    /// exception có hướng dẫn cách sửa. (docs/training/T02 §4.6)
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
