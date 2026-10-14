using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using CadCoreFramework.Core;

namespace CadCoreFramework.Extensions
{
    /// <summary>
    /// Extension methods cho ICadScope: tương tác nhập liệu từ người dùng và truy vấn dữ liệu.
    /// </summary>
    public static class CadScopeQueryExtensions
    {
        #region Data Queries

        /// <summary>
        /// Lấy danh sách các đối tượng thuộc kiểu T từ không gian hiện hành, có thể lọc theo Layer.
        /// </summary>
        public static System.Collections.Generic.List<T> GetEntities<T>(this ICadScope scope, string layer = null) where T : Entity
        {
            // Cache RXClass outside the loop to avoid repeated reflection lookups
            var rxClass = Autodesk.AutoCAD.Runtime.RXObject.GetClass(typeof(T));
            var btr = (BlockTableRecord)scope.Transaction.GetObject(scope.CurrentSpace.Id, OpenMode.ForRead);
            var result = new System.Collections.Generic.List<T>();
            foreach (ObjectId id in btr)
            {
                if (!id.ObjectClass.IsDerivedFrom(rxClass)) continue;
                var entity = scope.Transaction.GetObject(id, OpenMode.ForRead) as T;
                if (entity != null &&
                    (layer == null || string.Equals(entity.Layer, layer, StringComparison.OrdinalIgnoreCase)))
                {
                    result.Add(entity);
                }
            }
            return result;
        }

        #endregion

        #region User Input (Prompt helpers bridged to scope)

        /// <summary>
        /// Yêu cầu người dùng chọn một điểm trên màn hình. Trả về null nếu người dùng hủy lệnh.
        /// AN TOÀN khi gọi từ command context (bên trong transaction).
        /// KHÔNG GỌI từ Palette modeless (application context) mà không chuyển sang command context
        /// trước bằng ExecuteInCommandContextAsync hoặc SendStringToExecute.
        /// </summary>
        [Obsolete("Prompt bên trong scope giữ Transaction + Lock trong lúc chờ người dùng. Dùng CadPromptHelper.GetPoint() TRƯỚC khi gọi ICadContext.Execute (xem docs/09_GOLDEN_SAMPLE.md).")]
        public static Point3d? AskPoint(this ICadScope scope, string message, Point3d? basePoint = null)
        {
            var opts = new Autodesk.AutoCAD.EditorInput.PromptPointOptions($"\n{message}");
            if (basePoint.HasValue)
            {
                opts.UseBasePoint = true;
                opts.BasePoint = basePoint.Value;
            }
            var res = scope.Editor.GetPoint(opts);
            return res.Status == Autodesk.AutoCAD.EditorInput.PromptStatus.OK ? res.Value : (Point3d?)null;
        }

        /// <summary>
        /// Yêu cầu người dùng nhập một chuỗi văn bản. Trả về null nếu người dùng hủy lệnh.
        /// </summary>
        [Obsolete("Prompt bên trong scope giữ Transaction + Lock trong lúc chờ người dùng. Dùng CadPromptHelper.GetString() TRƯỚC khi gọi ICadContext.Execute (xem docs/09_GOLDEN_SAMPLE.md).")]
        public static string AskString(this ICadScope scope, string message, string defaultValue = null)
        {
            var opts = new Autodesk.AutoCAD.EditorInput.PromptStringOptions($"\n{message}") { AllowSpaces = true };
            if (!string.IsNullOrEmpty(defaultValue)) { opts.DefaultValue = defaultValue; opts.UseDefaultValue = true; }
            var res = scope.Editor.GetString(opts);
            return res.Status == Autodesk.AutoCAD.EditorInput.PromptStatus.OK ? res.StringResult : null;
        }

        /// <summary>
        /// Yêu cầu người dùng chọn một từ khóa trong danh sách cho trước. Trả về null nếu người dùng hủy lệnh.
        /// </summary>
        [Obsolete("Prompt bên trong scope giữ Transaction + Lock trong lúc chờ người dùng. Dùng CadPromptHelper.GetKeywords() TRƯỚC khi gọi ICadContext.Execute (xem docs/09_GOLDEN_SAMPLE.md).")]
        public static string AskKeyword(this ICadScope scope, string message, string[] keywords, string defaultKeyword = null)
        {
            if (keywords == null || keywords.Length == 0) return null;
            // Chỉ phần chữ: AutoCAD tự nối "[Yes/No] <Yes>:" (AppendKeywordsToMessage mặc định true) — B-33.
            var opts = new Autodesk.AutoCAD.EditorInput.PromptKeywordOptions($"\n{message}");
            foreach (var kw in keywords) opts.Keywords.Add(kw);
            if (!string.IsNullOrEmpty(defaultKeyword)) opts.Keywords.Default = defaultKeyword;
            var res = scope.Editor.GetKeywords(opts);
            return res.Status == Autodesk.AutoCAD.EditorInput.PromptStatus.OK ? res.StringResult : null;
        }

