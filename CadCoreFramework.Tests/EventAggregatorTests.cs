using System;
using System.Collections.Generic;
using CadCoreFramework.Core;
using FluentAssertions;
using Xunit;

namespace CadCoreFramework.Tests
{
    public class EventAggregatorTests
    {
        public class TestMessage
        {
            public string Text { get; set; }
        }

        [Fact]
        public void Publish_ShouldTriggerSubscriber()
        {
            var aggregator = new EventAggregator();
            bool triggered = false;
            
            aggregator.Subscribe<TestMessage>(msg => triggered = true);
            aggregator.Publish(new TestMessage());

            triggered.Should().BeTrue();
        }

        [Fact]
        public void Unsubscribe_ShouldStopTriggering()
        {
            var aggregator = new EventAggregator();
            int count = 0;
            Action<TestMessage> action = msg => count++;
            
            aggregator.Subscribe(action);
            aggregator.Publish(new TestMessage());
            
            aggregator.Unsubscribe(action);
            aggregator.Publish(new TestMessage());

            count.Should().Be(1);
        }

        [Fact]
        public void Subscribe_WithIDisposable_ShouldUnsubscribeOnDispose()
        {
            var aggregator = new EventAggregator();
            int count = 0;
            
            using (var token = aggregator.Subscribe<TestMessage>(msg => count++))
            {
                aggregator.Publish(new TestMessage());
                count.Should().Be(1);
            }
            
            // Should not trigger again since token is disposed
            aggregator.Publish(new TestMessage());
            count.Should().Be(1);
        }
    }
}
