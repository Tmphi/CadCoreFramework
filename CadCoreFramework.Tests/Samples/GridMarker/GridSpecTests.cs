using CadCoreFramework.Samples.GridMarker;
using FluentAssertions;
using Xunit;

namespace CadCoreFramework.Tests.Samples.GridMarker
{
    /// <summary>Luật validate của GridSpec — tầng Model, không cần AutoCAD.</summary>
    public class GridSpecTests
    {
        [Fact]
        public void Validate_DefaultValues_IsValid()
        {
            var spec = new GridSpec(3, 4);

            spec.Validate().Should().BeEmpty();
            spec.IsValid.Should().BeTrue();
            spec.CellCount.Should().Be(12);
        }

        [Theory]
        [InlineData(0, 4)]
        [InlineData(3, 0)]
        [InlineData(-1, 4)]
        [InlineData(GridSpec.MaxDimension + 1, 1)]
        [InlineData(1, GridSpec.MaxDimension + 1)]
        public void Validate_DimensionOutOfRange_ReturnsError(int rows, int columns)
        {
            new GridSpec(rows, columns).IsValid.Should().BeFalse();
        }

        [Fact]
        public void Validate_TooManyCells_ReturnsError()
        {
            var spec = new GridSpec(60, 60); // 3600 > MaxCells

            spec.Validate().Should().ContainSingle(e => e.Contains("3600"));
        }

        [Fact]
        public void Validate_SpacingSmallerThanCell_ReturnsError()
        {
            new GridSpec(2, 2, spacing: 10, cellSize: 15).IsValid.Should().BeFalse();
        }

        [Fact]
        public void Validate_SpacingEqualToCell_IsValid()
        {
            new GridSpec(2, 2, spacing: 15, cellSize: 15).IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-2)]
        [InlineData(16)] // lớn hơn cellSize = 15
        public void Validate_InvalidTextHeight_ReturnsError(double textHeight)
        {
            new GridSpec(2, 2, textHeight: textHeight).IsValid.Should().BeFalse();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        [InlineData("BAD:NAME")]
        [InlineData("A*B")]
        public void Validate_InvalidLayerName_ReturnsError(string? layerName)
        {
            new GridSpec(2, 2, layerName: layerName!).IsValid.Should().BeFalse();
        }

        [Fact]
        public void Validate_MultipleProblems_ReportsAllAtOnce()
        {
            var spec = new GridSpec(0, 0, spacing: 1, cellSize: 5, textHeight: 0, layerName: "");

            // Hàng, cột, khoảng cách, chiều cao chữ, layer → UI hiển thị đủ 5 lỗi cùng lúc.
            spec.Validate().Should().HaveCount(5);
        }
    }
}
