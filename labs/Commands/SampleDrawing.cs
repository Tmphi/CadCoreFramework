using System;
using System.IO;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

[assembly: CommandClass(typeof(CadCoreFramework.Labs.SampleDrawing))]

namespace CadCoreFramework.Labs
{
    /// <summary>
    /// Bản vẽ mẫu cho L02 và L05: lệnh <c>LAB_SAMPLE</c> vẽ vào bản vẽ đang mở (phải trống).
    /// Bộ chấm dựng bản vẽ này trước khi chấm; học viên chạy lệnh trong AutoCAD để có bản vẽ thử tay.
    /// <para>
    /// Dynamic block DOOR và WINDOW được chép từ bộ mẫu Autodesk cài cùng AutoCAD
    /// (<c>Sample\&lt;ngôn ngữ&gt;\Dynamic Blocks\Architectural - Metric.dwg</c>), vì accoreconsole
    /// không có Block Editor để tự dựng. Bản vẽ mẫu vì vậy không nằm trong repo; mỗi máy tự dựng.
    /// </para>
    /// </summary>
    public class SampleDrawing
    {
        public const string ColumnBlock = "LAB_COT";
        public const string DoorBlock = "DOOR";
        public const string WindowBlock = "WINDOW";
        private const string AutodeskSampleFile = "Architectural - Metric.dwg";

        /// <summary>Tên block trong file mẫu Autodesk → tên trong bản vẽ lab.</summary>
        private static readonly (string Source, string Target)[] DynamicBlocks =
        {
            ("Door - Metric", DoorBlock),
            ("Window - Metric", WindowBlock),
        };

        [CommandMethod("LAB_SAMPLE")]
        public void BuildCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            try
            {
                string summary = Build(doc.Database);
                doc.Editor.WriteMessage($"\nLAB_SAMPLE: {summary}\n");
            }
            catch (System.Exception ex)
            {
                doc.Editor.WriteMessage($"\nLAB_SAMPLE thất bại: {ex.Message}\n");
            }
        }

