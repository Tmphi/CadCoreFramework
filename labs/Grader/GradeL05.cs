using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using CadCoreFramework.Core;
using CadCoreFramework.Labs.L05;
using CadCoreFramework.Utils;
using static CadCoreFramework.Labs.Grader.Grade;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

[assembly: CommandClass(typeof(CadCoreFramework.Labs.Grader.GradeL05))]

namespace CadCoreFramework.Labs.Grader
{
    /// <summary>
    /// Chấm L05. Phần 1 (DoorSelector) trên bản vẽ mẫu. Phần 2 (B-17) chấm thẳng
    /// <c>SelectionFilterBuilder.OnLayersExact</c> của framework trên branch đang checkout:
    /// trên v1.0.0 hàm này chưa có, nên kiểm bằng reflection để bộ chấm vẫn biên dịch được.
    /// </summary>
    public class GradeL05
    {
        /// <summary>Tên layer HỢP LỆ nhưng chứa ký tự wildcard, kèm một tên mà wildcard đó khớp nhầm.</summary>
        private static readonly (string Exact, string LookAlike, string Why)[] WildcardLayers =
        {
            ("LAB#1", "LAB21", "'#' là một chữ số"),
            ("M.EQUIP", "M-EQUIP", "'.' là một ký tự không phải chữ/số"),
            ("LAB@X", "LABAX", "'@' là một chữ cái"),
            ("LAB[1]", "LAB1", "'[1]' là một ký tự trong ngoặc"),
        };

        [CommandMethod("LAB_GRADE_L05")]
        public void Run()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            var context = new CadContext();
            GradeL02.EnsureSample(doc.Database);
            Count("L05", 4);

            foreach (string block in new[] { SampleDrawing.DoorBlock, SampleDrawing.WindowBlock, SampleDrawing.ColumnBlock })
                Check($"L05.SELECT_{block}", () => SelectByEffectiveName(context, block));

            Check("L05.B17_ON_LAYERS_EXACT", () => OnLayersExact(doc));
            End("L05");
        }

        private static string SelectByEffectiveName(ICadContext context, string block)
        {
            string note = null;
            context.ExecuteRead(scope =>
            {
                bool hasDynamic = SampleDrawing.HasDynamicBlocks(scope.Transaction, scope.Database);
                if (!hasDynamic && block != SampleDrawing.ColumnBlock)
                    throw new LabSkip("bản vẽ mẫu không có dynamic block (máy không có bộ mẫu Autodesk)");

                var expected = new HashSet<ObjectId>();
                int anonymous = 0;
                foreach (ObjectId id in scope.CurrentSpace)
                {
                    if (id.ObjectClass.DxfName != "INSERT") continue;
                    var br = (BlockReference)scope.Transaction.GetObject(id, OpenMode.ForRead);
                    if (EffectiveName(scope.Transaction, br) != block) continue;
                    expected.Add(id);
                    if (br.Name.StartsWith("*U", StringComparison.OrdinalIgnoreCase)) anonymous++;
                }

                ObjectId[] actual = DoorSelector.SelectByEffectiveName(scope, block);
                Assert(actual != null, "bài làm trả null");
                var got = new HashSet<ObjectId>(actual);
                var extra = got.Where(id => !expected.Contains(id)).Select(id => Describe(scope.Transaction, id)).ToList();
                Assert(extra.Count == 0, $"chọn nhầm {Show(extra)}: *U… cũng có thể là dynamic block khác. Lọc lại bằng tên thật");
                int missing = expected.Count(id => !got.Contains(id));
                Assert(missing == 0, $"thiếu {missing}/{expected.Count} block {block}" +
                    (anonymous > 0 ? $" ({anonymous} cái đã chỉnh mang tên *U…: filter cần thêm \"`*U*\")" : ""));
                Assert(actual.Length == got.Count, "có ObjectId bị lặp trong kết quả");
                note = $"{expected.Count} block {block}" + (anonymous > 0 ? $", trong đó {anonymous} cái mang tên *U…" : "");
            });
            return note;
        }

        private static string Describe(Transaction tr, ObjectId id)
        {
            var obj = tr.GetObject(id, OpenMode.ForRead);
            return obj is BlockReference br ? $"{br.Name} (tên thật {EffectiveName(tr, br)})" : obj.GetType().Name;
        }

        private static string OnLayersExact(Document doc)
        {
            MethodInfo exact = typeof(SelectionFilterBuilder).GetMethod("OnLayersExact", new[] { typeof(string[]) });
            Assert(exact != null, "SelectionFilterBuilder chưa có OnLayersExact(params string[] layerNames). Đặt đúng tên này để bộ chấm gọi được");

            using (doc.LockDocument())
            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {
                // Không Commit: layer và Line thử bị hủy khi transaction Dispose.
                var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(doc.Database), OpenMode.ForWrite);
                var layers = (LayerTable)tr.GetObject(doc.Database.LayerTableId, OpenMode.ForWrite);
                foreach (var (a, b, _) in WildcardLayers)
                    foreach (string name in new[] { a, b })
                    {
                        var layer = new LayerTableRecord { Name = name };
                        layers.Add(layer);
                        tr.AddNewlyCreatedDBObject(layer, true);
                        var line = new Line(Point3d.Origin, new Point3d(1, 0, 0));
                        line.SetDatabaseDefaults(doc.Database);
                        line.Layer = name;
                        ms.AppendEntity(line);
                        tr.AddNewlyCreatedDBObject(line, true);
                    }

                foreach (var (name, lookAlike, why) in WildcardLayers)
                {
                    var builder = SelectionFilterBuilder.Create();
                    var built = (SelectionFilterBuilder)exact.Invoke(builder, new object[] { new[] { name } });
                    List<string> chosen = SelectLayers(doc.Editor, tr, built.Build());
                    Assert(chosen.Count == 1 && chosen[0] == name,
                        $"OnLayersExact(\"{name}\") chọn layer {Show(chosen)}, cần đúng [{name}] ({why}, không được khớp {lookAlike})");
                }
            }
            return "OnLayersExact khớp đúng nguyên văn " + Show(WildcardLayers.Select(w => w.Exact));
        }

        private static List<string> SelectLayers(Editor ed, Transaction tr, SelectionFilter filter)
        {
            PromptSelectionResult res = ed.SelectAll(filter);
            if (res.Status != PromptStatus.OK) return new List<string>();
            return res.Value.GetObjectIds()
                .Select(id => ((Entity)tr.GetObject(id, OpenMode.ForRead)).Layer)
                .OrderBy(n => n, StringComparer.Ordinal).ToList();
        }
    }
}
