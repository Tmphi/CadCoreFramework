namespace CadCoreFramework.Models
{
    /// <summary>
    /// Đại diện cho một đường tròn được xác định bởi tọa độ tâm và bán kính.
    /// Dùng làm đối tượng truyền dữ liệu (DTO) cho các thao tác vẽ hình.
    /// </summary>
    public class CircleData
    {
        /// <summary>Tọa độ X của tâm đường tròn.</summary>
        public double X { get; set; }

        /// <summary>Tọa độ Y của tâm đường tròn.</summary>
        public double Y { get; set; }

        /// <summary>Bán kính đường tròn. Phải lớn hơn 0.</summary>
        public double Radius { get; set; }

        /// <summary>
        /// Khởi tạo một đối tượng CircleData mặc định.
        /// </summary>
        public CircleData() { }

        /// <summary>
        /// Khởi tạo một đối tượng CircleData với tọa độ tâm và bán kính cụ thể.
        /// </summary>
        public CircleData(double x, double y, double radius)
        {
            X = x;
            Y = y;
            Radius = radius;
        }
    }
}
