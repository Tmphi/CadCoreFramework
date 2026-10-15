using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using CadCoreFramework.Core;
#if HAS_DEMOS
using CadCoreFramework.Demos;
#endif
using CadCoreFramework.Extensions;
using CadCoreFramework.Models;
using CadCoreFramework.Services;
using CadCoreFramework.Utils;
using static CadCoreFramework.HeadlessLab.LabCommands;

namespace CadCoreFramework.HeadlessLab
{
    /// <summary>
    /// IDataFileService (MiniExcel, CSV, JSON) chạy TRONG tiến trình AutoCAD, và BlockAttributeTable
    /// (DEMO_ATTR_EXPORT / DEMO_ATTR_IMPORT) trên database thật. Chạy trong transaction rollback như Tests.
    /// Dự án tạo không có Demos/ (HAS_DEMOS không được định nghĩa) chỉ còn kiểm thử đầu tiên.
    /// </summary>
    internal static class DataFileLab
    {
        private const string Block = "LAB_ATTR_TABLE";

        internal static readonly List<(string Name, Action<ICadScope> Body)> Tests = new List<(string, Action<ICadScope>)>
        {
            // MiniExcel (net48) chạy được bên trong AutoCAD: ghi rồi đọc lại cùng một bảng ở cả ba định dạng.
            ("DATAFILE_FORMATS_INSIDE_AUTOCAD", scope =>
            {
                var sheet = new DataSheet(new[] { "Tên", "Số", "X" });
                sheet.AddRow("Tủ điện \"MSB\", tầng 1", "001", 12.5);
                var service = new DataFileService(new FileSystemService());
                string dir = Path.Combine(Path.GetTempPath(), "CadCoreLab_" + Path.GetRandomFileName());
                Directory.CreateDirectory(dir);
                try
                {
                    foreach (string ext in new[] { ".xlsx", ".csv", ".json" })
                    {
                        string file = Path.Combine(dir, "bang" + ext);
                        CadResult written = service.WriteTable(file, sheet);
                        Assert(written.IsSuccess, $"{ext}: ghi lỗi: {written.ErrorMessage}");
                        CadResult<DataSheet> read = service.ReadTable(file);
                        Assert(read.IsSuccess, $"{ext}: đọc lỗi: {read.ErrorMessage}");
                        string row = string.Join("|", read.Value.Columns.Select(c => read.Value.GetText(0, c)));
                        Assert(row == "Tủ điện \"MSB\", tầng 1|001|12.5", $"{ext}: đọc lại '{row}'");
                    }
                }
                finally { Directory.Delete(dir, recursive: true); }
            }),
#if HAS_DEMOS
            // Model + Layout1, tag "X" trùng cột cố định → "ATTR:X", block không attribute bị bỏ.
            ("ATTR_EXPORT_MODEL_AND_LAYOUT", scope =>
            {
                ObjectId model = InsertTable(scope, scope.CurrentSpace, new Point3d(10, 20, 0), "MSB-01", "x1");
                ObjectId paper = InsertTable(scope, PaperSpace(scope, "Layout1"), new Point3d(5, 6, 0), "DB-02", "x2");
                scope.CreateBlockDefinition("LAB_NO_ATTR", new Entity[] { new Line(Point3d.Origin, new Point3d(1, 1, 0)) });
                scope.InsertBlock("LAB_NO_ATTR", Point3d.Origin);

                DataSheet sheet = BlockAttributeTable.Export(scope);

                Assert(sheet.Columns.SequenceEqual(new[] { "HANDLE", "BLOCK", "LAYOUT", "X", "Y", "SO", "ATTR:X" }),
                    "Cột: " + string.Join(",", sheet.Columns));
                Assert(sheet.RowCount == 2, $"RowCount = {sheet.RowCount}, kỳ vọng 2");
                int m = Row(sheet, model), p = Row(sheet, paper);
                Assert(sheet.GetText(m, "LAYOUT") == "Model" && sheet.GetText(p, "LAYOUT") == "Layout1",
                    $"LAYOUT = {sheet.GetText(m, "LAYOUT")}, {sheet.GetText(p, "LAYOUT")}");
                Assert(sheet.GetText(m, "SO") == "MSB-01" && sheet.GetText(p, "ATTR:X") == "x2", "Giá trị attribute sai");
                Assert(sheet.GetText(m, "X") == "10" && sheet.GetText(p, "Y") == "6", $"Vị trí: {sheet.GetText(m, "X")}, {sheet.GetText(p, "Y")}");
                Assert(BlockAttributeTable.Export(scope, "lab_attr_table").RowCount == 2, "Lọc theo tên block (không phân biệt hoa thường)");
                Assert(BlockAttributeTable.Export(scope, "KHAC").RowCount == 0, "Lọc theo tên block khác phải ra 0 dòng");
            }),

            // Xuất → file → sửa → đọc lại → nhập, qua cả ba định dạng, ngay trong tiến trình AutoCAD (net48).
            ("ATTR_ROUNDTRIP_XLSX_CSV_JSON", scope =>
            {
                ObjectId a = InsertTable(scope, scope.CurrentSpace, new Point3d(0, 0, 0), "001", "x");
                ObjectId b = InsertTable(scope, scope.CurrentSpace, new Point3d(50, 0, 0), "002", "y");
                var service = new DataFileService(new FileSystemService());
                string dir = Path.Combine(Path.GetTempPath(), "CadCoreLab_" + Path.GetRandomFileName());
                Directory.CreateDirectory(dir);
                try
                {
                    int round = 0;
                    foreach (string ext in new[] { ".xlsx", ".csv", ".json" })
                    {
                        round++;
                        string file = Path.Combine(dir, "bang" + ext);
                        CadResult written = service.WriteTable(file, BlockAttributeTable.Export(scope));
                        Assert(written.IsSuccess, $"{ext}: ghi lỗi: {written.ErrorMessage}");

                        CadResult<DataSheet> read = service.ReadTable(file);
                        Assert(read.IsSuccess, $"{ext}: đọc lỗi: {read.ErrorMessage}");
                        DataSheet sheet = read.Value;
                        Assert(sheet.GetText(Row(sheet, a), "SO") == (round == 1 ? "001" : $"Tủ {round - 1} — sửa"),
                            $"{ext}: đọc lại SO = '{sheet.GetText(Row(sheet, a), "SO")}'");
                        sheet.SetValue(Row(sheet, a), "SO", $"Tủ {round} — sửa");

                        AttributeImportResult result = BlockAttributeTable.Import(scope, sheet);
                        Assert(result.AttributesChanged == 1 && result.BlocksChanged == 1,
                            $"{ext}: đổi {result.AttributesChanged} attribute trên {result.BlocksChanged} block, kỳ vọng 1/1");
                        Assert(Attribute(scope, a, "SO") == $"Tủ {round} — sửa", $"{ext}: attribute = '{Attribute(scope, a, "SO")}'");
                        Assert(Attribute(scope, b, "SO") == "002", $"{ext}: block không sửa bị đổi thành '{Attribute(scope, b, "SO")}'");
                        Assert(result.MissingHandles.Count == 0 && result.LockedHandles.Count == 0 && result.UnknownColumns.Count == 0,
                            $"{ext}: missing {result.MissingHandles.Count}, locked {result.LockedHandles.Count}, unknown [{string.Join(",", result.UnknownColumns)}]");
                    }
                }
                finally { Directory.Delete(dir, recursive: true); }
            }),

            // Handle không có, block trên layer khóa, cột không khớp tag nào, ô rỗng = attribute rỗng.
            ("ATTR_IMPORT_REPORTS_MISSING_LOCKED_UNKNOWN", scope =>
            {
                ObjectId free = InsertTable(scope, scope.CurrentSpace, new Point3d(0, 0, 0), "A", "x");
                ObjectId locked = InsertTable(scope, scope.CurrentSpace, new Point3d(50, 0, 0), "B", "y");
                scope.EnsureLayer("LAB_ATTR_LOCKED");
                var reference = (BlockReference)scope.Transaction.GetObject(locked, OpenMode.ForWrite);
                reference.Layer = "LAB_ATTR_LOCKED";
                var layerTable = (LayerTable)scope.Transaction.GetObject(scope.Database.LayerTableId, OpenMode.ForRead);
                ((LayerTableRecord)scope.Transaction.GetObject(layerTable["LAB_ATTR_LOCKED"], OpenMode.ForWrite)).IsLocked = true;

                var sheet = new DataSheet(new[] { "HANDLE", "SO", "KHONG_CO" });
                sheet.AddRow(Handle(scope, free), null, "z");
                sheet.AddRow(Handle(scope, locked), "B2", "z");
                sheet.AddRow("FFFFFFF", "C", "z");
                sheet.AddRow("không phải hex", "D", "z");

                AttributeImportResult result = BlockAttributeTable.Import(scope, sheet);

                Assert(Attribute(scope, free, "SO") == "", $"Ô rỗng phải làm attribute rỗng, đang là '{Attribute(scope, free, "SO")}'");
                Assert(Attribute(scope, locked, "SO") == "B", "Block trên layer khóa bị sửa");
                Assert(result.LockedHandles.SequenceEqual(new[] { Handle(scope, locked) }), "LockedHandles: " + string.Join(",", result.LockedHandles));
                Assert(result.MissingHandles.SequenceEqual(new[] { "FFFFFFF", "không phải hex" }), "MissingHandles: " + string.Join(",", result.MissingHandles));
                Assert(result.UnknownColumns.SequenceEqual(new[] { "KHONG_CO" }), "UnknownColumns: " + string.Join(",", result.UnknownColumns));
                Assert(result.AttributesChanged == 1 && result.BlocksChanged == 1, $"Đổi {result.AttributesChanged}/{result.BlocksChanged}, kỳ vọng 1/1");

                bool threw = false;
                try { BlockAttributeTable.Import(scope, new DataSheet(new[] { "SO" })); }
                catch (ArgumentException) { threw = true; }
                Assert(threw, "Bảng không có cột HANDLE phải bị từ chối");
            }),
#endif
        };

