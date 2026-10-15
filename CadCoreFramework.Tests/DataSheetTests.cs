using System;
using System.Collections.Generic;
using CadCoreFramework.Models;
using FluentAssertions;
using Xunit;

namespace CadCoreFramework.Tests
{
    public class DataSheetTests
    {
        [Fact]
        public void Constructor_TrimsColumnNames_AndLooksThemUpCaseInsensitive()
        {
            var sheet = new DataSheet(new[] { " Handle ", "Tên" });

            sheet.Columns.Should().Equal("Handle", "Tên");
            sheet.IndexOf("handle").Should().Be(0);
            sheet.HasColumn("TÊN").Should().BeTrue();
            sheet.IndexOf("Khác").Should().Be(-1);
        }

        [Theory]
        [InlineData(new[] { "A", "" }, "Cột thứ 2 không có tên.")]
        [InlineData(new[] { "A", "  " }, "Cột thứ 2 không có tên.")]
        [InlineData(new[] { "Tên", "tên" }, "Tên cột 'tên' bị trùng.")]
        [InlineData(new string[0], "Bảng phải có ít nhất một cột.")]
        public void CheckColumns_ReportsTheFirstProblem(string[] columns, string expected)
        {
            DataSheet.CheckColumns(columns).Should().Be(expected);
            Action create = () => new DataSheet(columns);
            create.Should().Throw<ArgumentException>().Which.Message.Should().StartWith(expected);
        }

        [Fact]
        public void AddRow_WithWrongWidth_Throws_AndCopiesTheArray()
        {
            var sheet = new DataSheet(new[] { "A", "B" });
            Action wrong = () => sheet.AddRow("1");
            wrong.Should().Throw<ArgumentException>().Which.Message.Should().StartWith("Dòng có 1 giá trị, bảng có 2 cột.");

            var values = new object[] { "x", 1.0 };
            sheet.AddRow(values);
            values[0] = "đã đổi";
            sheet.GetValue(0, "A").Should().Be("x", "bảng giữ bản sao của dòng");
        }

        [Fact]
        public void GetValue_UnknownColumn_ListsTheColumns()
        {
            var sheet = new DataSheet(new[] { "A", "B" });
            sheet.AddRow(1.0, 2.0);
            Action read = () => sheet.GetValue(0, "C");
            read.Should().Throw<KeyNotFoundException>().WithMessage("Không có cột 'C'. Các cột: 'A', 'B'.");
        }

        [Fact]
        public void FormatCell_UsesInvariantCulture_AndShortDates()
        {
            DataSheet.FormatCell(null).Should().Be("");
            DataSheet.FormatCell(12.5).Should().Be("12.5");
            DataSheet.FormatCell(0.1).Should().Be("0.1");
            DataSheet.FormatCell(3).Should().Be("3");
            DataSheet.FormatCell(true).Should().Be("TRUE");
            DataSheet.FormatCell(new DateTime(2026, 10, 15)).Should().Be("2026-10-15");
            DataSheet.FormatCell(new DateTime(2026, 10, 15, 8, 5, 0)).Should().Be("2026-10-15 08:05:00");
        }

        [Fact]
        public void FormatCell_Double_RoundTrips()
        {
            foreach (double x in new[] { 0.1 + 0.2, 1e-300, 123456789.123456789, -0.0001, double.MaxValue })
                double.Parse(DataSheet.FormatCell(x), System.Globalization.CultureInfo.InvariantCulture).Should().Be(x);
        }

        [Fact]
        public void TryGetDouble_AcceptsNumbersAndInvariantNumberText()
        {
            var sheet = new DataSheet(new[] { "V" });
            sheet.AddRow(12.5);
            sheet.AddRow("12.5");
            sheet.AddRow(" 7 ");
            sheet.AddRow("12,5");
            sheet.AddRow(new object?[] { null });
            sheet.AddRow(true);

            var results = new List<(bool Ok, double Value)>();
            for (int r = 0; r < sheet.RowCount; r++)
                results.Add((sheet.TryGetDouble(r, "V", out double v), v));

            results.Should().Equal((true, 12.5), (true, 12.5), (true, 7.0), (false, 0.0), (false, 0.0), (false, 0.0));
        }
    }
}
