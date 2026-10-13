using System;
using System.Threading;
using CadCoreFramework.Core;
using FluentAssertions;
using Xunit;

namespace CadCoreFramework.Tests
{
    /// <summary>
    /// CadThread — chặn lời gọi ICadContext từ thread khác main thread (B-06).
    /// Trạng thái là static → mỗi test tự Reset trong finally.
    /// </summary>
    public class CadThreadTests
    {
        private static Exception? RunOnOtherThread(Action action)
        {
            Exception? caught = null;
            var t = new Thread(() =>
            {
                try { action(); }
                catch (Exception ex) { caught = ex; }
            });
            t.Start();
            t.Join();
            return caught;
        }

        [Fact]
        public void EnsureMainThread_NotCaptured_DoesNothing()
        {
            CadThread.ResetForTesting();
            Action act = () => CadThread.EnsureMainThread("Test");
            act.Should().NotThrow();
            RunOnOtherThread(() => CadThread.EnsureMainThread("Test")).Should().BeNull();
        }

        [Fact]
        public void EnsureMainThread_OnCapturedThread_DoesNotThrow()
        {
            try
            {
                CadThread.CaptureMainThread();
                CadThread.MainThreadId.Should().Be(Environment.CurrentManagedThreadId);
                Action act = () => CadThread.EnsureMainThread("Test");
                act.Should().NotThrow();
            }
            finally { CadThread.ResetForTesting(); }
        }

        [Fact]
        public void EnsureMainThread_OnOtherThread_ThrowsWithOperationName()
        {
            try
            {
                CadThread.CaptureMainThread();
                Exception? ex = RunOnOtherThread(() => CadThread.EnsureMainThread("CadContext.Execute"));

                ex.Should().BeOfType<InvalidOperationException>();
                ex!.Message.Should().Contain("CadContext.Execute").And.Contain("main thread");
            }
            finally { CadThread.ResetForTesting(); }
        }

        [Fact]
        public void ResetForTesting_ClearsCapturedThread()
        {
            CadThread.CaptureMainThread();
            CadThread.ResetForTesting();

            CadThread.MainThreadId.Should().Be(0);
            RunOnOtherThread(() => CadThread.EnsureMainThread("Test")).Should().BeNull();
        }
    }
}
