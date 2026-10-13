using System;
using System.Linq;
using System.Windows;
using CadCoreFramework.Core;
using CadCoreFramework.Utils;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace CadCoreFramework.Themes
{
    /// <summary>
    /// Quản lý WPF Theme cho Plugin, tự động adapt theo AutoCAD Dark/Light Mode.
    ///
    /// TRIẾT LÝ AUTODESK:
    /// - Palette và WPF Dialog nên follow theme của AutoCAD để UX đồng nhất.
    /// - Dùng System Variable COLORTHEME (0 = Dark, 1 = Light) để detect theme hiện tại.
    ///   (Không có sysvar DARKMODE — docs/training/T03 A13.)
    /// - Subscribe SystemVariableChanged của Application để live-update khi user đổi theme.
    ///
    /// MỘT NGUỒN MÀU DUY NHẤT:
    /// - AutoCadTheme.xaml / AutoCadLightTheme.xaml định nghĩa CÙNG bộ key màu (BackgroundBrush,
    ///   SurfaceBrush, TextPrimaryBrush, AccentBrush… và các key Cad*), mỗi file một bảng màu.
    /// - Styles.xaml chỉ chứa Style, tham chiếu màu bằng {DynamicResource} → khi đổi theme (swap
    ///   dictionary), mọi control đang hiển thị tự cập nhật.
    ///
    /// CÁCH DÙNG:
    /// - Gọi ThemeManager.Initialize() trong AppBootstrapper.InitializeCore().
    /// - Trong code-behind của palette, NGAY SAU InitializeComponent(): <c>ThemeManager.Attach(this);</c>
    ///   XAML dùng {DynamicResource} cho mọi key màu/style (chúng được nạp SAU InitializeComponent).
    /// - Gọi ThemeManager.Shutdown() trong AppBootstrapper.Terminate().
    /// </summary>
    public static class ThemeManager
    {
        // ─── URIs ─────────────────────────────────────────────────────────────────
        // Lazy evaluation: in Debug, the assembly name includes a timestamp suffix
        // (e.g. CadCoreFramework_20240101_120000), so pack URIs must use the actual
        // runtime name rather than the canonical hard-coded string.

        private static string AssemblyName => System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

        private static Uri DarkThemeUri =>
            new Uri($"/{AssemblyName};component/Themes/AutoCadTheme.xaml", UriKind.Relative);

        private static Uri LightThemeUri =>
            new Uri($"/{AssemblyName};component/Themes/AutoCadLightTheme.xaml", UriKind.Relative);

        private static Uri StylesUri =>
            new Uri($"/{AssemblyName};component/Themes/Styles.xaml", UriKind.Relative);

        // ─── State ────────────────────────────────────────────────────────────────

        private static bool _isInitialized;
        private static bool _isDark = true;   // COLORTHEME mặc định của AutoCAD = 0 (tối)
        private static ILoggerService? _logger;

        /// <summary>Theme hiện tại đang áp dụng. True = Dark, False = Light.</summary>
        public static bool IsDarkMode => _isDark;

        /// <summary>
        /// Sự kiện bắn ra mỗi khi theme được áp dụng (tham số: true = Dark).
        /// Palette gắn qua <see cref="Attach"/> tự nghe sự kiện này — không cần tự đăng ký.
        /// </summary>
        public static event EventHandler<bool> ThemeChanged;

        // ─── Lifecycle ────────────────────────────────────────────────────────────

        /// <summary>
        /// Khởi tạo ThemeManager: đọc COLORTHEME và áp dụng theme ngay lập tức.
        /// Đăng ký SystemVariableChanged để live-update.
        /// Gọi trong AppBootstrapper.InitializeCore().
        /// </summary>
        public static void Initialize(ILoggerService? logger = null)
        {
            if (_isInitialized) return;
            _logger = logger;

            ApplyTheme(ReadIsDark());

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
        /// Áp dụng theme theo trạng thái dark/light: cập nhật Application.Resources (nếu có WPF
        /// Application) và LUÔN bắn <see cref="ThemeChanged"/> — palette của AutoCAD không dùng
        /// Application.Resources (thường không có WPF Application) mà nạp theme vào Resources riêng.
        /// </summary>
        public static void ApplyTheme(bool isDark)
        {
            _isDark = isDark;

            System.Windows.Application wpfApp = System.Windows.Application.Current;
            if (wpfApp != null)
                SwapThemeDictionary(wpfApp.Resources);

            ThemeChanged?.Invoke(null, isDark);
        }

        /// <summary>
        /// Gắn hệ màu vào một palette/UserControl: nạp Styles.xaml + theme hiện hành vào
        /// <c>root.Resources</c>, và tự áp lại theme khi người dùng đổi COLORTHEME trong lúc palette
        /// đang hiển thị (đăng ký khi Loaded, gỡ khi Unloaded — không giữ palette đã đóng trong bộ nhớ).
        /// Gọi NGAY SAU InitializeComponent(). Không chạm AutoCAD API → dùng được trong unit test.
        /// </summary>
        public static void Attach(FrameworkElement root)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));

            Uri styles = StylesUri;
            if (root.Resources.MergedDictionaries.All(d => d.Source != styles))
                root.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = styles });
            ApplyThemeTo(root.Resources);

            EventHandler<bool> onThemeChanged = (_, __) => RunOnUiThread(root, () => ApplyThemeTo(root.Resources));
            root.Loaded += (_, __) =>
            {
                ThemeChanged -= onThemeChanged;   // Loaded có thể bắn nhiều lần (ẩn/hiện palette)
                ThemeChanged += onThemeChanged;
                ApplyThemeTo(root.Resources);     // theme có thể đã đổi trong lúc palette bị ẩn
            };
            root.Unloaded += (_, __) => ThemeChanged -= onThemeChanged;
        }

        /// <summary>
        /// Merge theme hiện hành vào một ResourceDictionary cụ thể (ví dụ của một UserControl), thay
        /// theme cũ nếu có. Không làm gì nếu đã đúng theme. Thường dùng <see cref="Attach"/> thay vì gọi trực tiếp.
        /// </summary>
        public static void ApplyThemeTo(ResourceDictionary resources)
        {
            if (resources == null) return;
            SwapThemeDictionary(resources);
        }

        private static void SwapThemeDictionary(ResourceDictionary resources)
        {
            Uri dark = DarkThemeUri, light = LightThemeUri;
            Uri target = _isDark ? dark : light;
            var dicts = resources.MergedDictionaries;

            var current = dicts.FirstOrDefault(d => d.Source == dark || d.Source == light);
            if (current != null && current.Source == target) return;
            if (current != null) dicts.Remove(current);

            dicts.Add(new ResourceDictionary { Source = target });
        }

        private static void RunOnUiThread(FrameworkElement root, Action action)
        {
            if (root.Dispatcher.CheckAccess()) action();
            else root.Dispatcher.BeginInvoke(action);
        }

        // ─── System Variable Event ────────────────────────────────────────────────

        private static bool ReadIsDark()
        {
            object raw = CadApplicationHelper.GetSystemVariable(CadApplicationHelper.ColorThemeVariable);
            if (raw == null)
                _logger?.Warning($"Không đọc được sysvar {CadApplicationHelper.ColorThemeVariable} — dùng theme tối mặc định.");
            return CadApplicationHelper.IsDarkColorTheme(raw);
        }

        private static void OnSystemVariableChanged(
            object sender,
            Autodesk.AutoCAD.ApplicationServices.SystemVariableChangedEventArgs e)
        {
            if (!string.Equals(e.Name, CadApplicationHelper.ColorThemeVariable, StringComparison.OrdinalIgnoreCase))
                return;

            bool nowDark = ReadIsDark();
            if (nowDark != _isDark)
                ApplyTheme(nowDark);
        }
    }
}
