using System;
using CadCoreFramework.Services;
using CadCoreFramework.Testing.Fakes;
using FluentAssertions;
using Xunit;

namespace CadCoreFramework.Tests
{
    /// <summary>
    /// Control-flow tests cho LayerService. Dùng FakeCadContext với ExecuteAction=false
    /// vì logic bên trong lambda truy cập scope.Database (cần runtime AutoCAD thật).
    /// Mục đích: xác nhận LayerService gọi đúng overload (ExecuteRead vs TryExecute),
    /// truyền đúng tham số (undoMarkName, autoRegen), và xử lý lỗi đúng quy ước.
    /// </summary>
    public class LayerServiceTests
    {
        private static LayerService NewService(FakeCadContext fake) => new LayerService(fake);

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
        public void ToggleLayerVisibility_UsesSingleTryExecute_WithUndoMarkAndRegen()
        {
            var fake = new FakeCadContext { ExecuteAction = false };

            NewService(fake).ToggleLayerVisibility("E_LAYER", isOn: true);

            fake.ExecuteCount.Should().Be(1);
            fake.ExecuteAsyncCount.Should().Be(0);
            fake.LastUndoMarkName.Should().Be(LayerService.ToggleUndoMarkName);
            fake.LastAutoRegen.Should().BeTrue();
        }

        [Fact]
        public void ToggleLayerVisibility_WhenCadFails_ReturnsFailure_DoesNotThrow()
        {
            var fake = new FakeCadContext
            {
                ExecuteAction = false,
                ExceptionToThrow = new InvalidOperationException("Layer locked")
            };

            var result = NewService(fake).ToggleLayerVisibility("E", true);

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Layer locked");
        }

        [Theory]
        [InlineData("")]
        [InlineData("  ")]
        public void ToggleLayerVisibility_EmptyName_Throws(string name)
        {
            var fake = new FakeCadContext { ExecuteAction = false };

            Action act = () => NewService(fake).ToggleLayerVisibility(name, true);

            act.Should().Throw<ArgumentException>();
            fake.ExecuteCount.Should().Be(0);
        }

        [Fact]
        public void Constructor_NullContext_Throws()
        {
            Action act = () => new LayerService(null!);
            act.Should().Throw<ArgumentNullException>();
        }
    }
}
