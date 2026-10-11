using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace CadCoreFramework.Extensions
{
    /// <summary>
    /// Các phương thức mở rộng toán học và hình học cho Point3d, Point2d, Vector3d và Extents3d.
    /// Giúp việc tính toán tọa độ, vector, góc xoay và khung bao trở nên trực quan, ngắn gọn.
    /// </summary>
    public static class GeometryExtensions
    {
        #region Point3d & Point2d Extensions

        /// <summary>
        /// Tính trung điểm giữa điểm hiện tại và điểm đích.
        /// </summary>
        public static Point3d MidPointTo(this Point3d p1, Point3d p2)
        {
            return new Point3d(
                (p1.X + p2.X) * 0.5,
                (p1.Y + p2.Y) * 0.5,
                (p1.Z + p2.Z) * 0.5
            );
        }

        /// <summary>
        /// Tính trung điểm giữa điểm 2D hiện tại và điểm đích.
        /// </summary>
        public static Point2d MidPointTo(this Point2d p1, Point2d p2)
        {
            return new Point2d(
                (p1.X + p2.X) * 0.5,
                (p1.Y + p2.Y) * 0.5
            );
        }

        /// <summary>
        /// Tịnh tiến điểm theo các khoảng dịch chuyển delta X, delta Y, delta Z.
        /// </summary>
        public static Point3d Offset(this Point3d pt, double dx, double dy, double dz = 0.0)
        {
            return new Point3d(pt.X + dx, pt.Y + dy, pt.Z + dz);
        }

        /// <summary>
        /// Tịnh tiến điểm 2D theo delta X, delta Y.
        /// </summary>
        public static Point2d Offset(this Point2d pt, double dx, double dy)
        {
            return new Point2d(pt.X + dx, pt.Y + dy);
        }

        /// <summary>
        /// Chuyển đổi Point3d sang Point2d (bỏ qua thành phần Z).
        /// </summary>
        public static Point2d ToPoint2d(this Point3d pt)
        {
            return new Point2d(pt.X, pt.Y);
        }

        /// <summary>
        /// Chuyển đổi Point2d sang Point3d với cao độ Z tùy chọn.
        /// </summary>
        public static Point3d ToPoint3d(this Point2d pt, double z = 0.0)
        {
            return new Point3d(pt.X, pt.Y, z);
        }

        /// <summary>
        /// Tính góc xoay (Radian từ 0 đến 2π) từ điểm hiện tại đến điểm đích trên mặt phẳng XY.
        /// </summary>
        public static double AngleTo2D(this Point3d fromPoint, Point3d toPoint)
        {
            double angle = Math.Atan2(toPoint.Y - fromPoint.Y, toPoint.X - fromPoint.X);
            return angle < 0 ? angle + Math.PI * 2 : angle;
        }

        /// <summary>
        /// Tính góc xoay (Radian) giữa hai điểm 2D.
        /// </summary>
        public static double AngleTo2D(this Point2d fromPoint, Point2d toPoint)
        {
            double angle = Math.Atan2(toPoint.Y - fromPoint.Y, toPoint.X - fromPoint.X);
            return angle < 0 ? angle + Math.PI * 2 : angle;
        }

        #endregion

        #region Vector3d Extensions

        /// <summary>
        /// Tạo một vector vuông góc 90 độ theo chiều ngược chiều kim đồng hồ trong mặt phẳng XY.
        /// </summary>
        public static Vector3d Perpendicular2D(this Vector3d vec)
        {
            return new Vector3d(-vec.Y, vec.X, 0.0);
        }

        /// <summary>
        /// Chuẩn hóa vector an toàn (tránh lỗi chia cho 0 nếu vector có độ dài xấp xỉ 0).
        /// Nếu vector là Zero, trả về Vector3d.XAxis làm dự phòng.
        /// </summary>
        public static Vector3d NormalizeSafe(this Vector3d vec)
        {
            if (vec.IsZeroLength())
                return Vector3d.XAxis;
            return vec.GetNormal();
        }

        #endregion

        #region Extents3d (Bounding Box) Extensions

        /// <summary>
        /// Lấy chiều rộng (kích thước theo phương X) của khung bao.
        /// </summary>
        public static double Width(this Extents3d extents)
        {
            return Math.Abs(extents.MaxPoint.X - extents.MinPoint.X);
        }

        /// <summary>
        /// Lấy chiều cao (kích thước theo phương Y) của khung bao.
        /// </summary>
        public static double Height(this Extents3d extents)
        {
            return Math.Abs(extents.MaxPoint.Y - extents.MinPoint.Y);
        }

        /// <summary>
        /// Lấy điểm tâm hình học của khung bao.
        /// </summary>
        public static Point3d Center(this Extents3d extents)
        {
            return extents.MinPoint.MidPointTo(extents.MaxPoint);
        }

        /// <summary>
        /// Mở rộng khung bao về mọi phía một khoảng offset.
        /// </summary>
        public static Extents3d Expand(this Extents3d extents, double offset)
        {
            var min = new Point3d(extents.MinPoint.X - offset, extents.MinPoint.Y - offset, extents.MinPoint.Z - offset);
            var max = new Point3d(extents.MaxPoint.X + offset, extents.MaxPoint.Y + offset, extents.MaxPoint.Z + offset);
            return new Extents3d(min, max);
        }

        /// <summary>
        /// Hợp nhất (Union) khung bao hiện tại với một khung bao khác để lấy vùng bao phủ chung lớn nhất.
        /// </summary>
        public static Extents3d UnionWith(this Extents3d extents, Extents3d other)
        {
            var result = new Extents3d(extents.MinPoint, extents.MaxPoint);
            result.AddExtents(other);
            return result;
        }

        #endregion
    }
}
