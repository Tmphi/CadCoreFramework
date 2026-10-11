using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using CadCoreFramework.Themes;

namespace CadCoreFramework.Views
{
    /// <summary>
    /// Code-behind cho giao diện CommonControlsPalette.xaml (bảng minh họa các điều khiển giao diện chuẩn).
    /// </summary>
    public partial class CommonControlsPalette : UserControl
    {
        /// <summary>
        /// Khởi tạo các thành phần giao diện của CommonControlsPalette.
        /// </summary>
        public CommonControlsPalette()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Load Styles.xaml using the actual runtime assembly name.
            // In Debug builds the assembly name has a timestamp suffix, so the hardcoded
            // pack URI in XAML would fail — loading here ensures both Debug and Release work.
            string assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            var stylesUri = new System.Uri($"/{assemblyName};component/Themes/Styles.xaml", System.UriKind.Relative);
            this.Resources.MergedDictionaries.Clear();
            this.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary { Source = stylesUri });

            // Apply the current AutoCAD theme (dark/light) on top of the styles
            ThemeManager.ApplyThemeTo(this.Resources);
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }
    }
}
