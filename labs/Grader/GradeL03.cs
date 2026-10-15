using System;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using CadCoreFramework.Core;
using CadCoreFramework.Labs.L03;
using static CadCoreFramework.Labs.Grader.Grade;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

[assembly: CommandClass(typeof(CadCoreFramework.Labs.Grader.GradeL03))]

namespace CadCoreFramework.Labs.Grader
{
    /// <summary>
    /// Chấm L03: mỗi thí nghiệm một lệnh, run-lab.ps1 chạy mỗi lệnh trong một phiên accoreconsole riêng.
    /// Bộ chấm không chấm bảng báo cáo; nó kiểm rằng code thí nghiệm TÁI HIỆN ĐÚNG hiện tượng đề bài nói tới,
    /// và in ra điều quan sát được để bạn chép vào bảng.
    /// </summary>
    public class GradeL03
    {
        private static Document Doc => Application.DocumentManager.MdiActiveDocument;

        [CommandMethod("LAB_GRADE_L03_TN1")]
        public void Tn1() => One("L03.TN1", () =>
        {
            Database db = Doc.Database;
            int before = CountModelSpace(db);
            int appendedLines = 0;
            ObjectEventHandler onAppended = (s, e) => { if (e.DBObject is Line) appendedLines++; };
            db.ObjectAppended += onAppended;
            try { Experiments.Tn1_NoCommit(db); }
            finally { db.ObjectAppended -= onAppended; }
            int after = CountModelSpace(db);
            Assert(appendedLines > 0, "chưa thêm Line nào vào database (AppendEntity)");
            Assert(after == before, $"model space có {after} entity, trước đó {before}: Line vẫn còn. Bạn có Commit không?");
            return $"đã AppendEntity {appendedLines} Line; transaction không Commit nên bị hủy khi Dispose: model space vẫn {before} entity, không có lỗi";
        });

        [CommandMethod("LAB_GRADE_L03_TN2")]
        public void Tn2() => One("L03.TN2", () =>
        {
            ObjectId lineId = AddLine(Doc.Database);
            Begin("L03.TN2");
            Experiments.Tn2_ModifyOpenedForRead(Doc.Database, lineId);
            throw new LabFail($"không có lỗi (màu Line giờ là {ColorOf(Doc.Database, lineId)}). Line có thật sự mở OpenMode.ForRead không?");
        });

        [CommandMethod("LAB_GRADE_L03_TN3")]
        public void Tn3() => One("L03.TN3", () =>
        {
            ObjectId lineId = AddLine(Doc.Database);
            Begin("L03.TN3");
            Experiments.Tn3_UseTransactionAfterDispose(Doc.Database, lineId);
            throw new LabFail("không có lỗi. Đề bài: gọi GetObject trên transaction ĐÃ Dispose. " +
                              "(Đọc thuộc tính của một DBObject sau khi transaction đóng có thể 'chạy được': đó là hành vi không xác định, không phải an toàn.)");
        });

        // CommandFlags.Session: lệnh chạy ở application context, AutoCAD KHÔNG tự lock bản vẽ.
        [CommandMethod("LAB_GRADE_L03_TN4", CommandFlags.Session)]
        public void Tn4() => One("L03.TN4", () =>
        {
            try { Experiments.Tn4_WriteWithoutLock(Doc); }
            catch (Autodesk.AutoCAD.Runtime.Exception ex) when (ex.ErrorStatus == ErrorStatus.LockViolation)
            {
                return $"Autodesk.AutoCAD.Runtime.Exception, ErrorStatus = {ex.ErrorStatus} (\"{ex.Message}\"): lệnh Session phải tự LockDocument trước khi ghi";
            }
            throw new LabFail("không có eLockViolation. Bạn có LockDocument không, hay hàm chưa ghi gì vào database?");
        });

        [CommandMethod("LAB_GRADE_L03_TN5")]
        public void Tn5() => One("L03.TN5", () =>
        {
            try { Experiments.Tn5_NestedExecute(new CadContext()); }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Nested CadContext.Execute"))
            {
                return $"InvalidOperationException từ CadContextGuard: \"{ex.Message.Split('\n')[0]}\"";
            }
            throw new LabFail("không có lỗi Nested CadContext.Execute. Lần gọi Execute thứ hai có nằm BÊN TRONG lambda của lần thứ nhất không?");
        });

        [CommandMethod("LAB_GRADE_L03_TN6")]
        public void Tn6() => One("L03.TN6", () =>
        {
            Database db = Doc.Database;
            ObjectId lineId = AddLine(db);
            Experiments.Tn6_WriteInsideExecuteRead(new CadContext(), lineId);
            int color = ColorOf(db, lineId);
            Assert(color == 1, $"màu Line vẫn là {color}: chưa đổi được màu (cần ColorIndex = 1 trong ExecuteRead)");
            return "màu Line đổi thành 1 và ĐÃ được commit: \"read-only\" của ExecuteRead chỉ là hợp đồng (B-07). Trên main, analyzer CAD003 báo lỗi lúc build";
        });

        private static void One(string id, Func<string> body)
        {
            Count("L03", 1);
            Check(id, body);
            End("L03");
        }

        private static ObjectId AddLine(Database db)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);
                var line = new Line(Point3d.Origin, new Point3d(100, 0, 0));
                line.SetDatabaseDefaults(db);
                ObjectId id = ms.AppendEntity(line);
                tr.AddNewlyCreatedDBObject(line, true);
                tr.Commit();
                return id;
            }
        }

        private static int ColorOf(Database db, ObjectId id)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                int color = ((Entity)tr.GetObject(id, OpenMode.ForRead)).ColorIndex;
                tr.Commit();
                return color;
            }
        }

        private static int CountModelSpace(Database db)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                int n = ((BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead)).Cast<ObjectId>().Count();
                tr.Commit();
                return n;
            }
        }
    }
}
