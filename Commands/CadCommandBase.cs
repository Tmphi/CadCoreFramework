using System;
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
        // Khi dùng Constructor Injection, _injectedLogger/_injectedCadContext được gán trực tiếp.
        // Khi dùng parameterless constructor, Lazy<T> resolve từ static ServiceProvider.

        private readonly ILoggerService _injectedLogger;
        private readonly ICadContext _injectedCadContext;

        private readonly Lazy<ILoggerService> _loggerLazy =
            new Lazy<ILoggerService>(() => AppBootstrapper.ServiceProvider?.GetService<ILoggerService>());
        private readonly Lazy<ICadContext> _cadContextLazy =
            new Lazy<ICadContext>(() => AppBootstrapper.ServiceProvider?.GetService<ICadContext>());

        /// <summary>Dịch vụ ghi log của plugin.</summary>
        protected ILoggerService Logger => _injectedLogger ?? _loggerLazy.Value;

        /// <summary>Phiên làm việc và điều phối transaction AutoCAD an toàn.</summary>
        protected ICadContext CadContext => _injectedCadContext ?? _cadContextLazy.Value;

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
        /// Bypass hoàn toàn static ServiceProvider, cải thiện testability.
        /// </summary>
        /// <param name="logger">Dịch vụ ghi log.</param>
        /// <param name="cadContext">Dịch vụ quản lý transaction AutoCAD.</param>
        protected CadCommandBase(ILoggerService logger, ICadContext cadContext)
        {
            _injectedLogger = logger;
            _injectedCadContext = cadContext;
        }

        /// <summary>
        /// Chạy một khối lệnh đồng bộ với bộ lọc bẫy lỗi (try-catch) tích hợp.
        /// Bất cứ lỗi nào ném ra sẽ được in màu chữ đỏ lên màn hình CAD và lưu vào Log.
        /// </summary>
        protected void ExecuteCommand(Action action)
        {
            if (Document == null)
            {
                Logger?.Warning("Lệnh được gọi nhưng không có bản vẽ nào đang mở.");
                return;
            }

            try
            {
                action();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception acEx)
            {
                if (acEx.ErrorStatus == Autodesk.AutoCAD.Runtime.ErrorStatus.UserBreak || acEx.ErrorStatus == Autodesk.AutoCAD.Runtime.ErrorStatus.OK)
                    return; // Người dùng ấn Esc hoặc hủy lệnh, bỏ qua không in lỗi

                string msg = $"\n[Lỗi AutoCAD] {acEx.ErrorStatus}: {acEx.Message}";
                Editor?.WriteMessage(msg);
                Logger?.Error("Ngoại lệ AutoCAD khi thực thi lệnh.", acEx);
            }
            catch (System.Exception ex)
            {
                string msg = $"\n[Lỗi Hệ Thống]: {ex.Message}";
                Editor?.WriteMessage(msg);
                Logger?.Error("Ngoại lệ hệ thống khi thực thi lệnh.", ex);
            }
        }

        /// <summary>
        /// Cú pháp viết tắt (Shortcut) cực kỳ hữu ích: Tự động khóa bản vẽ, mở Transaction,
        /// chạy khối lệnh vẽ, tự động bắt lỗi và commit nếu thành công.
        /// Thay vì viết: ExecuteCommand(() => CadContext.Execute(scope => { ... }))
        /// Dùng lệnh này: ExecuteWithScope(scope => { scope.DrawLine(...); })
        /// </summary>
        protected void ExecuteWithScope(Action<ICadScope> scopeAction, string undoMarkName = null)
        {
            ExecuteCommand(() => CadContext?.Execute(scopeAction, false, undoMarkName));
        }

        /// <summary>
        /// Biến thể Async (bất đồng bộ): Tải dữ liệu từ mạng hoặc tính toán nặng ở ngoài Transaction, 
        /// sau khi có dữ liệu mới khóa bản vẽ và vẽ bên trong Transaction.
        /// Cách dùng: await ExecuteWithScopeAsync(async () => { var data = await api.Get(); return scope => scope.DrawText(data); });
        /// </summary>
        protected async Task ExecuteWithScopeAsync(Func<Task<Action<ICadScope>>> asyncPreparation, string undoMarkName = null)
        {
            if (Document == null) return;
            try
            {
                if (CadContext != null)
                    await CadContext.ExecuteAsync(asyncPreparation, false, undoMarkName);
            }
            catch (Autodesk.AutoCAD.Runtime.Exception acEx)
            {
                if (acEx.ErrorStatus == Autodesk.AutoCAD.Runtime.ErrorStatus.UserBreak || acEx.ErrorStatus == Autodesk.AutoCAD.Runtime.ErrorStatus.OK)
                    return;

                Editor?.WriteMessage($"\n[Lỗi AutoCAD] {acEx.ErrorStatus}: {acEx.Message}");
                Logger?.Error("Ngoại lệ AutoCAD trong lệnh async.", acEx);
            }
            catch (System.Exception ex)
            {
                Editor?.WriteMessage($"\n[Lỗi Hệ Thống]: {ex.Message}");
                Logger?.Error("Ngoại lệ hệ thống trong lệnh async.", ex);
            }
        }
    }
}
