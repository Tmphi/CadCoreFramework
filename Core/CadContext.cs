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

        /// <summary>
        /// Thực thi một hành động ghi đồng bộ an toàn bên trong tài liệu được khóa và transaction.
        /// Tự động khóa bản vẽ (LockDocument), mở transaction và commit khi thành công.
        /// Nếu undoMarkName được chỉ định, gom toàn bộ thao tác thành 1 bước Ctrl+Z.
        /// </summary>
        /// <param name="cadAction">Hành động vẽ cần thực thi, nhận vào một đối tượng ICadScope.</param>
        /// <param name="autoRegen">
        /// Khi là true, tự động gọi Editor.Regen() sau khi commit.
        /// Mặc định là FALSE để tránh regen không cần thiết khi vẽ hàng loạt.
        /// Chỉ bật cho Hatch, Wipeout, Table hoặc các entity cần Regen để hiển thị chính xác.
        /// </param>
        /// <param name="undoMarkName">
        /// Nếu khác null/empty, gọi Database.BeginUndoMark() trước và EndUndoMark() sau transaction
        /// để gom toàn bộ thao tác thành MỘT bước Ctrl+Z duy nhất.
        /// </param>
        public void Execute(Action<ICadScope> cadAction, bool autoRegen = false, string undoMarkName = null)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
                throw new InvalidOperationException("Không tìm thấy bản vẽ AutoCAD nào đang hoạt động.");

            bool useUndoMark = !string.IsNullOrEmpty(undoMarkName);
            dynamic comDoc = null;

            // 1. Khóa bản vẽ (Lock document): thiết yếu khi gọi từ Palette / luồng UI modeless.
            using (doc.LockDocument())
            {
                // 2. Bắt đầu UndoMark nếu được yêu cầu — gom tất cả thao tác thành 1 bước Ctrl+Z.
                //    Sử dụng COM interop (AcadDocument.StartUndoMark) vì .NET API (Database)
                //    KHÔNG có BeginUndoMark — chỉ có ở tầng COM.
                if (useUndoMark)
                {
                    try
                    {
                        comDoc = doc.GetAcadDocument();
                        comDoc.StartUndoMark();
                    }
                    catch
                    {
                        // COM interop có thể không khả dụng trên một số cấu hình AutoCAD
                        comDoc = null;
                    }
                }

                try
                {
                    // 3. Mở transaction để tương tác an toàn với database.
                    using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                    {
                        BlockTableRecord currentSpace = (BlockTableRecord)tr.GetObject(
                            doc.Database.CurrentSpaceId, OpenMode.ForWrite);

                        var scope = new CadScope(doc, tr, currentSpace, false);
                        cadAction(scope);
                        tr.Commit();
                    }

                    if (autoRegen)
                        doc.Editor.Regen();
                }
                catch (Exception ex)
                {
                    // Transaction đã tự Abort trong Dispose() nếu chưa Commit — không cần gọi tr.Abort() thủ công.
                    doc.Editor.WriteMessage($"\n[CadContext Lỗi]: {ex.Message}");
                    throw;
                }
                finally
                {
                    // EndUndoMark PHẢI nằm trong finally để đảm bảo luôn được gọi,
                    // kể cả khi action throw exception — tránh để UndoMark mở vĩnh viễn.
                    if (comDoc != null)
                    {
                        try { comDoc.EndUndoMark(); } catch { /* Swallow COM errors */ }
                    }
                }
            }
        }


        /// <summary>
        /// Thực thi một hành động ghi đồng bộ an toàn có giá trị trả về.
        /// Trả về CadResult&lt;T&gt; thay vì throw exception khi lỗi.
        /// </summary>
        public CadResult<T> Execute<T>(Func<ICadScope, T> cadFunc, bool autoRegen = false, string undoMarkName = null)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return CadResult<T>.Fail("Không tìm thấy bản vẽ AutoCAD nào đang hoạt động.");

            bool useUndoMark = !string.IsNullOrEmpty(undoMarkName);
            dynamic comDoc = null;

            using (doc.LockDocument())
            {
                if (useUndoMark)
                {
                    try
                    {
                        comDoc = doc.GetAcadDocument();
                        comDoc.StartUndoMark();
                    }
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

                    if (autoRegen)
                        doc.Editor.Regen();

                    return CadResult<T>.Success(result);
                }
                catch (Exception ex)
                {
                    doc.Editor.WriteMessage($"\n[CadContext Lỗi]: {ex.Message}");
                    return CadResult<T>.Fail(ex.Message, ex);
                }
                finally
                {
                    if (comDoc != null)
                    {
                        try { comDoc.EndUndoMark(); } catch { /* Swallow COM errors */ }
                    }
                }
            }
        }


        /// <summary>
        /// Thực thi một hành động chỉ đọc (read-only) đồng bộ. KHÔNG khóa tài liệu
        /// (không cần thiết khi chỉ đọc dữ liệu từ command context).
        /// Luôn abort transaction — không bao giờ commit — để bảo đảm ngữ nghĩa chỉ đọc.
        /// </summary>
        public void ExecuteRead(Action<ICadScope> readAction)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
                throw new InvalidOperationException("Không tìm thấy bản vẽ AutoCAD nào đang hoạt động.");

            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {
                try
                {
                    BlockTableRecord currentSpace = (BlockTableRecord)tr.GetObject(
                        doc.Database.CurrentSpaceId, OpenMode.ForRead);

                    var scope = new CadScope(doc, tr, currentSpace, true);
                    readAction(scope);
                    tr.Abort(); // Chỉ đọc: luôn abort, không bao giờ commit.
                }
                catch (Exception ex)
                {
                    // Transaction tự Abort trong Dispose() nếu chưa Commit/Abort — không cần gọi thủ công.
                    doc.Editor.WriteMessage($"\n[CadContext Lỗi]: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// Thực thi thao tác hai giai đoạn bất đồng bộ (Async):
        ///   Giai đoạn 1 — Tính toán nặng hoặc gọi API bên ngoài (không khóa, không gây đơ AutoCAD).
        ///   Giai đoạn 2 — Thực hiện vẽ vào database bên trong transaction có khóa.
        /// </summary>
        /// <param name="asyncPreparation">
        /// Hàm async trả về một Action vẽ. Tuyệt đối KHÔNG gọi bất kỳ AutoCAD API nào trong hàm này.
        /// </param>
        /// <param name="autoRegen">Khi là true, gọi Regen() sau khi commit. Mặc định là FALSE.</param>
        /// <param name="undoMarkName">Nếu chỉ định, gom toàn bộ thao tác thành 1 bước Ctrl+Z.</param>
        /// <param name="cancellationToken">Token cho phép hủy giai đoạn chuẩn bị async.</param>
        public async Task ExecuteAsync(
            Func<Task<Action<ICadScope>>> asyncPreparation,
            bool autoRegen = false,
            string undoMarkName = null,
            CancellationToken cancellationToken = default)
        {
            // Giai đoạn 1: tính toán bên ngoài khóa — không làm nghẽn giao diện AutoCAD.
            cancellationToken.ThrowIfCancellationRequested();
            Action<ICadScope> drawAction = await asyncPreparation();

            // Giai đoạn 2: thực hiện vẽ đồng bộ bên trong transaction có khóa.
            cancellationToken.ThrowIfCancellationRequested();
            Execute(drawAction, autoRegen, undoMarkName);
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

            // Materialize collection để tránh multiple enumeration và lấy count cho progress
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