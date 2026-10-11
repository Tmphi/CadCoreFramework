namespace CadCoreFramework.ViewModels
{
    /// <summary>
    /// Interface nhỏ để PaletteService có thể bật/tắt trạng thái "đang tải"
    /// mà không cần biết đến ViewModelBase hay bất kỳ lớp cụ thể nào.
    /// Giải quyết vấn đề dependency ngược (Core layer → ViewModel layer).
    /// </summary>
    public interface IBusyIndicator
    {
        /// <summary>
        /// Đặt thành true để hiện spinner/loading, false để ẩn đi.
        /// </summary>
        bool IsBusy { get; set; }
    }
}
