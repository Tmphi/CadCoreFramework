using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;

namespace CadCoreFramework.Extensions
{
    /// <summary>
    /// Bộ extension method hỗ trợ làm việc với Layout và Viewport trong AutoCAD.
    /// Bao gồm quản lý Paper Space, chuyển đổi giữa Model/Paper Space, và thao tác Viewport.
    ///
    /// TRIẾT LÝ AUTODESK:
    /// - LayoutManager là cổng vào chính để làm việc với Layout (Paper Space).
    /// - Model Space là một Layout đặc biệt có tên "*Model_Space".
    /// - Mỗi Layout (Paper Space) có ít nhất một Viewport (để nhìn vào Model Space).
    /// - Dùng LayoutManager.SetCurrentLayout() thay vì thao tác trực tiếp vào database.
    /// </summary>
    public static class LayoutExtensions
    {
        // ─── Layout Queries ───────────────────────────────────────────────────────

        /// <summary>
        /// Lấy danh sách tất cả Layout đang có trong bản vẽ (bao gồm Model Space).
        /// Kết quả được sắp xếp theo TabOrder.
        /// </summary>
        public static IEnumerable<Layout> GetAllLayouts(this Database db, Transaction tr)
        {
            if (db == null || tr == null) yield break;

            var layoutDict = (DBDictionary)tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead);
            foreach (DBDictionaryEntry entry in layoutDict)
            {
                var layout = tr.GetObject(entry.Value, OpenMode.ForRead) as Layout;
                if (layout != null) yield return layout;
            }
        }

        /// <summary>
        /// Lấy danh sách chỉ các Paper Space Layout (không bao gồm Model Space).
        /// Sắp xếp theo TabOrder.
        /// </summary>
        public static IEnumerable<Layout> GetPaperLayouts(this Database db, Transaction tr)
            => db.GetAllLayouts(tr)
                 .Where(l => !l.ModelType)
                 .OrderBy(l => l.TabOrder);

        /// <summary>
        /// Lấy Layout theo tên. Trả về null nếu không tìm thấy.
        /// </summary>
        public static Layout GetLayoutByName(this Database db, Transaction tr, string layoutName)
        {
            if (string.IsNullOrWhiteSpace(layoutName)) return null;
            return db.GetAllLayouts(tr)
                     .FirstOrDefault(l => string.Equals(l.LayoutName, layoutName,
                         StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Lấy Layout hiện tại đang được kích hoạt.
        /// </summary>
        public static Layout GetCurrentLayout(this Database db, Transaction tr)
            => tr.GetObject(LayoutManager.Current.GetLayoutId(LayoutManager.Current.CurrentLayout), OpenMode.ForRead) as Layout;

        // ─── Layout Activation ────────────────────────────────────────────────────

        /// <summary>
        /// Chuyển sang Layout theo tên. Dùng LayoutManager.SetCurrentLayout() chuẩn Autodesk.
        /// Trả về true nếu chuyển thành công.
        /// </summary>
        public static bool SetActiveLayout(this Database db, string layoutName)
        {
            if (string.IsNullOrWhiteSpace(layoutName)) return false;
            try
            {
                LayoutManager.Current.CurrentLayout = layoutName;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Chuyển về Model Space.
        /// </summary>
        public static void SwitchToModelSpace(this Database db)
            => LayoutManager.Current.CurrentLayout = "Model";

        // ─── Layout BlockTableRecord ──────────────────────────────────────────────

        /// <summary>
        /// Lấy BlockTableRecord của Layout (để enumerate entity trong layout đó).
        /// </summary>
        public static BlockTableRecord GetLayoutBlock(this Layout layout, Transaction tr)
        {
            if (layout == null || tr == null) return null;
            return tr.GetObject(layout.BlockTableRecordId, OpenMode.ForRead) as BlockTableRecord;
        }

        /// <summary>
        /// Enumerate tất cả Entity trong một Layout cụ thể.
        /// </summary>
        public static IEnumerable<T> GetEntitiesInLayout<T>(
            this Layout layout, Transaction tr) where T : Entity
        {
            var btr = layout.GetLayoutBlock(tr);
            if (btr == null) yield break;

            foreach (ObjectId id in btr)
            {
                if (id.IsNull || !id.IsValid || id.IsErased) continue;
                var entity = tr.GetObject(id, OpenMode.ForRead, false, true) as T;
                if (entity != null) yield return entity;
            }
        }

        // ─── Viewport ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Lấy danh sách tất cả Viewport trong một Paper Space Layout.
        /// Lưu ý: Viewport số 1 (Overall Viewport) thường bị ẩn — được lọc ra mặc định.
        /// </summary>
        public static IEnumerable<Viewport> GetViewports(this Layout layout,
            Transaction tr, bool includeOverall = false)
        {
            var btr = layout.GetLayoutBlock(tr);
            if (btr == null) yield break;

            foreach (ObjectId id in btr)
            {
                if (!id.IsValid || id.IsErased) continue;
                var vp = tr.GetObject(id, OpenMode.ForRead, false, true) as Viewport;
                if (vp == null) continue;
                // Overall viewport (number 1) thường là Overall viewport dùng nội bộ
                if (!includeOverall && vp.Number == 1) continue;
                yield return vp;
            }
        }

        /// <summary>
        /// Tạo một Viewport mới trong Paper Space Layout với kích thước và vị trí chỉ định.
        /// </summary>
        public static ObjectId CreateViewport(this Layout layout, Transaction tr,
            Autodesk.AutoCAD.Geometry.Point3d center,
            double width, double height,
            double scale = 1.0 / 100.0)
        {
            var btr = tr.GetObject(layout.BlockTableRecordId, OpenMode.ForWrite) as BlockTableRecord;
            if (btr == null) return ObjectId.Null;

            var vp = new Viewport
            {
                CenterPoint    = center,
                Width          = width,
                Height         = height,
                CustomScale    = scale,
                On             = true
            };

            ObjectId vpId = btr.AppendEntity(vp);
            tr.AddNewlyCreatedDBObject(vp, true);
            vp.On = true; // phải gọi sau khi add vào database
            return vpId;
        }

        // ─── Layout Creation ──────────────────────────────────────────────────────

        /// <summary>
        /// Tạo một Paper Space Layout mới với tên chỉ định.
        /// Trả về tên Layout sau khi tạo (AutoCAD có thể tự điều chỉnh tên).
        /// </summary>
        public static ObjectId CreateLayout(string layoutName)
        {
            if (string.IsNullOrWhiteSpace(layoutName)) return ObjectId.Null;
            return LayoutManager.Current.CreateLayout(layoutName);
        }

        /// <summary>
        /// Xóa một Layout theo tên. Không thể xóa Model Space.
        /// </summary>
        public static bool DeleteLayout(string layoutName)
        {
            if (string.IsNullOrWhiteSpace(layoutName) ||
                string.Equals(layoutName, "Model", StringComparison.OrdinalIgnoreCase))
                return false;
            try
            {
                LayoutManager.Current.DeleteLayout(layoutName);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
