using System;
using Autodesk.AutoCAD.ApplicationServices;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace CadCoreFramework.Core
{
    /// <summary>
    /// Quản lý nhóm lệnh Undo (Undo Grouping).
    /// Hỗ trợ gom cụm nhiều thao tác nhỏ lại thành một bước Undo duy nhất trong lịch sử của AutoCAD.
    /// Sử dụng khối using để đảm bảo EndUndoMark luôn được gọi.
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
        private bool _isDisposed;

        /// <summary>
        /// Khởi tạo UndoGroupManager và đánh dấu điểm bắt đầu (StartUndoMark).
        /// </summary>
        public UndoGroupManager()
        {
            _doc = Application.DocumentManager.MdiActiveDocument;
            if (_doc == null)
                return;

            try
            {
                _acadDoc = _doc.GetAcadDocument();
                _acadDoc.StartUndoMark();
            }
            catch
            {
                // Bỏ qua nếu COM interop không khả dụng trên môi trường đang chạy
                _acadDoc = null;
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
                catch
                {
                    // Đảm bảo không ném ngoại lệ làm crash app khi dispose
                }
            }
            
            _isDisposed = true;
            GC.SuppressFinalize(this);
        }

        ~UndoGroupManager()
        {
            Dispose();
        }
    }
}
