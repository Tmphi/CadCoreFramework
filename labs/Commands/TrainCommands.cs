using System;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using CadCoreFramework.Core;
using CadCoreFramework.Labs.L02;
using CadCoreFramework.Labs.L04;
using CadCoreFramework.Labs.L05;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

[assembly: CommandClass(typeof(CadCoreFramework.Labs.TrainCommands))]

namespace CadCoreFramework.Labs
{
    /// <summary>
    /// Lệnh để học viên tự chạy bài làm trong AutoCAD (NETLOAD <c>CadCoreFramework.Labs.dll</c>).
    /// Chỉ gọi hàm trong L02/, L04/, L05/ rồi in kết quả; logic nằm hết trong bài làm.
    /// </summary>
    public class TrainCommands
    {
        private static readonly ICadContext Context = new CadContext();

        [CommandMethod("TRAIN_DUMP")]
        public void Dump() => Guarded(ed => Context.ExecuteRead(scope =>
        {
            ed.WriteMessage("\n— Layer —");
            foreach (LayerInfo layer in TrainDump.ListLayers(scope))
                ed.WriteMessage($"\n  {layer.Name}: {(layer.IsOff ? "tắt" : "bật")}, màu {layer.ColorIndex}");
            ed.WriteMessage($"\n— Entity trong {(scope.Database.TileMode ? "Model" : "Layout")} theo DxfName —");
            foreach (var pair in TrainDump.CountByDxfName(scope).OrderBy(p => p.Key, StringComparer.Ordinal))
                ed.WriteMessage($"\n  {pair.Key}: {pair.Value}");
            ed.WriteMessage("\n— Block reference theo tên thật —");
            foreach (var pair in TrainDump.CountByEffectiveName(scope).OrderBy(p => p.Key, StringComparer.Ordinal))
                ed.WriteMessage($"\n  {pair.Key}: {pair.Value}");
            ed.WriteMessage("\n");
        }));

        [CommandMethod("TRAIN_LINE2P")]
        public void TrainLine2P() => Guarded(ed => Report(ed, Line2P.Run(Context)));

        [CommandMethod("TRAIN_LINE2P_RAW")]
        public void TrainLine2PRaw() => Guarded(ed => Report(ed, Line2P.RunRaw(Context)));

        [CommandMethod("TRAIN_DOORS")]
        public void Doors() => Guarded(ed => Context.ExecuteRead(scope =>
        {
            ObjectId[] ids = DoorSelector.SelectByEffectiveName(scope, SampleDrawing.DoorBlock);
            ed.WriteMessage($"\nTRAIN_DOORS: {ids.Length} block {SampleDrawing.DoorBlock}");
            foreach (ObjectId id in ids)
            {
                var br = (BlockReference)scope.Transaction.GetObject(id, OpenMode.ForRead);
                ed.WriteMessage($"\n  handle {id.Handle}: tên block {br.Name}, tại {br.Position}");
            }
            ed.WriteMessage("\n");
        }));

        private static void Report(Editor ed, ObjectId lineId)
        {
            if (lineId.IsNull) { ed.WriteMessage("\nĐã hủy, không vẽ gì.\n"); return; }
            Context.ExecuteRead(scope =>
            {
                var line = (Line)scope.Transaction.GetObject(lineId, OpenMode.ForRead);
                ed.WriteMessage($"\nLine WCS: {line.StartPoint} → {line.EndPoint}\n");
            });
        }

        /// <summary>Bài làm chưa xong (NotImplementedException) hay lỗi khác: in ra, không làm lệnh đổ vỡ.</summary>
        private static void Guarded(Action<Editor> body)
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            try { body(ed); }
            catch (NotImplementedException) { ed.WriteMessage("\nBài làm chưa xong: còn throw NotImplementedException.\n"); }
            catch (System.Exception ex) { ed.WriteMessage($"\n{ex.GetType().Name}: {ex.Message}\n"); }
        }
    }
}
