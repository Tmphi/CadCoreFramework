using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;

namespace CadCoreFramework.Extensions
{
    /// <summary>
    /// Các phương thức mở rộng (Extension Methods) cho đối tượng Document của AutoCAD.
    /// </summary>
    public static class DocumentExtensions
    {
        /// <summary>
        /// Thực thi một hành động một cách an toàn bên trong khóa tài liệu (DocumentLock) và Transaction.
        /// Tự động Commit nếu thành công hoặc Abort nếu phát sinh lỗi.
        /// </summary>
        [Obsolete("Dùng ICadContext.Execute() thay thế để tuân thủ kiến trúc DI của dự án.")]
        public static void RunInTransaction(this Document doc, Action<Transaction> action)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                try
                {
                    action(tr);
                    tr.Commit();
                }
                catch (Exception)
                {
                    tr.Abort();
                    throw;
                }
            }
        }
    }
}
