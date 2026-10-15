using CadCoreFramework.Samples.GridMarker;
using CadCoreFramework.Utils;
using Exercises.C02;
using FluentAssertions;
using Xunit;

namespace Exercises.Tests.C02;

public class E1Tests
{
    [Fact]
    public void Mau_trong_de()
    {
        ConsoleOutput.Capture(E1.Run).Should().Equal("E-POWER, Lưới trục, M.EQUIP");
    }

    [Theory]
    [InlineData('<')]
    [InlineData('>')]
    [InlineData('/')]
    [InlineData('\\')]
    [InlineData('"')]
    [InlineData(':')]
    [InlineData(';')]
    [InlineData('?')]
    [InlineData('*')]
    [InlineData('|')]
    [InlineData(',')]
    [InlineData('=')]
    [InlineData('`')]
    public void Loai_ten_co_ky_tu_cam(char invalid)
    {
        string name = $"A{invalid}B";
        SymbolNameRules.ContainsInvalidCharacter(name).Should().BeTrue("đây là ký tự cấm trong bản thật");
        E1.KeepValid(new List<string> { name }).Should().BeEmpty("'{0}' là ký tự cấm", invalid);
    }

    [Fact]
    public void Loai_ten_rong_hoac_chi_co_dau_cach_va_giu_thu_tu()
    {
        var names = new List<string> { "Z-LAST", "", "   ", "A-FIRST", "Lưới trục", "M.EQUIP" };
        E1.KeepValid(names).Should().Equal("Z-LAST", "A-FIRST", "Lưới trục", "M.EQUIP");
    }
}

public class E2Tests
{
    [Fact]
    public void Dem_so_entity_tren_moi_layer()
    {
        var counts = E2.CountByLayer(new[] { "E-POWER", "A-WALL", "E-POWER", "0", "E-POWER", "A-WALL" });
        counts.Should().BeEquivalentTo(new Dictionary<string, int> { ["E-POWER"] = 3, ["A-WALL"] = 2, ["0"] = 1 });
    }

    [Fact]
    public void Mang_rong_cho_dictionary_rong()
    {
        E2.CountByLayer(Array.Empty<string>()).Should().BeEmpty();
    }

    [Fact]
    public void In_moi_layer_mot_dong()
    {
        // Dictionary không cam kết thứ tự (C02 §4), nên chỉ so tập các dòng.
        ConsoleOutput.Capture(E2.Run).Should().BeEquivalentTo("E-POWER: 3", "A-WALL: 2", "0: 1");
    }
}

public class E3Tests
{
    [Fact]
    public void Ba_truong_hop_trong_de()
    {
        E3.Validate(0, 120).Should().Equal("Số hàng phải trong khoảng 1–100.", "Số cột phải trong khoảng 1–100.");
        E3.Validate(60, 50).Should().Equal("Tổng số ô (3000) vượt quá giới hạn 2500.");
        E3.Validate(3, 4).Should().BeEmpty();
    }

    public static IEnumerable<object[]> Cases()
    {
        int[] values = { -60, -5, -1, 0, 1, 2, 25, 26, 50, 51, 100, 101, 120 };
        foreach (int rows in values)
            foreach (int columns in values)
                yield return new object[] { rows, columns };
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Giong_het_GridSpec_Validate_that(int rows, int columns)
    {
        E3.Validate(rows, columns).Should().Equal(new GridSpec(rows, columns).Validate(),
            "câu chữ và thứ tự lỗi phải giống GridSpec.Validate, kể cả khi số hàng và số cột đều âm");
    }
}
