using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace CadCoreFramework.Core
{
    /// <summary>
    /// Sinh Guid ỔN ĐỊNH cho PaletteSet: cùng (plugin, tên palette) → cùng Guid ở mọi lần chạy.
    ///
    /// TẠI SAO CẦN:
    /// AutoCAD lưu vị trí / kích thước / trạng thái dock của PaletteSet theo Guid. Tạo PaletteSet không có
    /// Guid → mỗi lần khởi động palette lại về vị trí mặc định. Guid lấy từ <c>[assembly: Guid]</c> của
    /// plugin (template sinh lại cho mỗi plugin mới) + tên palette, nên hai plugin dựng từ template này
    /// dùng cùng tên palette vẫn không đụng nhau.
    ///
    /// Thuần .NET (không chạm AutoCAD) → unit test được.
    /// </summary>
    internal static class PaletteGuids
    {
        /// <summary>
        /// Guid dựa trên tên (UUID phiên bản 8, băm SHA-256) cho một palette của một plugin.
        /// Không phân biệt hoa/thường — khớp với cách PaletteService tra palette theo tên.
        /// </summary>
        internal static Guid For(Guid pluginId, string paletteName)
        {
            if (string.IsNullOrWhiteSpace(paletteName)) throw new ArgumentNullException(nameof(paletteName));

            byte[] name = Encoding.UTF8.GetBytes(paletteName.Trim().ToUpperInvariant());
            byte[] input = new byte[16 + name.Length];
            Buffer.BlockCopy(pluginId.ToByteArray(), 0, input, 0, 16);
            Buffer.BlockCopy(name, 0, input, 16, name.Length);

            // IncrementalHash: có trên net48 lẫn net8 (không cần biên dịch có điều kiện — template engine cắt
            // khối đó khỏi dự án sinh ra, xem Properties/AssemblyInfo.cs) và không bị CA1850 như
            // SHA256.Create().ComputeHash trên net8.
            byte[] hash;
            using (var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
            {
                sha.AppendData(input);
                hash = sha.GetHashAndReset();
            }

            byte[] bytes = new byte[16];
            Array.Copy(hash, bytes, 16);
            // Byte 7 là byte cao của Data3 (Guid(byte[]) đọc Data3 little-endian) → nibble phiên bản = 8.
            bytes[7] = (byte)((bytes[7] & 0x0F) | 0x80);
            // Byte 8: variant RFC 4122/9562 (10xx xxxx).
            bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
            return new Guid(bytes);
        }

        /// <summary>
        /// Định danh plugin: <c>[assembly: Guid]</c> nếu có; không có thì dẫn xuất từ tên assembly.
        /// </summary>
        internal static Guid PluginIdOf(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            var attr = assembly.GetCustomAttribute<GuidAttribute>();
            if (attr != null && Guid.TryParse(attr.Value, out Guid id)) return id;
            return For(Guid.Empty, assembly.GetName().Name ?? "plugin");
        }
    }
}
