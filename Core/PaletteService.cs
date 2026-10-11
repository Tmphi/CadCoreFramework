using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Autodesk.AutoCAD.Windows;
using CadCoreFramework.ViewModels;

namespace CadCoreFramework.Core
{
    /// <summary>
    /// Dịch vụ quản lý các thanh công cụ neo (Dockable Palette) bên trong AutoCAD.
    /// Giúp hiển thị giao diện WPF mượt mà và quản lý trạng thái Đang tải (IsBusy).
    /// Implement IDisposable để Dispose toàn bộ PaletteSet khi plugin unload, tránh memory leak.
    ///
    /// TRIẾT LÝ AUTODESK:
    /// - PaletteSet.AddVisual() nhúng WPF UserControl vào Palette của AutoCAD.
    /// - Palette cần Guid ổn định để AutoCAD lưu vị trí/kích thước qua các lần khởi động.
    /// - Phải đặt Visible = false trước khi Dispose() (theo tài liệu Autodesk).
    /// - PaletteSet phải được tạo trên UI thread của AutoCAD.
    /// </summary>
    public class PaletteService : IPaletteService, IDisposable
    {
        private readonly Dictionary<string, PaletteSet> _palettes
            = new Dictionary<string, PaletteSet>(StringComparer.OrdinalIgnoreCase);

        // Dùng IBusyIndicator thay vì ViewModelBase để tránh dependency ngược
        private readonly Dictionary<string, IBusyIndicator> _paletteIndicators
            = new Dictionary<string, IBusyIndicator>(StringComparer.OrdinalIgnoreCase);

        private readonly ILoggerService _logger;

        public PaletteService(ILoggerService logger)
        {
            _logger = logger;
        }

        // ─── PaletteSet Management ────────────────────────────────────────────────

        /// <summary>
        /// Khởi tạo hoặc lấy ra một PaletteSet đã có sẵn.
        /// Tự động setup các thuộc tính tiêu chuẩn (Kích thước tối thiểu, nút Close, AutoHide).
        ///
        /// QUAN TRỌNG: Dùng Guid ổn định (guid) để AutoCAD lưu lại vị trí Palette
        /// qua các lần load plugin. Nếu không có Guid, AutoCAD sẽ quên vị trí mỗi lần.
        /// </summary>
        public PaletteSet GetOrCreatePaletteSet(string name, Guid? guid = null,
            DockSides defaultDock = DockSides.Right)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));

            if (_palettes.TryGetValue(name, out PaletteSet existingPs))
                return existingPs;

            PaletteSet ps = guid.HasValue
                ? new PaletteSet(name, guid.Value)
                : new PaletteSet(name);

            ps.Style = PaletteSetStyles.ShowAutoHideButton
                     | PaletteSetStyles.ShowCloseButton
                     | PaletteSetStyles.ShowPropertiesMenu;
            ps.MinimumSize = new System.Drawing.Size(320, 450);
            ps.Size        = new System.Drawing.Size(400, 720);
            ps.Dock        = defaultDock;

            _palettes[name] = ps;
            return ps;
        }

        /// <summary>
        /// Hiển thị một Palette và nhúng View (WPF UserControl) vào bên trong nó.
        /// </summary>
        public void ShowPalette(string paletteName, FrameworkElement content,
            string tabTitle = null, DockSides defaultDock = DockSides.Right)
        {
            try
            {
                var ps = GetOrCreatePaletteSet(paletteName, defaultDock: defaultDock);
                if (content != null)
                {
                    // Luôn xóa UI cũ để nạp UI mới (phục vụ Hot-Reload)
                    while (ps.Count > 0) ps.Remove(0);
                    ps.AddVisual(tabTitle ?? paletteName, content);
                }
                ps.Visible = true;
            }
            catch (Exception ex)
            {
                _logger?.Error($"Lỗi khi hiển thị PaletteSet '{paletteName}'.", ex);
            }
        }

        /// <summary>
        /// Hiển thị Palette có kèm theo IBusyIndicator.
        /// PaletteService sẽ tự động bật/tắt IsBusy khi chạy RunWithBusyAsync().
        /// </summary>
        public void ShowPalette(string paletteName, FrameworkElement content,
            IBusyIndicator busyIndicator, string tabTitle = null,
            DockSides defaultDock = DockSides.Right)
        {
            if (busyIndicator != null)
                _paletteIndicators[paletteName] = busyIndicator;
            ShowPalette(paletteName, content, tabTitle, defaultDock);
        }

        /// <summary>
        /// Bật/Tắt (Toggle) thanh Palette. Dùng Func (contentFactory) để chỉ khởi tạo
        /// View khi người dùng thực sự mở Palette (Lazy load pattern).
        /// </summary>
        public void TogglePalette(string paletteName, Func<FrameworkElement> contentFactory,
            string tabTitle = null, DockSides defaultDock = DockSides.Right)
        {
            try
            {
                var ps = GetOrCreatePaletteSet(paletteName, defaultDock: defaultDock);
                if (ps.Visible)
                {
                    ps.Visible = false;
                }
                else
                {
                    if (contentFactory != null)
                    {
                        while (ps.Count > 0) ps.Remove(0);
                        var content = contentFactory();
                        if (content != null)
                            ps.AddVisual(tabTitle ?? paletteName, content);
                    }
                    ps.Visible = true;
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"Lỗi khi bật/tắt PaletteSet '{paletteName}'.", ex);
            }
        }

        /// <summary>Ẩn Palette theo tên.</summary>
        public void HidePalette(string paletteName)
        {
            if (_palettes.TryGetValue(paletteName, out PaletteSet ps))
                ps.Visible = false;
        }

        /// <summary>Kiểm tra xem Palette có đang hiển thị hay không.</summary>
        public bool IsPaletteVisible(string paletteName)
            => _palettes.TryGetValue(paletteName, out PaletteSet ps) && ps.Visible;

        // ─── Busy Indicator ───────────────────────────────────────────────────────

        /// <summary>
        /// Bao bọc một hàm Async, tự động bật IsBusy = true trước khi chạy
        /// và tắt IsBusy = false sau khi chạy xong, bất kể có lỗi hay không.
        /// </summary>
        public async Task RunWithBusyAsync(string paletteName, Func<Task> asyncTask)
        {
            _paletteIndicators.TryGetValue(paletteName, out IBusyIndicator indicator);
            try
            {
                if (indicator != null) indicator.IsBusy = true;
                await asyncTask();
            }
            catch (Exception ex)
            {
                _logger?.Error($"Lỗi RunWithBusyAsync trong palette '{paletteName}'.", ex);
                throw;
            }
            finally
            {
                if (indicator != null) indicator.IsBusy = false;
            }
        }

        // ─── IDisposable ──────────────────────────────────────────────────────────

        /// <summary>
        /// Giải phóng toàn bộ PaletteSet.
        /// Theo tài liệu Autodesk: đặt Visible = false trước, sau đó mới Dispose() và set null.
        /// </summary>
        public void Dispose()
        {
            foreach (var kvp in _palettes)
            {
                try
                {
                    var ps = kvp.Value;
                    if (ps != null && !ps.IsDisposed)
                    {
                        ps.Visible = false;
                        ps.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    _logger?.Error($"Lỗi khi giải phóng PaletteSet '{kvp.Key}'.", ex);
                }
            }
            _palettes.Clear();
            _paletteIndicators.Clear();
        }
    }
}
