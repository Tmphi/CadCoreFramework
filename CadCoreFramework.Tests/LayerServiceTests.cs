using System;
using System.Threading.Tasks;
using CadCoreFramework.Core;
using CadCoreFramework.Services;
using CadCoreFramework.Testing.Fakes;
using FluentAssertions;
using Moq;
using Xunit;

namespace CadCoreFramework.Tests
{
    /// <summary>
    /// Control-flow tests cho LayerService. Dùng FakeCadContext với ExecuteAction=false
    /// vì logic bên trong lambda truy cập scope.Database (cần runtime AutoCAD thật).
    /// Mục đích: xác nhận LayerService gọi đúng overload (Execute vs ExecuteAsync vs ExecuteRead),
    /// truyền đúng tham số (undoMarkName, autoRegen), và propagate exceptions đúng cách.
    /// </summary>
    public class LayerServiceTests
    {
        private static LayerService NewService(FakeCadContext fake)
        {
            var notify = new Mock<INotificationService>().Object;
            return new LayerService(fake, notify);
        }

        [Fact]
        public void GetAllLayers_CallsExecuteRead_Once()
        {
            var fake = new FakeCadContext { ExecuteAction = false };
            NewService(fake).GetAllLayers();
            fake.ExecuteReadCount.Should().Be(1);
            fake.ExecuteCount.Should().Be(0);
            fake.ExecuteAsyncCount.Should().Be(0);
        }

        [Fact]
        public void GetAllLayers_ReturnsEmptyList_WhenLambdaSkipped()
        {
            var fake = new FakeCadContext { ExecuteAction = false };
            var result = NewService(fake).GetAllLayers();
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public void GetAllLayers_WhenExecuteReadThrows_PropagatesException()
        {
            var fake = new FakeCadContext
            {
                ExecuteAction = false,
                ExceptionToThrow = new InvalidOperationException("DB locked")
            };
            Action act = () => NewService(fake).GetAllLayers();
            act.Should().Throw<InvalidOperationException>().WithMessage("DB locked");
        }

        [Fact]
        public async Task ToggleLayerVisibilityAsync_CallsExecuteAsync_Once()
        {
            var fake = new FakeCadContext { ExecuteAction = false };
            await NewService(fake).ToggleLayerVisibilityAsync("E_LAYER", isOn: true);
            fake.ExecuteAsyncCount.Should().Be(1);
            fake.ExecuteReadCount.Should().Be(0);
        }

        [Fact]
        public async Task ToggleLayerVisibilityAsync_ReturnsFalse_WhenLambdaSkipped()
        {
            var fake = new FakeCadContext { ExecuteAction = false };
            bool result = await NewService(fake).ToggleLayerVisibilityAsync("E_LAYER", isOn: true);
            // Lambda bị skip → success stays at its initial value (false).
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ToggleLayerVisibilityAsync_WhenExecuteAsyncThrows_PropagatesException()
        {
            var fake = new FakeCadContext
            {
                ExecuteAction = false,
                ExceptionToThrow = new InvalidOperationException("Layer locked")
            };
            Func<Task> act = async () => await NewService(fake).ToggleLayerVisibilityAsync("E", true);
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Layer locked");
        }
    }
}
