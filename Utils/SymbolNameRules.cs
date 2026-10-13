using System;

namespace CadCoreFramework.Utils
{
    /// <summary>
    /// Luật đặt tên ký hiệu của AutoCAD (Layer, Block, TextStyle, Linetype, DimStyle, RegApp…).
    /// Thuần .NET — kiểm tra được TRƯỚC khi chạm bản vẽ và unit test được.
    ///
    /// VÌ SAO CẦN: gán tên sai cho một record AutoCAD ném <c>eInvalidInput</c> — không nói ký tự nào sai,
    /// tên nào sai (đã kiểm chứng trên AutoCAD 2024, HeadlessLab ENSURE_LAYER_REJECTS_INVALID_NAME).
    /// </summary>
    public static class SymbolNameRules
    {
        /// <summary>Độ dài tối đa của tên ký hiệu (extended symbol names).</summary>
        public const int MaxLength = 255;

        private static readonly char[] InvalidChars = { '<', '>', '/', '\\', '"', ':', ';', '?', '*', '|', ',', '=', '`' };

        /// <summary>Danh sách ký tự cấm, dạng hiển thị cho thông báo lỗi.</summary>
        public const string InvalidCharactersDisplay = "< > / \\ \" : ; ? * | , = `";

        /// <summary>True nếu tên chứa ít nhất một ký tự cấm.</summary>
        public static bool ContainsInvalidCharacter(string? name)
            => !string.IsNullOrEmpty(name) && name!.IndexOfAny(InvalidChars) >= 0;

        /// <summary>Thông điệp lỗi nếu tên không hợp lệ, hoặc null nếu hợp lệ.</summary>
        public static string? GetError(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Tên ký hiệu không được để trống.";
            if (name!.Length > MaxLength) return $"Tên ký hiệu dài {name.Length} ký tự, vượt giới hạn {MaxLength}.";
            int index = name.IndexOfAny(InvalidChars);
            if (index >= 0)
                return $"Tên ký hiệu '{name}' chứa ký tự không hợp lệ '{name[index]}' (cấm: {InvalidCharactersDisplay}).";
            return null;
        }

        /// <summary>True nếu tên hợp lệ.</summary>
        public static bool IsValid(string? name) => GetError(name) == null;

        /// <summary>Ném <see cref="ArgumentException"/> có thông điệp rõ ràng nếu tên không hợp lệ.</summary>
        public static string Validate(string? name, string paramName)
        {
            string? error = GetError(name);
            if (error != null) throw new ArgumentException(error, paramName);
            return name!;
        }
    }
}
