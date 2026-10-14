using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
// Core.Application (AcCoreMgd), không phải Application của AcMgd (UI): helper lõi không được kéo theo AcMgd
// — nhờ vậy nó chạy được cả trong accoreconsole (tests/integration/HeadlessLab).
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace CadCoreFramework.Utils
{
    /// <summary>
    /// Lớp tiện ích toàn diện hỗ trợ yêu cầu người dùng nhập liệu từ AutoCAD Command Line / Editor (Điểm, Góc, Chuỗi, Từ khóa, Số).
    /// </summary>
    public static class CadPromptHelper
    {
        private static Editor Editor => Application.DocumentManager.MdiActiveDocument?.Editor;

        /// <summary>
        /// Yêu cầu người dùng chọn một thực thể duy nhất, có thể giới hạn theo Type chỉ định.
        /// </summary>
        public static ObjectId GetEntity(string message, Type allowedType = null, string rejectMessage = "Kiểu đối tượng không hợp lệ.")
        {
            if (Editor == null) return ObjectId.Null;

            var options = new PromptEntityOptions($"\n{message}");
            if (allowedType != null)
            {
                options.SetRejectMessage($"\n{rejectMessage}");
                options.AddAllowedClass(allowedType, true);
            }

            var result = Editor.GetEntity(options);
            return result.Status == PromptStatus.OK ? result.ObjectId : ObjectId.Null;
        }

        /// <summary>
        /// Yêu cầu người dùng chọn một điểm trên màn hình. Mặc định tự động chuyển đổi điểm chọn từ hệ tọa độ người dùng (UCS) sang hệ tọa độ thế giới (WCS).
        /// </summary>
        public static Point3d? GetPoint(string message, Point3d? basePoint = null, bool convertToWcs = true)
        {
            if (Editor == null) return null;

            var options = new PromptPointOptions($"\n{message}");
            if (basePoint.HasValue)
            {
                options.UseBasePoint = true;
                options.BasePoint = convertToWcs ? CoordinateHelper.WcsToUcs(basePoint.Value) : basePoint.Value;
            }

            var result = Editor.GetPoint(options);
            if (result.Status != PromptStatus.OK) return null;

            return convertToWcs ? CoordinateHelper.UcsToWcs(result.Value) : result.Value;
        }

        /// <summary>
        /// Yêu cầu người dùng xác định góc đối diện của hình chữ nhật từ một điểm gốc.
        /// </summary>
        public static Point3d? GetCorner(string message, Point3d basePoint, bool convertToWcs = true)
        {
            if (Editor == null) return null;

            Point3d ucsBase = convertToWcs ? CoordinateHelper.WcsToUcs(basePoint) : basePoint;
            var options = new PromptCornerOptions($"\n{message}", ucsBase);

            var result = Editor.GetCorner(options);
            if (result.Status != PromptStatus.OK) return null;

            return convertToWcs ? CoordinateHelper.UcsToWcs(result.Value) : result.Value;
        }

        /// <summary>
        /// Yêu cầu người dùng nhập hoặc đo một khoảng cách.
        /// </summary>
        public static double? GetDistance(string message, Point3d? basePoint = null)
        {
            if (Editor == null) return null;

            var options = new PromptDistanceOptions($"\n{message}");
            if (basePoint.HasValue)
            {
                options.UseBasePoint = true;
                options.BasePoint = CoordinateHelper.WcsToUcs(basePoint.Value);
            }

            var result = Editor.GetDistance(options);
            return result.Status == PromptStatus.OK ? result.Value : (double?)null;
        }

        /// <summary>
        /// Yêu cầu người dùng nhập hoặc trỏ một góc (radian), có đường dóng cao su từ basePoint.
        /// </summary>
        public static double? GetAngle(string message, Point3d? basePoint = null)
        {
            if (Editor == null) return null;

            var options = new PromptAngleOptions($"\n{message}");
            if (basePoint.HasValue)
            {
                options.UseBasePoint = true;
                options.BasePoint = CoordinateHelper.WcsToUcs(basePoint.Value);
            }

            var result = Editor.GetAngle(options);
            return result.Status == PromptStatus.OK ? result.Value : (double?)null;
        }

        /// <summary>
        /// Yêu cầu người dùng nhập một chuỗi ký tự.
        /// </summary>
        public static string GetString(string message, bool allowSpaces = true, string defaultVal = null)
        {
            if (Editor == null) return null;

            var options = new PromptStringOptions($"\n{message}")
            {
                AllowSpaces = allowSpaces
            };
            if (!string.IsNullOrEmpty(defaultVal))
            {
                options.DefaultValue = defaultVal;
                options.UseDefaultValue = true;
            }

            var result = Editor.GetString(options);
            return result.Status == PromptStatus.OK ? result.StringResult : null;
        }

        /// <summary>
        /// Yêu cầu người dùng chọn một từ khóa trong danh sách (ví dụ: [Yes/No], [Horizontal/Vertical]).
        /// AutoCAD tự hiện danh sách và từ khóa mặc định sau câu nhắc: "message [Yes/No] &lt;Yes&gt;:".
        /// </summary>
        public static string GetKeywords(string message, string[] keywords, string defaultKeyword = null)
        {
            if (Editor == null || keywords == null || keywords.Length == 0) return null;

            // Câu nhắc CHỈ có phần chữ: PromptOptions.AppendKeywordsToMessage mặc định true → AutoCAD tự nối
            // "[Yes/No] <Yes>:". Tự viết thêm "[…]" ở đây thì danh sách bị in hai lần (B-33, đã kiểm chứng
            // trên AutoCAD 2024; lab HeadlessLab KEYWORDS_PROMPT_LISTS_ONCE).
            var options = new PromptKeywordOptions($"\n{message}");
            foreach (var kw in keywords)
            {
                options.Keywords.Add(kw);
            }

            if (!string.IsNullOrEmpty(defaultKeyword))
            {
                options.Keywords.Default = defaultKeyword;
            }

            var result = Editor.GetKeywords(options);
            return result.Status == PromptStatus.OK ? result.StringResult : null;
        }

        /// <summary>
        /// Yêu cầu người dùng nhập một số nguyên.
        /// </summary>
        public static int? GetInteger(string message, int? defaultValue = null, int? lowerLimit = null, int? upperLimit = null)
        {
            if (Editor == null) return null;

            var options = new PromptIntegerOptions($"\n{message}");
            if (defaultValue.HasValue)
            {
                options.DefaultValue = defaultValue.Value;
                options.UseDefaultValue = true;
            }
            if (lowerLimit.HasValue) options.LowerLimit = lowerLimit.Value;
            if (upperLimit.HasValue) options.UpperLimit = upperLimit.Value;

            var result = Editor.GetInteger(options);
            return result.Status == PromptStatus.OK ? result.Value : (int?)null;
        }

        /// <summary>
        /// Yêu cầu người dùng nhập một số thực dấu phẩy động.
        /// </summary>
        public static double? GetDouble(string message, double? defaultValue = null)
        {
            if (Editor == null) return null;

            var options = new PromptDoubleOptions($"\n{message}");
            if (defaultValue.HasValue)
            {
                options.DefaultValue = defaultValue.Value;
                options.UseDefaultValue = true;
            }

            var result = Editor.GetDouble(options);
            return result.Status == PromptStatus.OK ? result.Value : (double?)null;
        }

        /// <summary>
        /// Yêu cầu người dùng chọn nhiều đối tượng trên bản vẽ, có thể truyền bộ lọc SelectionFilter.
        /// </summary>
        public static ObjectId[] GetSelection(string message = "Chọn các đối tượng: ", SelectionFilter filter = null)
        {
            if (Editor == null) return new ObjectId[0];

            var options = new PromptSelectionOptions { MessageForAdding = $"\n{message}" };
            var result = filter == null ? Editor.GetSelection(options) : Editor.GetSelection(options, filter);

            return result.Status == PromptStatus.OK ? result.Value.GetObjectIds() : new ObjectId[0];
        }

        /// <summary>
        /// Yêu cầu người dùng chọn nhiều đối tượng sử dụng bộ dựng bộ lọc SelectionFilterBuilder.
        /// </summary>
        public static ObjectId[] GetSelection(string message, SelectionFilterBuilder filterBuilder)
        {
            return GetSelection(message, filterBuilder?.Build());
        }
    }
}
