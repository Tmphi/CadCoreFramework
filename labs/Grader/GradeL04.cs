using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using CadCoreFramework.Core;
using CadCoreFramework.Labs.L04;
using static CadCoreFramework.Labs.Grader.Grade;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

[assembly: CommandClass(typeof(CadCoreFramework.Labs.Grader.GradeL04))]

namespace CadCoreFramework.Labs.Grader
{
    /// <summary>
    /// Chấm L04 bằng prompt THẬT: run-lab.ps1 gõ "10,0" rồi "20,0" cho Run, rồi lại "10,0", "20,0" cho RunRaw.
    /// Điểm gõ từ bàn phím được AutoCAD hiểu theo UCS hiện hành, như khi người dùng click.
    /// </summary>
    public class GradeL04
    {
        /// <summary>Hai điểm runner gõ, tính trong UCS. Đổi ở đây thì đổi cả run-lab.ps1.</summary>
        private static readonly Point3d TypedStart = new Point3d(10, 0, 0);
        private static readonly Point3d TypedEnd = new Point3d(20, 0, 0);
        private const double UcsAngleDegrees = 30;

        [CommandMethod("LAB_GRADE_L04")]
        public void Run()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            var context = new CadContext();
            Matrix3d ucs = Matrix3d.Rotation(UcsAngleDegrees * Math.PI / 180, Vector3d.ZAxis, Point3d.Origin);
            Count("L04", 3);

            // Prompt trong lúc đang mở transaction là bẫy B4 (T03): AutoCAD "đơ" khi chờ người dùng.
            bool promptInsideTransaction = false;
            PromptPointOptionsEventHandler onPrompt = (s, e) =>
            {
                if (doc.Database.TransactionManager.TopTransaction != null) promptInsideTransaction = true;
            };

            // In kết quả của Run TRƯỚC khi gọi RunRaw: bài làm hỏi thừa hay thiếu điểm thì vẫn thấy được phần đã chấm.
            ObjectId lineId = ObjectId.Null, rawId = ObjectId.Null;
            System.Exception runError = null, rawError = null;
            ed.CurrentUserCoordinateSystem = ucs;
            try
            {
                ed.PromptingForPoint += onPrompt;
                try { lineId = Line2P.Run(context); }
                catch (System.Exception ex) { runError = ex; }
                finally { ed.PromptingForPoint -= onPrompt; }
                CheckRun(doc, ucs, lineId, runError, promptInsideTransaction);

                try { rawId = Line2P.RunRaw(context); }
                catch (System.Exception ex) { rawError = ex; }
            }
            finally { ed.CurrentUserCoordinateSystem = Matrix3d.Identity; }

            Check("L04.RAW_SHOWS_BUG", () =>
            {
                if (rawError != null) throw rawError;
                var (start, end) = Endpoints(doc.Database, rawId, "RunRaw");
                Assert(start.IsEqualTo(TypedStart) && end.IsEqualTo(TypedEnd),
                    $"bản thô vẽ {Fmt(start)} → {Fmt(end)}, cần đúng toạ độ UCS {Fmt(TypedStart)} → {Fmt(TypedEnd)} dùng như WCS (Editor.GetPoint thô, không đổi hệ)");
                return $"bản thô vẽ {Fmt(start)} → {Fmt(end)}: lệch {UcsAngleDegrees}° so với chỗ click";
            });

            End("L04");
        }

        private static void CheckRun(Document doc, Matrix3d ucs, ObjectId lineId, System.Exception runError, bool promptInsideTransaction)
        {
            Check("L04.UCS", () =>
            {
                if (runError != null) throw runError;
                Point3d wantStart = TypedStart.TransformBy(ucs), wantEnd = TypedEnd.TransformBy(ucs);
                var (start, end) = Endpoints(doc.Database, lineId, "Run");
                Assert(start.IsEqualTo(wantStart) && end.IsEqualTo(wantEnd),
                    $"Line vẽ từ {Fmt(start)} tới {Fmt(end)} (WCS), cần {Fmt(wantStart)} → {Fmt(wantEnd)}: điểm nhập theo UCS phải đổi sang WCS trước khi vẽ");
                return $"UCS xoay {UcsAngleDegrees}°: gõ {Fmt(TypedStart)} → {Fmt(TypedEnd)}, Line WCS {Fmt(start)} → {Fmt(end)}";
            });

            Check("L04.ASK_THEN_DRAW", () =>
            {
                if (runError != null) throw runError;
                Assert(!promptInsideTransaction, "có prompt điểm trong lúc đang mở transaction. Hỏi đủ 2 điểm TRƯỚC, rồi mới gọi context.Execute (T03 B4)");
                return "cả hai prompt đều nằm ngoài transaction";
            });
        }

        private static (Point3d Start, Point3d End) Endpoints(Database db, ObjectId id, string method)
        {
            Assert(!id.IsNull, $"{method} trả ObjectId.Null: không vẽ gì dù đã nhận đủ 2 điểm");
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var line = tr.GetObject(id, OpenMode.ForRead) as Line;
                Assert(line != null, $"{method} trả id của {tr.GetObject(id, OpenMode.ForRead).GetType().Name}, không phải Line");
                var result = (line.StartPoint, line.EndPoint);
                tr.Commit();
                return result;
            }
        }

        private static string Fmt(Point3d p) => FormattableString.Invariant($"({p.X:0.###}, {p.Y:0.###})");
    }
}
