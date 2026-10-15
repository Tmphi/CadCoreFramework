using System.ComponentModel;
using Exercises.U01;
using FluentAssertions;
using Xunit;

namespace Exercises.Tests.U01;

public class E1Tests
{
    [Fact]
    public void Mac_dinh_hop_le_va_nut_bat()
    {
        var vm = new PanelFormViewModel();
        vm.ValidationSummary.Should().BeEmpty();
        vm.DrawCommand.CanExecute(null).Should().BeTrue();
    }

    [Theory]
    [InlineData("2,5")]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("-1")]
    public void Rong_sai_thi_bao_loi_va_tat_nut(string width)
    {
        var vm = new PanelFormViewModel { WidthText = width };
        vm.ValidationSummary.Should().NotBeEmpty("'{0}' không phải chiều rộng hợp lệ", width);
        vm.DrawCommand.CanExecute(null).Should().BeFalse();
    }

    [Theory]
    [InlineData("2.5")]
    [InlineData("600")]
    [InlineData(" 600 ")]
    public void Rong_dung_thi_nut_bat(string width)
    {
        var vm = new PanelFormViewModel { WidthText = width };
        vm.ValidationSummary.Should().BeEmpty();
        vm.DrawCommand.CanExecute(null).Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("31")]
    [InlineData("2.5")]
    [InlineData("abc")]
    public void So_ngan_sai_thi_tat_nut(string count)
    {
        var vm = new PanelFormViewModel { CountText = count };
        vm.ValidationSummary.Should().NotBeEmpty("'{0}' không phải số ngăn hợp lệ (số nguyên 1–30)", count);
        vm.DrawCommand.CanExecute(null).Should().BeFalse();
    }

    [Theory]
    [InlineData("1")]
    [InlineData("30")]
    public void So_ngan_bien_van_hop_le(string count)
    {
        new PanelFormViewModel { CountText = count }.DrawCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void Hai_o_cung_sai_thi_hai_dong_loi()
    {
        var vm = new PanelFormViewModel { CountText = "", WidthText = "2,5" };
        vm.ValidationSummary.Split('\n').Should().HaveCount(2, "mỗi lỗi một dòng");
    }

    [Fact]
    public void Doi_theo_tung_phim_go()
    {
        var vm = new PanelFormViewModel();
        var changed = new List<string?>();
        ((INotifyPropertyChanged)vm).PropertyChanged += (_, e) => changed.Add(e.PropertyName);
        int canExecuteChanged = 0;
        vm.DrawCommand.CanExecuteChanged += (_, _) => canExecuteChanged++;

        vm.WidthText = "2,";
        vm.CountText = "";

        changed.Should().Contain(nameof(PanelFormViewModel.ValidationSummary),
            "dòng báo lỗi phải cập nhật ngay khi gõ ([NotifyPropertyChangedFor], U01 §6)");
        canExecuteChanged.Should().BeGreaterThanOrEqualTo(2,
            "nút Vẽ phải được báo kiểm lại sau MỖI lần đổi ô số ([NotifyCanExecuteChangedFor], U01 §6)");
    }
}
