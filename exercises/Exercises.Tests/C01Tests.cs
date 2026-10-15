using CadCoreFramework.Samples.GridMarker;
using Exercises.C01;
using FluentAssertions;
using Xunit;

namespace Exercises.Tests.C01;

public class E1Tests
{
    [Fact]
    public void In_dien_tich_va_chu_vi_voi_hai_chu_so_thap_phan()
    {
        ConsoleOutput.Capture(E1.Run).Should().Equal(
            new[] { "Diện tích: 1260000.00", "Chu vi: 5400.00" },
            "đề yêu cầu đúng hai dòng, định dạng {0:F2} (C01 §5)");
    }
}

public class E2Tests
{
    [Fact]
    public void In_bang_nhan_4_hang_5_cot()
    {
        ConsoleOutput.Capture(E2.Run).Should().Equal(
            "A1 A2 A3 A4 A5",
            "B1 B2 B3 B4 B5",
            "C1 C2 C3 C4 C5",
            "D1 D2 D3 D4 D5");
    }
}

public class E3Tests
{
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
    [InlineData(18277, "ZZZ")]
    public void Doi_so_hang_thanh_chu(int index, string expected)
    {
        E3.ToRowLetters(index).Should().Be(expected);
    }

    [Fact]
    public void Khop_GridPlanner_that_tren_20000_gia_tri()
    {
        for (int i = 0; i < 20000; i++)
            E3.ToRowLetters(i).Should().Be(GridPlanner.ToRowLetters(i), "kết quả phải giống bản thật với index = {0}", i);
    }
}

public class E4Tests
{
    [Fact]
    public void Mau_trong_de_60_x_50_chi_loi_tong_so_o()
    {
        ConsoleOutput.Capture(() => E4.Run(60, 50)).Should().Equal("Tổng số ô (3000) vượt quá giới hạn 2500.");
    }

    [Fact]
    public void Luoi_hop_le_khong_in_gi()
    {
        ConsoleOutput.Capture(() => E4.Run(3, 4)).Should().BeEmpty();
    }

    public static IEnumerable<object[]> Cases()
    {
        int[] values = { 0, 1, 2, 24, 25, 26, 49, 50, 51, 99, 100, 101, 120 };
        foreach (int rows in values)
            foreach (int columns in values)
                yield return new object[] { rows, columns };
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Giong_het_GridSpec_Validate_that(int rows, int columns)
    {
        var expected = new GridSpec(rows, columns).Validate();
        ConsoleOutput.Capture(() => E4.Run(rows, columns)).Should().Equal(expected,
            "câu chữ và thứ tự lỗi phải giống GridSpec.Validate (Samples/GridMarker/GridSpec.cs)");
    }
}
