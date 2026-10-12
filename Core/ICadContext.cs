using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CadCoreFramework.Core
{
    /// <summary>
    /// Giao diện quản lý phiên làm việc với bản vẽ AutoCAD.
    /// Cho phép tiêm (Inject) qua Dependency Injection thay vì gọi trực tiếp API AutoCAD.
    ///
    /// TRIẾT LÝ AUTODESK (.NET Developer's Guide):
    /// - Mọi thao tác GHI vào database phải nằm trong StartTransaction() và LockDocument().
    /// - Thao tác ĐỌC từ Command context (không phải từ event handler hay UI thread riêng)
    ///   không cần LockDocument, nhưng vẫn cần StartTransaction() để GetObject() hoạt động.
    /// - KHÔNG bao giờ commit transaction từ event handler của database — chỉ commit trong
    ///   code được gọi trực tiếp từ command handler.
    /// </summary>
    public interface ICadContext
    {
        /// <summary>
        /// Chạy một khối lệnh đồng bộ an toàn (tự động Lock Document và mở Transaction).
        /// Dùng cho các thao tác GHI vào database.
        /// </summary>
        /// <param name="cadAction">Khối lệnh cần thực thi, nhận vào một ICadScope.</param>
        /// <param name="autoRegen">
        /// Nếu true, tự động gọi Editor.Regen() sau khi commit.
        /// Mặc định FALSE để tránh lãng phí khi vẽ hàng loạt.
        /// Chỉ bật khi vẽ Hatch, Wipeout, Table hoặc các entity yêu cầu Regen hiển thị đúng.
        /// </param>
        /// <param name="undoMarkName">
        /// Nếu được chỉ định (khác null/empty), toàn bộ thao tác vẽ bên trong sẽ được gom
        /// thành MỘT bước Ctrl+Z duy nhất bằng cách gọi COM interop StartUndoMark / EndUndoMark.
        /// Nếu để null, bên trong một command AutoCAD vẫn gom toàn bộ transaction thành 1 bước Undo —
        /// tham số này hữu ích nhất khi gọi từ Palette (application context).
        /// </param>
        /// <remarks>
        /// QUY ƯỚC XỬ LÝ LỖI của ICadContext:
        ///   • Execute / ExecuteRead / ExecuteAsync / ExecuteBatch → NÉM exception khi lỗi.
        ///   • TryExecute → KHÔNG throw, trả <see cref="CadResult"/> / <see cref="CadResult{T}"/>.
        /// Dùng Execute trong Command (CadCommandBase bắt lỗi giúp); dùng TryExecute trong Service
        /// khi muốn lỗi AutoCAD trở thành giá trị cho caller quyết định.
        /// </remarks>
        void Execute(Action<ICadScope> cadAction, bool autoRegen = false, string? undoMarkName = null);

        /// <summary>
        /// Biến thể KHÔNG throw của <see cref="Execute(Action{ICadScope}, bool, string)"/>.
        /// Mọi lỗi (kể cả không có bản vẽ đang mở) được trả về trong CadResult.
        /// </summary>
        CadResult TryExecute(Action<ICadScope> cadAction, bool autoRegen = false, string? undoMarkName = null);

        /// <summary>
        /// Chạy một khối lệnh ghi có giá trị trả về, KHÔNG throw.
        /// Tự động Lock Document, mở Transaction, và trả về CadResult&lt;T&gt;.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu kết quả trả về.</typeparam>
        /// <param name="cadFunc">Hàm cần thực thi, nhận ICadScope và trả về T.</param>
        /// <param name="autoRegen">Nếu true, tự động Regen sau khi commit. Mặc định FALSE.</param>
        /// <param name="undoMarkName">Nếu chỉ định, gom toàn bộ thao tác thành 1 bước Ctrl+Z.</param>
        CadResult<T> TryExecute<T>(Func<ICadScope, T> cadFunc, bool autoRegen = false, string? undoMarkName = null);

        /// <summary>
        /// Tên cũ của <see cref="TryExecute{T}"/> — trả CadResult dù tên là "Execute", trái quy ước.
        /// </summary>
        [Obsolete("Dùng TryExecute<T>() — cùng hành vi, tên thể hiện rõ việc trả CadResult thay vì throw.")]
        CadResult<T> Execute<T>(Func<ICadScope, T> cadFunc, bool autoRegen = false, string? undoMarkName = null);

        /// <summary>
        /// Chạy một khối lệnh ĐỌC đồng bộ an toàn.
        /// Chỉ lock document khi gọi từ application context (Palette/modeless); trong command
        /// context document đã được lock sẵn.
        /// KHÔNG cho phép gọi AppendEntityToCurrentSpace bên trong scope này.
        /// </summary>
        /// <param name="readAction">Khối lệnh đọc dữ liệu, nhận vào một ICadScope (IsReadOnly = true).</param>
        void ExecuteRead(Action<ICadScope> readAction);

        /// <summary>
        /// Chạy một khối lệnh bất đồng bộ (tải dữ liệu không lock UI, sau đó vẽ đồng bộ vào CAD).
        /// Chia 2 giai đoạn: (1) tính toán/gọi API bên ngoài → (2) vẽ vào database trong transaction.
        /// </summary>
        /// <param name="asyncPreparation">Hàm async trả về một Action vẽ sẽ chạy trong Transaction.</param>
        /// <param name="autoRegen">Nếu true, tự động Regen sau khi commit. Mặc định FALSE.</param>
        /// <param name="undoMarkName">Nếu chỉ định, gom toàn bộ thao tác thành 1 bước Ctrl+Z.</param>
        /// <param name="cancellationToken">Token cho phép hủy giai đoạn chuẩn bị async.</param>
        Task ExecuteAsync(
            Func<Task<Action<ICadScope>>> asyncPreparation,
            bool autoRegen = false,
            string? undoMarkName = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Thực hiện vẽ hàng loạt (batch) nhiều items trong MỘT transaction duy nhất.
        /// Toàn bộ batch được gom thành 1 bước Ctrl+Z.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu của mỗi phần tử trong batch.</typeparam>
        /// <param name="items">Danh sách các phần tử cần xử lý.</param>
        /// <param name="drawAction">Hành động vẽ cho từng phần tử.</param>
        /// <param name="progress">Callback báo cáo tiến trình (số phần tử đã xử lý).</param>
        /// <param name="undoMarkName">Tên hiển thị cho bước Undo gộp. Mặc định "Batch".</param>
        void ExecuteBatch<T>(
            IEnumerable<T> items,
            Action<ICadScope, T> drawAction,
            IProgress<int>? progress = null,
            string undoMarkName = "Batch");
    }
}
