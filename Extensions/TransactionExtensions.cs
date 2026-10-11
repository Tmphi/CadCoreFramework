using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace CadCoreFramework.Extensions
{
    /// <summary>
    /// Các hàm mở rộng cho lớp Transaction. Giúp truy xuất đối tượng nhanh chóng và an toàn hơn.
    /// </summary>
    public static class TransactionExtensions
    {
        /// <summary>
        /// Mở và lấy một đối tượng kiểu T từ ObjectId. Tự động ép kiểu (Cast) an toàn.
        /// Trả về null nếu ép kiểu thất bại, ID bị null hoặc đối tượng đã bị xóa.
        /// </summary>
        public static T GetObjectAs<T>(this Transaction tr, ObjectId id, OpenMode mode = OpenMode.ForRead, bool openErased = false) where T : DBObject
        {
            if (id.IsNull || (!openErased && id.IsErased))
                return null;

            var obj = tr.GetObject(id, mode, openErased);
            return obj as T;
        }

        /// <summary>
        /// Mở và lấy hàng loạt đối tượng kiểu T từ một tập hợp các ObjectId.
        /// Tự động bỏ qua các Object bị null hoặc lỗi ép kiểu.
        /// </summary>
        public static IEnumerable<T> GetObjectsAs<T>(this Transaction tr, IEnumerable<ObjectId> ids, OpenMode mode = OpenMode.ForRead, bool openErased = false) where T : DBObject
        {
            foreach (var id in ids)
            {
                var obj = tr.GetObjectAs<T>(id, mode, openErased);
                if (obj != null)
                {
                    yield return obj;
                }
            }
        }

        /// <summary>
        /// Mở và lấy một đối tượng kiểu T ở chế độ ForWrite (ghi dữ liệu).
        /// </summary>
        public static T GetObjectForWrite<T>(this Transaction tr, ObjectId id) where T : DBObject
        {
            return tr.GetObjectAs<T>(id, OpenMode.ForWrite);
        }

        /// <summary>
        /// Mở và lấy một đối tượng kiểu T ở chế độ ForRead (chỉ đọc).
        /// </summary>
        public static T GetObjectForRead<T>(this Transaction tr, ObjectId id) where T : DBObject
        {
            return tr.GetObjectAs<T>(id, OpenMode.ForRead);
        }

        /// <summary>
        /// Duyệt qua danh sách ObjectId, mở đối tượng kiểu T và thực thi một hành động trên từng đối tượng.
        /// </summary>
        public static void ForEach<T>(this Transaction tr, IEnumerable<ObjectId> ids, Action<T> action, OpenMode mode = OpenMode.ForRead) where T : DBObject
        {
            if (ids == null || action == null) return;
            foreach (var obj in tr.GetObjectsAs<T>(ids, mode))
            {
                action(obj);
            }
        }
    }
}
