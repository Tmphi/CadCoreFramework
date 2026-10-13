using System;
using Autodesk.AutoCAD.ApplicationServices;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace CadCoreFramework.Core
{
    /// <summary>
    /// Quản lý nhóm lệnh Undo (Undo Grouping).
    /// Hỗ trợ gom cụm nhiều thao tác nhỏ lại thành một bước Undo duy nhất trong lịch sử của AutoCAD.
    /// Sử dụng khối using để đảm bảo EndUndoMark luôn được gọi.
    ///
    /// LƯU Ý:
    /// - BẮT BUỘC dùng trong khối using trên Main Thread. Class này CỐ Ý không có finalizer:
    ///   gọi COM (EndUndoMark) từ GC finalizer thread có thể gây Fatal Error cho AutoCAD.
    /// - Bên trong MỘT command, AutoCAD vốn đã gom mọi transaction thành 1 bước Undo.
    ///   UndoGroupManager hữu ích nhất khi thao tác được khởi chạy từ Palette (application context).
    /// </summary>
    /// <example>
    /// using (var undo = new UndoGroupManager())
    /// {
    ///     _cadContext.Execute(scope => { ... });
    ///     _cadContext.Execute(scope => { ... });
    /// }
    /// </example>
    public class UndoGroupManager : IDisposable
    {
        private readonly Document _doc;
        private readonly dynamic _acadDoc;
        private readonly ILoggerService _logger;
        private bool _isDisposed;

        /// <summary>
        /// Khởi tạo UndoGroupManager và đánh dấu điểm bắt đầu (StartUndoMark).
        /// </summary>
        /// <param name="logger">Tùy chọn — ghi lại lỗi COM StartUndoMark/EndUndoMark thay vì bỏ qua im lặng.</param>
        public UndoGroupManager(ILoggerService logger = null)
        {
            _logger = logger;
            _doc = Application.DocumentManager.MdiActiveDocument;
            if (_doc == null)
                return;

            try
            {
                _acadDoc = _doc.GetAcadDocument();
                _acadDoc.StartUndoMark();
            }
            catch (Exception ex)
            {
                // COM interop không khả dụng — vẫn chạy tiếp, chỉ mất việc gom bước Undo
                _acadDoc = null;
                _logger?.Warning($"StartUndoMark thất bại — các thao tác sẽ không gom thành 1 bước Undo: {ex.Message}");
            }
        }

        /// <summary>
        /// Giải phóng đối tượng và đánh dấu điểm kết thúc (EndUndoMark).
        /// Tất cả thay đổi từ lúc StartUndoMark sẽ được AutoCAD gom thành 1 bước Undo.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;

            if (_acadDoc != null)
            {
                try
                {
                    _acadDoc.EndUndoMark();
                }
                catch (Exception ex)
                {
                    // Không ném ngoại lệ khi dispose (làm crash app) — chỉ ghi log
                    _logger?.Warning($"EndUndoMark thất bại: {ex.Message}");
                }
            }

            _isDisposed = true;
        }
    }
}
