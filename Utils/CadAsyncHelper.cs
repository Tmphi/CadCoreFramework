using System;
using System.Threading;
using System.Threading.Tasks;

namespace CadCoreFramework.Utils
{
    /// <summary>
    /// Tiện ích hỗ trợ xử lý luồng (Threading) và Bất đồng bộ (Async) an toàn trong AutoCAD.
    /// Cho phép chạy các tác vụ nặng (như I/O mạng, API, truy xuất CSDL) dưới Background Thread,
    /// sau đó đưa kết quả về Main Thread của AutoCAD để cập nhật giao diện hoặc bản vẽ.
    ///
    /// QUY TẮC VÀNG:
    /// - AutoCAD API KHÔNG thread-safe. Mọi lời gọi Application/Document/Database/Editor
    ///   PHẢI chạy trên Main Thread của AutoCAD.
    /// - Các hàm ở đây PHẢI được gọi từ Main Thread (command handler, palette event handler...)
    ///   để có thể bắt được SynchronizationContext của AutoCAD và quay về đúng luồng.
    /// - KHÔNG dùng ConfigureAwait(false) trước khi gọi AutoCAD API — nó làm mất context
    ///   và khiến code phía sau chạy trên ThreadPool → crash ngẫu nhiên.
    /// </summary>
    public static class CadAsyncHelper
    {
        /// <summary>
        /// Thực thi một tác vụ ở Background Thread (Task.Run), sau đó gọi callback trên Main Thread
        /// của AutoCAD (thông qua SynchronizationContext đã capture trước khi chuyển luồng).
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu trả về từ tác vụ nền.</typeparam>
        /// <param name="backgroundTask">Hàm thực thi tác vụ nặng dưới nền. TUYỆT ĐỐI không gọi AutoCAD API ở đây.</param>
        /// <param name="mainThreadAction">Hành động được gọi trên Main Thread của AutoCAD khi tác vụ nền hoàn tất.</param>
        /// <exception cref="InvalidOperationException">Khi được gọi từ luồng không có SynchronizationContext (không phải Main Thread).</exception>
        public static async Task ExecuteSafeAsync<T>(Func<Task<T>> backgroundTask, Action<T> mainThreadAction)
        {
            if (backgroundTask == null) throw new ArgumentNullException(nameof(backgroundTask));
            if (mainThreadAction == null) throw new ArgumentNullException(nameof(mainThreadAction));

            // 1. Capture context của Main Thread TRƯỚC khi chuyển sang ThreadPool.
            SynchronizationContext mainContext = RequireMainThreadContext();

            // 2. Chạy trên ThreadPool (không khóa giao diện AutoCAD).
            T result = await Task.Run(backgroundTask).ConfigureAwait(false);

            // 3. Marshal kết quả về Main Thread một cách tường minh.
            await PostAsync(mainContext, () => mainThreadAction(result)).ConfigureAwait(false);
        }

        /// <summary>
        /// Thực thi một tác vụ ngầm không trả về dữ liệu (void), sau đó cập nhật trên Main Thread.
        /// </summary>
        public static Task ExecuteSafeAsync(Func<Task> backgroundTask, Action mainThreadAction)
        {
            if (backgroundTask == null) throw new ArgumentNullException(nameof(backgroundTask));
            if (mainThreadAction == null) throw new ArgumentNullException(nameof(mainThreadAction));

            return ExecuteSafeAsync<bool>(
                async () => { await backgroundTask().ConfigureAwait(false); return true; },
                _ => mainThreadAction());
        }

        /// <summary>
        /// Chạy một Task theo kiểu "fire-and-forget" một cách an toàn: mọi exception đều được
        /// chuyển tới <paramref name="onError"/> thay vì bị nuốt mất (hoặc làm sập tiến trình).
        /// Dùng trong [CommandMethod] khi không thể/không muốn await.
        /// </summary>
        /// <param name="task">Task cần theo dõi.</param>
        /// <param name="onError">Callback xử lý lỗi (ví dụ: ghi log + WriteMessage). Được gọi trên context hiện hành.</param>
        public static async void FireAndForget(this Task task, Action<Exception> onError)
        {
            // 'async void' là CỐ Ý và an toàn ở đây vì toàn bộ thân hàm được bọc try/catch.
            if (task == null) return;
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                try { onError?.Invoke(ex); }
                catch { /* Không để callback lỗi làm sập AutoCAD */ }
            }
        }

        // ─── Internal ─────────────────────────────────────────────────────────────

        private static SynchronizationContext RequireMainThreadContext()
        {
            SynchronizationContext context = SynchronizationContext.Current;
            if (context == null)
            {
                throw new InvalidOperationException(
                    "CadAsyncHelper phải được gọi từ Main Thread của AutoCAD " +
                    "(command handler hoặc event handler của Palette).");
            }
            return context;
        }

        private static Task PostAsync(SynchronizationContext context, Action action)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            context.Post(_ =>
            {
                try
                {
                    action();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }, null);
            return tcs.Task;
        }
    }
}
