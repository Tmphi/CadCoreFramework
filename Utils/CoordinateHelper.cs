using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Geometry;

namespace CadCoreFramework.Utils
{
    /// <summary>
    /// Các tiện ích chuyển đổi giữa các hệ trục tọa độ của AutoCAD (WCS, UCS, DCS).
    /// Tính toán ma trận một lần (snapshot) để tránh việc phải gọi hàm Inverse() liên tục tốn tài nguyên.
    /// </summary>
    public static class CoordinateHelper
    {
        /// <summary>
        /// Lấy ra cặp ma trận chuyển đổi (UcsToWcs, WcsToUcs) của hệ trục tọa độ hiện tại.
        /// Hãy gọi hàm này MỘT LẦN DUY NHẤT ở đầu quá trình chuyển đổi hàng loạt (batch).
        /// </summary>
        public static (Matrix3d ucsToWcs, Matrix3d wcsToUcs) GetCurrentMatrices()
        {
            Matrix3d ucsToWcs = Application.DocumentManager.MdiActiveDocument?.Editor
                .CurrentUserCoordinateSystem ?? Matrix3d.Identity;
            return (ucsToWcs, ucsToWcs.Inverse());
        }

        /// <summary>
        /// Chuyển đổi một điểm từ hệ trục tọa độ người dùng (UCS) sang tọa độ thế giới (WCS).
        /// CẢNH BÁO: Hàm này gọi Inverse() mỗi lần — chỉ dùng cho từng điểm lẻ.
        /// Nếu chuyển hàng loạt, hãy dùng GetCurrentMatrices() và UcsToWcs(IEnumerable).
        /// </summary>
        public static Point3d UcsToWcs(Point3d ucsPoint)
        {
            return ucsPoint.TransformBy(GetCurrentMatrices().ucsToWcs);
        }

        /// <summary>
        /// Chuyển đổi một điểm từ tọa độ thế giới (WCS) sang hệ trục tọa độ người dùng (UCS).
        /// CẢNH BÁO: Hàm này gọi Inverse() mỗi lần — chỉ dùng cho từng điểm lẻ.
        /// Nếu chuyển hàng loạt, hãy dùng GetCurrentMatrices() và WcsToUcs(IEnumerable).
        /// </summary>
        public static Point3d WcsToUcs(Point3d wcsPoint)
        {
            return wcsPoint.TransformBy(GetCurrentMatrices().wcsToUcs);
        }

        /// <summary>
        /// Chuyển đổi một vector từ hệ UCS sang WCS.
        /// CẢNH BÁO: Gọi Inverse() mỗi lần — dùng cho từng vector lẻ.
        /// </summary>
        public static Vector3d UcsToWcs(Vector3d ucsVector)
        {
            return ucsVector.TransformBy(GetCurrentMatrices().ucsToWcs);
        }

        /// <summary>
        /// Chuyển đổi một vector từ hệ WCS sang UCS.
        /// CẢNH BÁO: Gọi Inverse() mỗi lần — dùng cho từng vector lẻ.
        /// </summary>
        public static Vector3d WcsToUcs(Vector3d wcsVector)
        {
            return wcsVector.TransformBy(GetCurrentMatrices().wcsToUcs);
        }

        // --- BATCH OVERLOADS (chỉ gọi GetCurrentMatrices() đúng 1 lần cho cả danh sách) ---

        /// <summary>
        /// Chuyển đổi hàng loạt điểm từ UCS sang WCS.
        /// Chỉ gọi Inverse() một lần duy nhất — dùng cho batch convert hiệu quả.
        /// </summary>
        public static IEnumerable<Point3d> UcsToWcs(IEnumerable<Point3d> ucsPoints)
        {
            var matrices = GetCurrentMatrices();
            return ucsPoints.Select(p => p.TransformBy(matrices.ucsToWcs));
        }

        /// <summary>
        /// Chuyển đổi hàng loạt điểm từ WCS sang UCS.
        /// Chỉ gọi Inverse() một lần duy nhất — dùng cho batch convert hiệu quả.
        /// </summary>
        public static IEnumerable<Point3d> WcsToUcs(IEnumerable<Point3d> wcsPoints)
        {
            var matrices = GetCurrentMatrices();
            return wcsPoints.Select(p => p.TransformBy(matrices.wcsToUcs));
        }
    }
}
