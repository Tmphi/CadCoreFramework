using System;
using System.IO;
using System.Reflection;
using Autodesk.AutoCAD.Runtime;
using CadCoreFramework.Services;
using CadCoreFramework.Themes;
using Microsoft.Extensions.DependencyInjection;

// QUAN TRỌNG: ExtensionApplication và CommandClass được khai báo tập trung
// trong Properties/AssemblyInfo.cs — KHÔNG khai báo lại ở đây.

namespace CadCoreFramework.Core
{
    /// <summary>
    /// Lớp Entry Point chính của Plugin.
    /// Implement IExtensionApplication để AutoCAD tự động gọi khi DLL được nạp (NETLOAD).
    /// Chịu trách nhiệm:
    ///   1. Thiết lập AssemblyResolve handler trước mọi thứ khác (tránh lỗi JIT).
    ///   2. Cấu hình Dependency Injection Container.
    ///   3. Khởi động các Service cốt lõi theo đúng thứ tự.
    ///   4. Khởi động WPF Theme Manager.
    ///   5. Dọn dẹp khi AutoCAD đóng.
    ///
    /// THỨ TỰ KHỞI ĐỘNG AUTODESK CHUẨN:
    ///   1. RegisterAssemblyResolver → đảm bảo DLL phụ thuộc được tìm thấy
    ///   2. BuildServiceProvider → tạo DI container
    ///   3. EventService.Initialize() → bắt đầu lắng nghe events
    ///   4. RibbonService.InitializeRibbon() → tạo UI Ribbon
    ///   5. ThemeManager.Initialize() → áp dụng WPF theme
    /// </summary>
    public class AppBootstrapper : IExtensionApplication
    {
        /// <summary>
        /// DI Container toàn cục. Dùng để resolve (lấy) các service ở bất kỳ đâu trong plugin.
        /// KHÔNG gọi ServiceProvider.GetService&lt;T&gt;() bên trong constructor của Service
        /// vì container có thể chưa được build hoàn tất — hãy dùng constructor injection thay thế.
        /// </summary>
        public static IServiceProvider? ServiceProvider { get; private set; }

        /// <summary>
        /// Snapshot của ServiceCollection ĐÃ dùng để build ServiceProvider.
        /// Chỉ có giá trị sau khi Initialize() chạy xong. Dùng cho Debug Inspector
        /// Palette để liệt kê các service đã đăng ký — không có public API nào
        /// trên IServiceProvider để lấy danh sách này.
        /// </summary>
        public static IServiceCollection? RegisteredServices { get; private set; }

#if DEBUG
        /// <summary>
        /// CHỈ DÙNG CHO KIỂM THỬ (TEST ONLY). Tiêm một IServiceProvider tùy chỉnh để các unit test xUnit
        /// có thể chạy mà không cần khởi tạo AutoCAD. Chỉ được biên dịch trong cấu hình Debug.
        /// Truy cập được nhờ [assembly: InternalsVisibleTo("CadCoreFramework.Tests")].
        /// </summary>
        internal static void SetServiceProviderForTesting(IServiceProvider provider)
            => ServiceProvider = provider;
#endif

        // ─── IExtensionApplication ────────────────────────────────────────────────

        /// <summary>
        /// Hàm được gọi tự động MỘT LẦN DUY NHẤT khi người dùng NETLOAD Plugin.
        /// Hàm này phải nhanh và không được throw exception ra ngoài.
        /// </summary>
        public void Initialize()
        {
            // BẮT BUỘC: Đăng ký AssemblyResolve TRƯỚC KHI gọi bất cứ code nào
            // sử dụng các DLL phụ thuộc (DI, MVVM Toolkit, Newtonsoft...).
            // Lý do: CLR cố gắng JIT-compile InitializeCore() trước khi chạy nó,
            // và trong lúc đó sẽ tìm các DLL. Nếu chưa register handler → FileNotFoundException.
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            try
            {
                InitializeCore();
            }
            catch (System.Exception ex)
            {
                // Không để lỗi khởi động crash AutoCAD — luôn bắt ở đây
                System.Windows.Forms.MessageBox.Show(
                    $"CadCoreFramework — Lỗi Khởi Tạo:\n\n{ex}",
                    "Plugin Error",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);

                Autodesk.AutoCAD.ApplicationServices.Core.Application
                    .DocumentManager.MdiActiveDocument?.Editor
                    .WriteMessage($"\n[CadCoreFramework ERROR] {ex.Message}");
            }
        }

