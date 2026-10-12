using System;
using System.IO;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using CadCoreFramework.Core;
using CadCoreFramework.Extensions;
using CadCoreFramework.Services;
using CadCoreFramework.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace CadCoreFramework.Commands.Demos
{
    /// <summary>
    /// Các lệnh minh họa các dịch vụ hạ tầng trong hệ thống DI:
    /// ISettingsService, IProgressMeterService, INotificationService, IPlotService.
    /// </summary>
    public class DemoServicesCommands : CadCommandBase
    {
        private INotificationService Notify
            => AppBootstrapper.ServiceProvider?.GetService<INotificationService>();

        private ISettingsService Settings
            => AppBootstrapper.ServiceProvider?.GetService<ISettingsService>();

        private IProgressMeterService Progress
            => AppBootstrapper.ServiceProvider?.GetService<IProgressMeterService>();

        private IPlotService Plot
            => AppBootstrapper.ServiceProvider?.GetService<IPlotService>();

        /// <summary>
        /// DEMO_SETTINGS — Minh họa cách lưu/đọc cài đặt người dùng qua ISettingsService.
        /// Dữ liệu được lưu dưới dạng tệp JSON trên đĩa, giữ lại sau khi đóng AutoCAD.
        /// </summary>
        [CommandMethod("DEMO_SETTINGS")]
        public void DemoSettings()
        {
            Notify?.Section("DEMO: ISETTINGSSERVICE — LƯU/ĐỌC CÀI ĐẶT NGƯỜI DÙNG");

            ExecuteCommand(() =>
            {
                if (Settings == null)
                {
                    Notify?.Error("ISettingsService chưa được đăng ký trong DI.");
                    return;
                }

                // 1. Ghi cài đặt chuỗi
                Settings.SetSetting("Demo.LastUser", Environment.UserName);
                Notify?.Info($"1. SetSetting('Demo.LastUser', '{Environment.UserName}')");

                // 2. Ghi cài đặt số
                Settings.SetSetting("Demo.DefaultScale", 1.5);
                Notify?.Info("2. SetSetting('Demo.DefaultScale', 1.5)");

                // 3. Ghi cài đặt boolean
                Settings.SetSetting("Demo.DarkMode", true);
                Notify?.Info("3. SetSetting('Demo.DarkMode', true)");

                // 4. Ghi cài đặt phức tạp (object)
                var profile = new { Name = "CadCoreFramework", Version = "2.0", MaxUndo = 50 };
                Settings.SetSetting("Demo.Profile", profile);
                Notify?.Info("4. SetSetting('Demo.Profile', { Name, Version, MaxUndo })");

                // 5. Lưu xuống đĩa (JSON file)
                Settings.Save();
                Notify?.Info("5. Save() — Đã lưu tất cả xuống tệp settings.json trên đĩa.");

                // 6. Đọc lại
                Notify?.Info("─── ĐỌC LẠI CÀI ĐẶT ───");
                string user = Settings.GetSetting<string>("Demo.LastUser", "unknown");
                Notify?.Info($"  Demo.LastUser:     \"{user}\"");

                double scale = Settings.GetSetting<double>("Demo.DefaultScale", 1.0);
                Notify?.Info($"  Demo.DefaultScale: {scale}");

                bool darkMode = Settings.GetSetting<bool>("Demo.DarkMode", false);
                Notify?.Info($"  Demo.DarkMode:     {darkMode}");

                // 7. Đọc giá trị chưa tồn tại (trả về defaultValue)
                int missing = Settings.GetSetting<int>("Demo.NotExist", 42);
                Notify?.Info($"  Demo.NotExist:     {missing} (giá trị mặc định)");
            });

            Notify?.Section("HOÀN TẤT: DEMO SETTINGS");
        }

        /// <summary>
        /// DEMO_PROGRESS — Minh họa cách sử dụng IProgressMeterService
        /// để hiển thị thanh tiến trình trên Status Bar khi chạy tác vụ nặng.
        /// </summary>
        [CommandMethod("DEMO_PROGRESS")]
        public void DemoProgress()
        {
            Notify?.Section("DEMO: IPROGRESSMETERSERVICE — THANH TIẾN TRÌNH");

            ExecuteCommand(() =>
            {
                if (Progress == null)
                {
                    Notify?.Error("IProgressMeterService chưa được đăng ký trong DI.");
                    return;
                }

                const int totalSteps = 100;
                Notify?.Info($"Bắt đầu xử lý {totalSteps} bước. Theo dõi thanh tiến trình trên Status Bar...");

                try
                {
                    Progress.Start("Demo Progress — Đang xử lý...", totalSteps);

                    for (int i = 0; i < totalSteps; i++)
                    {
                        System.Threading.Thread.Sleep(20);
                        Progress.MeterProgress();

                        if ((i + 1) % 25 == 0)
                        {
                            int percent = (int)((i + 1.0) / totalSteps * 100);
                            Notify?.Info($"  → Tiến trình: {percent}% ({i + 1}/{totalSteps})");
                        }
                    }
                }
                finally
                {
                    Progress.Stop();
                }

                Notify?.Info("Đã hoàn tất 100%!");
            });

            Notify?.Section("HOÀN TẤT: DEMO PROGRESS");
        }

        /// <summary>
        /// DEMO_NOTIFICATION — Minh họa các loại thông báo của INotificationService:
        /// Info, Warn, Error, Section, Confirm.
        /// </summary>
        [CommandMethod("DEMO_NOTIFICATION")]
        public void DemoNotification()
        {
            if (Notify == null)
            {
                Editor?.WriteMessage("\n[Lỗi] INotificationService chưa được đăng ký trong DI.");
                return;
            }

            // 1. Section — Phân đoạn rõ ràng
            Notify.Section("DEMO: INOTIFICATIONSERVICE — CÁC LOẠI THÔNG BÁO");

            // 2. Info — Thông báo thông thường
            Notify.Info("Đây là thông báo Info — hiển thị tin nhắn thông thường.");

            // 3. Warn — Cảnh báo
            Notify.Warn("Đây là thông báo Warn — cảnh báo người dùng.");

            // 4. Error — Lỗi
            Notify.Error("Đây là thông báo Error — hiển thị thông tin lỗi.");

            // 5. Confirm — Hộp thoại xác nhận (MessageBox)
            bool confirmed = Notify.Confirm(
                "Đây là hộp thoại Confirm.\nBạn có muốn tiếp tục không?",
                "Demo Confirm"
            );
            Notify.Info($"Kết quả Confirm: {(confirmed ? "Đồng ý (Yes)" : "Từ chối (No)")}");

            Notify.Section("HOÀN TẤT: DEMO NOTIFICATION");
        }

        /// <summary>
        /// DEMO_DI_RESOLVE — Minh họa cách resolve service từ DI Container.
        /// In danh sách tất cả các service đã đăng ký và kiểm tra chúng có khả dụng không.
        /// </summary>
        [CommandMethod("DEMO_DI_RESOLVE")]
        public void DemoDiResolve()
        {
            Notify?.Section("DEMO: DEPENDENCY INJECTION — RESOLVE SERVICES");

            ExecuteCommand(() =>
            {
                var sp = AppBootstrapper.ServiceProvider;
                if (sp == null)
                {
                    Notify?.Error("ServiceProvider is null — AppBootstrapper chưa được khởi tạo.");
                    return;
                }

                Notify?.Info("─── KIỂM TRA CÁC SERVICE ĐÃ ĐĂNG KÝ ───");

                var servicesToCheck = new (string Name, Type ServiceType)[]
                {
                    ("ILoggerService",          typeof(ILoggerService)),
                    ("ICadContext",              typeof(ICadContext)),
                    ("IEventService",            typeof(IEventService)),
                    ("INotificationService",     typeof(INotificationService)),
                    ("IRibbonService",           typeof(IRibbonService)),
                    ("IPaletteService",          typeof(IPaletteService)),
                    ("IEventAggregator",         typeof(IEventAggregator)),
                    ("ISettingsService",         typeof(ISettingsService)),
                    ("IProgressMeterService",    typeof(IProgressMeterService)),
                    ("IDialogService",           typeof(IDialogService)),
                    ("IFileSystemService",       typeof(IFileSystemService)),
                    ("IPathProvider",            typeof(IPathProvider)),
                    ("IWindowManagerService",    typeof(IWindowManagerService)),
                    ("IPlotService",             typeof(IPlotService)),
                    ("IGridMarkerService",       typeof(Samples.GridMarker.IGridMarkerService)),
                    ("ILayerService",            typeof(ILayerService)),
                    ("ICommandHistoryService",   typeof(ICommandHistoryService)),
                    ("OverruleManager",          typeof(OverruleManager)),
                };

                int found = 0;
                int missing = 0;

                foreach (var (name, type) in servicesToCheck)
                {
                    var instance = sp.GetService(type);
                    if (instance != null)
                    {
                        string implName = instance.GetType().Name;
                        Notify?.Info($"  ✔ {name,-28} → {implName}");
                        found++;
                    }
                    else
                    {
                        Notify?.Warn($"  ✘ {name,-28} → CHƯA ĐĂNG KÝ");
                        missing++;
                    }
                }

                Notify?.Info($"─── KẾT QUẢ: {found} service khả dụng, {missing} thiếu ───");
            });

            Notify?.Section("HOÀN TẤT: DEMO DI RESOLVE");
        }

        /// <summary>
        /// DEMO_PLOT_PDF — Minh họa dịch vụ in ấn IPlotService: Liệt kê máy in và khổ giấy hỗ trợ.
        /// </summary>
        [CommandMethod("DEMO_PLOT_PDF")]
        public void DemoPlotPdf()
        {
            Notify?.Section("DEMO: IPLOTSERVICE — DỊCH VỤ IN ẤN TỰ ĐỘNG");

            ExecuteCommand(() =>
            {
                if (Plot == null)
                {
                    Notify?.Error("IPlotService chưa được đăng ký trong DI.");
                    return;
                }

                var plotters = Plot.GetAvailablePlotters();
                Notify?.Info("Danh sách Plotter có sẵn trong hệ thống:");
                int count = 0;
                foreach (var p in plotters)
                {
                    if (count++ < 5)
                        Notify?.Info($"  - {p}");
                }
                if (count > 5)
                    Notify?.Info($"  ... và {count - 5} plotter khác.");

                // Thử lấy danh sách khổ giấy của DWG To PDF.pc3
                var mediaNames = Plot.GetMediaNames("DWG To PDF.pc3");
                Notify?.Info("Khổ giấy mẫu hỗ trợ bởi 'DWG To PDF.pc3':");
                int mCount = 0;
                foreach (var m in mediaNames)
                {
                    if (mCount++ < 4)
                        Notify?.Info($"  * {m}");
                }
            });

            Notify?.Section("HOÀN TẤT: DEMO IPLOTSERVICE");
        }

        /// <summary>
        /// DEMO_UNDO_GROUP — Minh họa gom nhiều lệnh vẽ (nhiều Transaction) thành 1 bước Undo duy nhất.
        /// </summary>
        [CommandMethod("DEMO_UNDO_GROUP")]
        public void DemoUndoGroup()
        {
            Notify?.Section("DEMO: UNDO GROUP");

            // Mở Undo Mark
            using (var undo = new UndoGroupManager())
            {
                // Hành động 1
                ExecuteWithScope(scope =>
                {
                    scope.DrawCircle(new Point3d(0, 0, 0), 10, colorIndex: 1);
                });

                // Hành động 2
                ExecuteWithScope(scope =>
                {
                    scope.DrawCircle(new Point3d(25, 0, 0), 10, colorIndex: 2);
                });

                // Hành động 3
                ExecuteWithScope(scope =>
                {
                    scope.DrawCircle(new Point3d(50, 0, 0), 10, colorIndex: 3);
                });
            }

            Notify?.Info("Đã vẽ 3 đường tròn. Hãy thử nhấn Ctrl+Z (Undo) — cả 3 hình sẽ biến mất cùng lúc!");
        }

        /// <summary>
        /// DEMO_ASYNC_AWAIT — Minh họa chạy ngầm (không treo AutoCAD) và vẽ khi xong.
        /// Sử dụng CadAsyncHelper để chuyển luồng an toàn.
        /// [CommandMethod] không thể là 'async Task', nên ta dùng FireAndForget(onError)
        /// để mọi exception đều được log và hiển thị thay vì bị nuốt mất.
        /// </summary>
        [CommandMethod("DEMO_ASYNC_AWAIT")]
        public void DemoAsyncAwait()
        {
            Notify?.Section("DEMO: ASYNC / AWAIT (Không treo CAD)");
            Notify?.Info("Bắt đầu gọi API hoặc tải dữ liệu nặng (đợi 3 giây)... Bạn vẫn có thể pan/zoom bản vẽ!");

            // Fire-and-forget CÓ xử lý lỗi. ExecuteSafeAsync tự capture Main Thread context ngay tại đây.
            CadAsyncHelper.ExecuteSafeAsync<string>(
                async () =>
                {
                    // Mô phỏng tải dữ liệu (ví dụ gọi HTTP API) tốn 3 giây
                    await Task.Delay(3000);
                    return "Dữ liệu tải về từ máy chủ thành công!";
                },
                result =>
                {
                    // Trở lại Main Thread của AutoCAD an toàn
                    Notify?.Info(result);
                    
                    ExecuteWithScope(scope =>
                    {
                        scope.DrawCircle(new Point3d(0, 50, 0), 20, colorIndex: 4);
                        Notify?.Info("Đã vẽ một đường tròn sau khi tải xong.");
                    });
                }
            ).FireAndForget(ex => Notify?.Error("DEMO_ASYNC_AWAIT thất bại", ex));
        }
    }
}
