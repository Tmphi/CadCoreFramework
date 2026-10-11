using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.Windows;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace CadCoreFramework.Core
{
    /// <summary>
    /// Dịch vụ quản lý thanh Ribbon của AutoCAD (thanh menu trên cùng).
    /// Hỗ trợ tạo Tab mới, Panel mới, và thêm các nút bấm (Button, SplitButton) bằng code C#.
    /// Tự động rebuild Ribbon sau khi người dùng reset giao diện (CUI, QUICKCUI, MENULOAD).
    /// </summary>
    public class RibbonService : IRibbonService
    {
        private readonly ILoggerService _logger;
        private RibbonControl _ribbon;

        // Hàm dựng sẵn để rebuild Ribbon sau khi CUI reset.
        // Được gán bởi consumer (ví dụ SampleCommands) sau lần đầu tạo Ribbon.
        private Action _buildRibbonAction;

        // Lưu delegate reference để có thể -= khi Shutdown (tránh memory leak).
        private DocumentCollectionEventHandler _documentCreatedHandler;

        // Danh sách các lệnh AutoCAD có thể reset Ribbon khi chạy xong.
        private static readonly HashSet<string> UiResetCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CUI", "QUICKCUI", "CUIIMPORT", "CUIEXPORT", "MENULOAD", "MENUUNLOAD", "WSCURRENT", "WSSAVE"
        };

        public RibbonService(ILoggerService logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Khởi tạo và móc nối vào hệ thống Ribbon của AutoCAD.
        /// Phải gọi hàm này trước khi thêm bất kỳ Tab hay Panel nào.
        /// </summary>
        public void InitializeRibbon()
        {
            if (_ribbon != null) return;
            try
            {
                _ribbon = ComponentManager.Ribbon;
                if (_ribbon == null)
                {
                    ComponentManager.ItemInitialized += ComponentManager_ItemInitialized;
                }

                // Hook CommandEnded để phát hiện khi người dùng chạy lệnh CUI/MENULOAD.
                // Lưu delegate reference thay vì dùng lambda anonymous — cho phép -= khi cleanup.
                _documentCreatedHandler = (s, e) => HookDocumentCommandEvents(e.Document);
                Application.DocumentManager.DocumentCreated += _documentCreatedHandler;
                foreach (Document doc in Application.DocumentManager)
                    HookDocumentCommandEvents(doc);
            }
            catch (Exception ex)
            {
                _logger?.Error("Không thể khởi tạo Ribbon", ex);
            }
        }

        /// <summary>
        /// Hủy đăng ký sự kiện và dọn dẹp để tránh memory leak.
        /// </summary>
        public void Shutdown()
        {
            try
            {
                if (_documentCreatedHandler != null)
                {
                    Application.DocumentManager.DocumentCreated -= _documentCreatedHandler;
                    _documentCreatedHandler = null;
                }
                foreach (Document doc in Application.DocumentManager)
                {
                    doc.CommandEnded -= OnCommandEnded;
                }
                if (ComponentManager.Ribbon != null)
                {
                    ComponentManager.ItemInitialized -= ComponentManager_ItemInitialized;
                }
                _ribbon = null;
            }
            catch (Exception ex)
            {
                _logger?.Error("Lỗi khi dừng RibbonService", ex);
            }
        }

        private void HookDocumentCommandEvents(Document doc)
        {
            if (doc == null) return;
            // Gỡ trước rồi đăng ký lại để tránh đăng ký trùng
            doc.CommandEnded -= OnCommandEnded;
            doc.CommandEnded += OnCommandEnded;
        }

        private void ComponentManager_ItemInitialized(object sender, RibbonItemEventArgs e)
        {
            if (ComponentManager.Ribbon != null)
            {
                _ribbon = ComponentManager.Ribbon;
                ComponentManager.ItemInitialized -= ComponentManager_ItemInitialized;
                // Build ribbon now that it's available (deferred from InitializeRibbon)
                try { _buildRibbonAction?.Invoke(); }
                catch (Exception ex) { _logger?.Error("Lỗi khi dựng Ribbon sau ItemInitialized", ex); }
            }
        }

        /// <summary>
        /// Đăng ký hàm dựng lại Ribbon (buildAction) sẽ được gọi tự động sau khi CUI/MENULOAD reset giao diện.
        /// Gọi hàm này ngay sau khi bạn xây dựng xong Ribbon lần đầu.
        /// </summary>
        /// <param name="buildAction">Lambda chứa toàn bộ lệnh AddTab/AddPanel/AddButton của bạn.</param>
        public void RegisterBuildAction(Action buildAction)
        {
            _buildRibbonAction = buildAction;
        }

        private void OnCommandEnded(object sender, CommandEventArgs e)
        {
            if (UiResetCommands.Contains(e.GlobalCommandName))
            {
                // Dùng Application.Idle để đợi AutoCAD kết thúc reload UI rồi mới rebuild.
                // Đây là pattern được khuyến nghị bởi Autodesk developer documentation.
                Application.Idle += OnApplicationIdleRebuildRibbon;
            }
        }

        private void OnApplicationIdleRebuildRibbon(object sender, EventArgs e)
        {
            // Chỉ rebuild khi Ribbon đã sẵn sàng
            if (ComponentManager.Ribbon == null) return;

            Application.Idle -= OnApplicationIdleRebuildRibbon;
            try
            {
                _ribbon = ComponentManager.Ribbon;
                _buildRibbonAction?.Invoke();
                _logger?.Info("Ribbon đã được tạo dựng lại sau khi UI reset.");
            }
            catch (Exception ex)
            {
                _logger?.Error("Không thể dựng lại Ribbon sau khi CUI reset", ex);
            }
        }

        /// <summary>Thêm một thẻ (Tab) mới lên Ribbon.</summary>
        public void AddTab(string tabName, string tabTitle, string tabId = null)
        {
            if (_ribbon == null) return;
            var tab = _ribbon.Tabs.FirstOrDefault(t => t.Id == (tabId ?? tabName));
            if (tab == null)
            {
                tab = new RibbonTab { Title = tabTitle, Id = tabId ?? tabName, Name = tabName };
                _ribbon.Tabs.Add(tab);
            }
        }

        /// <summary>Thêm một nhóm chức năng (Panel) vào một Tab đã có.</summary>
        public void AddPanel(string tabName, string panelName, string panelTitle)
        {
            if (_ribbon == null) return;
            var tab = _ribbon.Tabs.FirstOrDefault(t => t.Name == tabName || t.Id == tabName);
            if (tab != null)
            {
                var panel = tab.Panels.FirstOrDefault(p => p.Source.Id == panelName || p.Source.Name == panelName);
                if (panel == null)
                {
                    var source = new RibbonPanelSource { Title = panelTitle, Id = panelName, Name = panelName };
                    panel = new RibbonPanel { Source = source };
                    tab.Panels.Add(panel);
                }
            }
        }

        /// <summary>
        /// Thêm một nút bấm (Button) cỡ lớn vào Panel.
        /// Khi bấm nút, AutoCAD sẽ tự động gõ lệnh tương ứng (commandName) vào Command Line.
        /// </summary>
        public void AddButton(string tabName, string panelName, string buttonText, string commandName, string iconPath = null, string tooltip = null)
        {
            if (_ribbon == null) return;
            var panelSource = GetPanelSource(tabName, panelName);
            if (panelSource != null)
            {
                var button = new RibbonButton
                {
                    Text = buttonText,
                    ShowText = true,
                    ShowImage = !string.IsNullOrEmpty(iconPath),
                    CommandParameter = commandName,
                    CommandHandler = new RibbonCommandHandler(),
                    ToolTip = tooltip,
                    Size = RibbonItemSize.Large,
                    Orientation = System.Windows.Controls.Orientation.Vertical
                };
                if (!string.IsNullOrEmpty(iconPath) && System.IO.File.Exists(iconPath))
                {
                    button.LargeImage = new BitmapImage(new Uri(iconPath));
                }
                panelSource.Items.Add(button);
            }
        }

        /// <summary>
        /// Thêm một nút xổ xuống (SplitButton) chứa danh sách nhiều nút con.
        /// Thích hợp để gộp các tính năng liên quan vào một menu thả xuống.
        /// </summary>
        public void AddSplitButton(string tabName, string panelName, string text, IEnumerable<(string text, string command, string iconPath)> items)
        {
            if (_ribbon == null) return;
            var panelSource = GetPanelSource(tabName, panelName);
            if (panelSource != null)
            {
                var splitBtn = new RibbonSplitButton
                {
                    Text = text,
                    ShowText = true,
                    ShowImage = true,
                    Size = RibbonItemSize.Large,
                    Orientation = System.Windows.Controls.Orientation.Vertical,
                    ListStyle = RibbonSplitButtonListStyle.List
                };
                
                foreach (var item in items)
                {
                    var btn = new RibbonButton
                    {
                        Text = item.text,
                        ShowText = true,
                        ShowImage = !string.IsNullOrEmpty(item.iconPath),
                        CommandParameter = item.command,
                        CommandHandler = new RibbonCommandHandler(),
                        Size = RibbonItemSize.Large
                    };
                    if (!string.IsNullOrEmpty(item.iconPath) && System.IO.File.Exists(item.iconPath))
                    {
                        btn.LargeImage = new BitmapImage(new Uri(item.iconPath));
                    }
                    splitBtn.Items.Add(btn);
                }
                
                panelSource.Items.Add(splitBtn);
            }
        }

        private RibbonPanelSource GetPanelSource(string tabName, string panelName)
        {
            var tab = _ribbon.Tabs.FirstOrDefault(t => t.Name == tabName || t.Id == tabName);
            var panel = tab?.Panels.FirstOrDefault(p => p.Source.Id == panelName || p.Source.Name == panelName);
            return panel?.Source;
        }
    }

    /// <summary>
    /// Trình xử lý sự kiện khi bấm nút trên Ribbon.
    /// Tự động lấy CommandParameter (tên lệnh) và đẩy vào giao diện dòng lệnh của AutoCAD.
    /// </summary>
    public class RibbonCommandHandler : System.Windows.Input.ICommand
    {
        // CanExecute luôn trả về true — nút Ribbon luôn có thể bấm được.
        // Event này bắt buộc phải khai báo theo interface ICommand nhưng không cần raise.
#pragma warning disable 67
        public event EventHandler CanExecuteChanged;
#pragma warning restore 67
        public bool CanExecute(object parameter) => true;
        public void Execute(object parameter)
        {
            if (parameter is RibbonButton button && button.CommandParameter != null)
            {
                string cmd = button.CommandParameter.ToString();
                var doc = Application.DocumentManager.MdiActiveDocument;
                if (doc != null)
                {
                    // \x03\x03 cancels any running command; _. uses English (locale-neutral) command name
                    doc.SendStringToExecute("\x03\x03_." + cmd + " ", true, false, false);
                }
            }
        }
    }
}

