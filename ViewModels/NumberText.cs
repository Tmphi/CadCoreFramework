using System.Globalization;

namespace CadCoreFramework.ViewModels
{
    /// <summary>
    /// Đọc/ghi số cho ô nhập trên palette theo MỘT quy tắc, giống dòng lệnh AutoCAD: dấu chấm thập phân,
    /// không dấu phân cách hàng nghìn, không phụ thuộc culture của Windows hay Language của WPF.
    ///
    /// VÌ SAO KHÔNG BIND THẲNG TextBox VÀO int/double (docs/training/U01 §5, T09 B-31/B-32):
    ///   • WPF đổi chuỗi → số theo Language mặc định en-US: "2,5" thành 25, "NaN" thành NaN — không báo lỗi.
    ///   • Chuỗi không đổi được ("", "abc") thì binding giữ số cũ — ViewModel không hề biết ô đang sai.
    /// Cách làm: TextBox bind vào một property CHUỖI; ViewModel gọi các hàm dưới đây và tự báo lỗi.
    /// </summary>
    public static class NumberText
    {
        /// <summary>Số thực hữu hạn: "2.5", "-1", "1e3", " 7 " → được; "2,5", "", "abc", "NaN", "Infinity" → không.</summary>
        public static bool TryParseDouble(string? text, out double value)
        {
            // NumberStyles.Float: dấu chấm, dấu âm, số mũ, khoảng trắng — KHÔNG có AllowThousands ("2,5" bị từ chối).
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                && !double.IsNaN(value) && !double.IsInfinity(value))
                return true;

            value = 0;
            return false;
        }

        /// <summary>Số nguyên: "3", "-2", " 3 " → được; "3.0", "1,000", "" → không.</summary>
        public static bool TryParseInt(string? text, out int value)
            => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);

        /// <summary>Chuỗi hiển thị một số: luôn dấu chấm thập phân, kể cả trên máy vi-VN.</summary>
        public static string Format(double value) => value.ToString(CultureInfo.InvariantCulture);

        /// <summary>Chuỗi hiển thị một số nguyên.</summary>
        public static string Format(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
