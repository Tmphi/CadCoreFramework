using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CadCoreFramework.Core;

namespace CadCoreFramework.Testing.Fakes
{
    /// <summary>
    /// Test double cho ICadContext. Không cần AutoCAD runtime.
    ///
    /// TÍNH NĂNG:
    /// - ExecuteCount / ExecuteReadCount: đếm số lần mỗi overload được gọi.
    /// - LastUndoMarkName / LastAutoRegen: kiểm tra tham số truyền vào.
    /// - LastScope: FakeCadScope được tạo trong lần Execute gần nhất.
    /// - ExceptionToThrow: chèn exception để test error-handling path.
    /// - ExecuteAction: set false để bỏ qua lambda (chỉ test control flow).
    ///
    /// VÍ DỤ:
    /// <code>
    /// var ctx = new FakeCadContext();
    /// var svc = new MyDrawingService(ctx);
    /// svc.Draw("panel", "E_LAYER");
    /// Assert.Equal(1, ctx.ExecuteCount);
    /// Assert.Equal("Vẽ Panel", ctx.LastUndoMarkName);
    /// </code>
    /// </summary>
    public class FakeCadContext : ICadContext
    {
        private int _executeCount;
        private int _executeReadCount;
        private int _executeAsyncCount;
        private int _executeBatchCount;

        /// <summary>Số lần Execute() (cả generic và void) được gọi.</summary>
        public int ExecuteCount => _executeCount;

        /// <summary>Số lần ExecuteRead() được gọi.</summary>
        public int ExecuteReadCount => _executeReadCount;

        /// <summary>Số lần ExecuteAsync() được gọi.</summary>
        public int ExecuteAsyncCount => _executeAsyncCount;

        /// <summary>Số lần ExecuteBatch() được gọi.</summary>
        public int ExecuteBatchCount => _executeBatchCount;

        /// <summary>undoMarkName truyền vào trong lần Execute gần nhất.</summary>
        public string? LastUndoMarkName { get; private set; }

        /// <summary>autoRegen truyền vào trong lần Execute gần nhất.</summary>
        public bool LastAutoRegen { get; private set; }

        /// <summary>FakeCadScope được tạo trong lần Execute gần nhất (null nếu ExecuteAction = false).</summary>
        public FakeCadScope? LastScope { get; private set; }

        /// <summary>
        /// Nếu khác null, Execute sẽ ném exception này thay vì chạy lambda.
        /// Dùng để kiểm tra error-handling path trong Service.
        /// </summary>
        public Exception? ExceptionToThrow { get; set; }

        /// <summary>
        /// Nếu false, lambda KHÔNG được thực thi (chỉ ghi nhận call metadata).
        /// Hữu ích khi lambda gọi drawing extensions mà cần database thật.
        /// Mặc định: true (thực thi lambda).
        /// </summary>
        public bool ExecuteAction { get; set; } = true;

        // ─── ICadContext ──────────────────────────────────────────────────────────

        public void Execute(Action<ICadScope> cadAction, bool autoRegen = false, string? undoMarkName = null)
        {
            _executeCount++;
            LastAutoRegen = autoRegen;
            LastUndoMarkName = undoMarkName;

            if (ExceptionToThrow != null) throw ExceptionToThrow;
            if (!ExecuteAction) return;

            LastScope = new FakeCadScope();
            cadAction(LastScope);
        }

        public CadResult<T> Execute<T>(Func<ICadScope, T> cadFunc, bool autoRegen = false, string? undoMarkName = null)
        {
            _executeCount++;
            LastAutoRegen = autoRegen;
            LastUndoMarkName = undoMarkName;

            if (ExceptionToThrow != null)
                return CadResult<T>.Fail(ExceptionToThrow.Message, ExceptionToThrow);
            if (!ExecuteAction)
                return CadResult<T>.Success(default!);

            LastScope = new FakeCadScope();
            var value = cadFunc(LastScope);
            return CadResult<T>.Success(value);
        }

        public void ExecuteRead(Action<ICadScope> readAction)
        {
            _executeReadCount++;

            if (ExceptionToThrow != null) throw ExceptionToThrow;
            if (!ExecuteAction) return;

            var scope = new FakeCadScope();
            readAction(scope);
        }

        public async Task ExecuteAsync(
            Func<Task<Action<ICadScope>>> asyncPreparation,
            bool autoRegen = false,
            string? undoMarkName = null,
            CancellationToken cancellationToken = default)
        {
            _executeAsyncCount++;
            LastAutoRegen = autoRegen;
            LastUndoMarkName = undoMarkName;

            if (ExceptionToThrow != null) throw ExceptionToThrow;
            if (!ExecuteAction) return;

            var drawAction = await asyncPreparation();
            LastScope = new FakeCadScope();
            drawAction(LastScope);
        }

        public void ExecuteBatch<T>(
            IEnumerable<T> items,
            Action<ICadScope, T> drawAction,
            IProgress<int>? progress = null,
            string undoMarkName = "Batch")
        {
            _executeBatchCount++;
            LastUndoMarkName = undoMarkName;

            if (ExceptionToThrow != null) throw ExceptionToThrow;
            if (!ExecuteAction) return;

            LastScope = new FakeCadScope();
            int i = 0;
            foreach (var item in items)
            {
                drawAction(LastScope, item);
                progress?.Report(++i);
            }
        }
    }
}
