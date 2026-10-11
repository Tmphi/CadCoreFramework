using System;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using CadCoreFramework.Utils;

namespace CadCoreFramework.Extensions
{
    /// <summary>
    /// Các hàm tiện ích (Extension Methods) bám trực tiếp vào đối tượng Entity.
    /// Giúp việc di chuyển, xoay, scale, đổi màu, và gán metadata (XData) chỉ tốn 1 dòng code.
    /// </summary>
    public static class EntityExtensions
    {
        /// <summary>
        /// Di chuyển đối tượng theo một vector khoảng cách cho trước.
        /// </summary>
        public static void Move(this Entity entity, Vector3d displacement)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            Matrix3d mat = Matrix3d.Displacement(displacement);
            entity.TransformBy(mat);
        }

        /// <summary>
        /// Di chuyển đối tượng từ điểm này tới điểm khác.
        /// </summary>
        public static void Move(this Entity entity, Point3d fromPoint, Point3d toPoint)
        {
            Move(entity, toPoint - fromPoint);
        }

        /// <summary>
        /// Xoay đối tượng quanh một điểm gốc với trục Z.
        /// </summary>
        /// <param name="angleRadians">Góc xoay tính bằng Radian (Không phải độ).</param>
        public static void Rotate(this Entity entity, Point3d basePoint, double angleRadians)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            Matrix3d mat = Matrix3d.Rotation(angleRadians, Vector3d.ZAxis, basePoint);
            entity.TransformBy(mat);
        }

        /// <summary>
        /// Phóng to / Thu nhỏ đối tượng từ một điểm gốc.
        /// </summary>
        public static void Scale(this Entity entity, Point3d basePoint, double scaleFactor)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            Matrix3d mat = Matrix3d.Scaling(scaleFactor, basePoint);
            entity.TransformBy(mat);
        }

        /// <summary>
        /// Lấy đối xứng (Mirror) đối tượng qua đường thẳng xác định bởi hai điểm trong mặt phẳng XY.
        /// </summary>
        public static void Mirror(this Entity entity, Point3d point1, Point3d point2)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            var mirrorLine = new Line3d(point1, point2);
            Matrix3d mat = Matrix3d.Mirroring(mirrorLine);
            entity.TransformBy(mat);
        }

        /// <summary>
        /// Xoay đối tượng quanh một trục bất kỳ trong không gian 3D.
        /// </summary>
        /// <param name="basePoint">Điểm gốc trên trục xoay.</param>
        /// <param name="axis">Vector chỉ phương của trục xoay.</param>
        /// <param name="angleRadians">Góc xoay (Radian).</param>
        public static void Rotate3D(this Entity entity, Point3d basePoint, Vector3d axis, double angleRadians)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            Matrix3d mat = Matrix3d.Rotation(angleRadians, axis, basePoint);
            entity.TransformBy(mat);
        }

        /// <summary>
        /// Tạo bản sao (Clone) của đối tượng và dịch chuyển theo một vector (chưa thêm vào Database).
        /// </summary>
        public static Entity CloneAndDisplace(this Entity entity, Vector3d displacement)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            var clone = (Entity)entity.Clone();
            clone.TransformBy(Matrix3d.Displacement(displacement));
            return clone;
        }

        /// <summary>
        /// Lấy khung bao giới hạn (Bounding Box / GeometricExtents) an toàn.
        /// Trả về null nếu đối tượng không có hình học hợp lệ (tránh quăng ngoại lệ eInvalidExtents).
        /// </summary>
        public static Extents3d? GetSafeExtents(this Entity entity)
        {
            if (entity == null) return null;
            try
            {
                return entity.GeometricExtents;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Bật hoặc tắt hiệu ứng sáng viền (Highlight) của đối tượng trên màn hình đồ họa.
        /// Rất hữu ích khi người dùng chọn đối tượng từ danh sách UI hoặc bảng Palette.
        /// </summary>
        public static void SetHighlight(this Entity entity, bool enable = true)
        {
            if (entity == null) return;
            if (enable)
                entity.Highlight();
            else
                entity.Unhighlight();
        }

        /// <summary>
        /// Thiết lập độ trong suốt (Transparency) cho đối tượng (0 = Trong suốt hoàn toàn, 255 = Đục hoàn toàn).
        /// </summary>
        public static void SetTransparency(this Entity entity, byte alpha = 255)
        {
            if (entity == null) return;
            entity.Transparency = new Transparency(alpha);
        }

        /// <summary>
        /// Thiết lập bề dày nét vẽ (LineWeight).
        /// </summary>
        public static void SetLineWeight(this Entity entity, LineWeight weight)
        {
            if (entity == null) return;
            entity.LineWeight = weight;
        }

        /// <summary>
        /// Phá vỡ (Explode) đối tượng phức hợp (Block, Region, Polyline...) và thêm các phần tử thành phần vào không gian hiện hành.
        /// </summary>
        public static System.Collections.Generic.List<ObjectId> ExplodeToCurrentSpace(this Entity entity, CadCoreFramework.Core.ICadScope scope)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (scope == null) throw new ArgumentNullException(nameof(scope));

            var resultIds = new System.Collections.Generic.List<ObjectId>();
            using (var dbObjs = new DBObjectCollection())
            {
                entity.Explode(dbObjs);
                foreach (DBObject obj in dbObjs)
                {
                    if (obj is Entity ent)
                    {
                        ObjectId newId = scope.AppendEntityToCurrentSpace(ent);
                        resultIds.Add(newId);
                    }
                    else
                    {
                        obj.Dispose();
                    }
                }
            }
            return resultIds;
        }

        /// <summary>
        /// Đổi Layer cho đối tượng. (Chỉ đổi nếu tên Layer không rỗng).
        /// </summary>
        public static void SetLayer(this Entity entity, string layerName)
        {
            if (entity == null || string.IsNullOrWhiteSpace(layerName)) return;
            entity.Layer = layerName;
        }

        /// <summary>
        /// Đổi màu cho đối tượng bằng mã màu AutoCAD (ACI - Từ 1 đến 255).
        /// </summary>
        public static void SetColor(this Entity entity, short colorIndex)
        {
            if (entity == null) return;
            entity.Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex);
        }

        /// <summary>
        /// Đính kèm dữ liệu ẩn (Metadata) vào đối tượng dưới dạng JSON thông qua XData.
        /// Tuyệt vời để lưu trữ thông tin thiết bị (Mã SP, Kích thước, Công suất...).
        /// </summary>
        public static void SetMetadata<T>(this Entity entity, Transaction tr, string appName, T data)
        {
            XDataHelper.SetMetadata(tr, entity, appName, data);
        }

        /// <summary>
        /// Đọc dữ liệu ẩn (Metadata) đã lưu trong XData của đối tượng và parse ngược lại thành C# Object.
        /// </summary>
        public static T GetMetadata<T>(this Entity entity, string appName, T defaultValue = default)
        {
            return XDataHelper.GetMetadata(entity, appName, defaultValue);
        }

        /// <summary>
        /// Kiểm tra xem đối tượng này có chứa loại dữ liệu ẩn này không.
        /// </summary>
        public static bool HasMetadata(this Entity entity, string appName)
        {
            return XDataHelper.HasMetadata(entity, appName);
        }
    }
}
