using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using CadCoreFramework.Core;
using CadCoreFramework.Services;
using CadCoreFramework.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace CadCoreFramework.Commands.Demos
{
    /// <summary>
    /// Lớp message mẫu dùng cho EventAggregator Demo.
    /// </summary>
    public class DemoMessage
    {
        /// <summary>Tiêu đề tin nhắn.</summary>
        public string Title { get; set; }

        /// <summary>Nội dung chi tiết.</summary>
        public string Content { get; set; }

        /// <summary>Thời điểm tạo tin nhắn.</summary>
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Các lệnh minh họa hệ thống sự kiện (Events & EventAggregator):
    /// Giao tiếp lỏng lẻo Publish/Subscribe và theo dõi sự kiện thêm/xóa đối tượng thời gian thực.
    /// </summary>
    public class DemoEventCommands : CadCommandBase
    {
        private INotificationService Notify
            => AppBootstrapper.ServiceProvider?.GetService<INotificationService>();

        private IEventAggregator EventAgg
            => AppBootstrapper.ServiceProvider?.GetService<IEventAggregator>();

        private IEventService EventService
            => AppBootstrapper.ServiceProvider?.GetService<IEventService>();

        private static Action<DemoMessage> _demoMessageHandler;
        private static bool _isSubscribed = false;

        private static EventHandler<DocumentObjectEventArgs> _objectAppendedHandler;
        private static bool _isInspecting = false;

        /// <summary>
        /// DEMO_EVENT_SUBSCRIBE — Đăng ký/Hủy lắng nghe message DemoMessage qua EventAggregator.
        /// Gọi lại lệnh này để bật/tắt (Toggle) trạng thái subscribe.
        /// </summary>
        [CommandMethod("DEMO_EVENT_SUBSCRIBE")]
        public void DemoEventSubscribe()
        {
            Notify?.Section("DEMO: EVENTAGGREGATOR — SUBSCRIBE/UNSUBSCRIBE");

            ExecuteCommand(() =>
            {
                if (EventAgg == null)
                {
                    Notify?.Error("IEventAggregator chưa được đăng ký trong DI.");
                    return;
                }

                if (!_isSubscribed)
                {
                    _demoMessageHandler = msg =>
                    {
                        Editor?.WriteMessage($"\n  ➤ [Subscriber nhận được] Tiêu đề: \"{msg.Title}\", Nội dung: \"{msg.Content}\", Thời gian: {msg.Timestamp:HH:mm:ss}");
                    };

                    EventAgg.Subscribe(_demoMessageHandler);
                    _isSubscribed = true;
                    Notify?.Info("✔ Đã ĐĂNG KÝ lắng nghe DemoMessage. Gõ DEMO_EVENT_PUBLISH để gửi tin nhắn.");
                }
                else
                {
                    EventAgg.Unsubscribe(_demoMessageHandler);
                    _demoMessageHandler = null;
                    _isSubscribed = false;
                    Notify?.Info("✘ Đã HỦY ĐĂNG KÝ lắng nghe DemoMessage.");
                }
            });
        }

        /// <summary>
        /// DEMO_EVENT_PUBLISH — Phát hành (Publish) một message DemoMessage qua EventAggregator.
        /// </summary>
        [CommandMethod("DEMO_EVENT_PUBLISH")]
        public void DemoEventPublish()
        {
            Notify?.Section("DEMO: EVENTAGGREGATOR — PUBLISH");

            ExecuteCommand(() =>
            {
                if (EventAgg == null)
                {
                    Notify?.Error("IEventAggregator chưa được đăng ký trong DI.");
                    return;
                }

                var content = CadPromptHelper.GetString("Nhập nội dung tin nhắn:", defaultVal: "Hello từ EventAggregator!");
                if (content == null) { Notify?.Warn("Đã hủy."); return; }

                var message = new DemoMessage
                {
                    Title = "Demo Message",
                    Content = content,
                    Timestamp = DateTime.Now
                };

                EventAgg.Publish(message);
                Notify?.Info($"Đã Publish DemoMessage: \"{content}\"");

                if (!_isSubscribed)
                {
                    Notify?.Warn("Chưa có subscriber nào đang lắng nghe. Gõ DEMO_EVENT_SUBSCRIBE trước.");
                }
            });
        }

        /// <summary>
        /// DEMO_REALTIME_INSPECT — Bật/Tắt chế độ theo dõi sự kiện đối tượng AutoCAD thời gian thực (Real-time).
        /// Khi bật, mỗi khi bạn vẽ bất kỳ đối tượng nào (Line, Circle...), EventService sẽ bắt được và thông báo.
        /// </summary>
        [CommandMethod("DEMO_REALTIME_INSPECT")]
        public void DemoRealtimeInspect()
        {
            Notify?.Section("DEMO: IEVENTSERVICE — THEO DÕI SỰ KIỆN DATABASE THỜI GIAN THỰC");

            ExecuteCommand(() =>
            {
                if (EventService == null)
                {
                    Notify?.Error("IEventService chưa được đăng ký trong DI.");
                    return;
                }

                if (!_isInspecting)
                {
                    _objectAppendedHandler = (s, e) =>
                    {
                        var objId = e.ObjectEventArgs.DBObject.ObjectId;
                        Editor?.WriteMessage($"\n  [Realtime Inspector] Đối tượng mới được thêm: {objId.ObjectClass.DxfName} (ID: {objId})");
                    };

                    EventService.ObjectAppended += _objectAppendedHandler;
                    _isInspecting = true;
                    Notify?.Info("✔ ĐÃ BẬT chế độ theo dõi sự kiện! Hãy thử vẽ một Line hoặc Circle để xem thông báo.");
                    Notify?.Info("  (Gõ lại DEMO_REALTIME_INSPECT để tắt chế độ này).");
                }
                else
                {
                    EventService.ObjectAppended -= _objectAppendedHandler;
                    _objectAppendedHandler = null;
                    _isInspecting = false;
                    Notify?.Info("✘ ĐÃ TẮT chế độ theo dõi sự kiện.");
                }
            });
        }
    }
}