        /// <summary>
        /// Tách riêng khỏi Initialize() để tránh lỗi JIT — CLR phải resolve các type
        /// trước khi compile method này, nên AssemblyResolve phải đã được đăng ký trước.
        /// </summary>
        private void InitializeCore()
        {
            // 1. Xây dựng DI Container
            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();
            RegisteredServices = services;  // keep snapshot for Debug Inspector

            // 2. Khởi động Event Service (lắng nghe các sự kiện AutoCAD)
            var eventService = ServiceProvider.GetService<IEventService>();
            eventService?.Initialize();

            // 3. Khởi động Ribbon Service (tạo menu và nút bấm trên Ribbon)
            var ribbonService = ServiceProvider.GetService<IRibbonService>();
            ribbonService?.InitializeRibbon();

            // 4. Khởi động WPF Theme Manager (tự động theo AutoCAD Dark/Light mode)
            ThemeManager.Initialize();

            // 5. Ghi log khởi động thành công
            var logger = ServiceProvider.GetService<ILoggerService>();
            logger?.Info($"CadCoreFramework initialized. Version: {Assembly.GetExecutingAssembly().GetName().Version}");
        }

        /// <summary>
        /// Hàm được gọi khi AutoCAD đóng hoặc plugin bị unload.
        /// Dùng để dọn dẹp bộ nhớ và hủy đăng ký sự kiện.
        /// QUAN TRỌNG: Thứ tự cleanup ngược lại với Initialize.
        /// </summary>
        public void Terminate()
        {
            try
            {
                // 1. Dừng Theme Manager
                ThemeManager.Shutdown();

                // 2. Dừng Event Service (hủy đăng ký tất cả các event handler)
                ServiceProvider?.GetService<IEventService>()?.Shutdown();
                
                // Dừng Ribbon Service (hủy đăng ký sự kiện UI)
                ServiceProvider?.GetService<IRibbonService>()?.Shutdown();

                // 3. Hủy đăng ký tất cả Overrules đang hoạt động — CỰC KỲ QUAN TRỌNG:
                //    bỏ qua bước này sẽ để lại con trỏ native treo lơ lửng và gây Fatal Error khi tắt.
                ServiceProvider?.GetService<OverruleManager>()?.Dispose();

                // 4. Giải phóng DI Container (tự động dispose các singleton IDisposable,
                //    bao gồm PaletteService đang nắm giữ các đối tượng WPF PaletteSet).
                if (ServiceProvider is IDisposable disposable)
                    disposable.Dispose();

                ServiceProvider = null;
            }
            catch (System.Exception ex)
            {
                // Không để lỗi cleanup crash AutoCAD khi đóng
                System.Diagnostics.Debug.WriteLine($"[CadCoreFramework] Terminate error: {ex.Message}");
            }
        }

        // ─── Service Registration ─────────────────────────────────────────────────

        /// <summary>
        /// Đăng ký tất cả Service, Repository, và ViewModel vào DI Container.
        ///
        /// HƯỚNG DẪN LIFETIME:
        /// - AddSingleton: 1 instance duy nhất cho toàn bộ vòng đời plugin (Service, Logger...).
        /// - AddTransient: Tạo instance mới mỗi lần GetService&lt;T&gt;() được gọi (ViewModel, Command...).
        /// - AddScoped: KHÔNG dùng — AutoCAD không có request scope concept.
        ///
        /// MỌI Service/ViewModel MỚI PHẢI được đăng ký ở đây.
        /// </summary>
        private void ConfigureServices(IServiceCollection services)
        {
            // ── Tầng Hạ Tầng (Infrastructure) ────────────────────────────────────
            services.AddSingleton<ILoggerService, LoggerService>();
            services.AddSingleton<IFileSystemService, FileSystemService>();
            services.AddSingleton<IPathProvider, PathProvider>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<IEventAggregator, EventAggregator>();
            services.AddSingleton<IWindowManagerService, WindowManagerService>();
            services.AddSingleton<IProgressMeterService, ProgressMeterService>();
            services.AddSingleton<IPaletteService, PaletteService>();
            services.AddSingleton<ISettingsService, SettingsService>();

            // ── Tầng Dịch Vụ Cốt Lõi AutoCAD (Core Services) ────────────────────
            services.AddSingleton<ICadContext, CadContext>();
            services.AddSingleton<IRibbonService, RibbonService>();
            services.AddSingleton<IEventService, EventService>();
            services.AddSingleton<INotificationService, NotificationService>();

            // ── Feature Services ──────────────────────────────────────────────────
            services.AddSingleton<IPlotService, PlotService>();
            services.AddSingleton<ILayerService, LayerService>();

            // ── Golden Sample: Grid Marker (Samples/GridMarker) ───────────────────
            services.AddSingleton<Samples.GridMarker.IGridMarkerService, Samples.GridMarker.GridMarkerService>();

            // ── Hạ tầng / Mở rộng (Infrastructure / Extensibility) ──────────────
            // OverruleManager BẮT BUỘC đăng ký dạng Singleton để có thể dispose
            // trong Terminate() — nếu không hủy đăng ký Overrules khi unload plugin
            // sẽ gây lỗi Fatal Error làm văng AutoCAD.
            services.AddSingleton<OverruleManager>();
            services.AddSingleton<ICommandHistoryService, CommandHistoryService>();

            // ── ViewModels ────────────────────────────────────────────────────────
            // Transient: mỗi lần mở một Palette sẽ tạo một instance ViewModel mới.
            services.AddTransient<Samples.GridMarker.GridMarkerViewModel>();
            services.AddTransient<ViewModels.CommonControlsViewModel>();
            services.AddTransient<ViewModels.LayerManagerViewModel>();
            services.AddTransient<ViewModels.DebugInspectorViewModel>();

            // ─── Đăng ký các Service / ViewModel mới tại đây ──────────────────────
        }

