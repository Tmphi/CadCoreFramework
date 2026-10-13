using CadCoreFramework.Core;

namespace CadCoreFramework.Commands
{
    /// <summary>
    /// Tab Ribbon "CadCore" của plugin — điểm vào bằng chuột cho các lệnh chính.
    ///
    /// Được <see cref="AppBootstrapper"/> đăng ký qua <see cref="IRibbonService.RegisterBuildAction"/>:
    /// RibbonService gọi <see cref="Build"/> khi Ribbon sẵn sàng (NETLOAD muộn hoặc lúc AutoCAD khởi động)
    /// và gọi LẠI sau mỗi lần CUI/MENULOAD/WSCURRENT reset giao diện. Vì thế Build phải idempotent:
    /// gỡ tab cũ trước rồi dựng mới.
    ///
    /// THÊM NÚT: một dòng AddButton(TabId, panel, "Nhãn", "TÊN_LỆNH", tooltip: "…") — tên lệnh là tên
    /// [CommandMethod] (nút gửi "_.TÊN_LỆNH" vào dòng lệnh). Xem docs/training/T08 §6.
    /// </summary>
    public static class CadCoreRibbon
    {
        /// <summary>Id (và Name) của tab — dùng cho RemoveTab.</summary>
        public const string TabId = "CADCORE_TAB";

        private const string ToolsPanel = "CADCORE_PANEL_TOOLS";
        private const string DevPanel   = "CADCORE_PANEL_DEV";

        /// <summary>Dựng (lại) toàn bộ tab. An toàn khi gọi nhiều lần.</summary>
        public static void Build(IRibbonService ribbon)
        {
            ribbon.RemoveTab(TabId);
            ribbon.AddTab(TabId, "CadCore", TabId);

            ribbon.AddPanel(TabId, ToolsPanel, "Công cụ");
            ribbon.AddButton(TabId, ToolsPanel, "Grid\nMarker", "SAMPLE_GRID",
                tooltip: "SAMPLE_GRID — Vẽ lưới ô đánh nhãn tại điểm chọn");
            ribbon.AddButton(TabId, ToolsPanel, "Palette\nGrid Marker", "SHOW_PALETTE",
                tooltip: "SHOW_PALETTE — Bật/tắt palette Grid Marker");
            ribbon.AddButton(TabId, ToolsPanel, "Layer\nManager", "CADCORE_LAYER_MANAGER",
                tooltip: "CADCORE_LAYER_MANAGER — Bật/tắt palette quản lý Layer");

            ribbon.AddPanel(TabId, DevPanel, "Phát triển");
            ribbon.AddButton(TabId, DevPanel, "Debug\nInspector", "CORE_DEBUG",
                tooltip: "CORE_DEBUG — Command history, DI services, event subscribers");
            ribbon.AddButton(TabId, DevPanel, "Thông tin\nplugin", "CORE_INFO",
                tooltip: "CORE_INFO — Kiểm tra plugin đã nạp và DI hoạt động");
        }
    }
}
