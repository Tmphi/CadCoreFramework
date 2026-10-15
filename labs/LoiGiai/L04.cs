// Lời giải mẫu L04. Làm bài trong ../L04/Line2P.cs trước, rồi mới đọc file này.
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using CadCoreFramework.Core;
using CadCoreFramework.Extensions;
using CadCoreFramework.Utils;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace CadCoreFramework.Labs.L04
{
    public static class Line2P
    {
        public static ObjectId Run(ICadContext context)
        {
            // HỎI trước: CadPromptHelper trả điểm WCS, và tự đổi basePoint WCS → UCS cho đường cao su.
            Point3d? start = CadPromptHelper.GetPoint("Điểm đầu: ");
            if (start == null) return ObjectId.Null;
            Point3d? end = CadPromptHelper.GetPoint("Điểm cuối: ", basePoint: start.Value);
            if (end == null) return ObjectId.Null;

            // VẼ sau: transaction chỉ mở khi đã có đủ dữ liệu.
            // Lambda dạng KHỐI { …; }: viết "scope => id = scope.DrawLine(…)" thì lambda có giá trị trả về,
            // C# chọn overload Execute<T>(Func<…>) đã Obsolete, và overload đó nuốt lỗi vào CadResult (T02 §4.2).
            ObjectId id = ObjectId.Null;
            context.Execute(scope => { id = scope.DrawLine(start.Value, end.Value); });
            return id;
        }

        public static ObjectId RunRaw(ICadContext context)
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            PromptPointResult first = ed.GetPoint("\nĐiểm đầu (thô): ");
            if (first.Status != PromptStatus.OK) return ObjectId.Null;
            var options = new PromptPointOptions("\nĐiểm cuối (thô): ") { UseBasePoint = true, BasePoint = first.Value };
            PromptPointResult second = ed.GetPoint(options);
            if (second.Status != PromptStatus.OK) return ObjectId.Null;

            // Lỗi cố ý để so sánh: Value là toạ độ UCS mà lại dùng như WCS.
            ObjectId id = ObjectId.Null;
            context.Execute(scope => { id = scope.DrawLine(first.Value, second.Value); });
            return id;
        }
    }
}
