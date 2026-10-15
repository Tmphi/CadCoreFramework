using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using CadCoreFramework.Utils;
using static CadCoreFramework.Labs.Grader.Grade;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

[assembly: CommandClass(typeof(CadCoreFramework.Labs.Grader.GradeL06))]

namespace CadCoreFramework.Labs.Grader
{
    /// <summary>
    /// Chấm L06 (B-09): chạy thẳng <c>XRecordHelper</c> của framework trên branch đang checkout.
    /// Trên v1.0.0 kiểm tra REMOVE_ERASES đỏ: đó là bằng chứng lỗi có thật, trước khi bạn sửa.
    /// </summary>
    public class GradeL06
    {
        private const string Key = "LAB_L06";

        [CommandMethod("LAB_GRADE_L06")]
        public void Run()
        {
            Count("L06", 2);
            Check("L06.SET_GET", () => InRolledBackTransaction((tr, line) =>
            {
                XRecordHelper.SetMetadata(tr, line, Key, new Dictionary<string, object> { ["v"] = 1, ["name"] = "tu-1" });
                var data = XRecordHelper.GetMetadata<Dictionary<string, object>>(tr, line, Key);
                Assert(data != null && (string)data["name"] == "tu-1", "Set rồi Get không đọc lại được dữ liệu");
                return "SetMetadata → GetMetadata đọc lại đúng";
            }));

            Check("L06.REMOVE_ERASES", () => InRolledBackTransaction((tr, line) =>
            {
                XRecordHelper.SetMetadata(tr, line, Key, new Dictionary<string, object> { ["v"] = 1 });
                var dict = (DBDictionary)tr.GetObject(line.ExtensionDictionary, OpenMode.ForRead);
                ObjectId xrecordId = dict.GetAt(Key);
                XRecordHelper.RemoveMetadata(tr, line, Key);
                Assert(!dict.Contains(Key), $"key {Key} vẫn còn trong Extension Dictionary");
                Assert(xrecordId.IsErased, "key đã gỡ khỏi dictionary nhưng Xrecord CHƯA bị Erase: nó thành đối tượng mồ côi trong bản vẽ (B-09)");
                Assert(XRecordHelper.GetMetadata<Dictionary<string, object>>(tr, line, Key) == null, "GetMetadata sau Remove vẫn trả dữ liệu");
                return "Remove gỡ key và Erase Xrecord (IsErased = true)";
            }));
            End("L06");
        }

        /// <summary>Chạy kiểm tra trên một Line mới trong transaction KHÔNG Commit: bản vẽ không đổi.</summary>
        private static string InRolledBackTransaction(System.Func<Transaction, Line, string> body)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            using (doc.LockDocument())
            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {
                var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(doc.Database), OpenMode.ForWrite);
                var line = new Line(Point3d.Origin, new Point3d(10, 0, 0));
                line.SetDatabaseDefaults(doc.Database);
                ms.AppendEntity(line);
                tr.AddNewlyCreatedDBObject(line, true);
                return body(tr, line);
            }
        }
    }
}
