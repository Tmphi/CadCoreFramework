using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.PlottingServices;
using CadCoreFramework.Core;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace CadCoreFramework.Services
{
    /// <summary>
    /// Dịch vụ in bản vẽ theo chương trình sử dụng AutoCAD Plotting API.
    ///
    /// TRIẾT LÝ AUTODESK (PlottingServices):
    /// - PlotEngine luôn lấy qua PlotFactory.CreatePublishEngine() hoặc PlotFactory.CreatePlotEngine(),
    ///   và phải Dispose sau khi in (using).
    /// - Mỗi lúc chỉ một job in: kiểm PlotFactory.ProcessPlotState == NotPlotting trước khi bắt đầu.
    /// - In từ API chạy ở foreground: tạm đặt BACKGROUNDPLOT = 0 (mặc định 2 = publish nền) rồi khôi phục.
    /// - Mỗi trang in là một PlotInfo, tập hợp nhiều PlotInfo tạo thành PlotInfoCollection.
    /// - PlotConfig (PC3 file) xác định thiết bị in và khả năng của nó.
    /// - KHÔNG mở dialog khi dùng PlotEngine.BeginPlot() với ProgressDialog = null.
    /// - Publish (multi-page PDF) dùng PublishEngine + DsdData.
    ///
    /// LUỒNG IN CƠ BẢN:
    /// 1. Lấy Layout từ LayoutManager
    /// 2. Tạo PlotSettings (sao chép từ Layout hoặc tạo mới)
    /// 3. Validate PlotSettings với PlotSettingsValidator
    /// 4. Tạo PlotInfo và gán PlotSettings
    /// 5. Gọi PlotEngine.BeginPlot() / BeginDocument() / BeginPage() / EndPage() / EndDocument() / EndPlot()
    /// </summary>
    public class PlotService : IPlotService
    {
        private readonly ILoggerService _logger;
        private readonly INotificationService _notification;
        private readonly IFileSystemService _fileSystem;

        /// <summary>
        /// Khởi tạo một phiên bản PlotService với các dịch vụ Logger, Notification và FileSystem được tiêm vào.
        /// </summary>
        public PlotService(ILoggerService logger, INotificationService notification, IFileSystemService fileSystem)
        {
            _logger       = logger;
            _notification = notification;
            _fileSystem   = fileSystem;
        }

        // ─── Single Layout → PDF ──────────────────────────────────────────────────

        /// <summary>
        /// In một Layout chỉ định ra một file PDF.
        /// </summary>
        public bool PlotLayoutToPdf(Database db, string layoutName,
            string outputFilePath, string printerName = "DWG To PDF.pc3")
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (string.IsNullOrWhiteSpace(layoutName))
                throw new ArgumentNullException(nameof(layoutName));
            if (string.IsNullOrWhiteSpace(outputFilePath))
                throw new ArgumentNullException(nameof(outputFilePath));

            try
            {
                // Đảm bảo thư mục đầu ra tồn tại
                string dir = Path.GetDirectoryName(outputFilePath);
                if (!string.IsNullOrEmpty(dir) && !_fileSystem.DirectoryExists(dir))
                    _fileSystem.CreateDirectory(dir);

                RunPlotJob(db, new[] { layoutName }, plotEngine =>
                {
                    MakeLayoutCurrent(layoutName);
                    PlotInfo plotInfo = BuildPlotInfo(db, layoutName, printerName);
                    using (PlotProgressDialog progressDlg = new PlotProgressDialog(false, 1, true))
                    {
                        progressDlg.set_PlotMsgString(PlotMessageIndex.DialogTitle,  "CadCoreFramework — Đang in...");
                        progressDlg.set_PlotMsgString(PlotMessageIndex.CancelJobButtonMessage, "Hủy");
                        progressDlg.LowerPlotProgressRange = 0;
                        progressDlg.UpperPlotProgressRange = 100;
                        progressDlg.PlotProgressPos        = 0;

                        plotEngine.BeginPlot(progressDlg, null);

                        try
                        {
                            plotEngine.BeginDocument(plotInfo, db.Filename,
                                null, 1, true, outputFilePath);

                            progressDlg.OnBeginSheet();
                            PlotPageInfo pageInfo = new PlotPageInfo();
                            plotEngine.BeginPage(pageInfo, plotInfo, true, null);
                            plotEngine.BeginGenerateGraphics(null);
                            progressDlg.PlotProgressPos = 50;
                            plotEngine.EndGenerateGraphics(null);
                            plotEngine.EndPage(null);
                            progressDlg.OnEndSheet();

                            plotEngine.EndDocument(null);
                            progressDlg.PlotProgressPos = 100;
                            progressDlg.OnEndPlot();
                        }
                        finally
                        {
                            plotEngine.EndPlot(null);
                        }
                    }
                });

                _notification?.Info($"Đã in '{layoutName}' → {outputFilePath}");
                _logger?.Info($"PlotService: Printed '{layoutName}' to '{outputFilePath}'.");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error($"PlotService.PlotLayoutToPdf failed for '{layoutName}'.", ex);
                _notification?.Error($"Lỗi in bản vẽ: {ex.Message}");
                return false;
            }
        }

        // ─── Multi-Layout → Multi-Page PDF ───────────────────────────────────────

        /// <summary>
        /// In nhiều Layout cùng lúc vào một file PDF nhiều trang (batch plot).
        /// Dùng PublishEngine cho multi-sheet PDF — chuẩn Autodesk.
        /// </summary>
        public bool PlotMultipleLayouts(Database db, IEnumerable<string> layoutNames,
            string outputFilePath, string printerName = "DWG To PDF.pc3")
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            var names = layoutNames?.ToList();
            if (names == null || names.Count == 0)
                throw new ArgumentException("Phải có ít nhất một Layout.", nameof(layoutNames));

            try
            {
                string dir = Path.GetDirectoryName(outputFilePath);
                if (!string.IsNullOrEmpty(dir) && !_fileSystem.DirectoryExists(dir))
                    _fileSystem.CreateDirectory(dir);

                RunPlotJob(db, names, engine =>
                {
                    using (PlotProgressDialog progressDlg = new PlotProgressDialog(false, names.Count, true))
                    {
                        progressDlg.set_PlotMsgString(PlotMessageIndex.DialogTitle,
                            $"CadCoreFramework — Đang in {names.Count} trang...");
                        progressDlg.LowerPlotProgressRange = 0;
                        progressDlg.UpperPlotProgressRange = 100;
                        progressDlg.PlotProgressPos        = 0;

                        engine.BeginPlot(progressDlg, null);

                        try
                        {
                            for (int i = 0; i < names.Count; i++)
                            {
                                // PlotInfoValidator.Validate ném eLayoutNotCurrent nếu layout không phải layout hiện hành.
                                MakeLayoutCurrent(names[i]);
                                PlotInfo pageInfo = BuildPlotInfo(db, names[i], printerName);

                                // Trang đầu mở document. Tham số thứ 4 là SỐ BẢN IN (copies), không phải
                                // số trang — số trang do số lần BeginPage quyết định.
                                if (i == 0)
                                    engine.BeginDocument(pageInfo, db.Filename, null, 1, true, outputFilePath);

                                bool isLast = i == names.Count - 1;
                                progressDlg.OnBeginSheet();
                                engine.BeginPage(new PlotPageInfo(), pageInfo, isLast, null);
                                engine.BeginGenerateGraphics(null);
                                progressDlg.PlotProgressPos = (int)((i + 0.5) / names.Count * 100);
                                engine.EndGenerateGraphics(null);
                                engine.EndPage(null);
                                progressDlg.OnEndSheet();
                                progressDlg.PlotProgressPos = (int)((i + 1.0) / names.Count * 100);
                            }

                            engine.EndDocument(null);
                            progressDlg.OnEndPlot();
                        }
                        finally
                        {
                            engine.EndPlot(null);
                        }
                    }
                });

                _notification?.Info($"Đã in {names.Count} trang → {outputFilePath}");
                _logger?.Info($"PlotService: Batch printed {names.Count} layouts to '{outputFilePath}'.");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error("PlotService.PlotMultipleLayouts failed.", ex);
                _notification?.Error($"Lỗi in bản vẽ: {ex.Message}");
                return false;
            }
        }

        // ─── Enumeration ──────────────────────────────────────────────────────────

        /// <summary>
        /// Lấy danh sách tên máy in/plotter có sẵn trong hệ thống AutoCAD.
        /// </summary>
        public IEnumerable<string> GetAvailablePlotters()
        {
            var result = new List<string>();
            try
            {
                using (PlotSettingsValidator psv = PlotSettingsValidator.Current)
                {
                    var deviceList = psv.GetPlotDeviceList();
                    foreach (string device in deviceList)
                        result.Add(device);
                }
            }
            catch (Exception ex)
            {
                _logger?.Error("PlotService.GetAvailablePlotters failed.", ex);
            }
            return result;
        }

        /// <summary>
        /// Lấy danh sách tên khổ giấy hỗ trợ bởi một plotter.
        /// </summary>
        public IEnumerable<string> GetMediaNames(string plotterName)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(plotterName)) return result;
            try
            {
                using (PlotSettings ps = new PlotSettings(true))
                using (PlotSettingsValidator psv = PlotSettingsValidator.Current)
                {
                    psv.SetPlotConfigurationName(ps, plotterName, null);
                    var mediaList = psv.GetCanonicalMediaNameList(ps);
                    foreach (string media in mediaList)
                        result.Add(media);
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"PlotService.GetMediaNames failed for '{plotterName}'.", ex);
            }
            return result;
        }

        // ─── Private Helpers ──────────────────────────────────────────────────────

        /// <summary>
        /// Chạy một job in với các điều kiện tiên quyết của PlotEngine (mọi điều đều đã kiểm chứng trên
        /// AutoCAD 2024 bằng HeadlessLab PLOT_*):
        /// - chỉ in được bản vẽ đang làm việc (layout cần là layout HIỆN HÀNH của nó);
        /// - mọi layout phải tồn tại — kiểm TRƯỚC khi bắt đầu, để không bỏ dở job giữa chừng;
        /// - không có job in/publish nào khác đang chạy (PlotFactory.ProcessPlotState);
        /// - BACKGROUNDPLOT = 0 trong lúc in (in từ API chạy foreground; mặc định 2 = publish nền);
        /// - sau đó khôi phục BACKGROUNDPLOT và layout hiện hành cũ; PlotEngine luôn được Dispose.
        /// </summary>
        private static void RunPlotJob(Database db, IReadOnlyList<string> layoutNames, Action<PlotEngine> plot)
        {
            if (db != HostApplicationServices.WorkingDatabase)
                throw new InvalidOperationException(
                    "PlotService chỉ in được bản vẽ đang làm việc (HostApplicationServices.WorkingDatabase) — mở bản vẽ đó trước.");
            EnsureLayoutsExist(db, layoutNames);
            if (PlotFactory.ProcessPlotState != ProcessPlotState.NotPlotting)
                throw new InvalidOperationException(
                    "Đang có một tác vụ in/publish khác chạy (PlotFactory.ProcessPlotState) — thử lại khi nó kết thúc.");

            string previousLayout = LayoutManager.Current.CurrentLayout;
            object oldBackgroundPlot = Application.GetSystemVariable("BACKGROUNDPLOT");
            Application.SetSystemVariable("BACKGROUNDPLOT", (short)0);
            try
            {
                using (PlotEngine engine = PlotFactory.CreatePublishEngine())
                    plot(engine);
            }
            finally
            {
                Application.SetSystemVariable("BACKGROUNDPLOT", oldBackgroundPlot);
                MakeLayoutCurrent(previousLayout);
            }
        }

        /// <summary>Đặt layout hiện hành (bỏ qua nếu đã là nó). PlotInfoValidator yêu cầu điều này.</summary>
        private static void MakeLayoutCurrent(string layoutName)
        {
            LayoutManager lm = LayoutManager.Current;
            if (!string.Equals(lm.CurrentLayout, layoutName, StringComparison.OrdinalIgnoreCase))
                lm.CurrentLayout = layoutName;
        }

        private static void EnsureLayoutsExist(Database db, IEnumerable<string> layoutNames)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var layoutDict = (DBDictionary)tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead);
                foreach (string name in layoutNames)
                {
                    if (string.IsNullOrWhiteSpace(name) || !layoutDict.Contains(name))
                        throw new ArgumentException($"Layout '{name}' không tồn tại trong bản vẽ.", nameof(layoutNames));
                }
                tr.Commit();
            }
        }

        /// <summary>
        /// Xây dựng PlotInfo cho một Layout với cài đặt chuẩn.
        /// </summary>
        private static PlotInfo BuildPlotInfo(Database db, string layoutName, string printerName)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var layoutDict = (DBDictionary)tr.GetObject(
                    db.LayoutDictionaryId, OpenMode.ForRead);

                if (!layoutDict.Contains(layoutName))
                    throw new ArgumentException($"Layout '{layoutName}' không tồn tại trong bản vẽ.");

                ObjectId layoutId = layoutDict.GetAt(layoutName);
                Layout layout = tr.GetObject(layoutId, OpenMode.ForRead) as Layout;
                if (layout == null)
                    throw new InvalidOperationException($"Không thể mở Layout '{layoutName}'.");

                // Tạo PlotSettings từ Layout hiện tại
                PlotSettings ps = new PlotSettings(layout.ModelType);
                ps.CopyFrom(layout);

                PlotSettingsValidator psv = PlotSettingsValidator.Current;

                // Cấu hình thiết bị in
                psv.SetPlotConfigurationName(ps, printerName, null);
                // Vùng in = Extents TRƯỚC khi Fit/Center: layout giấy mặc định là PlotType.Layout — với nó
                // SetPlotCentered ném eInvalidInput (HeadlessLab PLOT_MULTIPLE_LAYOUTS_TO_PDF). Extents cũng
                // xác định hơn Display (vốn phụ thuộc view đang mở lúc gọi service).
                psv.SetPlotType(ps, Autodesk.AutoCAD.DatabaseServices.PlotType.Extents);
                psv.SetUseStandardScale(ps, true);
                psv.SetStdScaleType(ps, StdScaleType.ScaleToFit);
                psv.SetPlotCentered(ps, true);

                // Tự động chọn khổ giấy A4 nếu có
                // GetCanonicalMediaNameList trả về StringCollection — chỉ là IEnumerable KHÔNG generic:
                // ép thẳng sang IEnumerable<string> ném InvalidCastException lúc chạy (HeadlessLab PLOT_LAYOUT_TO_PDF).
                var mediaList = psv.GetCanonicalMediaNameList(ps);
                string a4 = mediaList.Cast<string>().FirstOrDefault(
                    m => m.IndexOf("A4", StringComparison.OrdinalIgnoreCase) >= 0);
                if (!string.IsNullOrEmpty(a4))
                    psv.SetCanonicalMediaName(ps, a4);

                tr.Abort(); // Read-only — không commit

                // Dùng layoutId đã lấy TRONG transaction — không đọc thuộc tính của 'layout' sau khi đã đóng.
                PlotInfo pi = new PlotInfo { Layout = layoutId };
                pi.OverrideSettings = ps;
                PlotInfoValidator piv = new PlotInfoValidator
                    { MediaMatchingPolicy = MatchingPolicy.MatchEnabled };
                piv.Validate(pi);

                return pi;
            }
        }
    }
}
