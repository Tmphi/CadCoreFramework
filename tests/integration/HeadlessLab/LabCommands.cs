using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.PlottingServices;
using Autodesk.AutoCAD.Runtime;
using CadCoreFramework.Core;
using CadCoreFramework.Extensions;
using CadCoreFramework.Services;
using CadCoreFramework.Utils;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

[assembly: CommandClass(typeof(CadCoreFramework.HeadlessLab.LabCommands))]

namespace CadCoreFramework.HeadlessLab
{
    /// <summary>
    /// Kiểm thử headless trên AutoCAD THẬT (accoreconsole). Mỗi kiểm thử in đúng một dòng:
    /// <c>LAB|tên|PASS</c> hoặc <c>LAB|tên|FAIL|lý do</c>. Runner yêu cầu MỌI kiểm thử đều PASS.
    /// Mỗi kiểm thử chạy trong transaction riêng và được ROLLBACK (không Commit) — bản vẽ luôn sạch.
    /// </summary>
    public class LabCommands
    {
        private const string AttBlock = "LAB_ATT_BLOCK";

        /// <summary>
        /// Hai câu nhắc của kiểm thử KEYWORDS_PROMPT_LISTS_ONCE. Runner gõ "No" cho mỗi câu và tìm đúng dòng
        /// "… [Yes/No] &lt;Yes&gt;:" trong output — đổi chuỗi nào thì đổi cả run-headless-lab.ps1.
        /// </summary>
        internal const string KeywordPrompt = "LAB keyword prompt";
        internal const string ScopeKeywordPrompt = "LAB scope keyword prompt";