        /// <summary>
        /// Yêu cầu người dùng nhập một số thực. Trả về null nếu người dùng hủy lệnh.
        /// </summary>
        [Obsolete("Prompt bên trong scope giữ Transaction + Lock trong lúc chờ người dùng. Dùng CadPromptHelper.GetDouble() TRƯỚC khi gọi ICadContext.Execute (xem docs/09_GOLDEN_SAMPLE.md).")]
        public static double? AskDouble(this ICadScope scope, string message, double? defaultValue = null)
        {
            var opts = new Autodesk.AutoCAD.EditorInput.PromptDoubleOptions($"\n{message}");
            if (defaultValue.HasValue) { opts.DefaultValue = defaultValue.Value; opts.UseDefaultValue = true; }
            var res = scope.Editor.GetDouble(opts);
            return res.Status == Autodesk.AutoCAD.EditorInput.PromptStatus.OK ? res.Value : (double?)null;
        }

        /// <summary>
        /// Yêu cầu người dùng chọn một đối tượng (Entity). Trả về ObjectId.Null nếu người dùng hủy lệnh.
        /// </summary>
        [Obsolete("Prompt bên trong scope giữ Transaction + Lock trong lúc chờ người dùng. Dùng CadPromptHelper.GetEntity() TRƯỚC khi gọi ICadContext.Execute (xem docs/09_GOLDEN_SAMPLE.md).")]
        public static ObjectId AskEntity(this ICadScope scope, string message)
        {
            var opts = new Autodesk.AutoCAD.EditorInput.PromptEntityOptions($"\n{message}");
            var res = scope.Editor.GetEntity(opts);
            return res.Status == Autodesk.AutoCAD.EditorInput.PromptStatus.OK ? res.ObjectId : ObjectId.Null;
        }

        /// <summary>
        /// Yêu cầu người dùng đo hoặc nhập khoảng cách (có hiển thị đường dóng cao su kéo dài từ basePoint).
        /// </summary>
        [Obsolete("Prompt bên trong scope giữ Transaction + Lock trong lúc chờ người dùng. Dùng CadPromptHelper.GetDistance() TRƯỚC khi gọi ICadContext.Execute (xem docs/09_GOLDEN_SAMPLE.md).")]
        public static double? AskDistance(this ICadScope scope, string message, Point3d? basePoint = null)
        {
            var opts = new Autodesk.AutoCAD.EditorInput.PromptDistanceOptions($"\n{message}");
            if (basePoint.HasValue)
            {
                opts.UseBasePoint = true;
                opts.BasePoint = basePoint.Value;
            }
            var res = scope.Editor.GetDistance(opts);
            return res.Status == Autodesk.AutoCAD.EditorInput.PromptStatus.OK ? res.Value : (double?)null;
        }

        /// <summary>
        /// Yêu cầu người dùng nhập hoặc trỏ góc (Radian) với đường dóng cao su từ basePoint.
        /// </summary>
        [Obsolete("Prompt bên trong scope giữ Transaction + Lock trong lúc chờ người dùng. Dùng CadPromptHelper.GetAngle() TRƯỚC khi gọi ICadContext.Execute (xem docs/09_GOLDEN_SAMPLE.md).")]
        public static double? AskAngle(this ICadScope scope, string message, Point3d? basePoint = null)
        {
            var opts = new Autodesk.AutoCAD.EditorInput.PromptAngleOptions($"\n{message}");
            if (basePoint.HasValue)
            {
                opts.UseBasePoint = true;
                opts.BasePoint = basePoint.Value;
            }
            var res = scope.Editor.GetAngle(opts);
            return res.Status == Autodesk.AutoCAD.EditorInput.PromptStatus.OK ? res.Value : (double?)null;
        }

        /// <summary>
        /// Yêu cầu người dùng chọn điểm góc thứ hai (hiển thị khung chữ nhật cao su kéo dài từ basePoint).
        /// Rất tiện lợi cho các lệnh vẽ hình chữ nhật hoặc khoanh vùng tương tác.
        /// </summary>
        [Obsolete("Prompt bên trong scope giữ Transaction + Lock trong lúc chờ người dùng. Dùng CadPromptHelper.GetCorner() TRƯỚC khi gọi ICadContext.Execute (xem docs/09_GOLDEN_SAMPLE.md).")]
        public static Point3d? AskCorner(this ICadScope scope, string message, Point3d basePoint)
        {
            var opts = new Autodesk.AutoCAD.EditorInput.PromptCornerOptions($"\n{message}", basePoint);
            var res = scope.Editor.GetCorner(opts);
            return res.Status == Autodesk.AutoCAD.EditorInput.PromptStatus.OK ? res.Value : (Point3d?)null;
        }

        #endregion
    }
}
