using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CadCoreFramework.Core;
using FluentAssertions;
using Xunit;

namespace CadCoreFramework.Tests
{
    /// <summary>
    /// Tests cho EventAggregator — mở rộng bao phủ thread-safety, multi-subscriber, edge cases.
    /// </summary>
    public class EventAggregatorExtendedTests
    {
        public class OrderCreated { public int OrderId { get; set; } }
        public class OrderShipped { public int OrderId { get; set; } }

        [Fact]
        public void Publish_WithMultipleSubscribers_ShouldNotifyAll()
        {
            var aggregator = new EventAggregator();
            var received = new List<int>();

            aggregator.Subscribe<OrderCreated>(msg => received.Add(1));
            aggregator.Subscribe<OrderCreated>(msg => received.Add(2));
            aggregator.Subscribe<OrderCreated>(msg => received.Add(3));

            aggregator.Publish(new OrderCreated { OrderId = 100 });

            received.Should().BeEquivalentTo(new[] { 1, 2, 3 });
        }

        [Fact]
        public void Publish_WithDifferentMessageTypes_ShouldOnlyNotifyCorrectSubscribers()
        {
            var aggregator = new EventAggregator();
            bool orderCreatedReceived = false;
            bool orderShippedReceived = false;

            aggregator.Subscribe<OrderCreated>(msg => orderCreatedReceived = true);
            aggregator.Subscribe<OrderShipped>(msg => orderShippedReceived = true);

            aggregator.Publish(new OrderCreated { OrderId = 1 });

            orderCreatedReceived.Should().BeTrue();
            orderShippedReceived.Should().BeFalse();
        }

        [Fact]
        public void Publish_WithNoSubscribers_ShouldNotThrow()
        {
            var aggregator = new EventAggregator();
            Action act = () => aggregator.Publish(new OrderCreated { OrderId = 1 });
            act.Should().NotThrow();
        }

        [Fact]
        public void Subscribe_WithNull_ShouldReturnNull()
        {
            var aggregator = new EventAggregator();
            var token = aggregator.Subscribe<OrderCreated>(null);
            token.Should().BeNull();
        }

        [Fact]
        public void Unsubscribe_WithNull_ShouldNotThrow()
        {
            var aggregator = new EventAggregator();
            Action act = () => aggregator.Unsubscribe<OrderCreated>(null);
            act.Should().NotThrow();
        }

        [Fact]
        public void Unsubscribe_NonExistentHandler_ShouldNotThrow()
        {
            var aggregator = new EventAggregator();
            Action<OrderCreated> handler = msg => { };
            Action act = () => aggregator.Unsubscribe(handler);
            act.Should().NotThrow();
        }

        [Fact]
        public void Publish_ShouldPassMessageData()
        {
            var aggregator = new EventAggregator();
            int receivedId = 0;

            aggregator.Subscribe<OrderCreated>(msg => receivedId = msg.OrderId);
            aggregator.Publish(new OrderCreated { OrderId = 42 });

            receivedId.Should().Be(42);
        }

        [Fact]
        public void Dispose_MultipleTimes_ShouldNotThrow()
        {
            var aggregator = new EventAggregator();
            var token = aggregator.Subscribe<OrderCreated>(msg => { });
            token.Should().NotBeNull();

            Action act = () =>
            {
                token!.Dispose();
                token!.Dispose(); // Double dispose should be safe
            };
            act.Should().NotThrow();
        }

        [Fact]
        public void ThreadSafe_ConcurrentPublishSubscribe_ShouldNotThrow()
        {
            var aggregator = new EventAggregator();
            var tasks = new List<Task>();

            for (int i = 0; i < 20; i++)
            {
                int capture = i;
                tasks.Add(Task.Run(() =>
                {
                    aggregator.Subscribe<OrderCreated>(msg => { });
                    aggregator.Publish(new OrderCreated { OrderId = capture });
                }));
            }

            Action act = () => Task.WaitAll(tasks.ToArray());
            act.Should().NotThrow();
        }
    }
}
