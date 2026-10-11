using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;

namespace CadCoreFramework.Core
{
    /// <summary>
    /// Dịch vụ quản lý tập trung các sự kiện (Events) của AutoCAD.
    /// Giúp theo dõi việc mở/đóng bản vẽ và thêm/xóa đối tượng mà không bị lỗi rò rỉ bộ nhớ.
    /// </summary>
    public interface IEventService
    {
        /// <summary>Sự kiện bắn ra khi một bản vẽ mới được tạo hoặc mở.</summary>
        event EventHandler<DocumentCollectionEventArgs> DocumentCreated;

        /// <summary>Sự kiện bắn ra khi một bản vẽ sắp bị đóng.</summary>
        event EventHandler<DocumentCollectionEventArgs> DocumentToBeDestroyed;

        /// <summary>
        /// Sự kiện bắn ra khi có entity mới được thêm vào bất kỳ bản vẽ nào.
        /// Sử dụng e.Document để lọc đúng bản vẽ cần xử lý (tránh ảnh hưởng chéo giữa các bản vẽ trong môi trường MDI).
        /// </summary>
        event EventHandler<DocumentObjectEventArgs> ObjectAppended;

        /// <summary>
        /// Sự kiện bắn ra khi entity bị xóa khỏi bất kỳ bản vẽ nào.
        /// Sử dụng e.Document để lọc đúng bản vẽ cần xử lý.
        /// </summary>
        event EventHandler<DocumentObjectErasedEventArgs> ObjectErased;
        
        /// <summary>Bắt đầu khởi tạo và lắng nghe các sự kiện của AutoCAD DocumentManager.</summary>
        void Initialize();

        /// <summary>Hủy đăng ký toàn bộ sự kiện để giải phóng bộ nhớ khi tắt plugin.</summary>
        void Shutdown();
    }
}
