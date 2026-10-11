using System;
using System.Text;
using Autodesk.AutoCAD.DatabaseServices;
using Newtonsoft.Json;

namespace CadCoreFramework.Utils
{
    /// <summary>
    /// Các tiện ích hỗ trợ đọc/ghi dữ liệu ẩn (Metadata) dạng JSON
    /// đính kèm vào bất kỳ đối tượng AutoCAD nào thông qua XData.
    ///
    /// TRIẾT LÝ AUTODESK (XData):
    /// - XData được lưu theo từng ứng dụng (RegApp). Mỗi ứng dụng có một "namespace" riêng.
    /// - RegApp phải được đăng ký trong RegAppTable trước khi ghi XData.
    /// - Giới hạn: DxfCode.ExtendedDataAsciiString tối đa 255 ký tự ASCII mỗi string.
    ///   JSON dài phải được chia thành nhiều chunk.
    /// - DxfCode.ExtendedDataBinaryChunk: hỗ trợ binary data (byte[]) tối đa 127 byte/chunk.
    ///   Dùng cho Unicode hoặc dữ liệu nhị phân — encode JSON thành UTF-8 bytes.
    ///
    /// CHIẾN LƯỢC HIỆN TẠI: Dùng ExtendedDataAsciiString với multi-chunk.
    /// JSON được serialize thành ASCII (Newtonsoft.Json escape Unicode bằng \uXXXX).
    /// → Hoạt động đúng với mọi ký tự Unicode, kể cả tiếng Việt.
    /// </summary>
    public static class XDataHelper
    {
        // AutoCAD giới hạn mỗi ExtendedDataAsciiString tối đa 255 ký tự
        private const int MaxChunkLength = 250;

        // ─── Registration ──────────────────────────────────────────────────────────

        /// <summary>
        /// Đảm bảo tên Ứng dụng (AppName) dùng để lưu XData đã được đăng ký trong RegAppTable.
        /// PHẢI gọi trước khi ghi XData lần đầu tiên.
        /// </summary>
        public static void EnsureRegApp(Database db, Transaction tr, string appName)
        {
            if (string.IsNullOrWhiteSpace(appName)) throw new ArgumentNullException(nameof(appName));
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (tr == null) throw new ArgumentNullException(nameof(tr));

            var regTable = (RegAppTable)tr.GetObject(db.RegAppTableId, OpenMode.ForRead);
            if (!regTable.Has(appName))
            {
                regTable.UpgradeOpen();
                var regRecord = new RegAppTableRecord { Name = appName };
                regTable.Add(regRecord);
                tr.AddNewlyCreatedDBObject(regRecord, true);
            }
        }

        // ─── Write ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Chuyển đổi một đối tượng C# bất kỳ thành chuỗi JSON (ASCII-escaped)
        /// và gắn vào đối tượng AutoCAD (XData).
        ///
        /// JSON được tự động escape Unicode thành \uXXXX → an toàn với tiếng Việt,
        /// tiếng Trung, v.v. mà không cần dùng binary chunk.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu của object cần lưu.</typeparam>
        /// <param name="tr">Transaction đang mở.</param>
        /// <param name="obj">Đối tượng AutoCAD cần gắn XData.</param>
        /// <param name="appName">Tên ứng dụng đăng ký (RegApp namespace).</param>
        /// <param name="data">Dữ liệu cần lưu.</param>
        public static void SetMetadata<T>(Transaction tr, DBObject obj, string appName, T data)
        {
            if (obj  == null) throw new ArgumentNullException(nameof(obj));
            if (tr   == null) throw new ArgumentNullException(nameof(tr));
            if (string.IsNullOrWhiteSpace(appName)) throw new ArgumentNullException(nameof(appName));

            EnsureRegApp(obj.Database, tr, appName);

            // EscapeNonAscii = true: Newtonsoft sẽ encode \u0000 cho mọi ký tự non-ASCII
            string json = JsonConvert.SerializeObject(data,
                new JsonSerializerSettings { StringEscapeHandling = StringEscapeHandling.EscapeNonAscii });

            // AutoCAD XData per-application limit ≈ 16383 bytes; each 250-char chunk ≈ 254 bytes → ~64 chunks max
            const int MaxJsonLength = 15000;
            if (json.Length > MaxJsonLength)
                throw new InvalidOperationException(
                    $"Metadata quá lớn ({json.Length} ký tự sau escape, giới hạn {MaxJsonLength}). " +
                    "Dùng XRecord/ExtensionDictionary cho dữ liệu lớn hơn.");

            var rb = new ResultBuffer(
                new TypedValue((int)DxfCode.ExtendedDataRegAppName, appName));

            // Chia JSON thành các chunk ≤ 250 ký tự (giới hạn AutoCAD XData AsciiString)
            for (int i = 0; i < json.Length; i += MaxChunkLength)
            {
                int len   = Math.Min(MaxChunkLength, json.Length - i);
                string chunk = json.Substring(i, len);
                rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, chunk));
            }

            if (!obj.IsWriteEnabled)
                obj.UpgradeOpen();

            obj.XData = rb;
            rb.Dispose();
        }

        // ─── Read ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Đọc và deserialize chuỗi JSON ẩn trong XData của đối tượng trở lại thành object C#.
        /// Trả về defaultValue nếu không tìm thấy XData hoặc JSON không hợp lệ.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu cần đọc ra.</typeparam>
        /// <param name="obj">Đối tượng AutoCAD cần đọc XData.</param>
        /// <param name="appName">Tên ứng dụng đã đăng ký (RegApp namespace).</param>
        /// <param name="defaultValue">Giá trị mặc định nếu không tìm thấy dữ liệu.</param>
        public static T GetMetadata<T>(DBObject obj, string appName, T defaultValue = default)
        {
            if (obj == null || string.IsNullOrWhiteSpace(appName)) return defaultValue;

            using (ResultBuffer rb = obj.GetXDataForApplication(appName))
            {
                if (rb == null) return defaultValue;

                var sb = new StringBuilder();
                foreach (TypedValue tv in rb)
                {
                    if (tv.TypeCode == (int)DxfCode.ExtendedDataAsciiString)
                        sb.Append(tv.Value as string);
                }

                string json = sb.ToString();
                if (string.IsNullOrWhiteSpace(json)) return defaultValue;

                try
                {
                    return JsonConvert.DeserializeObject<T>(json);
                }
                catch (JsonException)
                {
                    return defaultValue;
                }
            }
        }

        // ─── Existence & Removal ──────────────────────────────────────────────────

        /// <summary>
        /// Kiểm tra xem đối tượng có chứa XData của appName hay không.
        /// </summary>
        public static bool HasMetadata(DBObject obj, string appName)
        {
            if (obj == null || string.IsNullOrWhiteSpace(appName)) return false;
            using (ResultBuffer rb = obj.GetXDataForApplication(appName))
                return rb != null;
        }

        /// <summary>
        /// Xóa toàn bộ XData của appName khỏi đối tượng.
        /// Theo Autodesk: ghi ResultBuffer chỉ có RegApp name = xóa dữ liệu cũ.
        /// </summary>
        public static void RemoveMetadata(Transaction tr, DBObject obj, string appName)
        {
            if (obj == null || string.IsNullOrWhiteSpace(appName)) return;
            if (!HasMetadata(obj, appName)) return;

            EnsureRegApp(obj.Database, tr, appName);

            if (!obj.IsWriteEnabled)
                obj.UpgradeOpen();

            using (var rb = new ResultBuffer(
                new TypedValue((int)DxfCode.ExtendedDataRegAppName, appName)))
            {
                obj.XData = rb;
            }
        }
    }
}
