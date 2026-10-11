using System;
using System.Collections.Generic;
using System.Linq;

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
        public IDisposable Subscribe<TMessage>(Action<TMessage> action)
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
            private readonly Action _unsubscribeAction;
            public Unsubscriber(Action unsubscribeAction)
            {
                _unsubscribeAction = unsubscribeAction;
            }

            public void Dispose()
            {
                _unsubscribeAction?.Invoke();
            }
        }

        /// <inheritdoc/>
        public void Unsubscribe<TMessage>(Action<TMessage> action)
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

            if (targets != null)
            {
                foreach (var action in targets.Cast<Action<TMessage>>())
                {
                    action.Invoke(message);
                }
            }
        }
    }
}
