using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using CadCoreFramework.Core;

namespace CadCoreFramework.Extensions
{
    /// <summary>
    /// Bộ extension method mạnh mẽ cho SelectionSet và Editor.GetSelection().
    /// Cho phép lọc và xử lý tập hợp đối tượng người dùng chọn một cách ngắn gọn, an toàn.
    ///
    /// TRIẾT LÝ AUTODESK:
    /// - SelectionFilter dùng TypedValue với DxfCode để lọc entity theo loại, layer, handle...
    /// - GetSelection() chỉ được gọi từ command context (không phải từ event handler hay palette thread).
    /// - KHÔNG bao giờ giữ SelectionSet sống qua ranh giới transaction — chỉ đọc ObjectId trong transaction.
    /// </summary>
    public static class SelectionSetExtensions
    {
        // ─── Selection Prompt ─────────────────────────────────────────────────────

        /// <summary>
        /// Hỏi người dùng chọn nhiều đối tượng với filter tùy chỉnh.
        /// Trả về danh sách ObjectId, hoặc mảng rỗng nếu người dùng nhấn Escape.
        /// </summary>
        /// <param name="editor">Editor của document hiện tại.</param>
        /// <param name="message">Thông báo hướng dẫn người dùng.</param>
        /// <param name="filter">SelectionFilter để lọc loại đối tượng. Null = không lọc.</param>
        public static ObjectId[] SelectEntities(this Editor editor, string message,
            SelectionFilter filter = null)
        {
            var opts = new PromptSelectionOptions
            {
                MessageForAdding = $"\n{message}",
                RejectObjectsOnLockedLayers = false
            };

            PromptSelectionResult result = filter != null
                ? editor.GetSelection(opts, filter)
                : editor.GetSelection(opts);

            if (result.Status != PromptStatus.OK)
                return Array.Empty<ObjectId>();

            return result.Value.GetObjectIds();
        }

        /// <summary>
        /// Hỏi người dùng chọn MỘT đối tượng duy nhất.
        /// Trả về ObjectId.Null nếu người dùng nhấn Escape hoặc không chọn gì.
        /// Lưu ý: PromptEntityOptions của AutoCAD không hỗ trợ SelectionFilter trực tiếp.
        /// Nếu cần filter chính xác, hãy dùng SelectEntities() thay thế.
        /// </summary>
        public static ObjectId SelectSingleEntity(this Editor editor, string message)
        {
            var opts = new PromptEntityOptions($"\n{message}")
            {
                AllowNone = true
            };

            var result = editor.GetEntity(opts);
            if (result.Status != PromptStatus.OK) return ObjectId.Null;

            return result.ObjectId;
        }

        /// <summary>
        /// Yêu cầu người dùng chọn tất cả đối tượng trong vùng hoặc toàn bản vẽ.
        /// Trả về mảng ObjectId đã được lọc theo filter.
        /// </summary>
        public static ObjectId[] SelectAll(this Editor editor, SelectionFilter filter = null)
        {
            PromptSelectionResult result = filter != null
                ? editor.SelectAll(filter)
                : editor.SelectAll();

            if (result.Status != PromptStatus.OK)
                return Array.Empty<ObjectId>();

            return result.Value.GetObjectIds();
        }

        // ─── Filter Builders ──────────────────────────────────────────────────────

        /// <summary>
        /// Tạo SelectionFilter chỉ chọn đối tượng trên các layer chỉ định.
        /// AutoCAD SelectionFilter hỗ trợ OR bằng cách nối tên layer bằng dấu phẩy.
        /// </summary>
        public static SelectionFilter FilterByLayers(params string[] layerNames)
        {
            if (layerNames == null || layerNames.Length == 0) return null;
            // AutoCAD SelectionFilter: giá trị DxfCode.LayerName nối bằng dấu phẩy
            // sẽ tự động hoạt động như OR logic — không cần <OR/OR> brackets.
            string combined = string.Join(",", layerNames);
            return new SelectionFilter(new[]
            {
                new TypedValue((int)DxfCode.LayerName, combined)
            });
        }

        /// <summary>
        /// Tạo SelectionFilter chỉ chọn một loại entity cụ thể (ví dụ "LINE", "LWPOLYLINE", "TEXT").
        /// </summary>
        public static SelectionFilter FilterByEntityType(string dxfTypeName)
        {
            if (string.IsNullOrWhiteSpace(dxfTypeName)) return null;
            return new SelectionFilter(new[]
            {
                new TypedValue((int)DxfCode.Start, dxfTypeName.ToUpper())
            });
        }

        /// <summary>
        /// Tạo SelectionFilter kết hợp loại entity VÀ layer.
        /// Ví dụ: chỉ chọn LWPOLYLINE trên layer "WALL".
        /// </summary>
        public static SelectionFilter FilterByTypeAndLayer(string dxfTypeName, string layerName)
        {
            return new SelectionFilter(new[]
            {
                new TypedValue((int)DxfCode.Start,    dxfTypeName.ToUpper()),
                new TypedValue((int)DxfCode.LayerName, layerName)
            });
        }

        /// <summary>
        /// Tạo SelectionFilter theo group code tùy chỉnh.
        /// Dùng cho các filter phức tạp (Block name, XData, v.v.).
        /// Ví dụ: FilterByGroupCode((70, 1), (8, "MY_LAYER"))
        /// </summary>
        public static SelectionFilter FilterByGroupCodes(params (int code, object value)[] codes)
        {
            var tvs = new TypedValue[codes.Length];
            for (int i = 0; i < codes.Length; i++)
                tvs[i] = new TypedValue(codes[i].code, codes[i].value);
            return new SelectionFilter(tvs);
        }

        // ─── ObjectId Collection Processing ──────────────────────────────────────

        /// <summary>
        /// Lấy tất cả entity kiểu T từ danh sách ObjectId trong transaction.
        /// Tự động bỏ qua các ObjectId không phải kiểu T.
        /// </summary>
        public static IEnumerable<T> OpenAs<T>(this IEnumerable<ObjectId> ids,
            Transaction tr, OpenMode mode = OpenMode.ForRead) where T : DBObject
        {
            if (ids == null || tr == null) yield break;
            foreach (var id in ids)
            {
                if (id.IsNull || !id.IsValid || id.IsErased) continue;
                var obj = tr.GetObject(id, mode, false, true) as T;
                if (obj != null) yield return obj;
            }
        }

        /// <summary>
        /// Lọc danh sách ObjectId theo layer.
        /// </summary>
        public static IEnumerable<ObjectId> WhereLayer(this IEnumerable<ObjectId> ids,
            Transaction tr, string layerName)
        {
            return ids.OpenAs<Entity>(tr)
                      .Where(e => string.Equals(e.Layer, layerName, StringComparison.OrdinalIgnoreCase))
                      .Select(e => e.Id);
        }

        /// <summary>
        /// Lọc danh sách ObjectId theo kiểu entity.
        /// </summary>
        public static IEnumerable<ObjectId> WhereType<T>(this IEnumerable<ObjectId> ids,
            Transaction tr) where T : Entity
        {
            return ids.OpenAs<Entity>(tr)
                      .OfType<T>()
                      .Select(e => e.Id);
        }

    }
}
