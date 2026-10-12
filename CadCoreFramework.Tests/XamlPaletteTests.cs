using System;
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
