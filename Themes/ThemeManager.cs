using System;
using System.Linq;
using System.Windows;
using CadCoreFramework.Utils;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace CadCoreFramework.Themes
{
    /// <summary>
    /// Quản lý WPF Theme cho Plugin, tự động adapt theo AutoCAD Dark/Light Mode.
    ///
    /// TRIẾT LÝ AUTODESK:
    /// - Palette và WPF Dialog nên follow theme của AutoCAD để UX đồng nhất.
    /// - Dùng System Variable DARKMODE (0 = Light, 1 = Dark) để detect theme hiện tại.
    /// - Subscribe SystemVariableChanged của Application để live-update khi user đổi theme.
    /// - ResourceDictionary được swap tại runtime — WPF StaticResource sẽ tự cập nhật
    ///   các control đang hiển thị.
    ///
    /// CÁCH DÙNG:
    /// - Gọi ThemeManager.Initialize() trong AppBootstrapper.InitializeCore().
    /// - Trong UserControl/Palette XAML: dùng các key như CadBackground.Primary,
    ///   CadButton, CadTextBox, v.v. — chúng sẽ tự đổi theo theme.
    /// - Gọi ThemeManager.Shutdown() trong AppBootstrapper.Terminate().
    /// </summary>
    public static class ThemeManager
    {
        // ─── URIs ─────────────────────────────────────────────────────────────────
        // Lazy evaluation: in Debug, the assembly name includes a timestamp suffix
        // (e.g. CadCoreFramework_20240101_120000), so pack URIs must use the actual
        // runtime name rather than the canonical hard-coded string.

        private static Uri DarkThemeUri =>
            new Uri($"/{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name};component/Themes/AutoCadTheme.xaml", UriKind.Relative);

        private static Uri LightThemeUri =>
            new Uri($"/{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name};component/Themes/AutoCadLightTheme.xaml", UriKind.Relative);

        // ─── State ────────────────────────────────────────────────────────────────

        private static bool _isInitialized;
        private static bool _isDark;

        /// <summary>Theme hiện tại đang áp dụng. True = Dark, False = Light.</summary>
        public static bool IsDarkMode => _isDark;

        /// <summary>Sự kiện bắn ra khi theme thay đổi.</summary>
        public static event EventHandler<bool> ThemeChanged;

        // ─── Lifecycle ────────────────────────────────────────────────────────────

        /// <summary>
        /// Khởi tạo ThemeManager: đọc DARKMODE sysvar và áp dụng theme ngay lập tức.
        /// Đăng ký SystemVariableChanged để live-update.
        /// Gọi trong AppBootstrapper.InitializeCore().
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized) return;

            _isDark = CadApplicationHelper.IsDarkMode;
            ApplyTheme(_isDark);

            Application.SystemVariableChanged += OnSystemVariableChanged;
            _isInitialized = true;
        }

        /// <summary>
        /// Hủy đăng ký event. Gọi trong AppBootstrapper.Terminate().
        /// </summary>
        public static void Shutdown()
        {
            if (!_isInitialized) return;
            Application.SystemVariableChanged -= OnSystemVariableChanged;
            _isInitialized = false;
        }

        // ─── Theme Application ────────────────────────────────────────────────────

        /// <summary>
        /// Áp dụng theme theo trạng thái dark/light.
        /// Swap ResourceDictionary trong Application.Current.Resources.MergedDictionaries.
        /// </summary>
        public static void ApplyTheme(bool isDark)
        {
            _isDark = isDark;

            System.Windows.Application wpfApp = System.Windows.Application.Current;
            if (wpfApp == null)
            {
                // WPF Application chưa khởi động — AutoCAD chưa có Application context
                // Thử lại sau khi Dispatcher khởi tạo (trong Palette load)
                return;
            }

            Uri targetUri = isDark ? DarkThemeUri : LightThemeUri;
            Uri removeUri = isDark ? LightThemeUri : DarkThemeUri;

            var dicts = wpfApp.Resources.MergedDictionaries;

            // Xóa theme cũ nếu có
            var existing = dicts.FirstOrDefault(d => d.Source == removeUri);
            if (existing != null) dicts.Remove(existing);

            // Thêm theme mới nếu chưa có
            if (dicts.All(d => d.Source != targetUri))
            {
                dicts.Add(new ResourceDictionary { Source = targetUri });
            }

            ThemeChanged?.Invoke(null, isDark);
        }

        /// <summary>
        /// Merge theme resource vào một ResourceDictionary cụ thể (ví dụ của một UserControl).
        /// Dùng khi UserControl cần theme nhưng không muốn ảnh hưởng Application.Resources.
        /// </summary>
        public static void ApplyThemeTo(ResourceDictionary resources)
        {
            if (resources == null) return;
            Uri targetUri = _isDark ? DarkThemeUri : LightThemeUri;

            // Xóa theme cũ
            var existing = resources.MergedDictionaries
                .FirstOrDefault(d => d.Source == DarkThemeUri || d.Source == LightThemeUri);
            if (existing != null) resources.MergedDictionaries.Remove(existing);

            resources.MergedDictionaries.Add(new ResourceDictionary { Source = targetUri });
        }

        // ─── System Variable Event ────────────────────────────────────────────────

        private static void OnSystemVariableChanged(
            object sender,
            Autodesk.AutoCAD.ApplicationServices.SystemVariableChangedEventArgs e)
        {
            if (string.Equals(e.Name, "DARKMODE", StringComparison.OrdinalIgnoreCase))
            {
                bool nowDark = CadApplicationHelper.IsDarkMode;
                if (nowDark != _isDark)
                {
                    ApplyTheme(nowDark);
                }
            }
        }
    }
}
