using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using CadCoreFramework.Core;
using CadCoreFramework.Utils;

namespace CadCoreFramework.Extensions
{
    /// <summary>
    /// Extension methods cho ICadScope: chèn Block, vẽ Table, vẽ Hatch, quản lý Group, Copy/Mirror.
    /// </summary>
    public static class CadScopeBlockExtensions
    {
        #region Block & Dynamic Block Insertion

        /// <summary>
        /// Chèn một Block Reference kèm các thuộc tính (attributes) và thuộc tính động (dynamic properties) chỉ bằng một lệnh duy nhất.
        /// </summary>
        public static ObjectId InsertBlock(this ICadScope scope, string blockName, Point3d position,
            double rotation = 0.0, double scale = 1.0,
            Dictionary<string, string> attributes = null,
            Dictionary<string, object> dynamicProperties = null,
            string layer = null)
        {
            ObjectId refId = BlockHelper.InsertBlock(scope.Transaction, scope.CurrentSpace, blockName, position, rotation, scale);
            if (refId.IsNull)
            {
                // BlockHelper.InsertBlock() trả về Null khi block không tồn tại trong DB.
                // Ghi cảnh báo ra Command Line để dev biết ngay, thay vì âm thầm fail.
                scope.Editor.WriteMessage("\n[InsertBlock Cảnh báo]: Block '" + blockName + "' không tìm thấy trong cơ sở dữ liệu bản vẽ.");
                return ObjectId.Null;
            }

            var blockRef = (BlockReference)scope.Transaction.GetObject(refId, OpenMode.ForWrite);

            if (!string.IsNullOrWhiteSpace(layer))
            {
                scope.EnsureLayer(layer);
                blockRef.Layer = layer;
            }

            // Gán các thuộc tính (attributes) nếu có
            if (attributes != null && attributes.Count > 0)
            {
                foreach (var kvp in attributes)
                {
                    BlockHelper.SetBlockAttribute(scope.Transaction, blockRef, kvp.Key, kvp.Value);
                }
            }

            // Gán các thuộc tính động (dynamic properties) nếu có
            if (dynamicProperties != null && dynamicProperties.Count > 0 && blockRef.IsDynamicBlock)
            {
                foreach (var kvp in dynamicProperties)
                {
                    DynamicBlockHelper.SetDynamicProperty(blockRef, kvp.Key, kvp.Value);
                }
            }

            return refId;
        }

        #endregion

        #region Table

        /// <summary>
        /// Vẽ một Bảng thống kê (Table / BOM) và tùy chọn điền sẵn dữ liệu vào bảng.
        /// </summary>
        public static ObjectId DrawTable(this ICadScope scope, Point3d insertPoint, int rows, int columns,
            double rowHeight = 8.0, double colWidth = 30.0, string title = "BOM", string[,] data = null, string layer = null,
            Action<Table> configure = null)
        {
            return CadScopeDrawingHelpers.AppendNew(scope, new Table(), table =>
            {
                table.SetDatabaseDefaults(scope.Database);
                table.TableStyle = scope.Database.Tablestyle;

                table.SetSize(rows, columns);
                table.SetRowHeight(rowHeight);
                table.SetColumnWidth(colWidth);
                table.Position = insertPoint;

                table.GenerateLayout();

                if (!string.IsNullOrWhiteSpace(title) && rows > 0 && columns > 0)
                {
                    table.Cells[0, 0].TextString = title;
                }

                if (data != null)
                {
                    int maxRow = Math.Min(rows, data.GetLength(0));
                    int maxCol = Math.Min(columns, data.GetLength(1));

                    int startRow = rows > 1 ? 1 : 0;
                    for (int r = 0; r < maxRow; r++)
                    {
                        if (r + startRow >= rows) break;
                        for (int c = 0; c < maxCol; c++)
                        {
                            table.Cells[r + startRow, c].TextString = data[r, c] ?? "";
                        }
                    }
                }
            }, layer, colorIndex: null, configure);
        }

        #endregion

        #region Hatch

        /// <summary>
        /// Vẽ mẫu mặt cắt (Hatch) bên trong đường bao khép kín (ví dụ: ObjectId của một hình chữ nhật hoặc Polyline).
        /// Tự động xử lý toàn bộ quy trình: AppendLoop, EvaluateHatch và tính liên kết (associativity).
        /// </summary>
        public static ObjectId DrawHatch(this ICadScope scope, ObjectId boundaryId,
            string patternName = "SOLID", HatchPatternType patternType = HatchPatternType.PreDefined,
            double scale = 1.0, double angle = 0.0, Vector3d? normal = null, string layer = null, short? colorIndex = null,
            Action<Hatch> configure = null)
        {
            if (boundaryId.IsNull) return ObjectId.Null;

            var hatch = new Hatch();
            // configure chạy SAU AppendLoop (cần hatch đã nằm trong database) → không truyền vào AppendNew.
            ObjectId hatchId = CadScopeDrawingHelpers.AppendNew(scope, hatch, h =>
            {
                h.SetDatabaseDefaults(scope.Database);
                if (normal.HasValue) h.Normal = normal.Value;
                h.SetHatchPattern(patternType, patternName);
                h.HatchStyle = HatchStyle.Normal;
                h.PatternScale = scale;
                h.PatternAngle = angle;
            }, layer, colorIndex, configure: null);

            // BẮT BUỘC theo API AutoCAD: Associative và AppendLoop PHẢI được gọi SAU KHI
            // Hatch đã được thêm vào Database (CurrentSpace.AppendEntity + AddNewlyCreatedDBObject).
            // Nếu gán Associative = true trước khi đối tượng có mặt trong Database, AutoCAD sẽ ném lỗi eNotInDatabase.
            hatch.Associative = true;
            var ids = new ObjectIdCollection { boundaryId };
            hatch.AppendLoop(HatchLoopTypes.Outermost, ids);
            hatch.EvaluateHatch(true);
            configure?.Invoke(hatch);

            return hatchId;
        }

