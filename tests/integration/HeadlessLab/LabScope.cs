using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using CadCoreFramework.Core;

namespace CadCoreFramework.HeadlessLab
{
    /// <summary>
    /// ICadScope tối giản cho lab headless — cùng hợp đồng với CadContext.CadScope
    /// (CurrentSpace mở ForRead, upgrade lười khi append; chặn ghi khi read-only).
    /// </summary>
    internal sealed class LabScope : ICadScope
    {
        public LabScope(Document doc, Transaction tr, bool isReadOnly = false)
        {
            Document = doc;
            Transaction = tr;
            IsReadOnly = isReadOnly;
            CurrentSpace = (BlockTableRecord)tr.GetObject(doc.Database.CurrentSpaceId, OpenMode.ForRead);
        }

        public Document Document { get; }
        public Database Database => Document.Database;
        public Transaction Transaction { get; }
        public BlockTableRecord CurrentSpace { get; }
        public Editor Editor => Document.Editor;
        public bool IsReadOnly { get; }

        public ObjectId AppendEntityToCurrentSpace(Entity entity)
        {
            if (IsReadOnly) throw new InvalidOperationException("Read-only scope.");
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (!CurrentSpace.IsWriteEnabled) CurrentSpace.UpgradeOpen();
            ObjectId id = CurrentSpace.AppendEntity(entity);
            Transaction.AddNewlyCreatedDBObject(entity, true);
            return id;
        }
    }
}
