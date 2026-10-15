using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using CadCoreFramework.Core;
using CadCoreFramework.Labs.L02;
using static CadCoreFramework.Labs.Grader.Grade;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

[assembly: CommandClass(typeof(CadCoreFramework.Labs.Grader.GradeL02))]

namespace CadCoreFramework.Labs.Grader
{
    /// <summary>Chấm L02 trên bản vẽ mẫu (LAB_SAMPLE), qua CadContext.ExecuteRead THẬT.</summary>
    public class GradeL02
    {
        [CommandMethod("LAB_GRADE_L02")]
        public void Run()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            var context = new CadContext();
            EnsureSample(db);

            Count("L02", 5);

            Check("L02.LAYERS", () =>
            {
                List<LayerInfo> actual = null;
                var expected = new List<LayerInfo>();
                context.ExecuteRead(scope =>
                {
                    actual = TrainDump.ListLayers(scope);
                    var table = (LayerTable)scope.Transaction.GetObject(db.LayerTableId, OpenMode.ForRead);
                    foreach (ObjectId id in table)
                    {
                        var layer = (LayerTableRecord)scope.Transaction.GetObject(id, OpenMode.ForRead);
                        expected.Add(new LayerInfo(layer.Name, layer.IsOff, layer.Color.ColorIndex));
                    }
                });
                Assert(actual != null, "bài làm trả null");
                var missing = expected.Select(l => l.Name).Except(actual.Select(l => l.Name)).ToList();
                Assert(missing.Count == 0, $"thiếu layer {Show(missing)}");
                Assert(actual.Count == expected.Count, $"có {actual.Count} dòng cho {expected.Count} layer: tên nào bị lặp?");
                foreach (LayerInfo want in expected)
                {
                    LayerInfo got = actual.First(l => l.Name == want.Name);
                    Assert(got.IsOff == want.IsOff, $"layer {want.Name}: IsOff phải là {want.IsOff}");
                    Assert(got.ColorIndex == want.ColorIndex, $"layer {want.Name}: màu phải là {want.ColorIndex}, nhận {got.ColorIndex}");
                }
                return $"{expected.Count} layer, có {expected.Count(l => l.IsOff)} layer tắt";
            });

            Check("L02.DXF_NAME", () =>
            {
                Dictionary<string, int> actual = null, expected = null;
                List<string> opened = null;
                context.ExecuteRead(scope =>
                {
                    actual = TrainDump.CountByDxfName(scope);
                    opened = OpenedEntities(scope.Transaction);
                    expected = CountByDxf(scope.CurrentSpace);
                });
                Assert(opened.Count == 0,
                    $"bước 2 đã mở {opened.Count} entity ({Show(opened.Distinct())}). Dùng id.ObjectClass.DxfName, không cần GetObject");
                string diff = Diff(expected, actual);
                Assert(diff == null, diff);
                return Show(expected) + ", không mở entity nào";
            });

            Check("L02.EFFECTIVE_NAME", () =>
            {
                Dictionary<string, int> actual = null;
                var expected = new Dictionary<string, int>(StringComparer.Ordinal);
                bool hasDynamic = false;
                context.ExecuteRead(scope =>
                {
                    actual = TrainDump.CountByEffectiveName(scope);
                    hasDynamic = SampleDrawing.HasDynamicBlocks(scope.Transaction, db);
                    foreach (ObjectId id in scope.CurrentSpace)
                    {
                        if (id.ObjectClass.DxfName != "INSERT") continue;
                        string name = EffectiveName(scope.Transaction, (BlockReference)scope.Transaction.GetObject(id, OpenMode.ForRead));
                        expected[name] = expected.TryGetValue(name, out int n) ? n + 1 : 1;
                    }
                });
                if (actual != null && actual.Keys.Any(k => k.StartsWith("*U", StringComparison.OrdinalIgnoreCase)))
                    throw new LabFail($"có khóa *U… trong {Show(actual)}: đó là tên ẩn danh của dynamic block đã chỉnh. Lấy tên thật bằng DynamicBlockHelper.GetEffectiveName");
                string diff = Diff(expected, actual);
                Assert(diff == null, diff);
                if (!hasDynamic) throw new LabSkip($"đúng {Show(expected)}, nhưng bản vẽ mẫu không có dynamic block (máy không có bộ mẫu Autodesk) nên chưa kiểm được *U…");
                return Show(expected);
            });

            Check("L02.READ_ONLY", () =>
            {
                List<string> writable = null;
                context.ExecuteRead(scope =>
                {
                    TrainDump.ListLayers(scope);
                    TrainDump.CountByDxfName(scope);
                    TrainDump.CountByEffectiveName(scope);
                    writable = scope.Transaction.GetAllObjects().Cast<DBObject>()
                        .Where(o => o.IsWriteEnabled).Select(o => o.GetType().Name).ToList();
                });
                Assert(writable.Count == 0, $"có đối tượng mở ForWrite: {Show(writable)}. TRAIN_DUMP chỉ đọc");
                return "cả ba bước không mở gì ForWrite";
            });

            // Câu phản tư của L02: ở Layout, CurrentSpace là paper space của layout đó.
            Check("L02.CURRENT_SPACE", () =>
            {
                Dictionary<string, int> actual = null, expected = null;
                string before = LayoutManager.Current.CurrentLayout;
                try
                {
                    LayoutManager.Current.CurrentLayout = "Layout1";
                    context.ExecuteRead(scope =>
                    {
                        actual = TrainDump.CountByDxfName(scope);
                        expected = CountByDxf(scope.CurrentSpace);
                    });
                }
                finally { LayoutManager.Current.CurrentLayout = before; }
                string diff = Diff(expected, actual);
                Assert(diff == null, $"ở Layout1: {diff}. Đếm trong scope.CurrentSpace, không phải ModelSpace");
                return "Layout1: " + Show(expected);
            });

            End("L02");
        }

        internal static void EnsureSample(Database db)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead);
                bool empty = !ms.Cast<ObjectId>().Any();
                tr.Commit();
                if (!empty) return;
            }
            Editor.WriteMessage($"\nLAB_SAMPLE: {SampleDrawing.Build(db)}\n");
        }

        private static List<string> OpenedEntities(Transaction tr)
            => tr.GetAllObjects().Cast<DBObject>().OfType<Entity>().Select(e => e.GetType().Name).ToList();
    }
}
