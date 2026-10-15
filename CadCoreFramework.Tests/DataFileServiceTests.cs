using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CadCoreFramework.Core;
using CadCoreFramework.Models;
using CadCoreFramework.Services;
using FluentAssertions;
using Xunit;

namespace CadCoreFramework.Tests
{
    /// <summary>
    /// DataFileService trên đĩa thật (thư mục tạm), FileSystemService thật, MiniExcel thật.
    /// </summary>
    public sealed class DataFileServiceTests : IDisposable
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "CadCoreFrameworkTests_" + Path.GetRandomFileName());
        private readonly DataFileService _service = new DataFileService(new FileSystemService());

        public DataFileServiceTests() => Directory.CreateDirectory(_dir);

        public void Dispose() => Directory.Delete(_dir, recursive: true);

        private string PathOf(string name) => Path.Combine(_dir, name);

        private static DataSheet SampleSheet()
        {
            var sheet = new DataSheet(new[] { "Handle", "Tên", "X", "Ngày", "Đạt" });
            sheet.AddRow("1A2", "Tủ điện \"MSB\", tầng 1", 12.5, new DateTime(2026, 10, 15), true);
            sheet.AddRow("001", null, 3.0, null, false);
            return sheet;
        }

        // ─── JSON ────────────────────────────────────────────────────────────

        private sealed class Config
        {
            public string Name { get; set; } = "";
            public double Spacing { get; set; }
            public List<string> Layers { get; set; } = new List<string>();
        }

        [Fact]
        public void Json_RoundTripsATypedObject_InUtf8()
        {
            string file = PathOf("config.json");
            var config = new Config { Name = "Lưới trục", Spacing = 7.5, Layers = { "TRUC", "KÍCH_THƯỚC" } };

            _service.WriteJson(file, config).IsSuccess.Should().BeTrue();
            CadResult<Config> back = _service.ReadJson<Config>(file);

            back.IsSuccess.Should().BeTrue();
            back.Value.Should().BeEquivalentTo(config);
            File.ReadAllText(file, Encoding.UTF8).Should().Contain("\"Lưới trục\"");
        }

        [Fact]
        public void ReadJson_BadJson_FailsWithTheFileName()
        {
            string file = PathOf("hong.json");
            File.WriteAllText(file, "{ \"Name\": ");

            CadResult<Config> result = _service.ReadJson<Config>(file);

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().StartWith("File 'hong.json' không phải JSON hợp lệ:");
        }

        [Fact]
        public void ReadJson_EmptyFile_Fails()
        {
            string file = PathOf("rong.json");
            File.WriteAllText(file, "  ");
            _service.ReadJson<Config>(file).ErrorMessage.Should().Be("File 'rong.json': File JSON rỗng.");
        }

        [Fact]
        public void Read_MissingFile_FailsWithThePath()
        {
            string file = PathOf("khong-co.xlsx");
            _service.ReadExcel(file).ErrorMessage.Should().Be($"Không tìm thấy file '{file}'.");
            _service.ReadCsv(PathOf("khong-co.csv")).IsSuccess.Should().BeFalse();
            _service.ReadJson<Config>(Path.Combine(_dir, "thu-muc-khong-co", "a.json")).ErrorMessage.Should().StartWith("Không tìm thấy file");
        }

        // ─── CSV ─────────────────────────────────────────────────────────────

        [Fact]
        public void Csv_WritesUtf8Bom_AndReadsBackAsText()
        {
            string file = PathOf("bang.csv");

            _service.WriteCsv(file, SampleSheet()).IsSuccess.Should().BeTrue();

            File.ReadAllBytes(file).Take(3).Should().Equal(new byte[] { 0xEF, 0xBB, 0xBF });
            DataSheet back = _service.ReadCsv(file).Value;
            back.Columns.Should().Equal("Handle", "Tên", "X", "Ngày", "Đạt");
            back.GetRow(0).Should().Equal("1A2", "Tủ điện \"MSB\", tầng 1", "12.5", "2026-10-15", "TRUE");
            back.GetRow(1).Should().Equal("001", "", "3", "", "FALSE");
        }

        [Fact]
        public void ReadCsv_AnsiFile_FailsWithHowToFix()
        {
            string file = PathOf("ansi.csv");
            File.WriteAllBytes(file, new byte[] { (byte)'T', 0xEA, (byte)'n', (byte)'\r', (byte)'\n', (byte)'x' }); // "Tên" theo Windows-1252

            CadResult<DataSheet> result = _service.ReadCsv(file);

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("không phải UTF-8").And.Contain("CSV UTF-8 (Comma delimited)");
        }

        [Fact]
        public void ReadCsv_Utf8WithoutBom_AndSemicolons_Works()
        {
            string file = PathOf("excel-de.csv");
            File.WriteAllText(file, "Tên;Số\r\nTủ 1;2,5\r\n", new UTF8Encoding(false));

            DataSheet sheet = _service.ReadCsv(file).Value;

            sheet.Columns.Should().Equal("Tên", "Số");
            sheet.GetRow(0).Should().Equal("Tủ 1", "2,5");
        }

        // ─── Excel ───────────────────────────────────────────────────────────

        [Fact]
        public void Excel_RoundTripsTypes()
        {
            string file = PathOf("bang.xlsx");

            _service.WriteExcel(file, SampleSheet(), "Thuộc tính").IsSuccess.Should().BeTrue();

            _service.GetExcelSheetNames(file).Value.Should().Equal("Thuộc tính");
            DataSheet back = _service.ReadExcel(file).Value;
            back.Columns.Should().Equal("Handle", "Tên", "X", "Ngày", "Đạt");
            back.GetRow(0).Should().Equal("1A2", "Tủ điện \"MSB\", tầng 1", 12.5, new DateTime(2026, 10, 15), true);
            back.GetRow(1).Should().Equal("001", null, 3.0, null, false);
        }

        [Fact]
        public void WriteExcel_HeaderOnly_ReadsBackEmpty()
        {
            string file = PathOf("rong.xlsx");
            _service.WriteExcel(file, new DataSheet(new[] { "A", "B" })).IsSuccess.Should().BeTrue();

            DataSheet back = _service.ReadExcel(file).Value;
            back.Columns.Should().Equal("A", "B");
            back.RowCount.Should().Be(0);
        }

        [Fact]
        public void ReadExcel_UnknownSheet_ListsTheSheets()
        {
            string file = PathOf("bang.xlsx");
            _service.WriteExcel(file, SampleSheet(), "Dữ liệu");

            _service.ReadExcel(file, "Khác").ErrorMessage.Should().Be("File 'bang.xlsx': Không có sheet 'Khác'. Các sheet: 'Dữ liệu'.");
        }

        [Theory]
        [InlineData("bang.xls", "File 'bang.xls': Định dạng .xls (Excel 97–2003) không được hỗ trợ. Mở trong Excel và lưu lại dạng .xlsx.")]
        [InlineData("bang.txt", "File 'bang.txt': File Excel phải có đuôi .xlsx (đang là '.txt').")]
        public void ReadExcel_WrongExtension_Fails(string name, string expected)
        {
            File.WriteAllText(PathOf(name), "x");
            _service.ReadExcel(PathOf(name)).ErrorMessage.Should().Be(expected);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Tên/sai")]
        [InlineData("1234567890123456789012345678901X")]
        public void WriteExcel_BadSheetName_FailsAndWritesNothing(string sheetName)
        {
            string file = PathOf("bang.xlsx");
            _service.WriteExcel(file, SampleSheet(), sheetName).ErrorMessage.Should().Contain("không hợp lệ");
            File.Exists(file).Should().BeFalse();
        }

        [Fact]
        public void ReadExcel_NotAZip_FailsWithoutThrowing()
        {
            string file = PathOf("gia.xlsx");
            File.WriteAllText(file, "đây không phải xlsx");
            CadResult<DataSheet> result = _service.ReadExcel(file);
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().StartWith("Đọc file 'gia.xlsx' thất bại:");
        }

        [Fact]
        public void FileOpenInAnotherProgram_CanBeRead_ButWriteFailsClearly_AndKeepsTheOldFile()
        {
            string file = PathOf("dang-mo.xlsx");
            _service.WriteExcel(file, SampleSheet()).IsSuccess.Should().BeTrue();
            byte[] before = File.ReadAllBytes(file);

            // Excel giữ file đang mở với quyền ghi và chỉ cho người khác đọc.
            using (new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            {
                _service.ReadExcel(file).IsSuccess.Should().BeTrue("đọc mở file với FileShare.ReadWrite");

                CadResult write = _service.WriteExcel(file, new DataSheet(new[] { "Khác" }));
                write.IsSuccess.Should().BeFalse();
                write.ErrorMessage.Should().Be("File 'dang-mo.xlsx' đang được mở trong chương trình khác (Excel?). Đóng file rồi thử lại.");
                write.Exception.Should().BeAssignableTo<IOException>("người gọi cần exception gốc để log có stack trace");
            }

            File.ReadAllBytes(file).Should().Equal(before, "ghi thất bại không được làm hỏng file cũ");
            Directory.GetFiles(_dir).Should().ContainSingle("không để lại tệp tạm");
        }

        // ─── Theo đuôi file ──────────────────────────────────────────────────

        [Theory]
        [InlineData("bang.xlsx")]
        [InlineData("bang.csv")]
        [InlineData("bang.json")]
        [InlineData("BANG.XLSX")]
        public void Table_RoundTripsTextAcrossAllFormats(string name)
        {
            string file = PathOf(name);

            _service.WriteTable(file, SampleSheet()).IsSuccess.Should().BeTrue();
            DataSheet back = _service.ReadTable(file).Value;

            back.Columns.Should().Equal("Handle", "Tên", "X", "Ngày", "Đạt");
            Enumerable.Range(0, back.RowCount).Select(r => back.Columns.Select(c => back.GetText(r, c)).ToArray())
                .Should().BeEquivalentTo(new[]
                {
                    new[] { "1A2", "Tủ điện \"MSB\", tầng 1", "12.5", "2026-10-15", "TRUE" },
                    new[] { "001", "", "3", "", "FALSE" },
                }, o => o.WithStrictOrdering());
        }

        [Fact]
        public void Table_UnsupportedExtension_Fails()
        {
            _service.WriteTable(PathOf("bang.txt"), SampleSheet()).ErrorMessage.Should().Be("Không hỗ trợ đuôi '.txt'. Dùng .xlsx, .csv hoặc .json.");
            _service.ReadTable(PathOf("bang")).ErrorMessage.Should().Be("Không hỗ trợ đuôi ''. Dùng .xlsx, .csv hoặc .json.");
        }

        [Fact]
        public void JsonTable_KeepsColumnOrder_UnionsKeys_AndDoesNotTurnDateTextIntoDates()
        {
            string file = PathOf("bang.json");
            File.WriteAllText(file, "[{\"B\": \"2026-10-15\", \"A\": 1}, {\"C\": {\"x\": 1}, \"A\": null}]");

            DataSheet sheet = _service.ReadTable(file).Value;

            sheet.Columns.Should().Equal("B", "A", "C");
            sheet.GetRow(0).Should().Equal("2026-10-15", 1.0, null);
            sheet.GetRow(1).Should().Equal(new object?[] { null, null, "{\"x\":1}" });
        }

        [Theory]
        [InlineData("{\"A\": 1}", "File 'bang.json': JSON dạng bảng phải là một mảng đối tượng: *")]
        [InlineData("[1, 2]", "File 'bang.json': Phần tử thứ 1 của mảng không phải đối tượng {…}.")]
        [InlineData("[{}]", "File 'bang.json': Mảng JSON không có đối tượng nào có thuộc tính.")]
        public void JsonTable_WrongShape_Fails(string json, string expected)
        {
            string file = PathOf("bang.json");
            File.WriteAllText(file, json);
            _service.ReadTable(file).ErrorMessage.Should().Match(expected);
        }

        // ─── Chuyển đổi hàng MiniExcel → bảng ───────────────────────────────

        private static Dictionary<string, object?> Row(params object?[] cells)
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < cells.Length; i++) row[((char)('A' + i)).ToString()] = cells[i];
            return row;
        }

        [Fact]
        public void ToSheet_SkipsBlankRows_UsesFirstFilledRowAsHeader_DropsTrailingEmptyHeaders()
        {
            DataSheet sheet = DataFileService.ToSheet(new[]
            {
                Row(null, null, null),
                Row("Tên", "X", null),
                Row("Tủ 1", 2.0, null),
                Row(" ", null, null),
                Row("", 3.0, null),
            });

            sheet.Columns.Should().Equal("Tên", "X");
            sheet.RowCount.Should().Be(2);
            sheet.GetRow(1).Should().Equal(null, 3.0);
        }

        [Fact]
        public void ToSheet_DataUnderAMissingHeader_FailsWithTheCell()
        {
            Action convert = () => DataFileService.ToSheet(new[] { Row("Tên", null), Row("Tủ 1", "lạc") });
            convert.Should().Throw<FormatException>().WithMessage("Ô B2 có dữ liệu nhưng cột B không có tiêu đề.");
        }

        [Fact]
        public void ToSheet_GapInHeader_Fails()
        {
            Action convert = () => DataFileService.ToSheet(new[] { Row("Tên", null, "Y") });
            convert.Should().Throw<FormatException>().WithMessage("Dòng tiêu đề (dòng 1): Cột thứ 2 không có tên.");
        }
    }
}
