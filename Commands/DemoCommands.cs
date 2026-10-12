using Autodesk.AutoCAD.Runtime;
using CadCoreFramework.Core;

namespace CadCoreFramework.Commands
{
    /// <summary>
    /// Lệnh điều phối và hướng dẫn sử dụng bộ lệnh minh họa (Demo Suite) của CadCoreFramework.
    /// Toàn bộ mã nguồn các lệnh demo đã được tách thành các file chuyên biệt trong thư mục Commands/Demos/.
    /// </summary>
    public class DemoCommands : CadCommandBase
    {
        private INotificationService Notify
            => GetRequiredService<INotificationService>();

        /// <summary>
        /// DEMO_HELP — In ra danh sách toàn bộ các lệnh minh họa (Demos) phân theo từng chuyên mục.
        /// </summary>
        [CommandMethod("DEMO_HELP")]
        public void DemoHelp() => ExecuteCommand(PrintHelp);

        private void PrintHelp()
        {
            Notify.Section("DANH MỤC CÁC LỆNH MINH HỌA (DEMO SUITE)");

            Notify.Info("1. NHÓM VẼ HÌNH HỌC & CHỮ (DemoDrawingCommands):");
            Notify.Info("   • DEMO_DRAW_SHAPES      : Vẽ hình học cơ bản & nâng cao (Line, Circle, Arc, Polyline, Slot, Hatch...)");
            Notify.Info("   • DEMO_DRAW_TEXTS       : Vẽ DBText, MText và MLeader");
            Notify.Info("   • DEMO_DRAW_TABLE       : Tạo bảng kỹ thuật CadTableBuilder chuẩn Fluent API");
            Notify.Info("   • DEMO_DRAW_LEADER      : Vẽ đường dẫn chú thích thiết bị (MLeader)");

            Notify.Info("\n2. NHÓM TƯƠNG TÁC NGƯỜI DÙNG & JIG (DemoInteractionCommands):");
            Notify.Info("   • DEMO_PROMPT           : Nhập liệu đủ kiểu dữ liệu với CadPromptHelper");
            Notify.Info("   • DEMO_SELECTION_FILTER : Lọc đối tượng nâng cao với SelectionFilterBuilder");
            Notify.Info("   • DEMO_JIG_RECTANGLE    : Kéo thả vẽ hình chữ nhật động (RectangleJig)");
            Notify.Info("   • DEMO_JIG_BLOCK        : Kéo thả đặt Block động (BlockPlacementJig)");

            Notify.Info("\n3. NHÓM DỮ LIỆU & METADATA (DemoDataCommands):");
            Notify.Info("   • DEMO_XDATA            : Ghi & đọc JSON qua XData (nhanh, giới hạn 16KB)");
            Notify.Info("   • DEMO_XRECORD          : Ghi & đọc JSON qua XRecord (không giới hạn dung lượng)");
            Notify.Info("   • DEMO_DYNAMIC_BLOCK    : Đọc thuộc tính và Visibility của Block động");

            Notify.Info("\n4. NHÓM DỊCH VỤ HẠ TẦNG (DemoServicesCommands):");
            Notify.Info("   • DEMO_SETTINGS         : Quản lý cấu hình JSON tự động (ISettingsService)");
            Notify.Info("   • DEMO_PROGRESS         : Hiển thị thanh tiến trình Status Bar (IProgressMeterService)");
            Notify.Info("   • DEMO_NOTIFICATION     : Hiển thị các định dạng thông báo (INotificationService)");
            Notify.Info("   • DEMO_DI_RESOLVE       : Kiểm tra tính sẵn sàng của toàn bộ DI Services");
            Notify.Info("   • DEMO_PLOT_PDF         : Danh sách máy in và khổ giấy hỗ trợ (IPlotService)");

            Notify.Info("\n5. NHÓM SỰ KIỆN & GIAO TIẾP (DemoEventCommands):");
            Notify.Info("   • DEMO_EVENT_SUBSCRIBE  : Đăng ký nhận thông điệp qua IEventAggregator");
            Notify.Info("   • DEMO_EVENT_PUBLISH    : Phát hành thông điệp qua IEventAggregator");
            Notify.Info("   • DEMO_REALTIME_INSPECT : Bật/tắt theo dõi sự kiện vẽ thêm đối tượng thời gian thực");

            Notify.Info("\n6. NHÓM OVERRULE HIỂN THỊ (DemoOverruleCommands):");
            Notify.Info("   • DEMO_OVERRULE_TOGGLE  : Bật/tắt hiệu ứng can thiệp đồ họa Overrule (vẽ tâm Circle)");

            Notify.Info("\n7. NHÓM GIAO DIỆN WPF (DemoUiCommands):");
            Notify.Info("   • DEMO_SHOW_LAYER_MANAGER: Mở Palette quản lý Layer theo mô hình MVVM");
            Notify.Info("   • DEMO_SHOW_WINDOW_DIALOG: Mở cửa sổ WPF Modal Dialog chuẩn AutoCAD Owner");

            Notify.Info("\n8. GOLDEN SAMPLE — KHUÔN MẪU CHUẨN (Samples/GridMarker, docs/09):");
            Notify.Info("   • SAMPLE_GRID           : Vẽ lưới ô đánh nhãn (prompt → validate → planner → service)");
            Notify.Info("   • SHOW_PALETTE          : Palette Grid Marker (MVVM, validate, EventAggregator)");

            Notify.Section("GÕ BẤT KỲ TÊN LỆNH NÀO Ở TRÊN ĐỂ CHẠY MINH HỌA");
        }
    }
}
