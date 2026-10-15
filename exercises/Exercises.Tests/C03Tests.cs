using Exercises.C03;
using FluentAssertions;
using Xunit;

namespace Exercises.Tests.C03;

public class E1Tests
{
    [Fact]
    public void MaxHeight_la_2400()
    {
        PanelSpec.MaxHeight.Should().Be(2400);
    }

    [Theory]
    [InlineData(2600, true)]
    [InlineData(2001, true)]
    [InlineData(2000, false)]
    [InlineData(1800, false)]
    public void IsTall_dung_khi_cao_hon_2000(double height, bool expected)
    {
        new PanelSpec("MSB-02", 800, height).IsTall.Should().Be(expected);
    }

    [Fact]
    public void Tu_qua_cao_bao_loi_chieu_cao()
    {
        new PanelSpec("MSB-02", 800, 2600).Validate().Should().Equal("Chiều cao phải ≤ 2400.");
    }

    [Fact]
    public void Dung_2400_van_hop_le()
    {
        new PanelSpec("MSB-02", 800, 2400).IsValid.Should().BeTrue("luật là ≤ 2400, không phải < 2400");
    }

    [Fact]
    public void Ba_luat_cu_giu_nguyen()
    {
        using (Culture.Invariant())
        {
            new PanelSpec("", 200, height: 0).Validate().Should().Equal(
                "Tên tủ không được để trống.", "Chiều rộng phải ≥ 300.", "Chiều cao phải > 0.");
        }
    }
}

public class E2Tests
{
    [Fact]
    public void Dien_tich_tam_giac()
    {
        new Triangle(3, 4).Area().Should().Be(6);
        new Triangle(10, 5).Area().Should().Be(25);
    }

    [Fact]
    public void Ten_va_mo_ta_tam_giac()
    {
        using (Culture.Invariant())
        {
            Shape shape = new Triangle(3, 4);   // Triangle LÀ một Shape
            shape.Name.Should().Be("Tam giác");
            shape.Describe().Should().Be("Tam giác: diện tích 6.0");
        }
    }

    [Fact]
    public void Run_in_them_dong_tam_giac_o_cuoi()
    {
        ConsoleOutput.Capture(E2.Run).Should().Equal(
            "Chữ nhật: diện tích 12.0",
            "⭕ Tròn: diện tích 3.1",
            "Tam giác: diện tích 6.0");
    }
}

public class E3Tests
{
    [Fact]
    public void InMemory_luu_dung_thu_tu()
    {
        var repo = new InMemoryPanelRepository();
        repo.Add("MSB-01");
        repo.Add("DB-02");
        repo.GetAll().Should().Equal("MSB-01", "DB-02");
    }

    [Fact]
    public void Summary_nhu_trong_de()
    {
        IPanelRepository repo = new InMemoryPanelRepository();
        var report = new PanelReport(repo);
        repo.Add("MSB-01");
        repo.Add("DB-02");
        report.Summary().Should().Be("2 tủ: MSB-01, DB-02", "report được tạo trước khi thêm tủ, nên phải đọc repository lúc gọi Summary()");
    }

    [Fact]
    public void Summary_khi_chua_co_tu()
    {
        new PanelReport(new InMemoryPanelRepository()).Summary().Should().Be("0 tủ: ");
    }

    [Fact]
    public void PanelReport_dung_dung_repository_duoc_truyen_vao()
    {
        var fake = new FakeRepository("TỦ-A", "TỦ-B", "TỦ-C");
        new PanelReport(fake).Summary().Should().Be("3 tủ: TỦ-A, TỦ-B, TỦ-C",
            "PanelReport chỉ được biết interface IPanelRepository, không tự tạo repository riêng (C03 §8)");
    }

    /// <summary>Bản giả do test tự viết — đúng như FakeClock ở C03 §6.</summary>
    private sealed class FakeRepository : IPanelRepository
    {
        private readonly string[] _names;
        public FakeRepository(params string[] names) { _names = names; }
        public void Add(string name) => throw new InvalidOperationException("Summary() không được thêm tủ.");
        public IReadOnlyList<string> GetAll() => _names;
    }
}
