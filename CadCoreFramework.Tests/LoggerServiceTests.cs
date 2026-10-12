using System;
using System.IO;
using System.Linq;
using CadCoreFramework.Core;
using FluentAssertions;
using Xunit;

namespace CadCoreFramework.Tests
{
    /// <summary>
    /// LoggerService là file logger tự viết (thay Serilog — xung đột với bản Serilog AutoCAD 2024 tự nạp).
    /// Mỗi test dùng một thư mục tạm riêng + đồng hồ giả.
    /// </summary>
    public sealed class LoggerServiceTests : IDisposable
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "CadCoreLoggerTests_" + Guid.NewGuid().ToString("N"));
        private DateTimeOffset _now = new DateTimeOffset(2026, 10, 12, 19, 30, 0, TimeSpan.FromHours(7));

        public void Dispose()
        {
            try { Directory.Delete(_dir, recursive: true); } catch { }
        }

        private LoggerService NewLogger(LogSeverity min = LogSeverity.Debug) => new LoggerService(_dir, min, () => _now);

        private string ReadToday(LoggerService logger) => File.ReadAllText(logger.GetLogFilePath(_now));

        [Fact]
        public void Info_WritesTimestampLevelAndMessage()
        {
            var logger = NewLogger();

            logger.Info("Plugin started");

            ReadToday(logger).Should().StartWith("2026-10-12 19:30:00.000 +07:00 [INF] Plugin started");
        }

        [Fact]
        public void EachLevel_UsesItsTag()
        {
            var logger = NewLogger();

            logger.Debug("d");
            logger.Info("i");
            logger.Warning("w");
            logger.Error("e");

            string text = ReadToday(logger);
            text.Should().Contain("[DBG] d").And.Contain("[INF] i").And.Contain("[WRN] w").And.Contain("[ERR] e");
        }

        [Fact]
        public void Error_WithException_WritesExceptionDetails()
        {
            var logger = NewLogger();

            logger.Error("Draw failed", new InvalidOperationException("Layer locked"));

            ReadToday(logger).Should().Contain("[ERR] Draw failed")
                .And.Contain("System.InvalidOperationException: Layer locked");
        }

        [Fact]
        public void BelowMinimumLevel_IsNotWritten()
        {
            var logger = NewLogger(LogSeverity.Information);

            logger.Debug("hidden");
            logger.Info("shown");

            string text = ReadToday(logger);
            text.Should().NotContain("hidden").And.Contain("shown");
        }

        [Fact]
        public void Performance_WritesInvariantMilliseconds()
        {
            var logger = NewLogger();

            // FromTicks: trên .NET Framework, TimeSpan.FromMilliseconds làm tròn về mili-giây nguyên.
            logger.Performance("SAMPLE_GRID", TimeSpan.FromTicks(125_000)); // 12.5 ms

            ReadToday(logger).Should().Contain("[PERF] SAMPLE_GRID took 12.5ms");
        }

        [Fact]
        public void NewDay_WritesToNewFile()
        {
            var logger = NewLogger();
            logger.Info("day 1");

            _now = _now.AddDays(1);
            logger.Info("day 2");

            Directory.GetFiles(_dir, "log-*.txt").Select(Path.GetFileName)
                .Should().BeEquivalentTo("log-20261012.txt", "log-20261013.txt");
        }

        [Fact]
        public void Constructor_KeepsOnlyRetainedDays()
        {
            Directory.CreateDirectory(_dir);
            var start = new DateTime(2026, 8, 1);
            for (int i = 0; i < LoggerService.RetainedDays + 5; i++)
                File.WriteAllText(Path.Combine(_dir, $"log-{start.AddDays(i):yyyyMMdd}.txt"), "x");

            NewLogger();

            var remaining = Directory.GetFiles(_dir, "log-*.txt").Select(Path.GetFileName).OrderBy(n => n).ToList();
            remaining.Should().HaveCount(LoggerService.RetainedDays);
            remaining.First().Should().Be($"log-{start.AddDays(5):yyyyMMdd}.txt"); // 5 file cũ nhất đã bị xóa
        }

        [Fact]
        public void AfterDispose_WritesAreIgnored_AndDoNotThrow()
        {
            var logger = NewLogger();
            logger.Dispose();
            logger.Dispose();

            Action act = () => logger.Info("after dispose");

            act.Should().NotThrow();
            File.Exists(logger.GetLogFilePath(_now)).Should().BeFalse();
        }

        [Fact]
        public void FileLockedByAnotherWriter_StillAppends()
        {
            var logger = NewLogger();
            logger.Info("first");

            // Mô phỏng một phiên AutoCAD khác đang mở file (FileShare.ReadWrite như logger).
            using (new FileStream(logger.GetLogFilePath(_now), FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                logger.Info("second");
            }

            ReadToday(logger).Should().Contain("first").And.Contain("second");
        }

        [Fact]
        public void UnwritableDirectory_NeverThrows()
        {
            // Một FILE chiếm đúng đường dẫn thư mục log → không thể tạo thư mục hay ghi file.
            File.WriteAllText(_dir, "not a directory");
            try
            {
                Action act = () =>
                {
                    var logger = NewLogger();
                    logger.Error("boom", new Exception("x"));
                };

                act.Should().NotThrow();
            }
            finally
            {
                File.Delete(_dir);
            }
        }
    }
}