        private static readonly List<(string Name, Action<ICadScope> Body)> Tests = new List<(string, Action<ICadScope>)>
        {
            ("PING", scope => Assert(scope.Database != null, "Database null")),

            // B-06: CadThread ghi nhận main thread trong Initialize → Initialize PHẢI chạy trên thread của lệnh.
            ("INITIALIZE_RUNS_ON_COMMAND_THREAD", scope =>
            {
                Assert(LabApp.InitializeThreadId != 0, "AutoCAD chưa gọi IExtensionApplication.Initialize");
                Assert(LabApp.InitializeThreadId == Environment.CurrentManagedThreadId,
                    $"Initialize chạy trên thread {LabApp.InitializeThreadId}, lệnh chạy trên thread {Environment.CurrentManagedThreadId}");
            }),

            // B-01: theme đọc từ COLORTHEME; sysvar DARKMODE không tồn tại.
            ("COLORTHEME_DRIVES_DARK_MODE", scope =>
            {
                object colorTheme = CadApplicationHelper.GetSystemVariable("COLORTHEME");
                Assert(colorTheme != null, "Không đọc được COLORTHEME");
                Assert(CadApplicationHelper.GetSystemVariable("DARKMODE") == null, "DARKMODE lại tồn tại — xem lại T03 A13");
                Assert(CadApplicationHelper.IsDarkMode == (Convert.ToInt32(colorTheme) == 0),
                    $"IsDarkMode = {CadApplicationHelper.IsDarkMode} nhưng COLORTHEME = {colorTheme}");
            }),

            // B-09: RemoveMetadata phải XÓA (Erase) Xrecord, không để lại đối tượng mồ côi.
            ("XRECORD_REMOVE_ERASES", scope =>
            {
                var line = AddLine(scope);
                XRecordHelper.SetMetadata(scope.Transaction, line, "LAB_KEY", new { v = 1, name = "x" });
                var dict = (DBDictionary)scope.Transaction.GetObject(line.ExtensionDictionary, OpenMode.ForRead);
                ObjectId xrecId = dict.GetAt("LAB_KEY");
                XRecordHelper.RemoveMetadata(scope.Transaction, line, "LAB_KEY");
                Assert(!dict.Contains("LAB_KEY"), "Key vẫn còn trong Extension Dictionary");
                Assert(xrecId.IsErased, "Xrecord chưa bị Erase (đối tượng mồ côi)");
            }),

            // B-13: Copy/Mirror block phải có attribute RIÊNG (ObjectId khác, owner là bản sao),
            // không chỉ "đếm được 1 attribute" (có thể là attribute của bản gốc bị chia sẻ).
            ("COPY_KEEPS_ATTRIBUTES", scope =>
            {
                ObjectId src = InsertAttBlock(scope, "V1");
                ObjectId copy = scope.CopyEntity(src, new Vector3d(100, 0, 0));
                AssertOwnAttribute(scope, src, copy, "V1");
            }),
            // Metadata của framework (XRecord trong Extension Dictionary, XData) phải đi theo bản sao,
            // và bản sao phải có Extension Dictionary RIÊNG.
            ("COPY_KEEPS_METADATA", scope =>
            {
                var line = AddLine(scope);
                XRecordHelper.SetMetadata(scope.Transaction, line, "LAB_KEY", new { v = 1, name = "xr" });
                XDataHelper.SetMetadata(scope.Transaction, line, "LAB_APP", new { v = 1, name = "xd" });
                ObjectId copyId = scope.CopyEntity(line.ObjectId, new Vector3d(0, 100, 0));
                var copy = (Entity)scope.Transaction.GetObject(copyId, OpenMode.ForRead);
                Assert(!copy.ExtensionDictionary.IsNull, "Bản sao không có Extension Dictionary");
                Assert(copy.ExtensionDictionary != line.ExtensionDictionary, "Bản sao DÙNG CHUNG Extension Dictionary với bản gốc");
                var xr = XRecordHelper.GetMetadata<Dictionary<string, object>>(scope.Transaction, copy, "LAB_KEY");
                Assert(xr != null && (string)xr["name"] == "xr", "Bản sao mất dữ liệu XRecord");
                var xd = XDataHelper.GetMetadata<Dictionary<string, object>>(copy, "LAB_APP");
                Assert(xd != null && (string)xd["name"] == "xd", "Bản sao mất dữ liệu XData");
            }),
            ("MIRROR_KEEPS_ATTRIBUTES", scope =>
            {
                ObjectId src = InsertAttBlock(scope, "V2");
                ObjectId mirror = scope.MirrorEntity(src, new Point3d(0, -50, 0), new Point3d(10, -50, 0));
                AssertOwnAttribute(scope, src, mirror, "V2");
            }),

            // B-30: InsertBlock với block không tồn tại → trả Null, cảnh báo (bản vẽ đang active) không ném lỗi.
            // Nhánh "bản vẽ KHÔNG active" (Editor ném eNotApplicable) cần nhiều bản vẽ → chỉ kiểm được trong GUI.
            ("INSERTBLOCK_MISSING_RETURNS_NULL", scope =>
            {
                ObjectId id = scope.InsertBlock("LAB_BLOCK_KHONG_CO", Point3d.Origin);
                Assert(id.IsNull, "InsertBlock với block không tồn tại phải trả ObjectId.Null");
            }),

            // B-25: đổi giá trị attribute căn giữa → hình học phải căn lại quanh điểm căn chỉnh.
            ("ATTRIBUTE_REALIGNED_AFTER_SET", scope =>
            {
                ObjectId refId = InsertAttBlock(scope, "A");
                var br = (BlockReference)scope.Transaction.GetObject(refId, OpenMode.ForWrite);
                BlockHelper.SetBlockAttribute(scope.Transaction, br, "TAG", "MOT CHUOI DAI HON NHIEU");
                var att = FirstAttribute(scope, br);
                Extents3d ext = att.GeometricExtents;
                double centerX = (ext.MinPoint.X + ext.MaxPoint.X) / 2.0;
                Assert(Math.Abs(centerX - att.AlignmentPoint.X) < 0.5,
                    $"Tâm chữ X={centerX:0.###} lệch khỏi điểm căn X={att.AlignmentPoint.X:0.###}");
            }),

            // B-16: tên ký hiệu không hợp lệ → ArgumentException rõ ràng (không phải eInvalidInput mơ hồ).
            ("ENSURE_LAYER_REJECTS_INVALID_NAME", scope =>
            {
                try { scope.EnsureLayer("BAD<NAME"); }
                catch (ArgumentException) { return; }
                throw new LabAssertException("Không ném ArgumentException cho tên chứa '<'");
            }),

            // B-12: block đã tồn tại → entity truyền vào phải được Dispose.
            ("CREATE_BLOCKDEF_EXISTING_DISPOSES", scope =>
            {
                scope.CreateBlockDefinition("LAB_BLK_DUP", new Entity[] { new Line(Point3d.Origin, new Point3d(1, 0, 0)) });
                var unused = new Line(Point3d.Origin, new Point3d(2, 0, 0));
                scope.CreateBlockDefinition("LAB_BLK_DUP", new Entity[] { unused });
                Assert(unused.IsDisposed, "Entity không dùng tới chưa được Dispose");
            }),

            // B-14: configure ném lỗi → entity mới tạo phải được Dispose.
            ("DRAW_CONFIGURE_THROWS_DISPOSES", scope =>
            {
                Line captured = null;
                try { scope.DrawLine(Point3d.Origin, new Point3d(5, 5, 0), configure: l => { captured = l; throw new InvalidOperationException("boom"); }); }
                catch (InvalidOperationException) { }
                Assert(captured != null, "configure không được gọi");
                Assert(captured.IsDisposed, "Entity bỏ dở chưa được Dispose");
            }),

            // B-17 (đặc tả hành vi AutoCAD): ký tự HỢP LỆ trong tên layer nhưng là wildcard trong filter.
            //   "#" = một chữ số → "LAB#1" KHÔNG khớp chính nó, lại khớp "LAB21".
            //   "." = một ký tự không phải chữ/số → "LAB.X" khớp cả "LAB-X".
            ("FILTER_ONLAYERS_IS_WILDCARD", scope =>
            {
                SeedWildcardLayers(scope);
                var hash = SelectLayers(scope, SelectionFilterBuilder.Create().OnLayers("LAB#1").Build());
                Assert(!hash.Contains("LAB#1") && hash.Contains("LAB21"),
                    $"OnLayers(\"LAB#1\") chọn [{string.Join(",", hash)}], kỳ vọng [LAB21] (wildcard '#')");
                var dot = SelectLayers(scope, SelectionFilterBuilder.Create().OnLayers("LAB.X").Build());
                Assert(dot.Count == 2, $"OnLayers(\"LAB.X\") chọn [{string.Join(",", dot)}], kỳ vọng 2 (wildcard '.')");
            }),

            // B-17: OnLayersExact khớp ĐÚNG nguyên văn tên.
            ("FILTER_EXACT_MATCHES_LITERALLY", scope =>
            {
                SeedWildcardLayers(scope);
                var hash = SelectLayers(scope, SelectionFilterBuilder.Create().OnLayersExact("LAB#1").Build());
                Assert(hash.Count == 1 && hash[0] == "LAB#1", $"OnLayersExact(\"LAB#1\") chọn [{string.Join(",", hash)}], kỳ vọng [LAB#1]");
                var dot = SelectLayers(scope, SelectionFilterBuilder.Create().OnLayersExact("LAB.X", "LAB21").Build());
                Assert(dot.Count == 2 && dot.Contains("LAB.X") && dot.Contains("LAB21") && !dot.Contains("LAB-X"),
                    $"OnLayersExact(\"LAB.X\",\"LAB21\") chọn [{string.Join(",", dot)}]");
            }),
        };

