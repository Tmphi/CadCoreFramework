using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using CadCoreFramework.Core;

namespace CadCoreFramework.Testing.Fakes
{
    /// <summary>
    /// Test double cho ICadScope. Không cần AutoCAD runtime — mọi AutoCAD property trả về null.
    ///
    /// MỤC ĐÍCH:
    /// Dùng trong unit test để kiểm tra logic của Service mà không cần cài AutoCAD.
    /// AppendEntityToCurrentSpace() ghi nhận entity vào AppendedEntities thay vì vào database thật.
    ///
    /// GIỚI HẠN:
    /// Extension methods như scope.DrawLine() sẽ fail (NullReferenceException) nếu chúng
    /// gọi scope.Database hoặc entity.SetDatabaseDefaults(). Dùng FakeCadContext.ExecuteAction = false
    /// để bỏ qua phần drawing và chỉ kiểm tra control flow (Execute được gọi mấy lần, undoMarkName...).
    /// </summary>
    public class FakeCadScope : ICadScope
    {
        private readonly List<Entity> _appended = new List<Entity>();

        public Document? Document => null;
        public Database? Database => null;
        public Transaction? Transaction => null;
        public BlockTableRecord? CurrentSpace => null;
        public Editor? Editor => null;
        public bool IsReadOnly => false;

        /// <summary>Danh sách Entity đã được "append" (thay cho database thật).</summary>
        public IReadOnlyList<Entity> AppendedEntities => _appended.AsReadOnly();

        /// <summary>Số lần AppendEntityToCurrentSpace được gọi.</summary>
        public int AppendCount => _appended.Count;

        public ObjectId AppendEntityToCurrentSpace(Entity entity)
        {
            if (entity != null) _appended.Add(entity);
            return ObjectId.Null;
        }
    }
}
