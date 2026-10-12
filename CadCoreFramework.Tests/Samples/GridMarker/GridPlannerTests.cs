using System;
using System.Linq;
using CadCoreFramework.Samples.GridMarker;
using FluentAssertions;
using Xunit;

namespace CadCoreFramework.Tests.Samples.GridMarker
{
    /// <summary>
    /// GridPlanner là logic thuần → test được KẾT QUẢ (tọa độ, nhãn), không chỉ luồng gọi hàm.
    /// Đây là lý do chính để tách logic ra khỏi lambda của ICadContext.
    /// </summary>
    public class GridPlannerTests
    {
        [Fact]
        public void Plan_ReturnsOneCellPerRowColumn()
        {
            var cells = GridPlanner.Plan(new GridSpec(3, 4), 0, 0);

            cells.Should().HaveCount(12);
            cells.Select(c => (c.Row, c.Column)).Should().OnlyHaveUniqueItems();
        }

        [Fact]
        public void Plan_FirstCellIsAtOrigin()
        {
            var first = GridPlanner.Plan(new GridSpec(2, 2), 100, 50)[0];

            first.CenterX.Should().Be(100);
            first.CenterY.Should().Be(50);
            first.Label.Should().Be("A1");
        }

        [Fact]
        public void Plan_ColumnsGoRight_RowsGoDown()
        {
            var cells = GridPlanner.Plan(new GridSpec(2, 3, spacing: 20), 0, 0);

            var b3 = cells.Single(c => c.Row == 1 && c.Column == 2);
            b3.CenterX.Should().Be(40);
            b3.CenterY.Should().Be(-20);
            b3.Label.Should().Be("B3");
        }

        [Fact]
        public void Plan_IsRowMajorOrdered()
        {
            var labels = GridPlanner.Plan(new GridSpec(2, 2), 0, 0).Select(c => c.Label);

            labels.Should().Equal("A1", "A2", "B1", "B2");
        }

        [Fact]
        public void Plan_SequentialStyle_NumbersRowMajor()
        {
            var spec = new GridSpec(2, 3, labelStyle: GridLabelStyle.Sequential);

            GridPlanner.Plan(spec, 0, 0).Select(c => c.Label)
                .Should().Equal("1", "2", "3", "4", "5", "6");
        }

        [Fact]
        public void Plan_SingleCell_Works()
        {
            GridPlanner.Plan(new GridSpec(1, 1), 0, 0).Should().ContainSingle();
        }

        [Fact]
        public void Plan_InvalidSpec_ThrowsWithValidationMessage()
        {
            Action act = () => GridPlanner.Plan(new GridSpec(0, 4), 0, 0);

            act.Should().Throw<ArgumentException>().WithMessage("*Số hàng*");
        }

        [Fact]
        public void Plan_NullSpec_Throws()
        {
            Action act = () => GridPlanner.Plan(null!, 0, 0);

            act.Should().Throw<ArgumentNullException>();
        }

        [Theory]
        [InlineData(0, "A")]
        [InlineData(1, "B")]
        [InlineData(25, "Z")]
        [InlineData(26, "AA")]
        [InlineData(27, "AB")]
        [InlineData(51, "AZ")]
        [InlineData(52, "BA")]
        [InlineData(701, "ZZ")]
        [InlineData(702, "AAA")]
        public void ToRowLetters_MatchesSpreadsheetConvention(int index, string expected)
        {
            GridPlanner.ToRowLetters(index).Should().Be(expected);
        }

        [Fact]
        public void ToRowLetters_Negative_Throws()
        {
            Action act = () => GridPlanner.ToRowLetters(-1);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}
