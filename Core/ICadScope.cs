using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace CadCoreFramework.Core
{
    /// <summary>
    /// Môi trường (Scope) được tạo ra mỗi khi chạy lệnh AutoCAD.
    /// Chứa tất cả các tham chiếu cần thiết (Transaction, Database, Editor) 
    /// để tránh việc gọi lặp lại Application.DocumentManager.MdiActiveDocument.
    /// 
    /// QUAN TRỌNG (Autodesk):
    /// - Luôn lấy tham chiếu Document/Database từ scope này, KHÔNG gọi
    ///   Application.DocumentManager.MdiActiveDocument bên trong transaction.
    /// - ICadScope chỉ hợp lệ trong phạm vi của lambda được truyền vào ICadContext.Execute().
    ///   Không được lưu giữ scope ngoài phạm vi đó.
    /// </summary>
    public interface ICadScope
    {
        /// <summary>Bản vẽ hiện tại (snapshot tại thời điểm tạo scope).</summary>
        Document Document { get; }

        /// <summary>Cơ sở dữ liệu của bản vẽ hiện tại.</summary>
        Database Database { get; }

        /// <summary>Phiên giao dịch đang mở (để Read/Write đối tượng).</summary>
        Transaction Transaction { get; }

        /// <summary>Không gian vẽ hiện tại (Model Space hoặc Layout Paper Space).</summary>
        BlockTableRecord CurrentSpace { get; }

        /// <summary>Trình soạn thảo, dùng để Prompt hỏi điểm, hỏi text, ghi Log.</summary>
        /// <remarks>
        /// ⚠️ Chỉ dùng được khi <see cref="Document"/> là bản vẽ ĐANG ACTIVE. Editor của bản vẽ không active
        /// ném <c>eNotApplicable</c> — kể cả <c>WriteMessage</c> (đã kiểm chứng trên AutoCAD 2024). Trường hợp
        /// điển hình: giai đoạn vẽ của <c>ICadContext.ExecuteAsync</c>, khi người dùng đã chuyển sang bản vẽ
        /// khác trong lúc chờ — vẽ vào <see cref="Database"/> vẫn được, nhưng đừng gọi Editor; báo kết quả
        /// SAU khi Execute xong, qua INotificationService (bản vẽ đang active).
        /// </remarks>
        Editor Editor { get; }

        /// <summary>
        /// True nếu scope được tạo từ <see cref="ICadContext.ExecuteRead"/>
        /// (không lock document, không cho phép ghi).
        /// False nếu tạo từ <see cref="ICadContext.Execute"/> (có lock, có thể ghi).
        /// </summary>
        bool IsReadOnly { get; }

        /// <summary>
        /// Thêm một Entity mới vào không gian bản vẽ hiện tại (ModelSpace/PaperSpace).
        /// Tự động gọi Transaction.AddNewlyCreatedDBObject() — bạn không cần gọi thêm.
        /// </summary>
        /// <exception cref="InvalidOperationException">Ném ra nếu scope là read-only.</exception>
        ObjectId AppendEntityToCurrentSpace(Entity entity);
    }
}