        // ─── Assembly Resolution ──────────────────────────────────────────────────

        /// <summary>
        /// Giúp CLR tìm các DLL phụ thuộc (DependencyInjection, MVVM Toolkit, Newtonsoft...)
        /// nằm cùng thư mục với Plugin DLL khi AutoCAD (.NET Framework 4.8) không tìm thấy chúng.
        ///
        /// THỨ TỰ:
        ///   1. Assembly ĐÃ NẠP khớp tên + version → trả lại chính nó (xem <see cref="FindLoadedAssembly"/>).
        ///   2. Plugin nạp từ byte array (không có Location) → trả null để handler khác xử lý.
        ///   3. Tìm file .dll cùng thư mục với plugin.
        /// </summary>
        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                var requested = new AssemblyName(args.Name);

                Assembly alreadyLoaded = FindLoadedAssembly(requested, AppDomain.CurrentDomain.GetAssemblies());
                if (alreadyLoaded != null) return alreadyLoaded;

                string dllName = requested.Name + ".dll";
                string executingLoc = Assembly.GetExecutingAssembly().Location;

                // Location trống khi load từ byte array (ví dụ CadDebugCompanion)
                if (string.IsNullOrWhiteSpace(executingLoc)) return null;

                string assemblyPath = Path.Combine(
                    Path.GetDirectoryName(executingLoc), dllName);

                if (File.Exists(assemblyPath))
                    return Assembly.LoadFrom(assemblyPath);

                // Thử tìm .resources assembly (tránh infinite resolve loop)
                if (dllName.EndsWith(".resources.dll", StringComparison.OrdinalIgnoreCase))
                    return null;
            }
            catch
            {
                // Bỏ qua mọi lỗi trong AssemblyResolve — không được throw
            }
            return null;
        }

        /// <summary>
        /// Tìm trong các assembly đã nạp một bản khớp tên (và version, nếu yêu cầu có version).
        ///
        /// VÌ SAO CẦN: khi plugin được nạp từ byte array (tool trung gian chống khóa DLL, hot-reload),
        /// assembly nằm ngoài mọi load context nên CLR KHÔNG tự tìm thấy nó theo tên. WPF gọi
        /// Assembly.Load("CadCoreFramework_…") trong InitializeComponent() để lấy BAML → AssemblyResolve.
        /// Nếu để handler khác nạp thêm bản thứ hai từ đĩa, kiểu UserControl (bản 1) và BAML (bản 2)
        /// không khớp → "The component '…' does not have a resource identified by the URI '…'".
        /// </summary>
        internal static Assembly FindLoadedAssembly(AssemblyName requested, System.Collections.Generic.IEnumerable<Assembly> loaded)
        {
            if (requested == null || loaded == null) return null;

            foreach (Assembly candidate in loaded)
            {
                AssemblyName name;
                try { name = candidate.GetName(); }
                catch { continue; }

                if (!string.Equals(name.Name, requested.Name, StringComparison.OrdinalIgnoreCase)) continue;

                // Yêu cầu có version thì phải khớp chính xác — không "ép" sang bản khác version
                // (ví dụ không trả Newtonsoft 12 của AutoCAD cho yêu cầu Newtonsoft 13 của plugin).
                if (requested.Version != null && name.Version != requested.Version) continue;

                return candidate;
            }
            return null;
        }
    }
}
