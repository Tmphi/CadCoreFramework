using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace CadCoreFramework.Extensions
{
    /// <summary>
    /// Các phương thức mở rộng cho BlockTableRecord (ModelSpace, PaperSpace hoặc Block definition).
    /// </summary>
    public static class BlockTableRecordExtensions
    {
        /// <summary>
        /// Thêm hàng loạt Entity vào BlockTableRecord và tự động đăng ký với Transaction.
        /// Tiện lợi khi cần thêm nhiều đối tượng cùng lúc vào ModelSpace.
        /// </summary>
        /// <param name="btr">BlockTableRecord cần thêm đối tượng vào.</param>
        /// <param name="tr">Transaction đang mở.</param>
        /// <param name="entities">Danh sách các entity cần thêm.</param>
        /// <returns>Tập hợp các ObjectId của các entity vừa được thêm.</returns>
        public static IEnumerable<ObjectId> AppendEntities(this BlockTableRecord btr, Transaction tr, params Entity[] entities)
        {
            var ids = new List<ObjectId>();
            foreach (var entity in entities)
            {
                if (entity != null)
                {
                    ObjectId id = btr.AppendEntity(entity);
                    tr.AddNewlyCreatedDBObject(entity, true);
                    ids.Add(id);
                }
            }
            return ids;
        }

        /// <summary>
        /// Thêm danh sách Entity (IEnumerable) vào BlockTableRecord và tự động đăng ký với Transaction.
        /// </summary>
        /// <param name="btr">BlockTableRecord cần thêm đối tượng vào.</param>
        /// <param name="tr">Transaction đang mở.</param>
        /// <param name="entities">Tập hợp các entity cần thêm.</param>
        /// <returns>Tập hợp các ObjectId của các entity vừa được thêm.</returns>
        public static IEnumerable<ObjectId> AppendEntities(this BlockTableRecord btr, Transaction tr, IEnumerable<Entity> entities)
        {
            var ids = new List<ObjectId>();
            foreach (var entity in entities)
            {
                if (entity != null)
                {
                    ObjectId id = btr.AppendEntity(entity);
                    tr.AddNewlyCreatedDBObject(entity, true);
                    ids.Add(id);
                }
            }
            return ids;
        }
    }
}
