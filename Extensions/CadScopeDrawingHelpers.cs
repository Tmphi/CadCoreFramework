using System;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using CadCoreFramework.Core;

namespace CadCoreFramework.Extensions
{
    /// <summary>
    /// Các hàm trợ giúp nội bộ (internal) dùng chung cho tất cả CadScope*Extensions.
    /// Không phải extension methods — chỉ là static helper methods.
    /// </summary>
    internal static class CadScopeDrawingHelpers
    {
        /// <summary>
        /// Hoàn tất một entity MỚI tạo và đưa vào không gian hiện hành:
        /// <paramref name="setup"/> (hình học, SetDatabaseDefaults…) → layer + màu → <paramref name="configure"/>
        /// của caller → append. Bất kỳ bước nào ném lỗi TRƯỚC khi entity vào database thì entity được
        /// Dispose rồi ném lại — đối tượng ngoài database phải tự giải phóng (T01 §9; HeadlessLab
        /// DRAW_CONFIGURE_THROWS_DISPOSES).
        /// </summary>
        internal static ObjectId AppendNew<T>(ICadScope scope, T entity, Action<T> setup,
            string layer, short? colorIndex, Action<T> configure) where T : Entity
        {
            try
            {
                setup?.Invoke(entity);
                ApplyCommonProperties(scope, entity, layer, colorIndex);
                configure?.Invoke(entity);
                return scope.AppendEntityToCurrentSpace(entity);
            }
            catch
            {
                DisposeIfNotInDatabase(entity);
                throw;
            }
        }

        /// <summary>Dispose một đối tượng chưa được thêm vào database (ObjectId còn Null).</summary>
        internal static void DisposeIfNotInDatabase(DBObject obj)
        {
            if (obj != null && !obj.IsDisposed && obj.ObjectId.IsNull) obj.Dispose();
        }

        /// <summary>
        /// Ghi lên command line CHỈ KHI bản vẽ của scope đang active. Editor của bản vẽ không active ném
        /// eNotApplicable (đã kiểm chứng trên AutoCAD 2024 — vd. giai đoạn vẽ của ExecuteAsync sau khi người
        /// dùng chuyển bản vẽ); một dòng cảnh báo không được phép làm hỏng cả transaction.
        /// </summary>
        internal static void WriteMessageIfActive(ICadScope scope, string message)
        {
            Autodesk.AutoCAD.ApplicationServices.Document doc = scope.Document;
            if (doc != null && doc == Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument)
                scope.Editor.WriteMessage(message);
        }

        /// <summary>
        /// Áp dụng Layer và Color cho entity. Tự động tạo Layer nếu chưa tồn tại.
        /// </summary>
        internal static void ApplyCommonProperties(ICadScope scope, Entity entity, string layer, short? colorIndex)
        {
            if (!string.IsNullOrWhiteSpace(layer))
            {
                scope.EnsureLayer(layer);
                entity.Layer = layer;
            }

            if (colorIndex.HasValue)
            {
                entity.Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex.Value);
            }
        }

        /// <summary>
        /// Áp dụng DimStyle theo tên cho đối tượng Dimension. Dùng mặc định của database nếu không tìm thấy.
        /// </summary>
        internal static void ApplyDimStyle(ICadScope scope, Dimension dim, string dimStyle)
        {
            if (string.IsNullOrWhiteSpace(dimStyle)) return;
            var dst = (DimStyleTable)scope.Transaction.GetObject(scope.Database.DimStyleTableId, OpenMode.ForRead);
            if (dst.Has(dimStyle))
                dim.DimensionStyle = dst[dimStyle];
        }
    }
}
