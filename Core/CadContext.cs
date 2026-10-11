using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace CadCoreFramework.Core
{
    /// <summary>
    /// Triển khai cụ thể của giao diện ICadContext.
    /// Quản lý việc khóa bản vẽ (LockDocument) và vòng đời của transaction (StartTransaction / Commit)
    /// để caller không bao giờ phải viết các đoạn code boilerplate rườm rà.
    /// </summary>
    public class CadContext : ICadContext
    {
        /// <summary>
        /// Môi trường (Scope) nội bộ đại diện cho một phiên làm việc an toàn với transaction đang mở.
        /// Cung cấp quyền truy cập trực tiếp tới Document, Database, Editor và
        /// BlockTableRecord của không gian hiện hành (Model Space hoặc Paper Space).
        /// </summary>
        public class CadScope : ICadScope
        {
            public Document Document { get; }
            public Database Database => Document.Database;
            public Transaction Transaction { get; }
            public BlockTableRecord CurrentSpace { get; }
            public Editor Editor => Document.Editor;
            public bool IsReadOnly { get; }

            public CadScope(Document doc, Transaction tr, BlockTableRecord currentSpace, bool isReadOnly = false)
            {
                Document = doc;
                Transaction = tr;
                CurrentSpace = currentSpace;
                IsReadOnly = isReadOnly;
            }

            /// <summary>
            /// Thêm một entity mới vào không gian bản vẽ hiện hành (Model Space hoặc Paper Space).
            /// </summary>
            /// <param name="entity">Đối tượng AutoCAD entity cần thêm (Line, Circle, Text...).</param>
            /// <returns>ObjectId của entity vừa được tạo trong database.</returns>
            public ObjectId AppendEntityToCurrentSpace(Entity entity)
            {
                if (IsReadOnly) throw new InvalidOperationException("Không thể ghi đối tượng trong scope ExecuteRead.");
                if (entity == null) throw new ArgumentNullException(nameof(entity));

                ObjectId entityId = CurrentSpace.AppendEntity(entity);
                Transaction.AddNewlyCreatedDBObject(entity, true);
                return entityId;
            }
        }

        // ─── Private core — receives an already-resolved Document ──────────────
        // ExecuteAsync captures the Document before await so the active document
        // cannot change between the async preparation and the actual draw phase.

        private void ExecuteCore(Document doc, Action<ICadScope> cadAction, bool autoRegen, string undoMarkName)
        {
            bool useUndoMark = !string.IsNullOrEmpty(undoMarkName);
            dynamic comDoc = null;

            CadContextGuard.Enter();
            try
            {
                using (doc.LockDocument())
                {
                    if (useUndoMark)
                    {
                        try { comDoc = doc.GetAcadDocument(); comDoc.StartUndoMark(); }
                        catch { comDoc = null; }
                    }

                    try
                    {
                        using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                        {
                            BlockTableRecord currentSpace = (BlockTableRecord)tr.GetObject(
                                doc.Database.CurrentSpaceId, OpenMode.ForWrite);
                            var scope = new CadScope(doc, tr, currentSpace, false);
                            cadAction(scope);
                            tr.Commit();
                        }

                        if (autoRegen) doc.Editor.Regen();
                    }
                    finally
                    {
                        if (comDoc != null) try { comDoc.EndUndoMark(); } catch { }
                    }
                }
            }
            finally { CadContextGuard.Exit(); }
        }

        private CadResult<T> ExecuteCore<T>(Document doc, Func<ICadScope, T> cadFunc, bool autoRegen, string undoMarkName)
        {
            bool useUndoMark = !string.IsNullOrEmpty(undoMarkName);
            dynamic comDoc = null;

            CadContextGuard.Enter();
            try
            {
                using (doc.LockDocument())
                {
                    if (useUndoMark)
                    {
                        try { comDoc = doc.GetAcadDocument(); comDoc.StartUndoMark(); }
                        catch { comDoc = null; }
                    }

                    try
                    {
                        T result;
                        using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                        {
                            BlockTableRecord currentSpace = (BlockTableRecord)tr.GetObject(
                                doc.Database.CurrentSpaceId, OpenMode.ForWrite);
                            var scope = new CadScope(doc, tr, currentSpace, false);
                            result = cadFunc(scope);
                            tr.Commit();
                        }

                        if (autoRegen) doc.Editor.Regen();
                        return CadResult<T>.Success(result);
                    }
                    catch (Exception ex)
                    {
                        return CadResult<T>.Fail(ex.Message, ex);
                    }
                    finally
                    {
                        if (comDoc != null) try { comDoc.EndUndoMark(); } catch { }
                    }
                }
            }
            finally { CadContextGuard.Exit(); }
        }

        // ─── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Thực thi một hành động ghi đồng bộ an toàn bên trong tài liệu được khóa và transaction.
        /// Tự động khóa bản vẽ (LockDocument), mở transaction và commit khi thành công.
        /// Nếu undoMarkName được chỉ định, gom toàn bộ thao tác thành 1 bước Ctrl+Z.
        /// Ném exception nếu có lỗi — caller chịu trách nhiệm hiển thị và xử lý.
        /// </summary>
        public void Execute(Action<ICadScope> cadAction, bool autoRegen = false, string undoMarkName = null)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
                throw new InvalidOperationException("Không tìm thấy bản vẽ AutoCAD nào đang hoạt động.");
            ExecuteCore(doc, cadAction, autoRegen, undoMarkName);
        }

        /// <summary>
        /// Thực thi một hành động ghi đồng bộ an toàn có giá trị trả về.
        /// Trả về CadResult&lt;T&gt; thay vì throw exception khi lỗi — caller quyết định hiển thị lỗi.
        /// </summary>
        public CadResult<T> Execute<T>(Func<ICadScope, T> cadFunc, bool autoRegen = false, string undoMarkName = null)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return CadResult<T>.Fail("Không tìm thấy bản vẽ AutoCAD nào đang hoạt động.");
            return ExecuteCore<T>(doc, cadFunc, autoRegen, undoMarkName);
        }

        /// <summary>
        /// Thực thi một hành động chỉ đọc (read-only) đồng bộ.
        /// KHÔNG khóa tài liệu (không cần thiết khi chỉ đọc từ command context).
        /// KHÔNG cho phép gọi AppendEntityToCurrentSpace bên trong scope này.
        /// </summary>
        public void ExecuteRead(Action<ICadScope> readAction)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
                throw new InvalidOperationException("Không tìm thấy bản vẽ AutoCAD nào đang hoạt động.");

            CadContextGuard.Enter();
            try
            {
                using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                {
                    BlockTableRecord currentSpace = (BlockTableRecord)tr.GetObject(
                        doc.Database.CurrentSpaceId, OpenMode.ForRead);
                    var scope = new CadScope(doc, tr, currentSpace, true);
                    readAction(scope);
                    tr.Commit(); // Commit is cheaper than Abort for a read-only transaction
                }
            }
            finally { CadContextGuard.Exit(); }
        }

        /// <summary>
        /// Thực thi thao tác hai giai đoạn bất đồng bộ (Async):
        ///   Giai đoạn 1 — Tính toán nặng hoặc gọi API bên ngoài (không khóa, không gây đơ AutoCAD).
        ///   Giai đoạn 2 — Thực hiện vẽ vào database bên trong transaction có khóa.
        /// Document được capture TRƯỚC khi await để tránh vẽ nhầm file nếu user đổi bản vẽ giữa chừng.
        /// </summary>
        public async Task ExecuteAsync(
            Func<Task<Action<ICadScope>>> asyncPreparation,
            bool autoRegen = false,
            string undoMarkName = null,
            CancellationToken cancellationToken = default)
        {
            // Capture document before the async gap — user may switch drawings during await
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
                throw new InvalidOperationException("Không tìm thấy bản vẽ AutoCAD nào đang hoạt động.");

            cancellationToken.ThrowIfCancellationRequested();
            Action<ICadScope> drawAction = await asyncPreparation();
            cancellationToken.ThrowIfCancellationRequested();

            ExecuteCore(doc, drawAction, autoRegen, undoMarkName);
        }

        /// <summary>
        /// Thực hiện vẽ hàng loạt (batch) nhiều items trong MỘT transaction duy nhất.
        /// Toàn bộ batch được gom thành 1 bước Ctrl+Z bằng UndoMark.
        /// Hiệu năng tốt hơn nhiều so với gọi Execute() riêng lẻ cho mỗi item.
        /// </summary>
        public void ExecuteBatch<T>(
            IEnumerable<T> items,
            Action<ICadScope, T> drawAction,
            IProgress<int> progress = null,
            string undoMarkName = "Batch")
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (drawAction == null) throw new ArgumentNullException(nameof(drawAction));

            var itemList = items as IList<T> ?? items.ToList();
            if (itemList.Count == 0) return;

            Execute(scope =>
            {
                int count = 0;
                foreach (var item in itemList)
                {
                    drawAction(scope, item);
                    count++;
                    progress?.Report(count);
                }
            }, autoRegen: false, undoMarkName: undoMarkName);
        }
    }
}
