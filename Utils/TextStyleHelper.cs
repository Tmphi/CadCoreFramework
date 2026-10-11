using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace CadCoreFramework.Utils
{
    /// <summary>
    /// Các tiện ích quản lý TextStyle trong bản vẽ (Tạo mới, thiết lập hiện hành, v.v.).
    /// </summary>
    public static class TextStyleHelper
    {
        /// <summary>
        /// Đảm bảo một TextStyle tồn tại trong bản vẽ. Nếu chưa có sẽ tự động tạo mới.
        /// </summary>
        /// <param name="db">Database hiện hành.</param>
        /// <param name="tr">Transaction đang mở.</param>
        /// <param name="styleName">Tên TextStyle.</param>
        /// <param name="fontName">Tên font chữ (VD: "Arial", "Times New Roman").</param>
        /// <param name="textSize">Kích thước chữ mặc định (0 = không cố định).</param>
        /// <returns>ObjectId của TextStyle.</returns>
        public static ObjectId EnsureTextStyle(Database db, Transaction tr, string styleName, string fontName, double textSize = 0)
        {
            Guard.NotNull(db, nameof(db));
            Guard.NotNull(tr, nameof(tr));
            Guard.NotEmpty(styleName, nameof(styleName));
            Guard.NotEmpty(fontName, nameof(fontName));

            var textStyleTable = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);

            if (textStyleTable.Has(styleName))
            {
                return textStyleTable[styleName];
            }

            textStyleTable.UpgradeOpen();

            var newStyle = new TextStyleTableRecord
            {
                Name = styleName,
                FileName = fontName,
                TextSize = textSize
            };

            var id = textStyleTable.Add(newStyle);
            tr.AddNewlyCreatedDBObject(newStyle, true);

            return id;
        }

        /// <summary>
        /// Thiết lập một TextStyle làm kiểu chữ hiện hành.
        /// </summary>
        public static void SetCurrentTextStyle(Database db, Transaction tr, string styleName)
        {
            Guard.NotNull(db, nameof(db));
            Guard.NotNull(tr, nameof(tr));
            Guard.NotEmpty(styleName, nameof(styleName));

            var textStyleTable = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);

            if (textStyleTable.Has(styleName))
            {
                db.Textstyle = textStyleTable[styleName];
            }
        }

        /// <summary>
        /// Lấy danh sách tên tất cả các TextStyle có trong bản vẽ.
        /// </summary>
        public static IReadOnlyList<string> GetAllTextStyleNames(Database db, Transaction tr)
        {
            Guard.NotNull(db, nameof(db));
            Guard.NotNull(tr, nameof(tr));

            var textStyleTable = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
            var names = new List<string>();

            foreach (ObjectId styleId in textStyleTable)
            {
                var style = (TextStyleTableRecord)tr.GetObject(styleId, OpenMode.ForRead);
                names.Add(style.Name);
            }

            return names;
        }
    }
}
