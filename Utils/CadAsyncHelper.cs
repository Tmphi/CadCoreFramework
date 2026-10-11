using System;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace CadCoreFramework.Utils
{
    /// <summary>
    /// Tiện ích hỗ trợ xử lý luồng (Threading) và Bất đồng bộ (Async) an toàn trong AutoCAD.
    /// Cho phép chạy các tác vụ nặng (như I/O mạng, API, truy xuất CSDL) dưới Background Thread,
    /// sau đó đưa kết quả về Main Thread của AutoCAD để cập nhật giao diện hoặc bản vẽ.
    /// </summary>
    public static class CadAsyncHelper
    {
        /// <summary>
        /// Thực thi một tác vụ ở Background Thread (Task.Run), sau đó gọi một callback trên Main Thread
        /// của AutoCAD thông qua ExecuteInCommandContextAsync để đảm bảo an toàn truy xuất Document.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu trả về từ tác vụ nền.</typeparam>
        /// <param name="backgroundTask">Hàm thực thi tác vụ nặng dưới nền.</param>
        /// <param name="mainThreadAction">Hành động được gọi trên Main Thread của AutoCAD khi tác vụ nền hoàn tất.</param>
        public static async Task ExecuteSafeAsync<T>(Func<Task<T>> backgroundTask, Action<T> mainThreadAction)
        {
            if (backgroundTask == null) throw new ArgumentNullException(nameof(backgroundTask));
            if (mainThreadAction == null) throw new ArgumentNullException(nameof(mainThreadAction));

            // 1. Chạy trên ThreadPool (ngăn khóa luồng giao diện)
            T result = await Task.Run(backgroundTask).ConfigureAwait(false);

            // 2. Marshal kết quả về Main Thread
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc != null)
            {
                // Chạy an toàn thông qua execution context của AutoCAD
                await Application.DocumentManager.ExecuteInCommandContextAsync(
                    async (obj) =>
                    {
                        mainThreadAction(result);
                        await Task.CompletedTask;
                    }, 
                    null);
            }
            else if (System.Windows.Application.Current != null)
            {
                // Fallback cho luồng WPF nếu không có document
                System.Windows.Application.Current.Dispatcher.Invoke(() => mainThreadAction(result));
            }
            else
            {
                mainThreadAction(result);
            }
        }

        /// <summary>
        /// Thực thi một tác vụ ngầm không trả về dữ liệu (void), sau đó cập nhật trên Main Thread.
        /// </summary>
        public static async Task ExecuteSafeAsync(Func<Task> backgroundTask, Action mainThreadAction)
        {
            if (backgroundTask == null) throw new ArgumentNullException(nameof(backgroundTask));
            if (mainThreadAction == null) throw new ArgumentNullException(nameof(mainThreadAction));

            await Task.Run(backgroundTask).ConfigureAwait(false);

            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc != null)
            {
                await Application.DocumentManager.ExecuteInCommandContextAsync(
                    async (obj) =>
                    {
                        mainThreadAction();
                        await Task.CompletedTask;
                    }, 
                    null);
            }
            else if (System.Windows.Application.Current != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => mainThreadAction());
            }
            else
            {
                mainThreadAction();
            }
        }
    }
}
