using System;
using CadCoreFramework.ViewModels;
using FluentAssertions;
using Moq;
using Xunit;

namespace CadCoreFramework.Tests
{
    /// <summary>
    /// Tests cho ViewModelBase — kiểm tra INotifyPropertyChanged, RunSafe/RunSafeAsync,
    /// IsBusy lifecycle, ErrorMessage behavior.
    /// </summary>
    public class ViewModelBaseTests
    {
        // Concrete test class vì ViewModelBase là abstract
        private class TestViewModel : ViewModelBase
        {
            public new bool IsBusy
            {
                get => base.IsBusy;
                set => base.IsBusy = value;
            }

            public new string Title
            {
                get => base.Title;
                set => base.Title = value;
            }

            public new string ErrorMessage
            {
                get => base.ErrorMessage;
                set => base.ErrorMessage = value;
            }

            public void InvokeRunSafe(Action action, string? errorPrefix = null)
                => RunSafe(action, errorPrefix);

            public async System.Threading.Tasks.Task InvokeRunSafeAsync(Func<System.Threading.Tasks.Task> asyncAction, string? errorPrefix = null)
                => await RunSafeAsync(asyncAction, errorPrefix);
        }

        [Fact]
        public void IsBusy_DefaultValue_ShouldBeFalse()
        {
            var vm = new TestViewModel();
            vm.IsBusy.Should().BeFalse();
        }

        [Fact]
        public void Title_DefaultValue_ShouldBeEmpty()
        {
            var vm = new TestViewModel();
            vm.Title.Should().BeEmpty();
        }

        [Fact]
        public void ErrorMessage_DefaultValue_ShouldBeEmpty()
        {
            var vm = new TestViewModel();
            vm.ErrorMessage.Should().BeEmpty();
        }

        [Fact]
        public void IsBusy_ShouldRaisePropertyChanged()
        {
            var vm = new TestViewModel();
            bool raised = false;
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(vm.IsBusy)) raised = true;
            };

            vm.IsBusy = true;
            raised.Should().BeTrue();
        }

        [Fact]
        public void Title_ShouldRaisePropertyChanged()
        {
            var vm = new TestViewModel();
            bool raised = false;
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(vm.Title)) raised = true;
            };

            vm.Title = "New Title";
            raised.Should().BeTrue();
        }

        [Fact]
        public void RunSafe_SuccessfulAction_ShouldClearErrorAndResetBusy()
        {
            var vm = new TestViewModel();
            vm.ErrorMessage = "Previous error";

            vm.InvokeRunSafe(() => { /* no-op success */ });

            vm.ErrorMessage.Should().BeEmpty();
            vm.IsBusy.Should().BeFalse();
        }

        [Fact]
        public void RunSafe_FailingAction_ShouldSetErrorMessage()
        {
            var vm = new TestViewModel();

            vm.InvokeRunSafe(() => throw new InvalidOperationException("test failure"));

            vm.ErrorMessage.Should().Contain("test failure");
            vm.IsBusy.Should().BeFalse();
        }

        [Fact]
        public void RunSafe_WithPrefix_ShouldPrependPrefix()
        {
            var vm = new TestViewModel();

            vm.InvokeRunSafe(() => throw new Exception("oops"), "Lỗi Import");

            vm.ErrorMessage.Should().StartWith("Lỗi Import:").And.Contain("oops");
        }

        [Fact]
        public async System.Threading.Tasks.Task RunSafeAsync_SuccessfulAction_ShouldResetBusy()
        {
            var vm = new TestViewModel();

            await vm.InvokeRunSafeAsync(async () =>
            {
                await System.Threading.Tasks.Task.Delay(10);
            });

            vm.IsBusy.Should().BeFalse();
            vm.ErrorMessage.Should().BeEmpty();
        }

        [Fact]
        public async System.Threading.Tasks.Task RunSafeAsync_FailingAction_ShouldSetError()
        {
            var vm = new TestViewModel();

            await vm.InvokeRunSafeAsync(async () =>
            {
                await System.Threading.Tasks.Task.Delay(10);
                throw new Exception("async failure");
            });

            vm.ErrorMessage.Should().Contain("async failure");
            vm.IsBusy.Should().BeFalse();
        }

        // ─── Logger (B-24): exception phải vào log kèm stack trace, không chỉ ErrorMessage ──

        private class LoggedViewModel : ViewModelBase
        {
            public LoggedViewModel(CadCoreFramework.Core.ILoggerService logger) : base(logger) { }

            public void InvokeRunSafe(Action action, string? errorPrefix = null) => RunSafe(action, errorPrefix);

            public System.Threading.Tasks.Task InvokeRunSafeAsync(Func<System.Threading.Tasks.Task> asyncAction)
                => RunSafeAsync(asyncAction);
        }

        [Fact]
        public void RunSafe_FailingAction_LogsExceptionWithPrefix()
        {
            var logger = new Moq.Mock<CadCoreFramework.Core.ILoggerService>();
            var vm = new LoggedViewModel(logger.Object);
            var boom = new InvalidOperationException("boom");

            vm.InvokeRunSafe(() => throw boom, "Tải layer");

            vm.ErrorMessage.Should().Be("Tải layer: boom");
            logger.Verify(l => l.Error(It.Is<string>(m => m.Contains(nameof(LoggedViewModel)) && m.Contains("Tải layer")), boom), Moq.Times.Once());
        }

        [Fact]
        public async System.Threading.Tasks.Task RunSafeAsync_FailingAction_LogsException()
        {
            var logger = new Moq.Mock<CadCoreFramework.Core.ILoggerService>();
            var vm = new LoggedViewModel(logger.Object);
            var boom = new InvalidOperationException("async boom");

            await vm.InvokeRunSafeAsync(async () => { await System.Threading.Tasks.Task.Yield(); throw boom; });

            logger.Verify(l => l.Error(It.IsAny<string>(), boom), Moq.Times.Once());
        }

        [Fact]
        public void RunSafe_SuccessfulAction_DoesNotLog()
        {
            var logger = new Moq.Mock<CadCoreFramework.Core.ILoggerService>();
            new LoggedViewModel(logger.Object).InvokeRunSafe(() => { });

            logger.Verify(l => l.Error(It.IsAny<string>(), It.IsAny<Exception>()), Moq.Times.Never());
        }
    }
}
