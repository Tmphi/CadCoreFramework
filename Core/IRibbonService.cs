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

        /// <summary>Thêm một Tab mới lên thanh Ribbon.</summary>
        void AddTab(string tabName, string tabTitle, string tabId = null);

        /// <summary>Thêm một Panel vào Tab chỉ định trên Ribbon.</summary>
        void AddPanel(string tabName, string panelName, string panelTitle);

        /// <summary>Thêm một nút bấm (Button) vào Panel trên Ribbon.</summary>
        void AddButton(string tabName, string panelName, string buttonText, string commandName, string iconPath = null, string tooltip = null);

        /// <summary>Thêm một nút thả xuống (SplitButton) chứa danh sách các nút con vào Panel.</summary>
        void AddSplitButton(string tabName, string panelName, string text, IEnumerable<(string text, string command, string iconPath)> items);

        /// <summary>
        /// Đăng ký hàm dựng lại Ribbon. Sẽ được gọi tự động sau khi người dùng reset giao diện (CUI, MENULOAD...).
        /// </summary>
        void RegisterBuildAction(Action buildAction);
    }
}
