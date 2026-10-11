using CommunityToolkit.Mvvm.ComponentModel;

namespace CadCoreFramework.Models
{
    /// <summary>
    /// Lớp dữ liệu (Model) đại diện cho một Layer trong AutoCAD.
    /// Kế thừa ObservableObject để hỗ trợ tính năng Data Binding trong MVVM (thông báo khi thuộc tính thay đổi).
    /// </summary>
    public partial class LayerModel : ObservableObject
    {
        /// <summary>
        /// Tên của Layer.
        /// </summary>
        [ObservableProperty]
        private string _name;

        /// <summary>
        /// Trạng thái Bật/Tắt (On/Off) của Layer.
        /// </summary>
        [ObservableProperty]
        private bool _isOn;

        /// <summary>
        /// Mã màu ACI (AutoCAD Color Index) từ 1 đến 255.
        /// </summary>
        [ObservableProperty]
        private short _colorIndex;

        /// <summary>
        /// Khởi tạo một đối tượng LayerModel mới với đầy đủ thông tin.
        /// </summary>
        /// <param name="name">Tên Layer.</param>
        /// <param name="isOn">Trạng thái Bật/Tắt.</param>
        /// <param name="colorIndex">Mã màu ACI.</param>
        public LayerModel(string name, bool isOn, short colorIndex)
        {
            _name = name;
            _isOn = isOn;
            _colorIndex = colorIndex;
        }
    }
}
