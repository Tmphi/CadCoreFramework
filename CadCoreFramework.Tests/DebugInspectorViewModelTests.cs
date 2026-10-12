using System;
using CadCoreFramework.Core;
using CadCoreFramework.ViewModels;
using FluentAssertions;
using Moq;
using Xunit;

namespace CadCoreFramework.Tests
{
    public class DebugInspectorViewModelTests
    {
        private class Ping { }

        [Fact]
        public void Refresh_ShowsSubscribersFromInjectedAggregator()
        {
            var events = new EventAggregator();
            events.Subscribe<Ping>(_ => { });
            var history = new CommandHistoryService();
            history.Record("SAMPLE_GRID", TimeSpan.FromMilliseconds(5), true);

            var vm = new DebugInspectorViewModel(history, events);

            vm.EventSubscribers.Should().ContainSingle(l => l.Contains(nameof(Ping)));
            vm.CommandHistory.Should().ContainSingle(r => r.Name == "SAMPLE_GRID");
        }

        [Fact]
        public void Refresh_NonDefaultAggregator_SkipsSubscriberSnapshot()
        {
            var vm = new DebugInspectorViewModel(new CommandHistoryService(), new Mock<IEventAggregator>().Object);

            vm.EventSubscribers.Should().BeEmpty();
        }
    }
}
