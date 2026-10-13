using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace CadCoreFramework.Utils
{
    /// <summary>
    /// Các tiện ích thao tác Block ở mức độ thấp (Low-level block manipulation).
    /// Khuyến khích sử dụng phương thức mở rộng <c>scope.InsertBlock(...)</c> từ <c>CadScopeDrawingExtensions</c>.
    /// Các phương thức này được khai báo internal phục vụ cho phần cài đặt nội bộ.
    /// </summary>
    internal static class BlockHelper
    {
        /// <summary>
        /// Chèn một tham chiếu khối (BlockReference) vào không gian chỉ định (thường là ModelSpace).
        /// </summary>
        public static ObjectId InsertBlock(Transaction tr, BlockTableRecord space, string blockName, Point3d position, double rotation = 0.0, double scale = 1.0)
        {
            var db = space.Database;
            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

            // Trả về ObjectId.Null thay vì bắn ngoại lệ khi block không tồn tại.
            // Caller (CadScopeDrawingExtensions.InsertBlock) đã kiểm tra IsNull để xử lý trường hợp này.
            // Pattern này nhất quán với các hàm AutoCAD .NET khác và tránh làm Abort transaction.
            if (!bt.Has(blockName))
            {
                return ObjectId.Null;
            }

            ObjectId btrId = bt[blockName];
            var blockRef = new BlockReference(position, btrId)
            {
                Rotation = rotation,
                ScaleFactors = new Scale3d(scale)
            };

            // CadContext opens CurrentSpace ForRead by default; lazy-upgrade to ForWrite here.
            if (!space.IsWriteEnabled) space.UpgradeOpen();

            ObjectId refId = space.AppendEntity(blockRef);
            tr.AddNewlyCreatedDBObject(blockRef, true);

            // Thêm các thuộc tính (attributes) nếu định nghĩa block có chứa thuộc tính
            var btrDef = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
            if (btrDef.HasAttributeDefinitions)
            {
                foreach (ObjectId id in btrDef)
                {
                    var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (ent is AttributeDefinition attDef && !attDef.Constant)
                    {
                        var attRef = new AttributeReference();
                        attRef.SetAttributeFromBlock(attDef, blockRef.BlockTransform);
                        blockRef.AttributeCollection.AppendAttribute(attRef);
                        tr.AddNewlyCreatedDBObject(attRef, true);
                        RefreshAlignment(attRef);
                    }
                }
            }

            return refId;
        }

        /// <summary>
        /// Tính lại hình học của attribute sau khi tạo / đổi chữ. Attribute căn lề khác BaseLeft (giữa, phải,
        /// Fit, Aligned…) giữ nguyên vị trí cũ cho tới lần regen nếu không gọi AdjustAlignment — chữ bị lệch
        /// khỏi điểm căn (đã kiểm chứng trên AutoCAD 2024 bằng tests/integration/HeadlessLab,
        /// ATTRIBUTE_REALIGNED_AFTER_SET). Attribute nhiều dòng (MText) cần UpdateMTextAttribute.
        /// </summary>
        private static void RefreshAlignment(AttributeReference attRef)
        {
            if (attRef.IsMTextAttribute)
                attRef.UpdateMTextAttribute();
            else if (attRef.Justify != AttachmentPoint.BaseLeft)
                attRef.AdjustAlignment(attRef.Database);
        }

        /// <summary>
        /// Lấy một từ điển gồm cặp Tag thuộc tính và giá trị văn bản từ một BlockReference.
        /// </summary>
        public static Dictionary<string, string> GetBlockAttributes(Transaction tr, BlockReference blockRef)
        {
            var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (ObjectId attId in blockRef.AttributeCollection)
            {
                if (tr.GetObject(attId, OpenMode.ForRead) is AttributeReference attRef)
                {
                    attributes[attRef.Tag] = attRef.TextString;
                }
            }

            return attributes;
        }

        /// <summary>
        /// Đặt hoặc cập nhật giá trị cho một thuộc tính cụ thể theo Tag trên BlockReference.
        /// </summary>
        public static bool SetBlockAttribute(Transaction tr, BlockReference blockRef, string tag, string value)
        {
            foreach (ObjectId attId in blockRef.AttributeCollection)
            {
                // Mở ForRead để so Tag; chỉ nâng quyền attribute cần sửa (không ghi undo cho mọi attribute).
                if (tr.GetObject(attId, OpenMode.ForRead) is AttributeReference attRef
                    && string.Equals(attRef.Tag, tag, StringComparison.OrdinalIgnoreCase))
                {
                    attRef.UpgradeOpen();
                    attRef.TextString = value;
                    RefreshAlignment(attRef);
                    return true;
                }
            }
            return false;
        }
    }
}