        /// <summary>Block có hai attribute: "SO" và "X" (trùng tên cột cố định X).</summary>
        private static ObjectId InsertTable(ICadScope scope, BlockTableRecord space, Point3d position, string so, string x)
        {
            if (!scope.HasBlock(Block))
            {
                var defs = new Entity[]
                {
                    new Line(new Point3d(-5, -5, 0), new Point3d(5, 5, 0)),
                    new AttributeDefinition(new Point3d(0, 0, 0), "", "SO", "Số", ObjectId.Null) { Height = 2.5 },
                    new AttributeDefinition(new Point3d(0, -4, 0), "", "X", "X", ObjectId.Null) { Height = 2.5 },
                };
                foreach (Entity e in defs) e.SetDatabaseDefaults(scope.Database);
                scope.CreateBlockDefinition(Block, defs);
            }
            ObjectId id = BlockHelper.InsertBlock(scope.Transaction, space, Block, position);
            var reference = (BlockReference)scope.Transaction.GetObject(id, OpenMode.ForRead);
            BlockHelper.SetBlockAttribute(scope.Transaction, reference, "SO", so);
            BlockHelper.SetBlockAttribute(scope.Transaction, reference, "X", x);
            return id;
        }

        private static BlockTableRecord PaperSpace(ICadScope scope, string layoutName)
        {
            var layouts = (DBDictionary)scope.Transaction.GetObject(scope.Database.LayoutDictionaryId, OpenMode.ForRead);
            var layout = (Layout)scope.Transaction.GetObject(layouts.GetAt(layoutName), OpenMode.ForRead);
            return (BlockTableRecord)scope.Transaction.GetObject(layout.BlockTableRecordId, OpenMode.ForRead);
        }

        private static string Handle(ICadScope scope, ObjectId id) => scope.Transaction.GetObject(id, OpenMode.ForRead).Handle.ToString();

        private static int Row(DataSheet sheet, ObjectId id)
        {
            string handle = id.Handle.ToString();
            for (int r = 0; r < sheet.RowCount; r++)
                if (sheet.GetText(r, "HANDLE") == handle) return r;
            throw new LabAssertException($"Không có dòng HANDLE = {handle}");
        }

        private static string Attribute(ICadScope scope, ObjectId id, string tag)
            => BlockHelper.GetBlockAttributes(scope.Transaction, (BlockReference)scope.Transaction.GetObject(id, OpenMode.ForRead))[tag];
    }
}