        /// <summary>Đường dẫn file dynamic block mẫu của Autodesk, hoặc null nếu máy không cài.</summary>
        public static string FindAutodeskSample()
        {
            string installDir = Path.GetDirectoryName(typeof(Database).Assembly.Location);
            string sampleDir = Path.Combine(installDir ?? "", "Sample");
            if (!Directory.Exists(sampleDir)) return null;
            return Directory.GetFiles(sampleDir, AutodeskSampleFile, SearchOption.AllDirectories)
                .FirstOrDefault(p => p.IndexOf("Dynamic Blocks", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>Có dynamic block trong bản vẽ không (false khi máy không có bộ mẫu Autodesk).</summary>
        public static bool HasDynamicBlocks(Transaction tr, Database db)
            => ((BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead)).Has(DoorBlock);

        public static string Build(Database db)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);
                if (ms.Cast<ObjectId>().Any())
                    throw new InvalidOperationException("cần một bản vẽ TRỐNG (QNEW rồi chạy lại).");

                AddLayer(tr, db, "LAB_TUONG", 1, isOff: false);
                AddLayer(tr, db, "LAB_CUA", 3, isOff: false);
                AddLayer(tr, db, "LAB_GHICHU", 2, isOff: true);
                AddLayer(tr, db, "LAB_THIETBI", 5, isOff: false);

                // Tường phòng 6000 × 4000: 4 LINE, cộng một vách ngăn LWPOLYLINE.
                var corners = new[] { new Point3d(0, 0, 0), new Point3d(6000, 0, 0), new Point3d(6000, 4000, 0), new Point3d(0, 4000, 0) };
                for (int i = 0; i < 4; i++)
                    Append(tr, ms, new Line(corners[i], corners[(i + 1) % 4]), "LAB_TUONG");
                var wall = new Polyline();
                wall.AddVertexAt(0, new Point2d(3000, 0), 0, 0, 0);
                wall.AddVertexAt(1, new Point2d(3000, 2500), 0, 0, 0);
                Append(tr, ms, wall, "LAB_TUONG");

                // Thiết bị: 3 CIRCLE. Ghi chú: 2 TEXT + 1 MTEXT trên layer đang TẮT.
                foreach (double x in new[] { 1000.0, 2000.0, 4500.0 })
                    Append(tr, ms, new Circle(new Point3d(x, 3000, 0), Vector3d.ZAxis, 150), "LAB_THIETBI");
                Append(tr, ms, new DBText { Position = new Point3d(500, 500, 0), Height = 200, TextString = "PHONG 1" }, "LAB_GHICHU");
                Append(tr, ms, new DBText { Position = new Point3d(3500, 500, 0), Height = 200, TextString = "PHONG 2" }, "LAB_GHICHU");
                Append(tr, ms, new MText { Location = new Point3d(500, 3800, 0), TextHeight = 150, Contents = "Ban ve mau L02/L05" }, "LAB_GHICHU");

                // Block thường: cột 300 × 300, chèn 2 lần.
                ObjectId columnId = AddColumnBlock(tr, db);
                foreach (var p in new[] { new Point3d(0, 0, 0), new Point3d(6000, 4000, 0) })
                    Append(tr, ms, new BlockReference(p, columnId), "LAB_TUONG");

                string dynamicNote = AddDynamicBlocks(tr, db, ms);

                // Paper space của Layout1: 1 LINE + 1 MTEXT (L02, câu phản tư về Layout).
                var layouts = (DBDictionary)tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead);
                var layout = (Layout)tr.GetObject(layouts.GetAt("Layout1"), OpenMode.ForRead);
                var ps = (BlockTableRecord)tr.GetObject(layout.BlockTableRecordId, OpenMode.ForWrite);
                Append(tr, ps, new Line(new Point3d(10, 10, 0), new Point3d(287, 10, 0)), "0");
                Append(tr, ps, new MText { Location = new Point3d(10, 30, 0), TextHeight = 5, Contents = "Layout1" }, "0");

                tr.Commit();
                return "đã vẽ 4 LINE, 1 LWPOLYLINE, 3 CIRCLE, 2 TEXT, 1 MTEXT, 2 " + ColumnBlock + "; " + dynamicNote
                       + "; Layout1: 1 LINE, 1 MTEXT.";
            }
        }

        /// <summary>
        /// DOOR: 3 cái (1 giữ nguyên, 2 đổi thuộc tính động → AutoCAD tạo block ẩn danh *U…).
        /// WINDOW: 2 cái, đều đã đổi thuộc tính → cũng mang tên *U….
        /// </summary>
        private static string AddDynamicBlocks(Transaction tr, Database db, BlockTableRecord ms)
        {
            string source = FindAutodeskSample();
            if (source == null)
                return "KHÔNG có DOOR/WINDOW (máy không có bộ dynamic block mẫu của Autodesk, " +
                       @"Sample\<ngôn ngữ>\Dynamic Blocks\" + AutodeskSampleFile + ")";

            var ids = new ObjectIdCollection();
            using (var sourceDb = new Database(false, true))
            {
                sourceDb.ReadDwgFile(source, FileOpenMode.OpenForReadAndAllShare, true, "");
                using (Transaction str = sourceDb.TransactionManager.StartTransaction())
                {
                    var sbt = (BlockTable)str.GetObject(sourceDb.BlockTableId, OpenMode.ForRead);
                    foreach (var (src, _) in DynamicBlocks)
                    {
                        if (!sbt.Has(src)) return $"KHÔNG có DOOR/WINDOW (file mẫu {source} không có block '{src}')";
                        ids.Add(sbt[src]);
                    }
                    str.Commit();
                }
                var mapping = new IdMapping();
                sourceDb.WblockCloneObjects(ids, db.BlockTableId, mapping, DuplicateRecordCloning.Ignore, false);

                for (int i = 0; i < DynamicBlocks.Length; i++)
                {
                    var btr = (BlockTableRecord)tr.GetObject(mapping[ids[i]].Value, OpenMode.ForWrite);
                    btr.Name = DynamicBlocks[i].Target;
                }
            }

            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            InsertDynamic(tr, ms, bt[DoorBlock], new Point3d(1000, 0, 0), changes: 0);
            InsertDynamic(tr, ms, bt[DoorBlock], new Point3d(3000, 1200, 0), changes: 1);
            InsertDynamic(tr, ms, bt[DoorBlock], new Point3d(4500, 0, 0), changes: 2);
            InsertDynamic(tr, ms, bt[WindowBlock], new Point3d(1500, 4000, 0), changes: 1);
            InsertDynamic(tr, ms, bt[WindowBlock], new Point3d(4500, 4000, 0), changes: 2);
            return "3 " + DoorBlock + " (2 cái đã đổi thuộc tính động), 2 " + WindowBlock + " (đều đã đổi)";
        }

        /// <summary>Chèn block động, rồi đổi <paramref name="changes"/> thuộc tính đầu tiên sang giá trị được phép khác.</summary>
        private static void InsertDynamic(Transaction tr, BlockTableRecord ms, ObjectId blockId, Point3d position, int changes)
        {
            var br = new BlockReference(position, blockId);
            Append(tr, ms, br, "LAB_CUA");
            int changed = 0;
            foreach (DynamicBlockReferenceProperty prop in br.DynamicBlockReferencePropertyCollection)
            {
                if (changed == changes) break;
                if (prop.ReadOnly) continue;
                object other = prop.GetAllowedValues().FirstOrDefault(v => !Equals(v, prop.Value));
                if (other == null) continue;
                prop.Value = other;
                changed++;
            }
            if (changed != changes)
                throw new InvalidOperationException($"chỉ đổi được {changed}/{changes} thuộc tính động của block chèn tại {position}.");
        }

        private static ObjectId AddColumnBlock(Transaction tr, Database db)
        {
            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
            var btr = new BlockTableRecord { Name = ColumnBlock, Origin = Point3d.Origin };
            ObjectId id = bt.Add(btr);
            tr.AddNewlyCreatedDBObject(btr, true);
            var square = new Polyline();
            square.AddVertexAt(0, new Point2d(-150, -150), 0, 0, 0);
            square.AddVertexAt(1, new Point2d(150, -150), 0, 0, 0);
            square.AddVertexAt(2, new Point2d(150, 150), 0, 0, 0);
            square.AddVertexAt(3, new Point2d(-150, 150), 0, 0, 0);
            square.Closed = true;
            btr.AppendEntity(square);
            tr.AddNewlyCreatedDBObject(square, true);
            return id;
        }

        private static void AddLayer(Transaction tr, Database db, string name, short colorIndex, bool isOff)
        {
            var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForWrite);
            var layer = new LayerTableRecord
            {
                Name = name,
                Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, colorIndex),
                IsOff = isOff,
            };
            lt.Add(layer);
            tr.AddNewlyCreatedDBObject(layer, true);
        }

        private static void Append(Transaction tr, BlockTableRecord space, Entity entity, string layer)
        {
            entity.SetDatabaseDefaults(space.Database);
            entity.Layer = layer;
            space.AppendEntity(entity);
            tr.AddNewlyCreatedDBObject(entity, true);
        }
    }
}
