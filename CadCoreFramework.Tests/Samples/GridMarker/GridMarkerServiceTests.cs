using System;
using System.Collections.Generic;
using CadCoreFramework.Core;
using CadCoreFramework.Samples.GridMarker;
using CadCoreFramework.Testing.Fakes;
using FluentAssertions;
using Moq;
using Xunit;

namespace CadCoreFramework.Tests.Samples.GridMarker
{
    /// <summary>
    /// Service là lớp vỏ AutoCAD → test bằng FakeCadContext với ExecuteAction = false:
    /// kiểm tra hợp đồng (overload nào, undo mark nào, có publish không), còn kết quả
    /// tính toán đã được GridPlannerTests bao phủ.
    /// </summary>
    public class GridMarkerServiceTests
    {
        private readonly FakeCadContext _ctx = new FakeCadContext { ExecuteAction = false };
        private readonly EventAggregator _events = new EventAggregator();
        private readonly Mock<ILoggerService> _logger = new Mock<ILoggerService>();

        private GridMarkerService NewService() => new GridMarkerService(_ctx, _events, _logger.Object);

        private static (GridSpec spec, IReadOnlyList<GridCell> cells) Grid(int rows = 2, int cols = 2)
        {
            var spec = new GridSpec(rows, cols);
            return (spec, GridPlanner.Plan(spec, 0, 0));
        }

        [Fact]
        public void Draw_UsesSingleExecute_WithUndoMark()
        {
            var (spec, cells) = Grid();

            NewService().Draw(spec, cells);

            _ctx.ExecuteCount.Should().Be(1);
            _ctx.ExecuteBatchCount.Should().Be(0);
            _ctx.LastUndoMarkName.Should().Be(GridMarkerService.UndoMarkName);
            _ctx.LastAutoRegen.Should().BeFalse();
        }

        [Fact]
        public void Draw_EmptyCells_DoesNotTouchDrawing()
        {
            var result = NewService().Draw(new GridSpec(1, 1), Array.Empty<GridCell>());

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(0);
            _ctx.ExecuteCount.Should().Be(0);
        }

        [Fact]
        public void Draw_Success_PublishesGridDrawnMessage()
        {
            GridDrawnMessage? received = null;
            _events.Subscribe<GridDrawnMessage>(m => received = m);
            var (spec, cells) = Grid();

            NewService().Draw(spec, cells);

            received.Should().NotBeNull();
            received!.LayerName.Should().Be(spec.LayerName);
        }

        [Fact]
        public void Draw_CadFailure_ReturnsFailAndDoesNotPublish()
        {
            bool published = false;
            _events.Subscribe<GridDrawnMessage>(_ => published = true);
            _ctx.ExceptionToThrow = new InvalidOperationException("Layer bị khóa");
            var (spec, cells) = Grid();

            var result = NewService().Draw(spec, cells);

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Layer bị khóa");
            published.Should().BeFalse();
        }

        [Fact]
        public void Draw_SubscriberThrows_StillReportsSuccessAndLogs()
        {
            _events.Subscribe<GridDrawnMessage>(_ => throw new InvalidOperationException("subscriber bug"));
            var (spec, cells) = Grid();

            var result = NewService().Draw(spec, cells);

            // Bản vẽ đã commit → lỗi subscriber không được biến thao tác thành thất bại.
            result.IsSuccess.Should().BeTrue();
            _logger.Verify(l => l.Error(It.IsAny<string>(), It.IsAny<Exception>()), Times.Once);
        }

        [Fact]
        public void Draw_NullArguments_Throw()
        {
            var service = NewService();
            var (spec, cells) = Grid();

            ((Action)(() => service.Draw(null!, cells))).Should().Throw<ArgumentNullException>();
            ((Action)(() => service.Draw(spec, null!))).Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void CountMarkers_UsesExecuteRead()
        {
            NewService().CountMarkers();

            _ctx.ExecuteReadCount.Should().Be(1);
            _ctx.ExecuteCount.Should().Be(0);
        }

        [Fact]
        public void Constructor_NullDependency_Throws()
        {
            Action act = () => new GridMarkerService(null!, _events, _logger.Object);

            act.Should().Throw<ArgumentNullException>();
        }
    }
}
