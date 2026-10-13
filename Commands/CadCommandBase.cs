using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using CadCoreFramework.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CadCoreFramework.Commands
{
    /// <summary>
    /// Lớp cơ sở (Base Class) cho tất cả các lệnh AutoCAD (Command).
    /// Mục đích: Tập trung toàn bộ logic bắt lỗi (try-catch) và cung cấp sẵn các Service từ DI,
    /// giúp code lệnh ngắn gọn và không bị crash văng AutoCAD khi có lỗi bất ngờ.
    ///
    /// HỖ TRỢ HAI CÁCH KHỞI TẠO:
    /// 1. Parameterless constructor: Backward-compatible, resolve services từ ServiceProvider static.
    /// 2. Constructor injection: Inject trực tiếp ILoggerService và ICadContext cho testability tốt hơn.
    /// </summary>
    public abstract class CadCommandBase
    {
        // ── Service Resolution ──────────────────────────────────────────────────────
        // Hỗ trợ cả hai pattern: Service Locator (backward-compatible) và Constructor Injection.

        private readonly ILoggerService _injectedLogger;
        private readonly ICadContext _injectedCadContext;
        private readonly ICommandHistoryService _injectedHistory;

        private readonly Lazy<ILoggerService> _loggerLazy =
            new Lazy<ILoggerService>(() => AppBootstrapper.ServiceProvider?.GetService<ILoggerService>());
        private readonly Lazy<ICadContext> _cadContextLazy =
            new Lazy<ICadContext>(() => AppBootstrapper.ServiceProvider?.GetService<ICadContext>());
        private readonly Lazy<ICommandHistoryService> _historyLazy =
            new Lazy<ICommandHistoryService>(() => AppBootstrapper.ServiceProvider?.GetService<ICommandHistoryService>());

        /// <summary>Dịch vụ ghi log của plugin.</summary>
        protected ILoggerService Logger => _injectedLogger ?? _loggerLazy.Value;

        /// <summary>
        /// Phiên làm việc và điều phối transaction AutoCAD an toàn.
        /// Ném InvalidOperationException nếu DI chưa sẵn sàng — dùng bên trong ExecuteCommand/ExecuteWithScope.
        /// </summary>
        protected ICadContext CadContext => _injectedCadContext ?? _cadContextLazy.Value
            ?? throw new InvalidOperationException(
                "ICadContext chưa được khởi tạo — kiểm tra lỗi khởi động plugin (AppBootstrapper.Initialize).");

        /// <summary>Lịch sử lệnh — tự động ghi lại mỗi ExecuteWithScope / ExecuteCommand.</summary>
        protected ICommandHistoryService CommandHistory => _injectedHistory ?? _historyLazy.Value;

        /// <summary>Bản vẽ AutoCAD đang được kích hoạt (Active Document).</summary>
        protected Document Document => Application.DocumentManager.MdiActiveDocument;

        /// <summary>Cơ sở dữ liệu của bản vẽ đang kích hoạt.</summary>
        protected Database Database => Document?.Database;

        /// <summary>Trình soạn thảo và tương tác người dùng của bản vẽ đang kích hoạt.</summary>
        protected Editor Editor => Document?.Editor;

        /// <summary>
        /// Parameterless constructor — backward-compatible.
        /// Services được resolve qua static ServiceProvider khi lần đầu truy cập.
        /// </summary>
        protected CadCommandBase() { }

        /// <summary>
        /// Constructor Injection — khuyến khích dùng cho code mới và unit test.
        /// Bypass static ServiceProvider cho cả ba service (logger, CadContext, CommandHistory),
        /// cải thiện testability. <paramref name="commandHistory"/> null → không ghi lịch sử
        /// trừ khi DI có sẵn (fallback về ServiceProvider như constructor không tham số).
        /// </summary>
        protected CadCommandBase(ILoggerService logger, ICadContext cadContext,
            ICommandHistoryService? commandHistory = null)
        {
            _injectedLogger = logger;
            _injectedCadContext = cadContext;
            _injectedHistory = commandHistory;
        }

        // ── Execution Helpers ───────────────────────────────────────────────────────

        /// <summary>
        /// Chạy một khối lệnh đồng bộ với bộ lọc bẫy lỗi (try-catch) tích hợp.
        /// Bất cứ lỗi nào ném ra sẽ được in màu chữ đỏ lên màn hình CAD và lưu vào Log.
        /// Tự động ghi kết quả vào CommandHistory (dùng tên method gọi lệnh qua CallerMemberName).
        ///
        /// Trả về true nếu thành công, false nếu có lỗi.
        /// </summary>
        protected bool ExecuteCommand(Action action, [CallerMemberName] string callerName = null)
        {
            if (Document == null)
            {
                Logger?.Warning("Lệnh được gọi nhưng không có bản vẽ nào đang mở.");
                return false;
            }

            var sw = Stopwatch.StartNew();
            bool success = false;
            bool record = true;
            try
            {
                action();
                success = true;
            }
            catch (Autodesk.AutoCAD.Runtime.Exception acEx)
            {
                if (acEx.ErrorStatus == Autodesk.AutoCAD.Runtime.ErrorStatus.UserBreak ||
                    acEx.ErrorStatus == Autodesk.AutoCAD.Runtime.ErrorStatus.OK)
                {
                    record = false; // user nhấn Esc — không ghi vào history
                }
                else
                {
                    Editor?.WriteMessage($"\n[Lỗi AutoCAD] {acEx.ErrorStatus}: {acEx.Message}");
                    Logger?.Error("Ngoại lệ AutoCAD khi thực thi lệnh.", acEx);
                }
            }
            catch (System.Exception ex)
            {
                Editor?.WriteMessage($"\n[Lỗi Hệ Thống]: {ex.Message}");
                Logger?.Error("Ngoại lệ hệ thống khi thực thi lệnh.", ex);
            }
            finally
            {
                sw.Stop();
                if (record)
                    CommandHistory?.Record(callerName ?? GetType().Name, sw.Elapsed, success);
            }
            return success;
        }

        /// <summary>
        /// Cú pháp viết tắt (Shortcut) cực kỳ hữu ích: Tự động khóa bản vẽ, mở Transaction,
        /// chạy khối lệnh vẽ, tự động bắt lỗi và commit nếu thành công.
        /// Tự động ghi CommandHistory với tên method gọi lệnh và thời gian thực thi.
        ///
        /// Thay vì viết: ExecuteCommand(() => CadContext.Execute(scope => { ... }))
        /// Dùng lệnh này: ExecuteWithScope(scope => { scope.DrawLine(...); })
        /// </summary>
        protected void ExecuteWithScope(Action<ICadScope> scopeAction, string undoMarkName = null,
            [CallerMemberName] string callerName = null)
        {
            ExecuteCommand(() => CadContext.Execute(scopeAction, false, undoMarkName), callerName);
        }

        /// <summary>
        /// Lấy một service từ DI container, ném exception nếu chưa đăng ký hoặc DI chưa sẵn sàng.
        /// Gọi BÊN TRONG ExecuteCommand/ExecuteWithScope để lỗi được bắt, in ra và ghi log —
        /// thay cho pattern <c>ServiceProvider?.GetService&lt;T&gt;()</c> vốn âm thầm trả về null.
        /// </summary>
        protected static T GetRequiredService<T>() where T : class
            => AppBootstrapper.ServiceProvider?.GetService<T>()
               ?? throw new InvalidOperationException(
                   $"Service {typeof(T).Name} chưa được đăng ký hoặc plugin chưa khởi tạo xong (AppBootstrapper.ConfigureServices).");

        /// <summary>
        /// Biến thể Async (bất đồng bộ): Tải dữ liệu từ mạng hoặc tính toán nặng ở ngoài Transaction,
        /// sau khi có dữ liệu mới khóa bản vẽ và vẽ bên trong Transaction.
        /// Tự động ghi CommandHistory với tên method gọi lệnh. Lỗi được bắt, báo người dùng, ghi log.
        /// </summary>
        /// <returns>true nếu giai đoạn vẽ đã chạy xong và commit; false nếu lỗi, bị hủy, hoặc không có bản vẽ.
        /// Dùng để chỉ báo kết quả khi thật sự đã vẽ (xem DEMO_ASYNC_AWAIT).</returns>
        /// <remarks>
        /// Giai đoạn vẽ chạy trên bản vẽ đã chụp lúc gọi — có thể KHÔNG còn active nếu người dùng chuyển
        /// bản vẽ trong lúc chờ: lambda vẽ chỉ được thao tác database, không gọi <c>scope.Editor</c>
        /// (ném eNotApplicable). Báo kết quả sau khi hàm này trả về, qua INotificationService.
        /// </remarks>
        protected async Task<bool> ExecuteWithScopeAsync(
            Func<Task<Action<ICadScope>>> asyncPreparation,
            string undoMarkName = null,
            [CallerMemberName] string callerName = null)
        {
            // Capture Document (và tên của nó) TRƯỚC await: sau khoảng chờ, MdiActiveDocument có thể đã là bản
            // vẽ khác, hoặc bản vẽ gốc đã bị đóng — lỗi vẫn phải đến được người dùng (xem WriteMessageSafe).
            Document doc = Document;
            if (doc == null) return false;
            string docName = System.IO.Path.GetFileName(doc.Name);

            var sw = Stopwatch.StartNew();
            bool success = false;
            bool record = true;
            try
            {
                await CadContext.ExecuteAsync(asyncPreparation, false, undoMarkName);
                success = true;
            }
            catch (Autodesk.AutoCAD.Runtime.Exception acEx)
            {
                if (acEx.ErrorStatus == Autodesk.AutoCAD.Runtime.ErrorStatus.UserBreak ||
                    acEx.ErrorStatus == Autodesk.AutoCAD.Runtime.ErrorStatus.OK)
                {
                    record = false;
                }
                else
                {
                    WriteMessageSafe(doc, docName, $"[Lỗi AutoCAD] {acEx.ErrorStatus}: {acEx.Message}");
                    Logger?.Error("Ngoại lệ AutoCAD trong lệnh async.", acEx);
                }
            }
            catch (System.Exception ex)
            {
                WriteMessageSafe(doc, docName, $"[Lỗi Hệ Thống]: {ex.Message}");
                Logger?.Error("Ngoại lệ hệ thống trong lệnh async.", ex);
            }
            finally
            {
                sw.Stop();
                if (record)
                    CommandHistory?.Record(callerName ?? GetType().Name, sw.Elapsed, success);
            }
            return success;
        }

        /// <summary>
        /// Báo một thông điệp của lệnh async cho người dùng, trên command line của bản vẽ ĐANG ACTIVE.
        /// Không ghi vào Editor của bản vẽ đã chạy lệnh nếu nó không còn active: Editor của bản vẽ không
        /// active ném eNotApplicable (đã kiểm chứng trên AutoCAD 2024 — T03 B31), và bản vẽ đó có thể đã
        /// bị đóng. Khi người dùng đã chuyển bản vẽ, thông điệp được ghi kèm tên bản vẽ gốc.
        /// </summary>
        private static void WriteMessageSafe(Document doc, string docName, string message)
        {
            try
            {
                Document active = Application.DocumentManager.MdiActiveDocument;
                if (active == null) return;
                bool sameDrawing = !doc.IsDisposed && doc == active;
                active.Editor.WriteMessage(sameDrawing ? $"\n{message}" : $"\n[{docName}] {message}");
            }
            catch { /* không để việc báo lỗi gây thêm lỗi */ }
        }
    }
}