        /// <summary>
        /// Kiểm thử cần database ĐÃ COMMIT (không chạy trong transaction rollback), ví dụ in ấn.
        /// Thay đổi của chúng bị bỏ khi runner thoát accoreconsole không lưu.
        /// </summary>
        private static readonly List<(string Name, Action<Document> Body)> DocumentTests = new List<(string, Action<Document>)>
        {
            // B-10: in thật ra PDF; BACKGROUNDPLOT phải được khôi phục; không để lại job in dở.
            ("PLOT_LAYOUT_TO_PDF", doc =>
            {
                using (doc.LockDocument())
                using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                {
                    var scope = new LabScope(doc, tr);
                    scope.DrawRectangle(new Point3d(0, 0, 0), new Point3d(200, 100, 0));
                    scope.DrawLine(new Point3d(0, 0, 0), new Point3d(200, 100, 0));
                    tr.Commit();
                }

                string dir = Path.Combine(Path.GetTempPath(), "CadCoreFramework_lab");
                string pdf = Path.Combine(dir, "lab-plot.pdf");
                if (File.Exists(pdf)) File.Delete(pdf);
                object backgroundPlotBefore = Application.GetSystemVariable("BACKGROUNDPLOT");

                var logger = new LabLogger();
                var plot = new PlotService(logger, new LabNotification(doc), new FileSystemService());
                bool ok = plot.PlotLayoutToPdf(doc.Database, "Model", pdf);

                Assert(ok, "PlotLayoutToPdf trả về false: " + string.Join(" | ", logger.Errors));
                Assert(File.Exists(pdf) && new FileInfo(pdf).Length > 0, $"Không có file PDF tại {pdf}");
                object backgroundPlotAfter = Application.GetSystemVariable("BACKGROUNDPLOT");
                Assert(Equals(backgroundPlotAfter, backgroundPlotBefore),
                    $"BACKGROUNDPLOT không được khôi phục ({backgroundPlotBefore} → {backgroundPlotAfter})");
                Assert(PlotFactory.ProcessPlotState == ProcessPlotState.NotPlotting,
                    $"Job in chưa kết thúc: ProcessPlotState = {PlotFactory.ProcessPlotState}");
            }),

            // B-10: in nhiều layout vào MỘT PDF nhiều trang (nhánh PublishEngine multi-sheet).
            ("PLOT_MULTIPLE_LAYOUTS_TO_PDF", doc =>
            {
                string pdf = Path.Combine(Path.GetTempPath(), "CadCoreFramework_lab", "lab-multi.pdf");
                if (File.Exists(pdf)) File.Delete(pdf);
                object backgroundPlotBefore = Application.GetSystemVariable("BACKGROUNDPLOT");
                string layoutBefore = LayoutManager.Current.CurrentLayout;

                var logger = new LabLogger();
                var plot = new PlotService(logger, new LabNotification(doc), new FileSystemService());
                bool ok = plot.PlotMultipleLayouts(doc.Database, new[] { "Layout1", "Layout2" }, pdf);

                Assert(ok, "PlotMultipleLayouts trả về false: " + string.Join(" | ", logger.Errors));
                Assert(File.Exists(pdf) && new FileInfo(pdf).Length > 0, $"Không có file PDF tại {pdf}");
                int pages = CountPdfPages(pdf);
                Assert(pages == 2, $"PDF có {pages} trang, kỳ vọng 2");
                Assert(Equals(Application.GetSystemVariable("BACKGROUNDPLOT"), backgroundPlotBefore), "BACKGROUNDPLOT không được khôi phục");
                Assert(PlotFactory.ProcessPlotState == ProcessPlotState.NotPlotting, "Job in chưa kết thúc");
                Assert(LayoutManager.Current.CurrentLayout == layoutBefore,
                    $"Layout hiện hành không được trả lại ({layoutBefore} → {LayoutManager.Current.CurrentLayout})");
            }),

            // B-10: tên layout sai → báo lỗi rõ TRƯỚC khi bắt đầu job (không bỏ dở PDF, không đổi layout).
            ("PLOT_UNKNOWN_LAYOUT_FAILS_CLEANLY", doc =>
            {
                string layoutBefore = LayoutManager.Current.CurrentLayout;
                var logger = new LabLogger();
                var plot = new PlotService(logger, new LabNotification(doc), new FileSystemService());
                bool ok = plot.PlotMultipleLayouts(doc.Database, new[] { "Layout1", "KHONG_CO" },
                    Path.Combine(Path.GetTempPath(), "CadCoreFramework_lab", "lab-missing.pdf"));

                Assert(!ok, "Phải trả về false khi có layout không tồn tại");
                Assert(logger.Errors.Any(e => e.Contains("KHONG_CO")), "Thông điệp lỗi không nêu tên layout: " + string.Join(" | ", logger.Errors));
                Assert(LayoutManager.Current.CurrentLayout == layoutBefore, "Layout hiện hành bị đổi");
                Assert(PlotFactory.ProcessPlotState == ProcessPlotState.NotPlotting, "Job in bị treo");
            }),

            // B-33: AutoCAD tự nối "[Yes/No] <Yes>:" vào câu nhắc — helper không được nối thêm lần nữa.
            // Prompt THẬT, hai lần: CadPromptHelper.GetKeywords, rồi extension cũ AskKeyword (cùng lỗi). Runner gõ "No"
            // cho mỗi lần (không gõ Enter: dòng trống ở dấu nhắc lệnh sẽ lặp lại LAB_ALL — T03 A6), rồi kiểm trong
            // output rằng mỗi danh sách chỉ xuất hiện MỘT lần.
            ("KEYWORDS_PROMPT_LISTS_ONCE", doc =>
            {
                string answer = CadPromptHelper.GetKeywords(KeywordPrompt, new[] { "Yes", "No" }, "Yes");
                Assert(answer == "No", $"GetKeywords trả '{answer ?? "null"}', kỳ vọng 'No' (runner gõ No)");

                using (doc.LockDocument())
                using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                {
#pragma warning disable CS0618 // AskKeyword đã Obsolete (prompt trong lúc giữ transaction) nhưng vẫn là API công khai
                    string scoped = new LabScope(doc, tr).AskKeyword(ScopeKeywordPrompt, new[] { "Yes", "No" }, "Yes");
#pragma warning restore CS0618
                    Assert(scoped == "No", $"AskKeyword trả '{scoped ?? "null"}', kỳ vọng 'No' (runner gõ No)");
                }
            }),
        };

