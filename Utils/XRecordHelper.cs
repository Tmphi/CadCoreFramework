using System;
using System.Text;
using Autodesk.AutoCAD.DatabaseServices;
using Newtonsoft.Json;

namespace CadCoreFramework.Utils
{
    /// <summary>
    /// Các tiện ích hỗ trợ đọc/ghi dữ liệu ẩn (Metadata) dạng JSON
    /// đính kèm vào bất kỳ đối tượng AutoCAD nào thông qua Extension Dictionary (XRecord).
    ///
    /// TẠI SAO DÙNG XRECORD MÀ KHÔNG DÙNG XDATA?
    /// - XData bị giới hạn cứng ở mức 16KB dữ liệu cho TỔNG CỘNG mọi ứng dụng trên một đối tượng.
    /// - XRecord lưu trong Extension Dictionary, không có giới hạn 16KB.
    /// - XRecord khó bị các công cụ LISP của bên thứ 3 làm hỏng hoặc xóa nhầm.
    /// - XRecord cực kỳ lý tưởng để lưu chuỗi JSON lớn cho các cấu kiện phức tạp (ví dụ dữ liệu Tủ điện).
    /// </summary>
    public static class XRecordHelper
    {
        // Chunk an toàn cho DxfCode.Text trong Xrecord
        private const int MaxChunkLength = 2000;

        /// <summary>
        /// Ghi đối tượng C# dưới dạng JSON vào Extension Dictionary của một Entity.
        /// Sử dụng DxfCode.Text để chia thành nhiều ResultBuffer chunks nếu cần.
        /// </summary>
        public static void SetMetadata<T>(Transaction tr, DBObject obj, string key, T data)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            if (tr == null) throw new ArgumentNullException(nameof(tr));
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));

            // Encode Unicode an toàn
            string json = JsonConvert.SerializeObject(data, 
                new JsonSerializerSettings { StringEscapeHandling = StringEscapeHandling.EscapeNonAscii });

            if (!obj.IsWriteEnabled)
                obj.UpgradeOpen();

            if (obj.ExtensionDictionary == ObjectId.Null)
            {
                obj.CreateExtensionDictionary();
            }

            var extDict = (DBDictionary)tr.GetObject(obj.ExtensionDictionary, OpenMode.ForWrite);

            Xrecord xRec;
            if (extDict.Contains(key))
            {
                xRec = (Xrecord)tr.GetObject(extDict.GetAt(key), OpenMode.ForWrite);
            }
            else
            {
                xRec = new Xrecord();
                extDict.SetAt(key, xRec);
                tr.AddNewlyCreatedDBObject(xRec, true);
            }

            var rb = new ResultBuffer();
            for (int i = 0; i < json.Length; i += MaxChunkLength)
            {
                int len = Math.Min(MaxChunkLength, json.Length - i);
                rb.Add(new TypedValue((int)DxfCode.Text, json.Substring(i, len)));
            }

            xRec.Data = rb;
            rb.Dispose();
        }

        /// <summary>
        /// Đọc và chuyển đổi JSON từ Extension Dictionary trả lại thành đối tượng C#.
        /// </summary>
        public static T GetMetadata<T>(Transaction tr, DBObject obj, string key, T defaultValue = default)
        {
            if (obj == null || string.IsNullOrWhiteSpace(key)) return defaultValue;
            if (obj.ExtensionDictionary == ObjectId.Null) return defaultValue;

            var extDict = (DBDictionary)tr.GetObject(obj.ExtensionDictionary, OpenMode.ForRead);
            if (!extDict.Contains(key)) return defaultValue;

            var xRec = (Xrecord)tr.GetObject(extDict.GetAt(key), OpenMode.ForRead);
            if (xRec.Data == null) return defaultValue;

            var sb = new StringBuilder();
            foreach (TypedValue tv in xRec.Data)
            {
                if (tv.TypeCode == (int)DxfCode.Text)
                {
                    sb.Append(tv.Value.ToString());
                }
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

        /// <summary>
        /// Xóa dữ liệu Metadata (XRecord) khỏi Extension Dictionary.
        /// </summary>
        public static void RemoveMetadata(Transaction tr, DBObject obj, string key)
        {
            if (obj == null || string.IsNullOrWhiteSpace(key)) return;
            if (obj.ExtensionDictionary == ObjectId.Null) return;

            var extDict = (DBDictionary)tr.GetObject(obj.ExtensionDictionary, OpenMode.ForRead);
            if (extDict.Contains(key))
            {
                extDict.UpgradeOpen();
                extDict.Remove(key);
            }
        }
    }
}
