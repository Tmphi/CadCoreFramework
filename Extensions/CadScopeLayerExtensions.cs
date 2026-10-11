using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using CadCoreFramework.Core;

namespace CadCoreFramework.Extensions
{
    /// <summary>
    /// Extension methods cho ICadScope: quản lý Layer, TextStyle, Linetype, DimStyle.
    /// Đảm bảo các style/layer luôn tồn tại trong bản vẽ trước khi sử dụng.
    /// </summary>
    public static class CadScopeLayerExtensions
    {
        /// <summary>
        /// Đảm bảo một Layer luôn tồn tại trong bản vẽ. Nếu chưa có thì tự động tạo mới.
        /// </summary>
        public static ObjectId EnsureLayer(this ICadScope scope, string layerName, short colorIndex = 7, string linetypeName = null)
        {
            if (string.IsNullOrWhiteSpace(layerName)) return ObjectId.Null;

            // Mở ForWrite ngay từ đầu: an toàn hơn UpgradeOpen() sau ForRead,
            // tránh được lỗi eVetoed trong trường hợp table đã bị khóa bởi code khác.
            var layerTable = (LayerTable)scope.Transaction.GetObject(scope.Database.LayerTableId, OpenMode.ForWrite);
            if (!layerTable.Has(layerName))
            {
                var newLayer = new LayerTableRecord
                {
                    Name = layerName,
                    Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex)
                };

                if (!string.IsNullOrWhiteSpace(linetypeName))
                {
                    ObjectId ltId = scope.EnsureLinetype(linetypeName);
                    if (!ltId.IsNull)
                    {
                        newLayer.LinetypeObjectId = ltId;
                    }
                }

                ObjectId layerId = layerTable.Add(newLayer);
                scope.Transaction.AddNewlyCreatedDBObject(newLayer, true);
                return layerId;
            }
            return layerTable[layerName];
        }

        /// <summary>
        /// Đảm bảo TextStyle tồn tại trong cơ sở dữ liệu. Tạo mới nếu chưa có.
        /// </summary>
        public static ObjectId EnsureTextStyle(this ICadScope scope, string styleName, string fontFile = "arial.ttf", double textHeight = 0.0)
        {
            if (string.IsNullOrWhiteSpace(styleName)) return ObjectId.Null;

            // Mở ForWrite ngay từ đầu để tránh UpgradeOpen() sau ForRead.
            var styleTable = (TextStyleTable)scope.Transaction.GetObject(scope.Database.TextStyleTableId, OpenMode.ForWrite);
            if (!styleTable.Has(styleName))
            {
                var newStyle = new TextStyleTableRecord
                {
                    Name = styleName,
                    FileName = fontFile,
                    TextSize = textHeight
                };

                ObjectId styleId = styleTable.Add(newStyle);
                scope.Transaction.AddNewlyCreatedDBObject(newStyle, true);
                return styleId;
            }
            return styleTable[styleName];
        }

        /// <summary>
        /// Đảm bảo Linetype (kiểu đường nét) tồn tại trong cơ sở dữ liệu. Tự động nạp từ acad.lin nếu cần.
        /// </summary>
        public static ObjectId EnsureLinetype(this ICadScope scope, string linetypeName)
        {
            if (string.IsNullOrWhiteSpace(linetypeName)) return ObjectId.Null;

            // ForRead là đủ ở đây vì LoadLineTypeFile() tự xử lý việc ghi vào DB.
            // Không cần UpgradeOpen() cho LinetypeTable trong trường hợp này.
            var ltTable = (LinetypeTable)scope.Transaction.GetObject(scope.Database.LinetypeTableId, OpenMode.ForRead);
            if (ltTable.Has(linetypeName))
            {
                return ltTable[linetypeName];
            }

            try
            {
                // LoadLineTypeFile tự quản lý transaction nội bộ của nó
                scope.Database.LoadLineTypeFile(linetypeName, "acad.lin");
                // Sau khi load, cần lấy lại reference vì table đã thay đổi
                var ltTableReloaded = (LinetypeTable)scope.Transaction.GetObject(scope.Database.LinetypeTableId, OpenMode.ForRead);
                if (ltTableReloaded.Has(linetypeName))
                {
                    return ltTableReloaded[linetypeName];
                }
            }
            catch
            {
                // Sử dụng dự phòng Continuous nếu không tìm thấy trong tệp acad.lin
            }

            return ltTable["Continuous"];
        }

