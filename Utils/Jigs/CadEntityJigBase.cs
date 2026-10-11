using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace CadCoreFramework.Utils.Jigs
{
    /// <summary>
    /// Lớp cơ sở dạng Generic (Strongly-typed) cho EntityJig.
    /// Giúp tránh việc phải ép kiểu (cast) thuộc tính Entity mỗi khi dùng trong Update().
    /// </summary>
    /// <typeparam name="TEntity">Kiểu đối tượng cần vẽ (VD: Line, BlockReference, Polyline)</typeparam>
    public abstract class CadEntityJigBase<TEntity> : EntityJig where TEntity : Entity
    {
        /// <summary>
        /// Khởi tạo một phiên bản CadEntityJigBase bao bọc đối tượng thực thể kiểu TEntity.
        /// </summary>
        protected CadEntityJigBase(TEntity entity) : base(entity)
        {
        }

        /// <summary>
        /// Truy cập đối tượng Entity đang được thao tác dưới đúng kiểu dữ liệu gốc.
        /// </summary>
        public new TEntity Entity => (TEntity)base.Entity;
    }
}
