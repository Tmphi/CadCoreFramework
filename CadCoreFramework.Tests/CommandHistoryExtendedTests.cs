using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CadCoreFramework.Core;
using FluentAssertions;
using Xunit;

namespace CadCoreFramework.Tests
{
    /// <summary>
    /// Tests cho CommandHistoryService — mở rộng bao phủ thread-safety và edge cases.
    /// </summary>
    public class CommandHistoryExtendedTests
    {
        [Fact]
        public void Record_WithEmptyName_ShouldBeIgnored()
        {
            var service = new CommandHistoryService();
            service.Record("", TimeSpan.Zero, true);
            service.Record("   ", TimeSpan.Zero, true);
            service.Record(null, TimeSpan.Zero, true);

            service.RecentCommands.Should().BeEmpty();
        }

        [Fact]
        public void Record_ShouldTrackDuration()
        {
            var service = new CommandHistoryService();
            var duration = TimeSpan.FromMilliseconds(1234);
            service.Record("PLOT", duration, true);

            service.RecentCommands[0].Duration.Should().Be(duration);
        }

        [Fact]
        public void Record_ShouldTrackFailureState()
        {
            var service = new CommandHistoryService();
            service.Record("BAD_CMD", TimeSpan.FromSeconds(1), false);

            service.RecentCommands[0].Success.Should().BeFalse();
        }

        [Fact]
        public void Record_ShouldSetStartedAtUtc()
        {
            var before = DateTime.UtcNow;
            var service = new CommandHistoryService();
            service.Record("LINE", TimeSpan.Zero, true);
            var after = DateTime.UtcNow;

            service.RecentCommands[0].StartedAtUtc.Should()
                .BeOnOrAfter(before).And.BeOnOrBefore(after);
        }

        [Fact]
        public void Constructor_WithZeroCapacity_ShouldThrow()
        {
            Action act = () => new CommandHistoryService(0);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Constructor_WithNegativeCapacity_ShouldThrow()
        {
            Action act = () => new CommandHistoryService(-5);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void RecentCommands_ShouldReturnDefensiveCopy()
        {
            var service = new CommandHistoryService();
            service.Record("LINE", TimeSpan.Zero, true);

            var snapshot1 = service.RecentCommands;
            service.Record("CIRCLE", TimeSpan.Zero, true);
            var snapshot2 = service.RecentCommands;

            // snapshot1 should NOT have been mutated
            snapshot1.Should().HaveCount(1);
            snapshot2.Should().HaveCount(2);
        }

        [Fact]
        public void Record_ThreadSafe_ShouldNotLoseRecords()
        {
            var service = new CommandHistoryService(200);
            const int threadCount = 10;
            const int recordsPerThread = 20;

            var tasks = Enumerable.Range(0, threadCount).Select(t =>
                Task.Run(() =>
                {
                    for (int i = 0; i < recordsPerThread; i++)
                    {
                        service.Record($"CMD_{t}_{i}", TimeSpan.FromMilliseconds(i), true);
                    }
                })).ToArray();

            Task.WaitAll(tasks);

            service.RecentCommands.Should().HaveCount(threadCount * recordsPerThread);
        }

        [Fact]
        public void CommandRecord_ToString_ShouldFormatCorrectly()
        {
            var record = new CommandRecord("LINE", DateTime.UtcNow, TimeSpan.FromMilliseconds(42), true);
            record.ToString().Should().Contain("[OK]").And.Contain("LINE").And.Contain("42");

            var failRecord = new CommandRecord("ERR", DateTime.UtcNow, TimeSpan.FromMilliseconds(100), false);
            failRecord.ToString().Should().Contain("[FAIL]").And.Contain("ERR");
        }
    }
}
