using System.Globalization;
using CadCoreFramework.ViewModels;
using FluentAssertions;
using Xunit;

namespace CadCoreFramework.Tests
{
    /// <summary>
    /// Quy tắc đọc số của ô nhập trên palette (B-31/B-32): giống dòng lệnh AutoCAD — dấu chấm thập phân,
    /// không dấu phân cách hàng nghìn, không phụ thuộc culture của máy.
    /// </summary>
    public class NumberTextTests
    {
        [Theory]
        [InlineData("2.5", 2.5)]
        [InlineData("-1", -1.0)]
        [InlineData("1e3", 1000.0)]
        [InlineData(" 7 ", 7.0)]
        [InlineData("0.125", 0.125)]
        public void TryParseDouble_AcceptsDotDecimals(string text, double expected)
        {
            NumberText.TryParseDouble(text, out double value).Should().BeTrue();
            value.Should().Be(expected);
        }

        [Theory]
        [InlineData("2,5")]         // B-31: WPF mặc định đọc thành 25
        [InlineData("1,234.5")]
        [InlineData("")]            // B-32: ô trống
        [InlineData("   ")]
        [InlineData("abc")]
        [InlineData("NaN")]         // WPF mặc định nhận NaN mà không báo lỗi (B-31, B-34)
        [InlineData("Infinity")]
        [InlineData("-Infinity")]
        [InlineData("1e999")]       // tràn số: .NET 8 trả ∞, .NET Framework báo không đọc được — cả hai đều phải bị chặn
        [InlineData(null)]
        public void TryParseDouble_RejectsEverythingElse(string? text)
        {
            NumberText.TryParseDouble(text, out double value).Should().BeFalse();
            value.Should().Be(0);
        }

        [Theory]
        [InlineData("3", 3)]
        [InlineData("-2", -2)]
        [InlineData(" 3 ", 3)]
        [InlineData("+3", 3)]
        public void TryParseInt_AcceptsIntegers(string text, int expected)
        {
            NumberText.TryParseInt(text, out int value).Should().BeTrue();
            value.Should().Be(expected);
        }

        [Theory]
        [InlineData("3.0")]
        [InlineData("1,000")]
        [InlineData("")]
        [InlineData("x")]
        [InlineData("2147483648")]  // vượt int.MaxValue
        [InlineData(null)]
        public void TryParseInt_RejectsEverythingElse(string? text)
        {
            NumberText.TryParseInt(text, out _).Should().BeFalse();
        }

        [Fact]
        public void ParseAndFormat_IgnoreTheMachineCulture()
        {
            CultureInfo saved = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("vi-VN");   // như máy dev của dự án: dấu phẩy thập phân

                NumberText.Format(2.5).Should().Be("2.5");
                NumberText.TryParseDouble("2,5", out _).Should().BeFalse();
                NumberText.TryParseDouble("2.5", out double value).Should().BeTrue();
                value.Should().Be(2.5);
            }
            finally
            {
                CultureInfo.CurrentCulture = saved;
            }
        }
    }
}
