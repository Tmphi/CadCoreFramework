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

                // Lazy upgrade: CurrentSpace is opened ForRead by Execute; promote to ForWrite
                // only now that we actually need to write. UpgradeOpen is idempotent — calling it
                // when already open ForWrite is a no-op.
                if (!CurrentSpace.IsWriteEnabled)
                    CurrentSpace.UpgradeOpen();

                ObjectId entityId = CurrentSpace.AppendEntity(entity);
                Transaction.AddNewlyCreatedDBObject(entity, true);
                return entityId;
            }
        }

        private const string NoActiveDocumentMessage = "Không tìm thấy bản vẽ AutoCAD nào đang hoạt động.";

        private readonly ILoggerService? _logger;

        /// <param name="logger">Ghi lại lỗi COM UndoMark (không làm hỏng lệnh, nhưng Ctrl+Z sẽ không gom bước).</param>
        public CadContext(ILoggerService? logger = null)
        {
            _logger = logger;
        }

        // ─── Private core — receives an already-resolved Document ──────────────
        // ExecuteAsync captures the Document before await so the active document
        // cannot change between the async preparation and the actual draw phase.
        // MỘT core duy nhất cho mọi overload ghi: luôn throw khi lỗi; các biến thể
        // Try* bọc nó và chuyển exception thành CadResult.

        private T ExecuteCore<T>(Document doc, Func<ICadScope, T> cadFunc, bool autoRegen, string? undoMarkName)
        {
            CadThread.EnsureMainThread("CadContext.Execute");
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
                        catch (Exception ex)
                        {
                            comDoc = null;
                            _logger?.Warning($"StartUndoMark '{undoMarkName}' thất bại — thao tác vẫn chạy nhưng không gom thành 1 bước Undo: {ex.Message}");
                        }
                    }

                    try
                    {
                        T result;
                        using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                        {
                            // Open ForRead by default — AppendEntityToCurrentSpace calls UpgradeOpen()
                            // only when the caller actually writes an entity. Avoids holding a write
                            // lock on CurrentSpace during pure-read passes inside Execute.
                            BlockTableRecord currentSpace = (BlockTableRecord)tr.GetObject(
                                doc.Database.CurrentSpaceId, OpenMode.ForRead);
                            var scope = new CadScope(doc, tr, currentSpace, false);
                            result = cadFunc(scope);
                            tr.Commit();
                        }

                        if (autoRegen) doc.Editor.Regen();
                        return result;
                    }
                    finally
                    {
                        if (comDoc != null)
                        {
                            try { comDoc.EndUndoMark(); }
                            catch (Exception ex) { _logger?.Warning($"EndUndoMark '{undoMarkName}' thất bại: {ex.Message}"); }
                        }
                    }
                }
            }
            finally { CadContextGuard.Exit(); }
        }

        private void ExecuteCore(Document doc, Action<ICadScope> cadAction, bool autoRegen, string? undoMarkName)
            => ExecuteCore<object?>(doc, scope => { cadAction(scope); return null; }, autoRegen, undoMarkName);

        private static Document RequireActiveDocument()
        {
            // Điểm vào chung của mọi Execute*/ExecuteRead/ExecuteAsync — kiểm thread TRƯỚC khi chạm MdiActiveDocument.
            CadThread.EnsureMainThread("CadContext");
            return Application.DocumentManager.MdiActiveDocument
                   ?? throw new InvalidOperationException(NoActiveDocumentMessage);
        }

        // ─── Public API ─────────────────────────────────────────────────────────
        // QUY ƯỚC: Execute* ném exception khi lỗi · TryExecute* trả CadResult, không bao giờ throw.

        /// <summary>
        /// Thực thi một hành động ghi đồng bộ an toàn bên trong tài liệu được khóa và transaction.
        /// Tự động khóa bản vẽ (LockDocument), mở transaction và commit khi thành công.
        /// Nếu undoMarkName được chỉ định, gom toàn bộ thao tác thành 1 bước Ctrl+Z.
        /// Ném exception nếu có lỗi — caller chịu trách nhiệm hiển thị và xử lý.
        /// </summary>
        public void Execute(Action<ICadScope> cadAction, bool autoRegen = false, string? undoMarkName = null)
        {
            if (cadAction == null) throw new ArgumentNullException(nameof(cadAction));
            ExecuteCore(RequireActiveDocument(), cadAction, autoRegen, undoMarkName);
        }

        /// <inheritdoc/>
        [Obsolete("Dùng TryExecute<T>() — cùng hành vi, tên thể hiện rõ việc trả CadResult thay vì throw.")]
        public CadResult<T> Execute<T>(Func<ICadScope, T> cadFunc, bool autoRegen = false, string? undoMarkName = null)
            => TryExecute(cadFunc, autoRegen, undoMarkName);

        /// <summary>
        /// Như <see cref="Execute(Action{ICadScope}, bool, string)"/> nhưng KHÔNG throw:
        /// mọi lỗi (kể cả không có bản vẽ) được trả về dưới dạng <see cref="CadResult"/>.
        /// </summary>
        public CadResult TryExecute(Action<ICadScope> cadAction, bool autoRegen = false, string? undoMarkName = null)
        {
            if (cadAction == null) throw new ArgumentNullException(nameof(cadAction));
            try
            {
                ExecuteCore(RequireActiveDocument(), cadAction, autoRegen, undoMarkName);
                return CadResult.Ok();
            }
            catch (Exception ex)
            {
                return CadResult.Fail(ex.Message, ex);
            }
        }

        /// <summary>
        /// Thực thi một hành động ghi có giá trị trả về, KHÔNG throw:
        /// trả CadResult&lt;T&gt; thành công kèm giá trị, hoặc thất bại kèm thông điệp + exception.
        /// </summary>
        public CadResult<T> TryExecute<T>(Func<ICadScope, T> cadFunc, bool autoRegen = false, string? undoMarkName = null)
        {
            if (cadFunc == null) throw new ArgumentNullException(nameof(cadFunc));
            try
            {
                return CadResult<T>.Success(ExecuteCore(RequireActiveDocument(), cadFunc, autoRegen, undoMarkName));
            }
            catch (Exception ex)
            {
                return CadResult<T>.Fail(ex.Message, ex);
            }
        }

        /// <summary>
        /// Thực thi một hành động chỉ đọc (read-only) đồng bộ.
        /// Lock document CHỈ khi đang ở application context (Palette, modeless dialog) — nơi AutoCAD
        /// không tự lock. Trong command context, document đã được command lock sẵn → bỏ qua.
        /// KHÔNG cho phép gọi AppendEntityToCurrentSpace / SetXRecord / RemoveXRecord bên trong scope này.
        /// </summary>
        /// <remarks>
        /// "Read-only" là HỢP ĐỒNG, chỉ được chặn một phần lúc chạy: transaction kết thúc bằng Commit
        /// (rẻ hơn Abort), nên nếu lambda tự <c>GetObject(…, OpenMode.ForWrite)</c> / <c>UpgradeOpen()</c> rồi sửa,
        /// thay đổi SẼ được ghi. Analyzer CAD003 báo lỗi các lời gọi đó ngay lúc build.
        /// </remarks>
        public void ExecuteRead(Action<ICadScope> readAction)
        {
            if (readAction == null) throw new ArgumentNullException(nameof(readAction));
            Document doc = RequireActiveDocument();

            CadContextGuard.Enter();
            try
            {
                DocumentLock? docLock = Application.DocumentManager.IsApplicationContext
                    ? doc.LockDocument()
                    : null;

                using (docLock)
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
            string? undoMarkName = null,
            CancellationToken cancellationToken = default)
        {
            if (asyncPreparation == null) throw new ArgumentNullException(nameof(asyncPreparation));

            // Capture document before the async gap — user may switch drawings during await
            Document doc = RequireActiveDocument();

            // Không có SynchronizationContext → phần sau await chạy trên ThreadPool (trừ khi task xong đồng bộ).
            // Báo lỗi NGAY, trước khi làm việc nặng, thay vì để kết quả phụ thuộc thời điểm.
            if (SynchronizationContext.Current == null)
                throw new InvalidOperationException(
                    "CadContext.ExecuteAsync cần SynchronizationContext của main thread (gọi từ command handler hoặc " +
                    "event handler của Palette, không đi qua ConfigureAwait(false)) — nếu không, phần vẽ sau await " +
                    "sẽ chạy trên thread nền.");

            cancellationToken.ThrowIfCancellationRequested();
            Action<ICadScope> drawAction = await asyncPreparation();
            cancellationToken.ThrowIfCancellationRequested();

            // Phần tiếp theo của await chỉ quay về main thread nếu lúc gọi có SynchronizationContext.
            // Không có → đang ở ThreadPool: dừng ở đây với thông điệp rõ thay vì vẽ từ thread nền.
            CadThread.EnsureMainThread("CadContext.ExecuteAsync (sau await)");

            // User có thể đã đóng bản vẽ trong lúc await — LockDocument() trên Document đã dispose
            // sẽ gây Fatal Error, nên phải kiểm tra trước khi vào giai đoạn vẽ.
            if (doc.IsDisposed)
                throw new InvalidOperationException("Bản vẽ đã bị đóng trong lúc chuẩn bị dữ liệu — hủy thao tác vẽ.");

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
            IProgress<int>? progress = null,
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