        [CommandMethod("LAB_ALL")]
        public void RunAll()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Write(doc, $"LAB|COUNT|{Tests.Count + DataFileLab.Tests.Count + DocumentTests.Count}");
            foreach (var (name, body) in Tests.Concat(DataFileLab.Tests))
            {
                Run(doc, name, () =>
                {
                    using (doc.LockDocument())
                    using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                    {
                        body(new LabScope(doc, tr));
                        // Không Commit: mọi kiểm thử được rollback, không ảnh hưởng kiểm thử sau.
                    }
                });
            }
            foreach (var (name, body) in DocumentTests)
                Run(doc, name, () => body(doc));
            Write(doc, "LAB|DONE");
        }

        private static void Run(Document doc, string name, Action test)
        {
            try
            {
                test();
                Write(doc, $"LAB|{name}|PASS");
            }
            catch (System.Exception ex)
            {
                Write(doc, $"LAB|{name}|FAIL|{ex.GetType().Name}: {ex.Message.Replace('\n', ' ')}");
            }
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        internal static void Assert(bool condition, string message)
        {
            if (!condition) throw new LabAssertException(message);
        }

        /// <summary>Đếm trang PDF bằng số đối tượng "/Type /Page" (không tính "/Pages").</summary>
        private static int CountPdfPages(string path)
        {
            string raw = System.Text.Encoding.GetEncoding("ISO-8859-1").GetString(File.ReadAllBytes(path));
            return System.Text.RegularExpressions.Regex.Matches(raw, @"/Type\s*/Page(?![a-zA-Z])").Count;
        }

        private static Line AddLine(ICadScope scope)
        {
            ObjectId id = scope.DrawLine(Point3d.Origin, new Point3d(10, 0, 0));
            return (Line)scope.Transaction.GetObject(id, OpenMode.ForWrite);
        }

        /// <summary>Định nghĩa block có 1 attribute "TAG" căn MiddleCenter, rồi chèn một tham chiếu.</summary>
        private static ObjectId InsertAttBlock(ICadScope scope, string value)
        {
            if (!scope.HasBlock(AttBlock))
            {
                var attDef = new AttributeDefinition
                {
                    Tag = "TAG",
                    Prompt = "Tag",
                    TextString = "X",
                    Height = 2.5,
                    Justify = AttachmentPoint.MiddleCenter,
                    AlignmentPoint = new Point3d(0, 0, 0),
                };
                attDef.SetDatabaseDefaults(scope.Database);
                var frame = new Line(new Point3d(-5, -5, 0), new Point3d(5, 5, 0));
                scope.CreateBlockDefinition(AttBlock, new Entity[] { frame, attDef });
                attDef.AdjustAlignment(scope.Database);
            }
            return scope.InsertBlock(AttBlock, new Point3d(20, 20, 0),
                attributes: new Dictionary<string, string> { ["TAG"] = value });
        }

        private static AttributeReference FirstAttribute(ICadScope scope, BlockReference br)
        {
            foreach (ObjectId id in br.AttributeCollection)
                return (AttributeReference)scope.Transaction.GetObject(id, OpenMode.ForRead);
            throw new LabAssertException("BlockReference không có attribute nào");
        }

        private static void AssertOwnAttribute(ICadScope scope, ObjectId srcId, ObjectId copyId, string expected)
        {
            Assert(!copyId.IsNull, "Không tạo được bản sao");
            Assert(copyId != srcId, "Bản sao trùng ObjectId với bản gốc");
            var src = (BlockReference)scope.Transaction.GetObject(srcId, OpenMode.ForRead);
            var copy = (BlockReference)scope.Transaction.GetObject(copyId, OpenMode.ForRead);
            Assert(copy.AttributeCollection.Count == 1, $"Bản sao có {copy.AttributeCollection.Count} attribute, kỳ vọng 1");
            var srcAtt = FirstAttribute(scope, src);
            var att = FirstAttribute(scope, copy);
            Assert(att.ObjectId != srcAtt.ObjectId, "Attribute của bản sao CHÍNH LÀ attribute của bản gốc (chia sẻ)");
            Assert(att.OwnerId == copyId, $"Owner của attribute là {att.OwnerId}, kỳ vọng bản sao {copyId}");
            Assert(att.TextString == expected, $"Giá trị attribute '{att.TextString}', kỳ vọng '{expected}'");
        }

        private static void SeedWildcardLayers(ICadScope scope)
        {
            foreach (string name in new[] { "LAB#1", "LAB21", "LAB.X", "LAB-X" })
                scope.DrawLine(Point3d.Origin, new Point3d(1, 0, 0), layer: name);
        }

        /// <summary>Tên layer của các entity được SelectAll(filter) chọn.</summary>
        private static List<string> SelectLayers(ICadScope scope, SelectionFilter filter)
        {
            PromptSelectionResult res = scope.Editor.SelectAll(filter);
            if (res.Status != PromptStatus.OK) return new List<string>();
            return res.Value.GetObjectIds()
                .Select(id => ((Entity)scope.Transaction.GetObject(id, OpenMode.ForRead)).Layer)
                .OrderBy(n => n, StringComparer.Ordinal).ToList();
        }

        // Xuống dòng CẢ trước lẫn sau: AutoCAD có thể in tiếp ngay sau dòng LAB (vd. "Effective plotting area…"),
        // làm runner không nhận ra "PASS".
        private static void Write(Document doc, string line) => doc.Editor.WriteMessage("\n" + line + "\n");
    }

    internal sealed class LabAssertException : System.Exception
    {
        public LabAssertException(string message) : base(message) { }
    }
}
