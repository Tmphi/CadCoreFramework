using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;

namespace CadCoreFramework.Core
{
    /// <summary>
    /// Triển khai dịch vụ IEventAggregator an toàn đa luồng (thread-safe).
    /// Hỗ trợ gửi/nhận thông điệp giữa các thành phần độc lập trong ứng dụng.
    /// </summary>
    public class EventAggregator : IEventAggregator
    {
        private readonly Dictionary<Type, List<Delegate>> _subscribers = new Dictionary<Type, List<Delegate>>();
        private readonly object _lock = new object();

        /// <inheritdoc/>
        public IDisposable? Subscribe<TMessage>(Action<TMessage>? action)
        {
            if (action == null) return null;
            var messageType = typeof(TMessage);

            lock (_lock)
            {
                if (!_subscribers.ContainsKey(messageType))
                {
                    _subscribers[messageType] = new List<Delegate>();
                }
                _subscribers[messageType].Add(action);
            }
            
            return new Unsubscriber(() => Unsubscribe(action));
        }

        private class Unsubscriber : IDisposable
        {
            private Action? _unsubscribeAction;
            public Unsubscriber(Action unsubscribeAction)
            {
                _unsubscribeAction = unsubscribeAction;
            }

            public void Dispose()
            {
                // One-shot: Dispose nhiều lần chỉ unsubscribe đúng 1 lần — tránh gỡ nhầm
                // một bản đăng ký khác của cùng delegate.
                System.Threading.Interlocked.Exchange(ref _unsubscribeAction, null)?.Invoke();
            }
        }

        /// <inheritdoc/>
        public void Unsubscribe<TMessage>(Action<TMessage>? action)
        {
            if (action == null) return;
            var messageType = typeof(TMessage);

            lock (_lock)
            {
                if (_subscribers.TryGetValue(messageType, out var delegates))
                {
                    delegates.Remove(action);
                    if (delegates.Count == 0)
                    {
                        _subscribers.Remove(messageType);
                    }
                }
            }
        }

        /// <summary>
        /// Snapshot số lượng subscriber theo message type — dùng cho Debug Inspector
        /// Palette để kiểm tra "ai đang nghe cái gì" tại thời điểm hiện tại.
        /// Format: "MessageTypeName — N subscribers".
        /// </summary>
        public IReadOnlyList<string> GetSubscriberSnapshot()
        {
            lock (_lock)
            {
                return _subscribers
                    .OrderBy(kv => kv.Key.Name)
                    .Select(kv => $"{kv.Key.Name} — {kv.Value.Count} subscriber{(kv.Value.Count == 1 ? "" : "s")}")
                    .ToList();
            }
        }

        /// <inheritdoc/>
        public void Publish<TMessage>(TMessage message)
        {
            var messageType = typeof(TMessage);
            List<Delegate> targets = null;

            lock (_lock)
            {
                if (_subscribers.TryGetValue(messageType, out var delegates))
                {
                    targets = delegates.ToList(); // Tạo bản sao danh sách để tránh ngoại lệ khi collection bị sửa đổi giữa chừng
                }
            }

            if (targets == null) return;

            // Cô lập lỗi: một subscriber ném exception KHÔNG được chặn các subscriber còn lại.
            // Lỗi được gom lại và ném ra SAU KHI mọi subscriber đã nhận message.
            List<Exception>? errors = null;
            foreach (var action in targets.Cast<Action<TMessage>>())
            {
                try
                {
                    action.Invoke(message);
                }
                catch (Exception ex)
                {
                    (errors ??= new List<Exception>()).Add(ex);
                }
            }

            if (errors == null) return;
            if (errors.Count == 1)
                ExceptionDispatchInfo.Capture(errors[0]).Throw(); // giữ nguyên kiểu + stack trace
            throw new AggregateException(
                $"{errors.Count} subscriber của {messageType.Name} đã ném exception.", errors);
        }
    }
}
