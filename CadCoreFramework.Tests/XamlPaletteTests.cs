using System;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using CadCoreFramework.Core;
using CadCoreFramework.Samples.GridMarker;
using CadCoreFramework.Services;
using CadCoreFramework.ViewModels;
using CadCoreFramework.Views;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CadCoreFramework.Tests
{
    /// <summary>
    /// Dựng THẬT từng palette (InitializeComponent + nạp Styles/Theme) trên luồng STA.
    /// Bắt các lỗi chỉ lộ ra khi chạy trong AutoCAD: StaticResource tới key chưa nạp,
    /// BAML hỏng, ViewModel chưa đăng ký DI, binding tới converter không tồn tại…
    /// Không cần AutoCAD runtime.
    ///
    /// Tạm gán AppBootstrapper.ServiceProvider (static) → chạy tuần tự, không song song.
    /// </summary>
    [Collection(nameof(XamlPaletteTests))]
    [CollectionDefinition(nameof(XamlPaletteTests), DisableParallelization = true)]
    public class XamlPaletteTests
    {
        [Fact]
        public void CommonControlsPalette_Loads_WithStylesAvailable()
        {
            RunWithServices(() =>
            {
                var palette = new CommonControlsPalette();

                palette.TryFindResource("PrimaryButton").Should().BeOfType<Style>();
                palette.TryFindResource("BackgroundBrush").Should().NotBeNull();
                // B-26: DataContext trên CHÍNH UserControl (PaletteService Dispose được), lấy từ DI.
                palette.DataContext.Should().BeOfType<CommonControlsViewModel>();
            });
        }

        [Fact]
        public void GridMarkerPalette_Loads_WithViewModelAndStyles()
        {
            RunWithServices(() =>
            {
                var palette = new GridMarkerPalette();

                palette.DataContext.Should().BeOfType<GridMarkerViewModel>();
                palette.TryFindResource("InputStyle").Should().BeOfType<Style>();
            });
        }

        [Fact]
        public void LayerManagerControl_Loads_WithViewModel()
        {
            RunWithServices(() =>
                new LayerManagerControl().DataContext.Should().BeOfType<LayerManagerViewModel>());
        }

        [Fact]
        public void DebugInspectorPaletteControl_Loads_WithViewModel()
        {
            RunWithServices(() =>
                new DebugInspectorPaletteControl().DataContext.Should().BeOfType<DebugInspectorViewModel>());
        }

        // ─── Theme (B-01/B-02/B-03/B-23) ─────────────────────────────────────────

        /// <summary>Mỗi palette: file XAML nguồn + cách dựng.</summary>
        private static readonly (string Xaml, Func<FrameworkElement> Create)[] Palettes =
        {
            (@"Views\CommonControlsPalette.xaml",         () => new CommonControlsPalette()),
            (@"Samples\GridMarker\GridMarkerPalette.xaml", () => new GridMarkerPalette()),
            (@"Views\LayerManagerControl.xaml",           () => new LayerManagerControl()),
            (@"Views\DebugInspectorPaletteControl.xaml",  () => new DebugInspectorPaletteControl()),
        };

        [Fact]
        public void ThemeDictionaries_DefineExactlyTheSameKeys()
        {
            // ThemeManager GỠ theme này khi nạp theme kia → một key chỉ có ở một bên sẽ biến mất khi đổi theme.
            RunOnSta(() =>
            {
                var dark = LoadDictionary("Themes/AutoCadTheme.xaml").Keys.Cast<object>().Select(k => k.ToString()).ToList();
                var light = LoadDictionary("Themes/AutoCadLightTheme.xaml").Keys.Cast<object>().Select(k => k.ToString()).ToList();

                dark.Should().BeEquivalentTo(light);
                dark.Should().Contain(new[] { "BackgroundBrush", "SurfaceBrush", "TextPrimaryBrush", "AccentBrush", "OverlayBrush" });
            });
        }

        [Fact]
        public void StylesDictionary_DefinesNoColors()
        {
            // Một nguồn màu duy nhất: màu nằm trong file theme, Styles.xaml chỉ có Style.
            RunOnSta(() =>
                LoadDictionary("Themes/Styles.xaml").Values.Cast<object>().Should().AllBeOfType<Style>());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void EveryResourceKeyReferencedByPalettesAndStyles_Resolves(bool dark)
        {
            // Bắt đúng loại lỗi B-03: XAML tham chiếu key không tồn tại → WPF im lặng dùng giá trị mặc định.
            var stylesKeys = ReferencedKeys(@"Themes\Styles.xaml");
            RunWithServices(() => WithTheme(dark, () =>
            {
                foreach (var (xaml, create) in Palettes)
                {
                    FrameworkElement palette = create();
                    foreach (string key in ReferencedKeys(xaml).Concat(stylesKeys))
                        palette.TryFindResource(key).Should().NotBeNull($"{xaml} (theme {(dark ? "tối" : "sáng")}) tham chiếu key '{key}'");
                }
            }));
        }

        [Fact]
        public void AttachedPalette_FollowsThemeChangesWhileLoaded_AndStopsAfterUnloaded()
        {
            RunWithServices(() => WithTheme(true, () =>
            {
                var palette = new GridMarkerPalette();
                BrushColor(palette, "BackgroundBrush").Should().Be(Color("#1E1E2E"));

                palette.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));   // palette hiện lên
                CadCoreFramework.Themes.ThemeManager.ApplyTheme(false);                  // người dùng đổi COLORTHEME
                BrushColor(palette, "BackgroundBrush").Should().Be(Color("#F3F4F6"));
                ((System.Windows.Media.SolidColorBrush)palette.Background).Color.Should().Be(Color("#F3F4F6"),
                    "Background dùng DynamicResource phải tự cập nhật");

                palette.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent)); // palette đóng
                CadCoreFramework.Themes.ThemeManager.ApplyTheme(true);
                BrushColor(palette, "BackgroundBrush").Should().Be(Color("#F3F4F6"), "đã gỡ đăng ký khi Unloaded");

                palette.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));   // mở lại → bắt kịp theme hiện hành
                BrushColor(palette, "BackgroundBrush").Should().Be(Color("#1E1E2E"));
            }));
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData((short)0, true)]
        [InlineData((short)1, false)]
        [InlineData(1, false)]
        [InlineData(null, true)]       // không đọc được → mặc định AutoCAD (tối)
        [InlineData("x", true)]
        public void IsDarkColorTheme_MapsColorThemeValues(object? colorTheme, bool expectedDark)
        {
            CadCoreFramework.Utils.CadApplicationHelper.IsDarkColorTheme(colorTheme!).Should().Be(expectedDark);
        }

        private static void WithTheme(bool dark, Action action)
        {
            CadCoreFramework.Themes.ThemeManager.ApplyTheme(dark);
            try { action(); }
            finally { CadCoreFramework.Themes.ThemeManager.ApplyTheme(true); }   // mặc định: tối
        }

        private static ResourceDictionary LoadDictionary(string relativePath)
        {
            string asm = typeof(AppBootstrapper).Assembly.GetName().Name!;
            return new ResourceDictionary { Source = new Uri($"/{asm};component/{relativePath}", UriKind.Relative) };
        }

        private static System.Windows.Media.Color BrushColor(FrameworkElement element, string key)
            => ((System.Windows.Media.SolidColorBrush)element.TryFindResource(key)).Color;

        private static System.Windows.Media.Color Color(string hex)
            => (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex);

        /// <summary>Mọi key trong {DynamicResource X} / {StaticResource X} của một file XAML trong repo.</summary>
        private static string[] ReferencedKeys(string repoRelativeXaml)
        {
            string text = System.IO.File.ReadAllText(System.IO.Path.Combine(RepoRoot, repoRelativeXaml));
            return System.Text.RegularExpressions.Regex.Matches(text, @"\{(?:Dynamic|Static)Resource\s+([^}\s]+)\}")
                .Cast<System.Text.RegularExpressions.Match>()
                .Select(m => m.Groups[1].Value)
                .Distinct()
                .ToArray();
        }

        private static string RepoRoot
        {
            get
            {
                var dir = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
                while (dir != null && !System.IO.File.Exists(System.IO.Path.Combine(dir.FullName, "CadCoreFramework.csproj")))
                    dir = dir.Parent;
                return dir?.FullName ?? throw new InvalidOperationException("Không tìm thấy thư mục gốc repo (CadCoreFramework.csproj).");
            }
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private static IServiceProvider BuildServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IEventAggregator, EventAggregator>();
            services.AddSingleton<ICommandHistoryService, CommandHistoryService>();
            services.AddSingleton(new Mock<INotificationService>().Object);
            services.AddSingleton(new Mock<ILayerService>().Object);
            services.AddSingleton(new Mock<IGridMarkerService>().Object);
            services.AddTransient<GridMarkerViewModel>();
            services.AddTransient<LayerManagerViewModel>();
            services.AddTransient<DebugInspectorViewModel>();
            services.AddTransient<CommonControlsViewModel>();
            return services.BuildServiceProvider();
        }

        /// <summary>Gán ServiceProvider tạm thời và chạy action trên một luồng STA (bắt buộc với WPF).</summary>
        private static void RunWithServices(Action action)
        {
            PropertyInfo prop = typeof(AppBootstrapper).GetProperty(nameof(AppBootstrapper.ServiceProvider))!;
            object? previous = prop.GetValue(null);
            prop.SetValue(null, BuildServices());
            try
            {
                RunOnSta(action);
            }
            finally
            {
                prop.SetValue(null, previous);
            }
        }

        private static void RunOnSta(Action action)
        {
            // Đăng ký scheme pack:// (WPF tự làm khi có Application; test không có Application).
            _ = System.IO.Packaging.PackUriHelper.UriSchemePack;
            // Static constructor của System.Windows.Application đăng ký gói "pack://application:,,,/" —
            // thiếu nó, ResourceDictionary.Source với URI tương đối ném "The URI prefix is not recognized".
            // InitializeComponent tình cờ chạm tới nó, nên trước đây test chỉ pass nếu có test dựng palette
            // chạy trước (phụ thuộc thứ tự — lộ ra khi tên test đổi trong dự án sinh từ template).
            _ = System.Windows.Application.Current;

            ExceptionDispatchInfo? failure = null;
            var thread = new Thread(() =>
            {
                try { action(); }
                catch (Exception ex) { failure = ExceptionDispatchInfo.Capture(ex); }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            failure?.Throw();
        }
    }
}