        #endregion

        #region Group Management

        /// <summary>
        /// Tạo một Nhóm (Group) đối tượng. Nếu tên nhóm đã tồn tại, tự động thêm hậu tố để tạo tên duy nhất.
        /// </summary>
        public static ObjectId CreateGroup(this ICadScope scope, string baseGroupName, IEnumerable<ObjectId> entityIds, string description = "")
        {
            var ids = entityIds?.ToList();
            if (ids == null || ids.Count == 0) return ObjectId.Null;

            // Theo Autodesk Managed .NET Developer's Guide: mở ForWrite ngay từ đầu vì ta luôn thêm group mới vào Dictionary
            var groupDict = (DBDictionary)scope.Transaction.GetObject(scope.Database.GroupDictionaryId, OpenMode.ForWrite);

            string groupName = baseGroupName;
            int counter = 1;
            while (groupDict.Contains(groupName))
            {
                groupName = $"{baseGroupName}_{counter}";
                counter++;
            }

            var group = new Group(description, true);
            ObjectId groupId = groupDict.SetAt(groupName, group);
            scope.Transaction.AddNewlyCreatedDBObject(group, true);

            group.Append(new ObjectIdCollection(ids.ToArray()));
            return groupId;
        }

        #endregion

        #region Copy and Mirror

        /// <summary>
        /// Tạo bản sao của một đối tượng bị dịch chuyển theo một vector. Trả về ObjectId của đối tượng mới.
        /// Sao chép SÂU (<c>DeepCloneObjects</c>): giữ Extension Dictionary (metadata XRecord), XData và
        /// attribute của block — <c>Entity.Clone()</c> làm MẤT Extension Dictionary (đã kiểm chứng trên
        /// AutoCAD 2024 bằng tests/integration/HeadlessLab, COPY_KEEPS_METADATA).
        /// </summary>
        public static ObjectId CopyEntity(this ICadScope scope, ObjectId sourceId, Vector3d displacement)
        {
            if (sourceId.IsNull) return ObjectId.Null;

            Entity clone = DeepCloneToCurrentSpace(scope, sourceId);
            if (clone == null) return ObjectId.Null;

            clone.TransformBy(Matrix3d.Displacement(displacement));
            return clone.ObjectId;
        }

        /// <summary>
        /// Tạo bản sao của một đối tượng di chuyển từ điểm này sang điểm khác. Trả về ObjectId của đối tượng mới.
        /// </summary>
        public static ObjectId CopyEntity(this ICadScope scope, ObjectId sourceId, Point3d fromPoint, Point3d toPoint)
            => scope.CopyEntity(sourceId, toPoint - fromPoint);

        /// <summary>
        /// Tạo bản sao lấy đối xứng (Mirror) của đối tượng qua trục đối xứng xác định bởi 2 điểm.
        /// Nếu eraseSource là true, đối tượng gốc sẽ bị xóa. Sao chép SÂU như <see cref="CopyEntity(ICadScope, ObjectId, Vector3d)"/>.
        /// </summary>
        public static ObjectId MirrorEntity(this ICadScope scope, ObjectId sourceId,
            Point3d mirrorLineStart, Point3d mirrorLineEnd, bool eraseSource = false)
        {
            if (sourceId.IsNull) return ObjectId.Null;

            Entity clone = DeepCloneToCurrentSpace(scope, sourceId);
            if (clone == null) return ObjectId.Null;

            clone.TransformBy(Matrix3d.Mirroring(new Line3d(mirrorLineStart, mirrorLineEnd)));

            if (eraseSource)
            {
                var source = (Entity)scope.Transaction.GetObject(sourceId, OpenMode.ForWrite);
                source.Erase();
            }

            return clone.ObjectId;
        }

        /// <summary>
        /// Deep clone một entity vào không gian hiện hành. Trả về bản sao (mở ForWrite) hoặc null nếu
        /// <paramref name="sourceId"/> không phải Entity.
        /// </summary>
        private static Entity DeepCloneToCurrentSpace(ICadScope scope, ObjectId sourceId)
        {
            if (scope.IsReadOnly)
                throw new InvalidOperationException("Không thể sao chép đối tượng trong scope ExecuteRead.");
            if (!(scope.Transaction.GetObject(sourceId, OpenMode.ForRead) is Entity)) return null;

            // CurrentSpace được CadContext mở ForRead — nâng quyền trước khi AutoCAD thêm bản sao vào nó.
            if (!scope.CurrentSpace.IsWriteEnabled) scope.CurrentSpace.UpgradeOpen();

            using (var mapping = new IdMapping())
            {
                scope.Database.DeepCloneObjects(new ObjectIdCollection { sourceId }, scope.CurrentSpace.ObjectId, mapping, false);
                ObjectId cloneId = mapping[sourceId].Value;
                return (Entity)scope.Transaction.GetObject(cloneId, OpenMode.ForWrite);
            }
        }

        #endregion
    }
}
