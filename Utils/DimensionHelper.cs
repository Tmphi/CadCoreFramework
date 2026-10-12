using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace CadCoreFramework.Utils
{
    /// <summary>
    /// Các tiện ích hỗ trợ tạo và cấu hình Dimension.
    /// </summary>
    public static class DimensionHelper
    {
        /// <summary>
        /// Tạo một AlignedDimension (Đo khoảng cách nghiêng/thẳng giữa 2 điểm).
        /// </summary>
        public static AlignedDimension CreateAlignedDimension(Point3d point1, Point3d point2, Point3d dimLinePoint)
        {
            var dim = new AlignedDimension
            {
                XLine1Point = point1,
                XLine2Point = point2,
                DimLinePoint = dimLinePoint,
                DimensionStyle = CadApplicationHelper.ActiveDatabase.Dimstyle
            };
            return dim;
        }

        /// <summary>
        /// Tạo một RotatedDimension (Đo khoảng cách theo phương trục ngang hoặc dọc, phổ biến nhất).
        /// Rotation = 0 (Đo ngang), Rotation = PI/2 (Đo dọc).
        /// </summary>
        public static RotatedDimension CreateLinearDimension(Point3d point1, Point3d point2, Point3d dimLinePoint, double rotation = 0)
        {
            var dim = new RotatedDimension
            {
                XLine1Point = point1,
                XLine2Point = point2,
                DimLinePoint = dimLinePoint,
                Rotation = rotation,
                DimensionStyle = CadApplicationHelper.ActiveDatabase.Dimstyle
            };
            return dim;
        }
    }
}
