using System;
using System.Collections.Generic;

namespace CadCoreFramework.Core
{
    /// <summary>
    /// Giao diện dịch vụ quản lý thanh Ribbon của AutoCAD.
    /// Cho phép tạo Tab, Panel, Button, SplitButton và tự động phục hồi Ribbon khi giao diện bị reset.
    /// </summary>
    public interface IRibbonService
    {
        /// <summary>Khởi tạo kết nối tới hệ thống Ribbon của AutoCAD.</summary>
        void InitializeRibbon();

        /// <summary>Hủy đăng ký sự kiện và dọn dẹp Ribbon để tránh memory leak.</summary>
        void Shutdown();

        /// <summary>Thêm một Tab mới lên thanh Ribbon (đã có Tab cùng Id thì dùng lại).</summary>
        void AddTab(string tabName, string tabTitle, string tabId = null);

        /// <summary>
        /// Gỡ Tab có Id (hoặc Name) chỉ định khỏi Ribbon, nếu có. Gọi đầu hàm build để việc dựng lại
        /// (sau CUI/WSCURRENT…) luôn cho ra đúng một bản, không nhân đôi Panel/Button.
        /// </summary>
        void RemoveTab(string tabId);

        /// <summary>Thêm một Panel vào Tab chỉ định trên Ribbon.</summary>
        void AddPanel(string tabName, string panelName, string panelTitle);

        /// <summary>Thêm một nút bấm (Button) vào Panel trên Ribbon.</summary>
        void AddButton(string tabName, string panelName, string buttonText, string commandName, string iconPath = null, string tooltip = null);

        /// <summary>Thêm một nút thả xuống (SplitButton) chứa danh sách các nút con vào Panel.</summary>
        void AddSplitButton(string tabName, string panelName, string text, IEnumerable<(string text, string command, string iconPath)> items);

        /// <summary>
        /// Đăng ký hàm dựng Ribbon. Được gọi ngay khi Ribbon sẵn sàng (lúc này hoặc khi AutoCAD tạo xong
        /// Ribbon — thứ tự gọi so với <see cref="InitializeRibbon"/> không quan trọng), và tự động gọi lại
        /// sau khi người dùng reset giao diện (CUI, MENULOAD, WSCURRENT...). Hàm build nên idempotent:
        /// bắt đầu bằng <see cref="RemoveTab"/>.
        /// </summary>
        void RegisterBuildAction(Action buildAction);
    }
}
