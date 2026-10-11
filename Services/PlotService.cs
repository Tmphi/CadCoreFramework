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
    /// - PlotEngine là singleton — luôn lấy qua PlotFactory.CreatePublishEngine()
    ///   hoặc PlotFactory.CreatePlotEngine().
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

                PlotInfo   plotInfo   = BuildPlotInfo(db, layoutName, printerName, outputFilePath);
                PlotEngine plotEngine = PlotFactory.CreatePublishEngine();

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

                // Xây dựng PlotInfoCollection cho multi-sheet
                using (PlotProgressDialog progressDlg = new PlotProgressDialog(false, names.Count, true))
                {
                    progressDlg.set_PlotMsgString(PlotMessageIndex.DialogTitle,
                        $"CadCoreFramework — Đang in {names.Count} trang...");
                    progressDlg.LowerPlotProgressRange = 0;
                    progressDlg.UpperPlotProgressRange = 100;
                    progressDlg.PlotProgressPos        = 0;

                    PlotEngine engine = PlotFactory.CreatePublishEngine();
                    engine.BeginPlot(progressDlg, null);

                    try
                    {
                        // Dùng trang đầu để BeginDocument
                        PlotInfo firstInfo = BuildPlotInfo(db, names[0], printerName, outputFilePath);
                        engine.BeginDocument(firstInfo, db.Filename, null,
                            names.Count, true, outputFilePath);

                        for (int i = 0; i < names.Count; i++)
                        {
                            PlotInfo pageInfo = i == 0
                                ? firstInfo
                                : BuildPlotInfo(db, names[i], printerName, outputFilePath);

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
        /// Xây dựng PlotInfo cho một Layout với cài đặt chuẩn.
        /// </summary>
        private static PlotInfo BuildPlotInfo(Database db, string layoutName,
            string printerName, string outputFilePath)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var layoutDict = (DBDictionary)tr.GetObject(
                    db.LayoutDictionaryId, OpenMode.ForRead);

                if (!layoutDict.Contains(layoutName))
                    throw new ArgumentException($"Layout '{layoutName}' không tồn tại trong bản vẽ.");

                Layout layout = tr.GetObject(layoutDict.GetAt(layoutName), OpenMode.ForRead) as Layout;
                if (layout == null)
                    throw new InvalidOperationException($"Không thể mở Layout '{layoutName}'.");

                // Tạo PlotSettings từ Layout hiện tại
                PlotSettings ps = new PlotSettings(layout.ModelType);
                ps.CopyFrom(layout);

                PlotSettingsValidator psv = PlotSettingsValidator.Current;

                // Cấu hình thiết bị in
                psv.SetPlotConfigurationName(ps, printerName, null);
                psv.SetUseStandardScale(ps, true);
                psv.SetStdScaleType(ps, StdScaleType.ScaleToFit);
                psv.SetPlotCentered(ps, true);

                // Tự động chọn khổ giấy A4 nếu có
                var mediaList = psv.GetCanonicalMediaNameList(ps);
                string a4 = ((IEnumerable<string>)mediaList).FirstOrDefault(
                    m => m.IndexOf("A4", StringComparison.OrdinalIgnoreCase) >= 0);
                if (!string.IsNullOrEmpty(a4))
                    psv.SetCanonicalMediaName(ps, a4);

                tr.Abort(); // Read-only — không commit

                PlotInfo pi = new PlotInfo { Layout = layout.ObjectId };
                pi.OverrideSettings = ps;
                PlotInfoValidator piv = new PlotInfoValidator
                    { MediaMatchingPolicy = MatchingPolicy.MatchEnabled };
                piv.Validate(pi);

                return pi;
            }
        }
    }
}
