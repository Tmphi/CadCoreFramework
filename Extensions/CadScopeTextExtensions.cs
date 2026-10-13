using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using CadCoreFramework.Core;

namespace CadCoreFramework.Extensions
{
    /// <summary>
    /// Extension methods cho ICadScope: vẽ văn bản (DBText, MText) và đường chỉ dẫn (MLeader).
    /// </summary>
    public static class CadScopeTextExtensions
    {
        /// <summary>
        /// Vẽ văn bản nhiều dòng có định dạng (MText).
        /// </summary>
        public static ObjectId DrawMText(this ICadScope scope, string text, Point3d location,
            double textHeight = 2.5, double width = 0.0, AttachmentPoint attachment = AttachmentPoint.BottomLeft,
            string layer = null, short? colorIndex = null, string textStyle = null,
            Action<MText> configure = null)
        {
            return CadScopeDrawingHelpers.AppendNew(scope, new MText(), mtext =>
            {
                mtext.SetDatabaseDefaults(scope.Database);
                mtext.Location = location;
                mtext.Contents = text;
                mtext.TextHeight = textHeight;
                if (width > 0) mtext.Width = width;
                mtext.Attachment = attachment;

                if (!string.IsNullOrWhiteSpace(textStyle))
                {
                    ObjectId styleId = scope.EnsureTextStyle(textStyle);
                    if (!styleId.IsNull) mtext.TextStyleId = styleId;
                }
            }, layer, colorIndex, configure);
        }

        /// <summary>
        /// Vẽ văn bản đơn dòng (DBText / DTEXT). Khuyên dùng thay cho MText đối với các nhãn ngắn, ký hiệu thẻ và chú thích.
        /// Tham số justification điều khiển căn lề (ví dụ: TextHorizontalMode.TextCenter).
        /// </summary>
        public static ObjectId DrawText(this ICadScope scope, string text, Point3d position,
            double height = 2.5, double rotation = 0.0, string layer = null, short? colorIndex = null,
            string textStyle = null,
            TextHorizontalMode justification = TextHorizontalMode.TextLeft,
            Action<DBText> configure = null)
        {
            var dtext = new DBText();
            ObjectId textId = CadScopeDrawingHelpers.AppendNew(scope, dtext, t =>
            {
                t.SetDatabaseDefaults(scope.Database);
                t.TextString = text;
                t.Height = height;
                t.Rotation = rotation;
                t.HorizontalMode = justification;

                // Theo tài liệu Autodesk chính thức:
                // - Khi HorizontalMode == TextLeft (default): dùng Position để xác định tọa độ.
                // - Khi HorizontalMode != TextLeft: AutoCAD bỏ qua Position và chỉ dùng AlignmentPoint.
                //   Position phải được set về Point3d.Origin để tránh AutoCAD tính toán sai vị trí.
                //   Sau khi set xong, PHẢI gọi AdjustAlignment(Database) để geometry được tính lại.
                if (justification == TextHorizontalMode.TextLeft)
                {
                    t.Position = position;
                }
                else
                {
                    t.Position = Point3d.Origin;   // Bắt buộc reset theo API spec
                    t.AlignmentPoint = position;   // Điểm căn chỉnh thực sự
                }

                if (!string.IsNullOrWhiteSpace(textStyle))
                {
                    ObjectId styleId = scope.EnsureTextStyle(textStyle);
                    if (!styleId.IsNull) t.TextStyleId = styleId;
                }
            }, layer, colorIndex, configure);

            // AdjustAlignment() PHẢI được gọi SAU KHI entity đã được thêm vào Database,
            // vì hàm này cần Database context để tính toán lại geometry của text.
            if (justification != TextHorizontalMode.TextLeft)
            {
                dtext.AdjustAlignment(scope.Database);
            }

            return textId;
        }

        /// <summary>
        /// Vẽ đường chỉ dẫn đa năng (MLeader) kèm văn bản và mũi tên chỉ đến vị trí cụ thể.
        /// </summary>
        public static ObjectId DrawMLeader(this ICadScope scope, string text, Point3d arrowPoint, Point3d textPoint,
            string styleName = null, string layer = null, short? colorIndex = null, Action<MLeader> configure = null)
        {
            return CadScopeDrawingHelpers.AppendNew(scope, new MLeader(), mleader =>
            {
                mleader.SetDatabaseDefaults(scope.Database);

                if (!string.IsNullOrWhiteSpace(styleName))
                {
                    var mlst = (DBDictionary)scope.Transaction.GetObject(scope.Database.MLeaderStyleDictionaryId, OpenMode.ForRead);
                    if (mlst.Contains(styleName))
                    {
                        mleader.MLeaderStyle = mlst.GetAt(styleName);
                    }
                }
                else
                {
                    mleader.MLeaderStyle = scope.Database.MLeaderstyle;
                }

                mleader.ContentType = ContentType.MTextContent;

                // MText TẠM để thiết lập nội dung: setter MLeader.MText tự clone nội bộ, nên đối tượng này
                // KHÔNG vào database (không AddNewlyCreatedDBObject) → using để luôn Dispose, kể cả khi lỗi.
                using (var mtext = new MText())
                {
                    mtext.SetDatabaseDefaults(scope.Database);
                    mtext.Contents = text;
                    mtext.Annotative = AnnotativeStates.True;
                    mleader.MText = mtext;
                }

                mleader.TextLocation = textPoint;

                int leaderIndex = mleader.AddLeader();
                int leaderLineIndex = mleader.AddLeaderLine(leaderIndex);
                mleader.AddFirstVertex(leaderLineIndex, arrowPoint);
                mleader.AddLastVertex(leaderLineIndex, textPoint);
            }, layer, colorIndex, configure);
        }
    }
}
