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
