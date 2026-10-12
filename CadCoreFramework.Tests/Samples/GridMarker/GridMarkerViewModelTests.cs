using System;
using System.Collections.Generic;
using CadCoreFramework.Core;
using CadCoreFramework.Samples.GridMarker;
using FluentAssertions;
using Moq;
using Xunit;

namespace CadCoreFramework.Tests.Samples.GridMarker
{
    /// <summary>
    /// ViewModel không dùng kiểu WPF hay AutoCAD → test trực tiếp bằng Mock service
    /// và EventAggregator thật.
    /// </summary>
    public class GridMarkerViewModelTests
    {
        private readonly Mock<IGridMarkerService> _service = new Mock<IGridMarkerService>();
        private readonly EventAggregator _events = new EventAggregator();

        private GridMarkerViewModel NewViewModel() => new GridMarkerViewModel(_service.Object, _events);

        private void SetupDraw(CadResult<int> result)
            => _service.Setup(s => s.Draw(It.IsAny<GridSpec>(), It.IsAny<IReadOnlyList<GridCell>>()))
                       .Returns(result);

        [Fact]
        public void NewViewModel_DefaultsAreValid_DrawEnabled()
        {
            var vm = NewViewModel();

            vm.HasValidationErrors.Should().BeFalse();
            vm.DrawCommand.CanExecute(null).Should().BeTrue();
            vm.Status.Should().Be(GridMarkerStatus.Idle);
        }

        [Fact]
        public void InvalidInput_DisablesDraw_AndShowsErrors()
        {
            var vm = NewViewModel();

            vm.Rows = 0;

            vm.HasValidationErrors.Should().BeTrue();
            vm.ValidationSummary.Should().Contain("Số hàng");
            vm.DrawCommand.CanExecute(null).Should().BeFalse();
        }

        [Fact]
        public void FixingInput_ReenablesDraw()
        {
            var vm = NewViewModel();
            vm.LayerName = "";

            vm.LayerName = "OK_LAYER";

            vm.HasValidationErrors.Should().BeFalse();
            vm.DrawCommand.CanExecute(null).Should().BeTrue();
        }

        [Fact]
        public void InputChange_RaisesCanExecuteChanged()
        {
            var vm = NewViewModel();
            int raised = 0;
            vm.DrawCommand.CanExecuteChanged += (_, _) => raised++;

            vm.Columns = 0;

            raised.Should().BeGreaterThan(0);
        }

        [Fact]
        public void Draw_PassesSpecAndPlannedCellsToService()
        {
            SetupDraw(CadResult<int>.Success(6));
            var vm = NewViewModel();
            vm.Rows = 2;
            vm.Columns = 3;
            vm.OriginX = 10;

            vm.DrawCommand.Execute(null);

            _service.Verify(s => s.Draw(
                It.Is<GridSpec>(spec => spec.Rows == 2 && spec.Columns == 3),
                It.Is<IReadOnlyList<GridCell>>(cells => cells.Count == 6 && cells[0].CenterX == 10)),
                Times.Once);
        }

        [Fact]
        public void Draw_Success_SetsSuccessStatus()
        {
            SetupDraw(CadResult<int>.Success(12));
            var vm = NewViewModel();

            vm.DrawCommand.Execute(null);

            vm.Status.Should().Be(GridMarkerStatus.Success);
            vm.StatusMessage.Should().Contain("12");
            vm.IsBusy.Should().BeFalse();
        }

        [Fact]
        public void Draw_ServiceReturnsFail_SetsErrorStatus()
        {
            SetupDraw(CadResult<int>.Fail("Bản vẽ read-only"));
            var vm = NewViewModel();

            vm.DrawCommand.Execute(null);

            vm.Status.Should().Be(GridMarkerStatus.Error);
            vm.StatusMessage.Should().Contain("Bản vẽ read-only");
        }

        [Fact]
        public void Draw_ServiceThrows_SetsErrorStatus_DoesNotCrash()
        {
            _service.Setup(s => s.Draw(It.IsAny<GridSpec>(), It.IsAny<IReadOnlyList<GridCell>>()))
                    .Throws(new InvalidOperationException("Không có bản vẽ"));
            var vm = NewViewModel();

            vm.DrawCommand.Execute(null);

            vm.Status.Should().Be(GridMarkerStatus.Error);
            vm.StatusMessage.Should().Contain("Không có bản vẽ");
            vm.IsBusy.Should().BeFalse();
        }

        [Fact]
        public void GridDrawnMessage_RefreshesMarkerCount()
        {
            _service.Setup(s => s.CountMarkers()).Returns(42);
            var vm = NewViewModel();

            _events.Publish(new GridDrawnMessage(12, "SAMPLE_GRID"));

            vm.MarkerCount.Should().Be(42);
        }

        [Fact]
        public void Dispose_StopsListeningToMessages()
        {
            var vm = NewViewModel();
            vm.Dispose();

            _events.Publish(new GridDrawnMessage(12, "SAMPLE_GRID"));

            _service.Verify(s => s.CountMarkers(), Times.Never);
        }

        [Fact]
        public void RefreshCount_ServiceThrows_ResetsCountAndShowsError()
        {
            _service.Setup(s => s.CountMarkers()).Throws(new InvalidOperationException("Không có bản vẽ"));
            var vm = NewViewModel();

            vm.RefreshCountCommand.Execute(null);

            vm.MarkerCount.Should().Be(0);
            vm.Status.Should().Be(GridMarkerStatus.Error);
        }

        [Fact]
        public void Constructor_NullDependency_Throws()
        {
            Action act = () => new GridMarkerViewModel(null!, _events);

            act.Should().Throw<ArgumentNullException>();
        }
    }
}
