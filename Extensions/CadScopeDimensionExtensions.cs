using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using CadCoreFramework.Core;

namespace CadCoreFramework.Extensions
{
    /// <summary>
    /// Extension methods cho ICadScope: vẽ kích thước (Dimension) các loại.
    /// </summary>
    public static class CadScopeDimensionExtensions
    {
        /// <summary>
        /// Vẽ kích thước thẳng hàng (Aligned Dimension) giữa hai điểm.
        /// </summary>
        public static ObjectId DrawAlignedDimension(this ICadScope scope, Point3d point1, Point3d point2,
            Point3d dimLinePoint, string dimStyle = null, string layer = null, Action<AlignedDimension> configure = null)
        {
            return AppendDimension(scope, new AlignedDimension(point1, point2, dimLinePoint, string.Empty, scope.Database.Dimstyle),
                dimStyle, layer, configure);
        }

        /// <summary>
        /// Vẽ kích thước dạng thẳng (Linear / Rotated Dimension) giữa hai điểm.
        /// </summary>
        public static ObjectId DrawLinearDimension(this ICadScope scope, Point3d point1, Point3d point2,
            Point3d dimLinePoint, double rotation = 0.0, string dimStyle = null, string layer = null, Action<RotatedDimension> configure = null)
        {
            return AppendDimension(scope, new RotatedDimension(rotation, point1, point2, dimLinePoint, string.Empty, scope.Database.Dimstyle),
                dimStyle, layer, configure);
        }

        /// <summary>
        /// Vẽ kích thước đường kính (Diametric Dimension) trên đường tròn/cung tròn (ghi ký hiệu đường kính ∅).
        /// chordPoint là điểm trên đường tròn đối diện với điểm mút đường dẫn.
        /// </summary>
        public static ObjectId DrawDiametricDimension(this ICadScope scope,
            Point3d chordPoint, Point3d farChordPoint, double leaderLength = 5.0,
            string dimStyle = null, string layer = null, Action<DiametricDimension> configure = null)
        {
            return AppendDimension(scope, new DiametricDimension(chordPoint, farChordPoint, leaderLength, string.Empty, scope.Database.Dimstyle),
                dimStyle, layer, configure);
        }

        /// <summary>
        /// Vẽ kích thước bán kính (Radial Dimension) trên đường tròn/cung tròn (ghi ký hiệu bán kính R).
        /// center là tâm cung tròn; chordPoint là điểm trên cung tròn.
        /// </summary>
        public static ObjectId DrawRadialDimension(this ICadScope scope,
            Point3d center, Point3d chordPoint, double leaderLength = 5.0,
            string dimStyle = null, string layer = null, Action<RadialDimension> configure = null)
        {
            return AppendDimension(scope, new RadialDimension(center, chordPoint, leaderLength, string.Empty, scope.Database.Dimstyle),
                dimStyle, layer, configure);
        }

        /// <summary>
        /// Vẽ kích thước bán kính gãy khúc (Jogged Large Radial Dimension) cho các cung tròn rất lớn có tâm nằm ngoài màn hình.
        /// jogPoint là điểm uốn của đường chỉ dẫn kích thước.
        /// </summary>
        public static ObjectId DrawJoggedRadialDimension(this ICadScope scope,
            Point3d center, Point3d chordPoint, Point3d jogPoint, double jogAngle = 0.785398,
            string dimStyle = null, string layer = null,
            Point3d? overrideCenter = null,
            Action<RadialDimensionLarge> configure = null)
        {
            var oc = overrideCenter ?? center;
            return AppendDimension(scope, new RadialDimensionLarge(center, chordPoint, oc, jogPoint, jogAngle, string.Empty, scope.Database.Dimstyle),
                dimStyle, layer, configure);
        }

        /// <summary>
        /// Vẽ kích thước góc (Angular Dimension) giữa hai đoạn thẳng xác định bởi 3 điểm:
        /// vertex (đỉnh góc), firstPoint (điểm trên cạnh 1), secondPoint (điểm trên cạnh 2).
        /// </summary>
        public static ObjectId DrawAngularDimension(this ICadScope scope,
            Point3d vertex, Point3d firstPoint, Point3d secondPoint, Point3d arcPoint,
            string dimStyle = null, string layer = null, Action<LineAngularDimension2> configure = null)
        {
            return AppendDimension(scope, new LineAngularDimension2(vertex, firstPoint, vertex, secondPoint, arcPoint, string.Empty, scope.Database.Dimstyle),
                dimStyle, layer, configure);
        }

        /// <summary>
        /// Vẽ kích thước tọa độ (Ordinate Dimension - ghi tọa độ X hoặc Y so với điểm gốc chuẩn datum).
        /// useXAxis = true → ghi tọa độ X; false → ghi tọa độ Y.
        /// </summary>
        public static ObjectId DrawOrdinateDimension(this ICadScope scope,
            Point3d definingPoint, Point3d leaderEndPoint, bool useXAxis = true,
            string dimStyle = null, string layer = null, Action<OrdinateDimension> configure = null)
        {
            return AppendDimension(scope, new OrdinateDimension(useXAxis, definingPoint, leaderEndPoint, string.Empty, scope.Database.Dimstyle),
                dimStyle, layer, configure);
        }

        /// <summary>Đuôi chung của mọi hàm vẽ kích thước: defaults → dimstyle → layer → configure → append (Dispose nếu lỗi).</summary>
        private static ObjectId AppendDimension<T>(ICadScope scope, T dim, string dimStyle, string layer, Action<T> configure)
            where T : Dimension
            => CadScopeDrawingHelpers.AppendNew(scope, dim, d =>
            {
                d.SetDatabaseDefaults(scope.Database);
                CadScopeDrawingHelpers.ApplyDimStyle(scope, d, dimStyle);
            }, layer, colorIndex: null, configure);
    }
}
