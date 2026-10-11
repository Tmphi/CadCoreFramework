using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace CadCoreFramework.Core
{
    /// <summary>
    /// EventArgs mở rộng cho sự kiện ObjectAppended. Bao gồm Document nguồn phát sinh sự kiện
    /// để bên nhận có thể lọc theo bản vẽ tương ứng (tránh xung đột dữ liệu chéo giữa các bản vẽ trong MDI).
    /// </summary>
    public class DocumentObjectEventArgs : EventArgs
    {
        public Document Document { get; }
        public ObjectEventArgs ObjectEventArgs { get; }
        public DocumentObjectEventArgs(Document doc, ObjectEventArgs args)
        {
            Document = doc;
            ObjectEventArgs = args;
        }
    }

    /// <summary>
    /// EventArgs mở rộng cho sự kiện ObjectErased. Bao gồm Document nguồn phát sinh sự kiện
    /// để bên nhận có thể lọc theo bản vẽ tương ứng (tránh xung đột dữ liệu chéo giữa các bản vẽ trong MDI).
    /// </summary>
    public class DocumentObjectErasedEventArgs : EventArgs
    {
        public Document Document { get; }
        public ObjectErasedEventArgs ObjectErasedEventArgs { get; }
        public DocumentObjectErasedEventArgs(Document doc, ObjectErasedEventArgs args)
        {
            Document = doc;
            ObjectErasedEventArgs = args;
        }
    }

    /// <summary>
    /// Quản lý tất cả các sự kiện ứng dụng AutoCAD tại một nơi tập trung duy nhất.
    /// Theo dõi việc tạo/đóng bản vẽ và chuyển tiếp các sự kiện cấp đối tượng (thêm/xóa)
    /// kèm theo tham chiếu Document nguồn để đảm bảo an toàn trong môi trường nhiều bản vẽ (MDI).
    ///
    /// Lazy subscribe: ObjectAppended / ObjectErased chỉ được hook vào AutoCAD khi có ít nhất
    /// một subscriber. Điều này tránh overhead trên mọi INSERT/XREF/PASTE khi plugin không dùng tới.
    /// </summary>
    public class EventService : IEventService
    {
        private readonly ILoggerService _logger;
        private bool _isInitialized;

        // Lưu trữ tham chiếu delegate để có thể hủy đăng ký (-=) chính xác khi tắt.
        private readonly Dictionary<Document, ObjectEventHandler> _appendedHandlers
            = new Dictionary<Document, ObjectEventHandler>();
        private readonly Dictionary<Document, ObjectErasedEventHandler> _erasedHandlers
            = new Dictionary<Document, ObjectErasedEventHandler>();

        // Lazy subscribe state: DB-level hooks are only active when someone is subscribed
        private bool _docObjectHooksActive;
        private EventHandler<DocumentObjectEventArgs> _objectAppendedHandlers;
        private EventHandler<DocumentObjectErasedEventArgs> _objectErasedHandlers;

        /// <summary>Bắn ra khi một bản vẽ mới được tạo hoặc mở.</summary>
        public event EventHandler<DocumentCollectionEventArgs> DocumentCreated;

        /// <summary>Bắn ra khi một bản vẽ sắp bị đóng.</summary>
        public event EventHandler<DocumentCollectionEventArgs> DocumentToBeDestroyed;

        /// <summary>
        /// Bắn ra khi một đối tượng được thêm vào cơ sở dữ liệu của bất kỳ bản vẽ nào đang theo dõi.
        /// Lazy: chỉ subscribe AutoCAD DB events khi có ít nhất một subscriber.
        /// </summary>
        public event EventHandler<DocumentObjectEventArgs> ObjectAppended
        {
            add
            {
                if (!_docObjectHooksActive)
                {
                    _docObjectHooksActive = true;
                    if (_isInitialized) HookAllDocumentObjects();
                }
                _objectAppendedHandlers += value;
            }
            remove
            {
                _objectAppendedHandlers -= value;
                if (_objectAppendedHandlers == null && _objectErasedHandlers == null && _docObjectHooksActive)
                {
                    _docObjectHooksActive = false;
                    UnhookAllDocumentObjects();
                }
            }
        }

        /// <summary>
        /// Bắn ra khi một đối tượng bị xóa khỏi cơ sở dữ liệu của bất kỳ bản vẽ nào đang theo dõi.
        /// Lazy: chỉ subscribe AutoCAD DB events khi có ít nhất một subscriber.
        /// </summary>
        public event EventHandler<DocumentObjectErasedEventArgs> ObjectErased
        {
            add
            {
                if (!_docObjectHooksActive)
                {
                    _docObjectHooksActive = true;
                    if (_isInitialized) HookAllDocumentObjects();
                }
                _objectErasedHandlers += value;
            }
            remove
            {
                _objectErasedHandlers -= value;
                if (_objectAppendedHandlers == null && _objectErasedHandlers == null && _docObjectHooksActive)
                {
                    _docObjectHooksActive = false;
                    UnhookAllDocumentObjects();
                }
            }
        }

        public EventService(ILoggerService logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Bắt đầu dịch vụ và móc nối vào DocumentManager của AutoCAD.
        /// Được gọi tự động bởi AppBootstrapper.InitializeCore().
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;
            try
            {
                Application.DocumentManager.DocumentCreated += OnDocumentCreated;
                Application.DocumentManager.DocumentToBeDestroyed += OnDocumentToBeDestroyed;

                // Only hook DB object events if someone already subscribed before Initialize()
                if (_docObjectHooksActive)
                {
                    foreach (Document doc in Application.DocumentManager)
                        HookDocumentObjects(doc);
                }

                _isInitialized = true;
                _logger?.Info("EventService khởi tạo thành công.");
            }
            catch (Exception ex)
            {
                _logger?.Error("Không thể khởi tạo EventService", ex);
            }
        }

        /// <summary>
        /// Hủy toàn bộ đăng ký sự kiện để ngăn chặn rò rỉ bộ nhớ khi tắt plugin.
        /// Được gọi bởi AppBootstrapper.Terminate().
        /// </summary>
        public void Shutdown()
        {
            if (!_isInitialized) return;
            try
            {
                Application.DocumentManager.DocumentCreated -= OnDocumentCreated;
                Application.DocumentManager.DocumentToBeDestroyed -= OnDocumentToBeDestroyed;
                UnhookAllDocumentObjects();
                _isInitialized = false;
            }
            catch (Exception ex)
            {
                _logger?.Error("Lỗi khi dừng EventService", ex);
            }
        }

        private void OnDocumentCreated(object sender, DocumentCollectionEventArgs e)
        {
            // Only hook DB events on the new document if someone is subscribed
            if (_docObjectHooksActive) HookDocumentObjects(e.Document);
            DocumentCreated?.Invoke(this, e);
        }

        private void OnDocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)
        {
            UnhookDocumentObjects(e.Document);
            DocumentToBeDestroyed?.Invoke(this, e);
        }

        private void HookAllDocumentObjects()
        {
            foreach (Document doc in Application.DocumentManager)
                HookDocumentObjects(doc);
        }

        private void UnhookAllDocumentObjects()
        {
            var documents = new List<Document>(_appendedHandlers.Keys);
            foreach (var doc in documents)
                UnhookDocumentObjects(doc);
        }

        /// <summary>
        /// Móc nối sự kiện database của một bản vẽ và lưu tham chiếu delegate
        /// để có thể gỡ bỏ chuẩn xác trong UnhookDocumentObjects.
        /// </summary>
        private void HookDocumentObjects(Document doc)
        {
            if (doc?.Database == null) return;
            if (_appendedHandlers.ContainsKey(doc)) return;

            ObjectEventHandler appendedHandler = (s, e) => OnObjectAppended(doc, e);
            ObjectErasedEventHandler erasedHandler = (s, e) => OnObjectErased(doc, e);

            _appendedHandlers[doc] = appendedHandler;
            _erasedHandlers[doc] = erasedHandler;

            doc.Database.ObjectAppended += appendedHandler;
            doc.Database.ObjectErased += erasedHandler;
        }

        /// <summary>
        /// Hủy đăng ký chuẩn xác các delegate đã lưu cho một bản vẽ cụ thể.
        /// </summary>
        private void UnhookDocumentObjects(Document doc)
        {
            if (doc?.Database == null) return;

            if (_appendedHandlers.TryGetValue(doc, out var appendedHandler))
            {
                doc.Database.ObjectAppended -= appendedHandler;
                _appendedHandlers.Remove(doc);
            }

            if (_erasedHandlers.TryGetValue(doc, out var erasedHandler))
            {
                doc.Database.ObjectErased -= erasedHandler;
                _erasedHandlers.Remove(doc);
            }
        }

        private void OnObjectAppended(Document doc, ObjectEventArgs e)
        {
            _objectAppendedHandlers?.Invoke(this, new DocumentObjectEventArgs(doc, e));
        }

        private void OnObjectErased(Document doc, ObjectErasedEventArgs e)
        {
            _objectErasedHandlers?.Invoke(this, new DocumentObjectErasedEventArgs(doc, e));
        }
    }
}
