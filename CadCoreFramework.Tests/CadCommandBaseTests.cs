using CadCoreFramework.Commands;
using CadCoreFramework.Core;
using CadCoreFramework.Testing.Fakes;
using FluentAssertions;
using Moq;
using Xunit;

namespace CadCoreFramework.Tests
{
    public class CadCommandBaseTests
    {
        private sealed class InjectedCommand : CadCommandBase
        {
            public InjectedCommand(ILoggerService logger, ICadContext cadContext, ICommandHistoryService? history = null)
                : base(logger, cadContext, history) { }

            public ILoggerService ExposedLogger => Logger;
            public ICadContext ExposedCadContext => CadContext;
            public ICommandHistoryService ExposedHistory => CommandHistory;
        }

        [Fact]
        public void InjectionConstructor_UsesInjectedServices_WithoutServiceProvider()
        {
            var logger = new Mock<ILoggerService>().Object;
            var cadContext = new FakeCadContext();
            var history = new CommandHistoryService();

            var command = new InjectedCommand(logger, cadContext, history);

            command.ExposedLogger.Should().BeSameAs(logger);
            command.ExposedCadContext.Should().BeSameAs(cadContext);
            command.ExposedHistory.Should().BeSameAs(history);
        }
    }
}
