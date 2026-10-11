using System;
using CadCoreFramework.Core;
using FluentAssertions;
using Xunit;

namespace CadCoreFramework.Tests
{
    public class CommandHistoryServiceTests
    {
        [Fact]
        public void Record_ShouldAddCommandToHistory()
        {
            var service = new CommandHistoryService();
            service.Record("LINE", TimeSpan.FromSeconds(1), true);

            var history = service.RecentCommands;
            history.Should().HaveCount(1);
            history[0].CommandName.Should().Be("LINE");
            history[0].Success.Should().BeTrue();
        }

        [Fact]
        public void Record_ShouldRespectCapacity()
        {
            var service = new CommandHistoryService(2);
            service.Record("CMD1", TimeSpan.Zero, true);
            service.Record("CMD2", TimeSpan.Zero, true);
            service.Record("CMD3", TimeSpan.Zero, true);

            var history = service.RecentCommands;
            history.Should().HaveCount(2);
            history[0].CommandName.Should().Be("CMD3");
            history[1].CommandName.Should().Be("CMD2");
        }

        [Fact]
        public void Clear_ShouldEmptyHistory()
        {
            var service = new CommandHistoryService();
            service.Record("LINE", TimeSpan.Zero, true);
            service.Clear();

            service.RecentCommands.Should().BeEmpty();
        }
    }
}