        /// <summary>
        /// Đảm bảo Dimension Style (DimStyle) tồn tại trong Database. Tạo mới nếu chưa có.
        /// Cho phép thiết lập chi tiết các thuộc tính chuẩn kích thước qua callback configure.
        /// </summary>
        public static ObjectId EnsureDimStyle(this ICadScope scope, string styleName, Action<DimStyleTableRecord> configure = null)
        {
            if (string.IsNullOrWhiteSpace(styleName)) return ObjectId.Null;

            var dst = (DimStyleTable)scope.Transaction.GetObject(scope.Database.DimStyleTableId, OpenMode.ForWrite);
            if (!dst.Has(styleName))
            {
                var newStyle = new DimStyleTableRecord
                {
                    Name = styleName
                };
                configure?.Invoke(newStyle);

                ObjectId styleId = dst.Add(newStyle);
                scope.Transaction.AddNewlyCreatedDBObject(newStyle, true);
                return styleId;
            }
            return dst[styleName];
        }

        /// <summary>
        /// Thiết lập trạng thái toàn diện cho Layer (Khóa, Đóng băng, Tắt, In ấn).
        /// </summary>
        public static void SetLayerStatus(this ICadScope scope, string layerName,
            bool? isLocked = null, bool? isFrozen = null, bool? isOff = null, bool? isPlottable = null)
        {
            if (string.IsNullOrWhiteSpace(layerName)) return;

            var lt = (LayerTable)scope.Transaction.GetObject(scope.Database.LayerTableId, OpenMode.ForRead);
            if (!lt.Has(layerName)) return;

            var ltr = (LayerTableRecord)scope.Transaction.GetObject(lt[layerName], OpenMode.ForWrite);
            if (isLocked.HasValue) ltr.IsLocked = isLocked.Value;
            if (isFrozen.HasValue && !ltr.Name.Equals("0", StringComparison.OrdinalIgnoreCase)) ltr.IsFrozen = isFrozen.Value;
            if (isOff.HasValue) ltr.IsOff = isOff.Value;
            if (isPlottable.HasValue) ltr.IsPlottable = isPlottable.Value;
        }

        /// <summary>
        /// Lấy danh sách tên của tất cả các Layer hiện có trong bản vẽ.
        /// </summary>
        public static List<string> GetAllLayerNames(this ICadScope scope)
        {
            var names = new List<string>();
            var lt = (LayerTable)scope.Transaction.GetObject(scope.Database.LayerTableId, OpenMode.ForRead);
            foreach (ObjectId id in lt)
            {
                var ltr = (LayerTableRecord)scope.Transaction.GetObject(id, OpenMode.ForRead);
                names.Add(ltr.Name);
            }
            return names;
        }

        /// <summary>
        /// Kiểm tra xem một Block Definition có tồn tại trong BlockTable hay không.
        /// </summary>
        public static bool HasBlock(this ICadScope scope, string blockName)
        {
            if (string.IsNullOrWhiteSpace(blockName)) return false;
            var bt = (BlockTable)scope.Transaction.GetObject(scope.Database.BlockTableId, OpenMode.ForRead);
            return bt.Has(blockName);
        }

        /// <summary>
        /// Tạo một định nghĩa Block mới (BlockTableRecord) từ tập hợp các Entity.
        /// Tự động thêm vào BlockTable và đăng ký Transaction chuẩn mực Autodesk.
        /// </summary>
        public static ObjectId CreateBlockDefinition(this ICadScope scope, string blockName,
            IEnumerable<Entity> entities, Point3d basePoint = default, string description = "")
        {
            if (string.IsNullOrWhiteSpace(blockName)) throw new ArgumentNullException(nameof(blockName));
            if (entities == null) throw new ArgumentNullException(nameof(entities));

            var bt = (BlockTable)scope.Transaction.GetObject(scope.Database.BlockTableId, OpenMode.ForWrite);
            if (bt.Has(blockName))
            {
                return bt[blockName];
            }

            var btr = new BlockTableRecord
            {
                Name = blockName,
                Origin = basePoint,
                Comments = description
            };

            ObjectId btrId = bt.Add(btr);
            scope.Transaction.AddNewlyCreatedDBObject(btr, true);

            foreach (var ent in entities)
            {
                if (ent != null)
                {
                    btr.AppendEntity(ent);
                    scope.Transaction.AddNewlyCreatedDBObject(ent, true);
                }
            }

            return btrId;
        }
    }
}
