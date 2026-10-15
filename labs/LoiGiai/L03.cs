// Lời giải mẫu L03. Làm bài trong ../L03/Experiments.cs trước, rồi mới đọc file này.
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using CadCoreFramework.Core;

namespace CadCoreFramework.Labs.L03
{
    public static class Experiments
    {
        public static void Tn1_NoCommit(Database db)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);
                var line = new Line(Point3d.Origin, new Point3d(50, 50, 0));
                ms.AppendEntity(line);
                tr.AddNewlyCreatedDBObject(line, true);
                // Không tr.Commit(): Dispose (cuối using) hủy mọi thay đổi.
            }
        }

        public static void Tn2_ModifyOpenedForRead(Database db, ObjectId lineId)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var line = (Line)tr.GetObject(lineId, OpenMode.ForRead);
                line.ColorIndex = 1;
                tr.Commit();
            }
        }

        public static void Tn3_UseTransactionAfterDispose(Database db, ObjectId lineId)
        {
            Transaction kept;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                kept = tr;
                tr.Commit();
            }
            kept.GetObject(lineId, OpenMode.ForRead);
        }

        public static void Tn4_WriteWithoutLock(Document doc)
        {
            Database db = doc.Database;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);
                var line = new Line(Point3d.Origin, new Point3d(0, 50, 0));
                ms.AppendEntity(line);
                tr.AddNewlyCreatedDBObject(line, true);
                tr.Commit();
            }
        }

        public static void Tn5_NestedExecute(ICadContext context)
            => context.Execute(outer => context.Execute(inner => { }));

        public static void Tn6_WriteInsideExecuteRead(ICadContext context, ObjectId lineId)
            => context.ExecuteRead(scope =>
            {
                var line = (Line)scope.Transaction.GetObject(lineId, OpenMode.ForWrite);
                line.ColorIndex = 1;
            });
    }
}
