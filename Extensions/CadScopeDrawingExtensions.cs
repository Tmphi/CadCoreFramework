// ═══════════════════════════════════════════════════════════════════════════════
// CadScopeDrawingExtensions.cs — BACKWARD COMPATIBILITY WRAPPER
//
// File gốc (1,159 dòng) đã được tách thành các file nhỏ hơn:
//   • CadScopeLayerExtensions.cs      — Layer, TextStyle, Linetype, DimStyle, Block definitions
//   • CadScopeLineExtensions.cs       — Line, Polyline, Rectangle, Circle, Arc, Ellipse, Spline, Wipeout
//   • CadScopeTextExtensions.cs       — DBText, MText, MLeader
//   • CadScopeDimensionExtensions.cs   — Aligned, Linear, Diametric, Radial, Angular, Ordinate dimensions
//   • CadScopeBlockExtensions.cs      — InsertBlock, Table, Hatch, Group, Copy, Mirror
//   • CadScopeQueryExtensions.cs      — GetEntities, AskPoint, AskString, AskKeyword, AskDouble...
//   • CadScopeDrawingHelpers.cs       — Internal shared helpers (ApplyCommonProperties, ApplyDimStyle)
//
// File này CHỈ giữ lại struct PolylineVertex (dùng chung bởi nhiều file) và
// class rỗng CadScopeDrawingExtensions để code cũ using CadCoreFramework.Extensions
// vẫn hoạt động bình thường — KHÔNG cần sửa using ở bất kỳ đâu.
// ═══════════════════════════════════════════════════════════════════════════════

using Autodesk.AutoCAD.Geometry;

namespace CadCoreFramework.Extensions
{
    /// <summary>
    /// Định nghĩa đỉnh của Polyline kèm độ cong (Bulge) và bề rộng nét.
    /// Bulge = tan(góc_ôm_cung / 4). Bulge = 0 là đoạn thẳng, Bulge = 1 là nửa đường tròn 180°.
    /// </summary>
    public struct PolylineVertex
    {
        public Point2d Point { get; set; }
        public double Bulge { get; set; }
        public double StartWidth { get; set; }
        public double EndWidth { get; set; }

        public PolylineVertex(Point2d point, double bulge = 0.0, double startWidth = 0.0, double endWidth = 0.0)
        {
            Point = point;
            Bulge = bulge;
            StartWidth = startWidth;
            EndWidth = endWidth;
        }

        public PolylineVertex(double x, double y, double bulge = 0.0, double startWidth = 0.0, double endWidth = 0.0)
            : this(new Point2d(x, y), bulge, startWidth, endWidth) { }
    }

    // ─── BACKWARD COMPATIBILITY NOTE ────────────────────────────────────────────
    // Class CadScopeDrawingExtensions không còn chứa methods.
    // Tất cả extension methods đã chuyển sang các class chuyên biệt ở trên.
    // Vì tất cả đều nằm trong namespace CadCoreFramework.Extensions,
    // code hiện hữu chỉ cần `using CadCoreFramework.Extensions;` là đủ —
    // KHÔNG cần thay đổi bất kỳ using nào.
    // ─────────────────────────────────────────────────────────────────────────────
}
