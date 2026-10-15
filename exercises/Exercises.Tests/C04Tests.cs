using CadCoreFramework.Utils;
using Exercises.C04;
using FluentAssertions;
using Xunit;

namespace Exercises.Tests.C04;

public class E1Tests
{
    private static readonly List<string> Layers = new List<string> { "0", "E-POWER", "A-WALL", "E-LIGHT", "Defpoints", "E-DATA", "A-DOOR" };

    [Fact]
    public void Mau_trong_de()
    {
        ConsoleOutput.Capture(E1.Run).Should().Equal(
            "A-DOOR, A-WALL",
            "6",
            "True",
            "0, e-power, a-wall, e-light, defpoints, e-data, a-door");
    }

    [Fact]
    public void Layer_bat_dau_bang_A_xep_theo_bang_chu_cai()
    {
        var layers = new List<string> { "A-ZONE", "B-A-1", "A-ANNO", "a-lower", "A-MID" };
        E1.StartingWithA(layers).Should().Equal("A-ANNO", "A-MID", "A-ZONE");
    }

    [Fact]
    public void Dem_ten_dai_hon_5_ky_tu()
    {
        E1.CountLongerThan5(new List<string> { "12345", "123456", "", "ABCDEFG" }).Should().Be(2, "đúng 5 ký tự thì KHÔNG tính");
    }

    [Fact]
    public void Co_layer_chua_LIGHT_hay_khong()
    {
        E1.AnyContainsLight(Layers).Should().BeTrue();
        E1.AnyContainsLight(new List<string> { "0", "E-POWER" }).Should().BeFalse();
    }

    [Fact]
    public void Ten_viet_thuong_giu_thu_tu()
    {
        E1.ToLower(new List<string> { "E-POWER", "Lưới TRỤC" }).Should().Equal("e-power", "lưới trục");
    }
}

public class E2Tests
{
    [Theory]
    [InlineData("E-POWER", true)]
    [InlineData("A*B", false)]
    [InlineData("", false)]
    [InlineData("Lưới trục", true)]
    [InlineData("LAYER:1", false)]
    public void Cac_ten_trong_de(string name, bool expected)
    {
        name.IsValidSymbolName().Should().Be(expected);
    }

    public static IEnumerable<object[]> Names()
    {
        yield return new object[] { "   " };
        yield return new object[] { new string('A', 254) };
        yield return new object[] { new string('A', 255) };
        yield return new object[] { new string('A', 256) };
        yield return new object[] { "M.EQUIP" };
        yield return new object[] { "E POWER" };
        for (char c = ' '; c <= '~'; c++)
            yield return new object[] { $"X{c}Y" };
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void Giong_SymbolNameRules_that(string name)
    {
        name.IsValidSymbolName().Should().Be(SymbolNameRules.IsValid(name),
            "kết quả phải giống SymbolNameRules.IsValid (Utils/SymbolNameRules.cs) với tên dài {0} ký tự", name.Length);
    }
}

public class E3Tests
{
    [Fact]
    public void Run_4_phat_25_50_75_100()
    {
        var job = new DrawJob();
        var received = new List<int>();
        job.Progress += (sender, percent) => received.Add(percent);

        job.Run(4);

        received.Should().Equal(25, 50, 75, 100);
    }

    [Fact]
    public void Run_5_phat_20_den_100()
    {
        var job = new DrawJob();
        var received = new List<int>();
        job.Progress += (sender, percent) => received.Add(percent);

        job.Run(5);

        received.Should().Equal(20, 40, 60, 80, 100);
    }

    [Fact]
    public void Sender_la_chinh_job()
    {
        var job = new DrawJob();
        object? sender = null;
        job.Progress += (s, _) => sender = s;

        job.Run(1);

        sender.Should().BeSameAs(job);
    }

    [Fact]
    public void Go_dang_ky_thi_khong_nhan_nua()
    {
        var job = new DrawJob();
        var received = new List<int>();
        EventHandler<int> onProgress = (sender, percent) => received.Add(percent);

        job.Progress += onProgress;
        job.Run(2);
        job.Progress -= onProgress;
        job.Run(2);

        received.Should().Equal(50, 100);
    }

    [Fact]
    public void Khong_ai_dang_ky_thi_van_chay()
    {
        var job = new DrawJob();
        Action act = () => job.Run(3);
        act.Should().NotThrow("gọi event chưa có ai đăng ký phải dùng ?.Invoke (C04 §9)");
    }
}
