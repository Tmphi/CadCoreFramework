using Autodesk.AutoCAD.DatabaseServices;

namespace CadCoreFramework.Extensions
{
    /// <summary>
    /// Các phương thức mở rộng (Extension Methods) cho đối tượng ObjectId của AutoCAD.
    /// </summary>
    public static class ObjectIdExtensions
    {
        /// <summary>
        /// Kiểm tra xem một ObjectId có hợp lệ (không null) và đối tượng chưa bị xóa (IsErased = false) hay không.
        /// </summary>
        public static bool IsValidAndNotErased(this ObjectId id)
        {
            return !id.IsNull && id.IsValid && !id.IsErased;
        }

        /// <summary>
        /// Mở nhanh (Quick Open) đối tượng kiểu T thông qua TopTransaction hiện hành của Database chứa nó.
        /// Giúp lập trình viên mở đối tượng ngay lập tức mà không cần truyền biến Transaction qua lại.
        /// Trả về null nếu ID không hợp lệ, không có transaction hoặc sai kiểu.
        /// </summary>
        public static T QOpen<T>(this ObjectId id, OpenMode mode = OpenMode.ForRead, bool openErased = false) where T : DBObject
        {
            if (!id.IsValidAndNotErased() && !openErased) return null;
            var tr = id.Database?.TransactionManager?.TopTransaction;
            if (tr == null) return null;
            return tr.GetObject(id, mode, openErased) as T;
        }

        /// <summary>
        /// Lấy tên mã loại DXF (ví dụ: "LINE", "LWPOLYLINE", "INSERT", "TEXT") thông qua RXClass.
        /// Cực nhanh vì KHÔNG mở đối tượng vào bộ nhớ (Fast Property Retrieval theo chuẩn Autodesk).
        /// </summary>
        public static string GetDxfName(this ObjectId id)
        {
            if (id.IsNull) return string.Empty;
            return id.ObjectClass?.DxfName ?? string.Empty;
        }

        /// <summary>
        /// Kiểm tra xem đối tượng có thuộc hoặc kế thừa từ kiểu T (DBObject/Entity) hay không.
        /// Thực hiện trực tiếp trên RXClass mà KHÔNG cần mở đối tượng.
        /// </summary>
        public static bool IsA<T>(this ObjectId id) where T : DBObject
        {
            if (id.IsNull) return false;
            var rxClass = Autodesk.AutoCAD.Runtime.RXObject.GetClass(typeof(T));
            return id.ObjectClass.IsDerivedFrom(rxClass);
        }

        /// <summary>
        /// Chuyển đổi danh sách ObjectId (IEnumerable) sang ObjectIdCollection chuyên dụng của AutoCAD.
        /// </summary>
        public static ObjectIdCollection ToObjectIdCollection(this System.Collections.Generic.IEnumerable<ObjectId> ids)
        {
            var col = new ObjectIdCollection();
            if (ids != null)
            {
                foreach (var id in ids)
                {
                    if (!id.IsNull) col.Add(id);
                }
            }
            return col;
        }

        /// <summary>
        /// Xóa (Erase) hoặc Khôi phục (Un-erase) an toàn một đối tượng thông qua TopTransaction hiện hành.
        /// </summary>
        public static bool SafeErase(this ObjectId id, bool erase = true)
        {
            if (!id.IsValidAndNotErased() && erase) return false;
            var obj = id.QOpen<DBObject>(OpenMode.ForWrite);
            if (obj != null)
            {
                obj.Erase(erase);
                return true;
            }
            return false;
        }
    }
}
