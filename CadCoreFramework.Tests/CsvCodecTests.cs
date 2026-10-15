using System;
using CadCoreFramework.Models;
using CadCoreFramework.Utils;
using FluentAssertions;
using Xunit;

namespace CadCoreFramework.Tests
{
    public class CsvCodecTests
    {
        [Fact]
        public void Format_QuotesOnlyWhenNeeded_AndUsesCrLf()
        {
            var sheet = new DataSheet(new[] { "Tên", "Ghi chú", "X" });
            sheet.AddRow("Tủ 1", "có dấu, phẩy", 12.5);
            sheet.AddRow("Tủ \"A\"", "hai\ndòng", null);
            sheet.AddRow(" lề ", "", 3);

            CsvCodec.Format(sheet).Should().Be(
                "Tên,Ghi chú,X\r\n" +
                "Tủ 1,\"có dấu, phẩy\",12.5\r\n" +
                "\"Tủ \"\"A\"\"\",\"hai\ndòng\",\r\n" +
                "\" lề \",,3\r\n");
        }

        [Fact]
        public void Parse_ReadsWhatFormatWrote()
        {
            var sheet = new DataSheet(new[] { "Tên", "Ghi chú" });
            sheet.AddRow("Tủ \"A\"", "a,b\r\nc");
            sheet.AddRow(" lề ", "");

            DataSheet back = CsvCodec.Parse(CsvCodec.Format(sheet));

            back.Columns.Should().Equal("Tên", "Ghi chú");
            back.GetRow(0).Should().Equal("Tủ \"A\"", "a,b\r\nc");
            back.GetRow(1).Should().Equal(" lề ", "");
        }

        [Fact]
        public void Parse_StripsBom_SkipsBlankLines_PadsShortRows_AcceptsLfAndNoFinalNewline()
        {
            DataSheet sheet = CsvCodec.Parse("﻿A,B,C\n\n1,2,3\n4\n\n5,,6");

            sheet.Columns.Should().Equal("A", "B", "C");
            sheet.RowCount.Should().Be(3);
            sheet.GetRow(1).Should().Equal("4", "", "");
            sheet.GetRow(2).Should().Equal("5", "", "6");
        }

        [Theory]
        [InlineData("A;B;C\r\n1;2,5;3", ';')]
        [InlineData("A\tB\r\n1\t2", '\t')]
        [InlineData("A,B\r\n1;2", ',')]
        [InlineData("\"A;B\",C\r\n1,2", ',')]
        [InlineData("Chỉ một cột\r\nx", ',')]
        public void DetectDelimiter_UsesTheHeaderLine(string text, char expected)
        {
            CsvCodec.DetectDelimiter(text).Should().Be(expected);
        }

        [Fact]
        public void Parse_SemicolonFile_KeepsDecimalCommaAsText()
        {
            DataSheet sheet = CsvCodec.Parse("Tên;X\r\nTủ 1;2,5");
            sheet.GetValue(0, "X").Should().Be("2,5");
            sheet.TryGetDouble(0, "X", out _).Should().BeFalse("CSV dùng số InvariantCulture; '2,5' không được đoán thành 2.5 hay 25");
        }

        [Theory]
        [InlineData("A,B\r\n1,2,3", "Dòng 2 có 3 ô, nhiều hơn 2 cột của dòng tiêu đề.")]
        [InlineData("A,B\r\n1,\"2", "Dòng 2: thiếu dấu nháy kép đóng ô.")]
        [InlineData("A,B\r\n1,x\"y\"", "Dòng 2: dấu nháy kép nằm giữa ô.*")]
        [InlineData("A,B\r\n1,\"y\"z", "Dòng 2: có ký tự sau dấu nháy đóng của ô.")]
        [InlineData("A,,C\r\n1,2,3", "Dòng tiêu đề (dòng 1): Cột thứ 2 không có tên.")]
        [InlineData("A,a\r\n1,2", "Dòng tiêu đề (dòng 1): Tên cột 'a' bị trùng.")]
        [InlineData("\r\n\r\n", "File CSV rỗng: không có dòng tiêu đề.")]
        [InlineData("A\r\n\"x\ny\"\r\n1,2", "Dòng 4 có 2 ô, nhiều hơn 1 cột của dòng tiêu đề.")]
        public void Parse_BadCsv_ThrowsFormatExceptionWithLineNumber(string text, string expected)
        {
            Action parse = () => CsvCodec.Parse(text);
            parse.Should().Throw<FormatException>().WithMessage(expected);
        }
    }
}
